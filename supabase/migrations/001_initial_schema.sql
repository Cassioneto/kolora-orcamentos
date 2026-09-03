-- KOLORA - Supabase Initial Schema (Fase 1)
-- Executar no SQL Editor do Supabase Studio
-- Stack: Postgres multi-tenant com coluna grafica_id

-- Extensions
create extension if not exists "pgcrypto";

-- Graficas
create table if not exists graficas (
  id uuid primary key default gen_random_uuid(),
  nome text not null,
  telefone text,
  localizacao text,
  logo_path text,
  criado_em timestamptz not null default now(),
  atualizado_em timestamptz not null default now()
);

-- Configuracoes
create table if not exists configuracoes_grafica (
  id uuid primary key default gen_random_uuid(),
  grafica_id uuid not null unique references graficas(id) on delete cascade,
  logo_path text,
  nome_exibicao_pdf text,
  mensagem_rodape_pdf text,
  margem_padrao_global decimal(5,4) not null default 0.40,
  validade_padrao_dias int not null default 3,
  atualizado_em timestamptz not null default now()
);

-- Produtos
create table if not exists produtos (
  id uuid primary key default gen_random_uuid(),
  grafica_id uuid not null references graficas(id) on delete cascade,
  nome text not null,
  tipo_calculo text not null check (tipo_calculo in ('M2','Unidade','MetroLinear')),
  preco_custo_base decimal(12,2) not null default 0,
  margem_padrao decimal(5,4) not null default 0.40,
  ativo boolean not null default true,
  atualizado_em timestamptz not null default now()
);
create index idx_produtos_grafica on produtos(grafica_id, atualizado_em);

-- Materiais
create table if not exists materiais (
  id uuid primary key default gen_random_uuid(),
  grafica_id uuid not null references graficas(id) on delete cascade,
  nome text not null,
  unidade text not null,
  stock_atual decimal(12,2) not null default 0,
  stock_minimo decimal(12,2) not null default 0,
  atualizado_em timestamptz not null default now()
);
create index idx_materiais_grafica on materiais(grafica_id);

-- ProdutoMateriais
create table if not exists produto_materiais (
  produto_id uuid not null references produtos(id) on delete cascade,
  material_id uuid not null references materiais(id) on delete cascade,
  consumo_por_unidade decimal(12,4) not null default 0,
  primary key (produto_id, material_id)
);

-- Clientes
create table if not exists clientes (
  id uuid primary key default gen_random_uuid(),
  grafica_id uuid not null references graficas(id) on delete cascade,
  nome text not null,
  telefone text,
  criado_em timestamptz not null default now(),
  atualizado_em timestamptz not null default now()
);
create index idx_clientes_grafica on clientes(grafica_id);

-- Orcamentos
create table if not exists orcamentos (
  id uuid primary key default gen_random_uuid(),
  grafica_id uuid not null references graficas(id) on delete cascade,
  cliente_id uuid not null references clientes(id),
  data timestamptz not null default now(),
  total decimal(12,2) not null default 0,
  itens_json text not null,
  validade timestamptz not null,
  caminho_pdf text,
  atualizado_em timestamptz not null default now()
);
create index idx_orcamentos_grafica on orcamentos(grafica_id, atualizado_em);

-- Pedidos de Orcamento (Inbox)
create table if not exists pedidos_orcamento (
  id uuid primary key default gen_random_uuid(),
  grafica_id uuid not null references graficas(id) on delete cascade,
  cliente_nome text,
  cliente_telefone text,
  texto_original_cliente text,
  produto_desejado text,
  descricao_pedido text,
  quantidade decimal(12,2),
  status text not null default 'Novo' check (status in ('Novo','Visto','RespondidoLocal','Expirado')),
  orcamento_id uuid references orcamentos(id),
  criado_em_origem timestamptz not null default now(),
  recebido_local_em timestamptz,
  atualizado_em timestamptz not null default now()
);
create index idx_pedidos_grafica_status on pedidos_orcamento(grafica_id, status, criado_em_origem);

-- RLS (Fase 1: anon key + header x-grafica-id, JWT custom na Fase 2)
alter table graficas enable row level security;
alter table produtos enable row level security;
alter table materiais enable row level security;
alter table clientes enable row level security;
alter table orcamentos enable row level security;
alter table pedidos_orcamento enable row level security;

-- Politica permissiva Fase 1 (validacao via Worker + anon key)
-- Na Fase 2 trocar por JWT com claim grafica_id
do $$ begin
  if not exists (select 1 from pg_policies where policyname='allow_all_anon_graficas') then
    create policy allow_all_anon_graficas on graficas for all using (true) with check (true);
  end if;
  if not exists (select 1 from pg_policies where policyname='allow_all_anon_produtos') then
    create policy allow_all_anon_produtos on produtos for all using (true) with check (true);
  end if;
  if not exists (select 1 from pg_policies where policyname='allow_all_anon_materiais') then
    create policy allow_all_anon_materiais on materiais for all using (true) with check (true);
  end if;
  if not exists (select 1 from pg_policies where policyname='allow_all_anon_clientes') then
    create policy allow_all_anon_clientes on clientes for all using (true) with check (true);
  end if;
  if not exists (select 1 from pg_policies where policyname='allow_all_anon_orcamentos') then
    create policy allow_all_anon_orcamentos on orcamentos for all using (true) with check (true);
  end if;
  if not exists (select 1 from pg_policies where policyname='allow_all_anon_pedidos') then
    create policy allow_all_anon_pedidos on pedidos_orcamento for all using (true) with check (true);
  end if;
end $$;

-- RPC criar grafica (SECURITY DEFINER)
create or replace function rpc_criar_grafica(p_nome text, p_telefone text)
returns uuid language plpgsql security definer as $$
declare novo_id uuid := gen_random_uuid();
begin
  insert into graficas (id, nome, telefone) values (novo_id, p_nome, p_telefone);
  return novo_id;
end;
$$;

-- Trigger atualizado_em
create or replace function trg_set_atualizado_em() returns trigger language plpgsql as $$
begin new.atualizado_em := now(); return new; end; $$;

drop trigger if exists trg_produtos_atualizado on produtos;
create trigger trg_produtos_atualizado before update on produtos for each row execute function trg_set_atualizado_em();
drop trigger if exists trg_materiais_atualizado on materiais;
create trigger trg_materiais_atualizado before update on materiais for each row execute function trg_set_atualizado_em();
drop trigger if exists trg_orcamentos_atualizado on orcamentos;
create trigger trg_orcamentos_atualizado before update on orcamentos for each row execute function trg_set_atualizado_em();

-- Storage bucket para backups (criar via Dashboard > Storage)
-- bucket: backups (public: false)
-- policy: graficas so leem/escrevem em backups/{grafica_id}/*
