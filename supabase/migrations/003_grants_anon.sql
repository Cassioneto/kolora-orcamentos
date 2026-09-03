-- KOLORA - Supabase Migration 003: GRANTS para anon (Fase 1) + reload do schema
-- FIX do erro: 42501 "permission denied for table graficas/produtos/..."
-- Causa: tabelas criadas via SQL Editor pertencem ao role postgres; o role anon
-- (quem usa a anon key) nao recebe privilegios automaticamente. Politicas RLS
-- NAO substituem GRANTs — sem GRANT, anon recebe "permission denied".
-- Executar no SQL Editor do Supabase Studio (idempotente, pode rodar 2x).

-- 1) Privilegios de schema e tabelas existentes para anon e authenticated
grant usage on schema public to anon, authenticated;
grant select, insert, update, delete on all tables in schema public to anon, authenticated;

-- 2) Tabelas criadas no futuro ja nascem com privilegios (nao precisa re-rodar)
alter default privileges in schema public
  grant select, insert, update, delete on tables to anon, authenticated;

-- 3) Garante RLS + politica permissiva (Fase 1) em todas as tabelas de negocio
--    (idempotente — re-executa o bloco do 001 caso alguma parte tenha falhado)
do $$
declare t text;
begin
  foreach t in array array['graficas','produtos','materiais','clientes','orcamentos','pedidos_orcamento','configuracoes_grafica','produto_materiais']
  loop
    execute format('alter table %I enable row level security', t);
    if not exists (select 1 from pg_policies where schemaname='public' and tablename=t and policyname='allow_all_anon_' || t) then
      execute format('create policy allow_all_anon_%s on %I for all using (true) with check (true)', t, t);
    end if;
  end loop;
end $$;

-- 4) Forca o PostgREST a recarregar o cache de schema (mata PGRST205 residual)
notify pgrst, 'reload schema';
