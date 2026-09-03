# SPEC — Gestor Gráfico Offline (Fase 1) - KOLORA
**Versão:** 1.1 (com best practices)
**Autor:** Cássio
**Stack:** .NET 8 + WPF + SQLite (local) + Supabase (Postgres remoto, sync assíncrono) + QuestPDF + Supabase C# Client

---

## 1. Objetivo

Entregar um sistema desktop **offline-first** para gráficas em Luanda gerarem orçamentos (DTF, vinil, lona, cartão, sublimação e outros) em minutos, com cálculo automático de custo/margem, controlo de stock de materiais e emissão de PDF — funcionando 100% sem internet — e que sincroniza os dados em segundo plano com o Supabase sempre que houver rede disponível, sem nunca bloquear o uso da aplicação.

Marca: **KOLORA** - Produtos: `KOLORA Gestor` (desktop) + `KOLORA Market` (Fase 2)

## 2. Escopo da Fase 1

### Dentro do escopo
- Cadastro de Produtos (genérico, não fixo em DTF)
- Cadastro de Materiais + consumo por produto
- Calculadora universal (custo real + margem → preço final)
- Controlo de stock com alerta de mínimo
- Geração de orçamento em PDF offline (com logo local)
- Cadastro de Clientes + histórico
- Sincronização assíncrona com Supabase (fila local, resolução de conflitos, retry)
- **Inbox de Pedidos de Orçamento** (pull de `pedidos_orcamento` do Supabase, badge de novos, converter em orçamento) - Base para Fase 2 + Worker AI
- Configuração da gráfica + gestão de assets offline (logo)
- Logging local, backup manual e migrações automáticas
- App single-user (1 PC por gráfica)

### Fora do escopo (não fazer agora)
- Fatura fiscal AGT
- Multi-usuário / multi-loja simultâneo real-time
- Marketplace completo com pagamento automático (Fase 2 e 3)
- Auto-updater sofisticado (Fase 1 usa distribuição manual via WhatsApp com .exe único)

## 3. Princípio arquitetural: Offline-first + Sync assíncrono

A aplicação **nunca espera pela rede** para funcionar. Todo o CRUD acontece localmente no SQLite. Um processo de sincronização em background lê uma **fila de outbox** e envia para o Supabase quando há conectividade, sem travar a UI.

```
┌─────────────┐        ┌───────────────┐        ┌──────────────────┐
│   WPF UI    │ ─────▶ │  SQLite local  │ ─────▶ │  Outbox (tabela)  │
│ (sempre ok) │        │  (fonte de     │        │  eventos pendentes│
└─────────────┘        │   verdade      │        └────────┬──────────┘
                        │   local)       │                 │
                        └───────────────┘                  │ background
                                                             ▼
                                                   ┌───────────────────┐
                                                   │ SyncService        │
                                                   │ (Timer/Task async) │
                                                   │  - checa internet  │
                                                   │  - envia batch     │
                                                   │  - retry/backoff   │
                                                   └────────┬───────────┘
                                                             ▼
                                                   ┌───────────────────┐
                                                   │  Supabase (Postgres)│
                                                   │  REST/PostgREST    │
                                                   │  + Realtime (pull) │
                                                   └───────────────────┘
```

## 4. Stack técnica detalhada

| Camada | Tecnologia | Nota |
|---|---|---|
| UI | .NET 8, WPF | MVVM leve |
| Banco local | SQLite + EF Core 8 (Code First) | WAL mode obrigatório |
| PDF | QuestPDF 2024.12+ | Licença Community |
| Banco remoto | Supabase (Postgres) | Projeto único multi-tenant |
| Comunicação | supabase-csharp ou HttpClient + PostgREST | anon key apenas |
| Sync engine | BackgroundService + System.Threading.Timer + outbox | Ver seção 7.1 |
| Empacotamento | Single File Publish (.exe único) | `PublishSingleFile=true` |
| Detecção de rede | NetworkChange + ping leve ao Supabase `/rest/v1/` | |
| Logging | Serilog -> arquivo local | Ver seção 9.2 |
| Config local | %AppData%/Kolora/config.json + assets/ | Ver seção 9.3 |

## 5. Modelo de dados local (SQLite) + Remoto (Supabase)

**Regras gerais:** Todos os Ids são UUID v4 gerados no cliente. Todas as tabelas de negócio têm `AtualizadoEm DATETIME` (UTC) e `GraficaId`.

```sql
-- Entidades de negócio
Graficas (
  Id UUID PK,
  Nome TEXT,
  Telefone TEXT,
  Localizacao TEXT,
  LogoPath TEXT NULL, -- caminho local %AppData%/Kolora/assets/logo.png
  CriadoEm DATETIME,
  AtualizadoEm DATETIME
)

ConfiguracoesGrafica (
  Id UUID PK,
  GraficaId UUID FK UNIQUE,
  LogoPath TEXT,
  NomeExibicaoPdf TEXT,
  MensagemRodapePdf TEXT,
  MargemPadraoGlobal DECIMAL, -- fallback se produto não tiver margem
  ValidadePadraoDias INT DEFAULT 3,
  AtualizadoEm DATETIME
)

Produtos (
  Id UUID PK,
  GraficaId UUID FK,
  Nome TEXT,               -- "Lona 440g", "DTF", "Cartão de Visita"
  TipoCalculo TEXT,        -- 'M2' | 'Unidade' | 'MetroLinear'
  PrecoCustoBase DECIMAL,
  MargemPadrao DECIMAL,    -- ex: 0.40
  Ativo BOOLEAN,
  AtualizadoEm DATETIME
)

Materiais (
  Id UUID PK,
  GraficaId UUID FK,
  Nome TEXT,                -- "Filme DTF", "Tinta Branca", "Lona"
  Unidade TEXT,              -- 'm' | 'litro' | 'unidade'
  StockAtual DECIMAL,
  StockMinimo DECIMAL,
  AtualizadoEm DATETIME
)

ProdutoMateriais (
  ProdutoId UUID FK,
  MaterialId UUID FK,
  ConsumoPorUnidade DECIMAL, -- ex: consumo de filme por m² de DTF
  PRIMARY KEY (ProdutoId, MaterialId)
)

Clientes (
  Id UUID PK,
  GraficaId UUID FK,
  Nome TEXT,
  Telefone TEXT,
  CriadoEm DATETIME,
  AtualizadoEm DATETIME
)

Orcamentos (
  Id UUID PK,
  GraficaId UUID FK,
  ClienteId UUID FK,
  Data DATETIME,
  Total DECIMAL,
  ItensJson TEXT,   -- snapshot dos itens (produto, medidas, qtd, preço, custo)
  Validade DATETIME,
  CaminhoPdf TEXT NULL, -- onde PDF foi salvo localmente
  AtualizadoEm DATETIME
)

-- Inbox - pedidos vindos do site/marketplace/Worker AI
PedidosOrcamento (
  Id UUID PK,
  GraficaId UUID FK,
  ClienteNome TEXT,
  ClienteTelefone TEXT,
  TextoOriginalCliente TEXT, -- texto livre que cliente digitou (para Worker AI)
  ProdutoDesejado TEXT,     -- parseado pelo Worker AI
  DescricaoPedido TEXT,     -- parseado
  Quantidade DECIMAL NULL,
  Status TEXT,              -- 'Novo' | 'Visto' | 'RespondidoLocal' | 'Expirado'
  OrcamentoId UUID NULL,
  CriadoEmOrigem DATETIME,
  RecebidoLocalEm DATETIME,
  AtualizadoEm DATETIME
)

-- Controlo de sincronização - REGRA DE OURO
OutboxEvents (
  Id UUID PK,
  Entidade TEXT,          -- 'Produto' | 'Material' | 'Orcamento' | 'PedidosOrcamento'
  EntidadeId UUID,
  TipoOperacao TEXT,      -- 'INSERT' | 'UPDATE' | 'DELETE'
  PayloadJson TEXT,       -- JSON completo da entidade para idempotência
  CriadoEm DATETIME,
  TentativasEnvio INT DEFAULT 0,
  StatusSync TEXT,        -- 'Pendente' | 'Enviado' | 'Erro'
  UltimoErro TEXT NULL
)

-- Controlo interno
SyncState (
  Id INT PK DEFAULT 1 CHECK (Id=1), -- singleton
  UltimoPullCatalogo DATETIME,
  UltimoPullPedidos DATETIME,
  VersaoSchemaLocal INT
)
```

## 6. Gestão de Identidade da Gráfica (GraficaId)

**Fluxo no primeiro start:**
1. App verifica `%AppData%/Kolora/config.json`
2. Se não existe `grafica_id`:
   - Gera UUID v4 localmente
   - Cria `config.json` com `{ grafica_id, criado_em, versao_app }`
   - Insere `Graficas` local + `OutboxEvents` tipo INSERT
   - SyncService vai criar no Supabase quando online via `rpc_criar_grafica()`
3. Se existe, usa o Id existente. Nunca regenera.

Isso garante multi-tenant seguro sem login complexo na Fase 1.

## 7. Sync Engine - Especificação Técnica Obrigatória

### 7.1 Transação Atômica Outbox (REGRA DE OURO - nunca quebrar)

Toda escrita de negócio DEVE estar na mesma transação do Outbox. Se PC desligar no meio, ou grava tudo ou nada.

```csharp
// Padrão obrigatório em todo repositório
using var transaction = await _db.Database.BeginTransactionAsync();
try {
    _db.Orcamentos.Add(orcamento);
    _db.OutboxEvents.Add(new OutboxEvent {
        Entidade = "Orcamento",
        EntidadeId = orcamento.Id,
        TipoOperacao = "INSERT",
        PayloadJson = JsonSerializer.Serialize(orcamento),
        StatusSync = "Pendente"
    });
    await _db.SaveChangesAsync();
    await transaction.CommitAsync();
} catch {
    await transaction.RollbackAsync();
    throw;
}
```

### 7.2 SQLite WAL Mode (obrigatório para não travar UI)

No `OnConfiguring` ou no startup do DbContext:

```csharp
await _db.Database.ExecuteSqlRawAsync("PRAGMA journal_mode=WAL;");
await _db.Database.ExecuteSqlRawAsync("PRAGMA busy_timeout=5000;"); // 5s retry
```

Justificativa: SyncService escreve no Outbox em background enquanto UI escreve Orçamentos. Sem WAL = `database is locked`.

### 7.3 Push (local -> Supabase)

- SyncBackgroundService roda Timer a cada 30s quando online
- Lote de 50 eventos `Pendente` ordenados por `CriadoEm ASC`
- Envia via `POST /rest/v1/{tabela}` ou `PATCH` em batch com `Prefer: resolution=merge-duplicates`
- Em sucesso: marca `StatusSync='Enviado'`
- Em erro: incrementa `TentativasEnvio`, grava `UltimoErro`, aplica backoff exponencial (30s, 2min, 10min, 1h)
- Outbox cresce indefinidamente offline - sem limite na Fase 1, mas com aviso na UI se >500 pendentes

### 7.4 Pull de Catálogo (Supabase -> local)

- A cada 60s quando online
- `GET /rest/v1/produtos?grafica_id=eq.{id}&atualizado_em=gt.{ultimoPull}`
- Aplica last-write-wins por `atualizado_em` UTC
- Conflito raro (reinstalação) = timestamp mais recente vence. Sem merge campo-a-campo na Fase 1.

### 7.5 Pull de Pedidos de Orçamento (inbox sensível a tempo)

Fluxo separado, intervalo mais curto (15-30s online):

1. `GET /rest/v1/pedidos_orcamento?grafica_id=eq.{id}&status=eq.Novo&order=criado_em_origem.asc`
2. Insere local com `Status='Novo'` + `RecebidoLocalEm=now()` se Id não existe
3. Dispara notificação UI: badge "🔔 3 novos pedidos"
4. Ao visualizar: `Status='Visto'` local + outbox UPDATE para Supabase
5. Botão "Converter em Orçamento" pré-preenche calculadora com dados do pedido
6. Ao gerar PDF: preenche `OrcamentoId` + `Status='RespondidoLocal'` + outbox
7. Expiração: pedidos com `criado_em_origem` > 3 dias => `Status='Expirado'` local

### 7.6 Resiliência offline
- App 100% funcional sem internet indefinidamente
- Indicador rodapé: `● Sincronizado` / `● 12 pendentes` / `● Erro sync (ver log)`
- Nunca modal bloqueante

### 7.7 Segurança e RLS (SQL obrigatório)

**Nunca embutir service_role key no cliente. Usar anon key + RLS.**

SQL a criar no Supabase (adicionar no spec como migration):

```sql
-- Habilitar RLS
ALTER TABLE graficas ENABLE ROW LEVEL SECURITY;
ALTER TABLE produtos ENABLE ROW LEVEL SECURITY;
ALTER TABLE materiais ENABLE ROW LEVEL SECURITY;
ALTER TABLE orcamentos ENABLE ROW LEVEL SECURITY;
ALTER TABLE pedidos_orcamento ENABLE ROW LEVEL SECURITY;

-- Política: gráfica só vê seus dados (grafica_id vem do JWT custom ou do header)
-- Na Fase 1, usamos abordagem simples: cliente envia grafica_id e RLS verifica
-- Opção recomendada: usar Supabase Auth com custom claim grafica_id

CREATE POLICY "isolamento_por_grafica" ON produtos
FOR ALL USING (grafica_id::text = (current_setting('request.jwt.claims', true)::jsonb ->> 'grafica_id') 
               OR grafica_id::text = current_setting('request.headers', true)::jsonb ->> 'x-grafica-id');

-- Para Fase 1 simples (sem Auth), alternativa: RLS desabilitado mas com validação via API key por grafica
-- Recomendação final para Fase 1: usar anon key + coluna grafica_id + validação no Worker que insere pedidos
-- E criar função rpc_criar_grafica() com SECURITY DEFINER

CREATE OR REPLACE FUNCTION rpc_criar_grafica(p_nome TEXT, p_telefone TEXT)
RETURNS UUID LANGUAGE plpgsql SECURITY DEFINER AS $$
DECLARE novo_id UUID := gen_random_uuid();
BEGIN
  INSERT INTO graficas (id, nome, telefone) VALUES (novo_id, p_nome, p_telefone);
  RETURN novo_id;
END;
$$;
```

**Decisão documentada:** Fase 1 usa anon key + header `x-grafica-id` validado no Worker AI que faz broadcast de pedidos. RLS completo com JWT custom entra na Fase 2.

## 8. Assets Offline e PDF

- Logo da gráfica salvo em `%AppData%/Kolora/assets/logo.png`
- Ao cadastrar gráfica, copia logo para pasta local
- Tabela `ConfiguracoesGrafica` guarda `LogoPath`
- QuestPDF lê logo do disco, não da rede. Se não existir, usa logo padrão KOLORA (embedado no .exe como recurso)
- PDF inclui: logo, nome da gráfica, dados cliente, itens (com medidas), total, validade (ConfiguracoesGrafica.ValidadePadraoDias), QR com WhatsApp, mensagem rodapé
- Geração <2s, salva em `Documentos/Kolora/Orcamentos/{ano}/{mes}/ORC-{IdCurto}.pdf`

## 9. Requisitos Não Funcionais e Operacionais

### 9.1 Performance e Compatibilidade
- Inicia e 100% operacional sem internet em <3s
- Sync não adiciona latência perceptível na UI
- .exe único Single File Publish, <80MB
- Roda em Windows 10 64-bit, 4GB RAM, sem GPU, resolução 1366x768
- SQLite arquivo <500MB na Fase 1

### 9.2 Logging (obrigatório)
- Serilog configurado: `Log.Logger = new LoggerConfiguration().WriteTo.File("%AppData%/Kolora/logs/kolora-.log", rollingInterval: RollingInterval.Day).CreateLogger();`
- Nível: Information para fluxo, Error para exceções de sync
- Tela "Sobre -> Abrir logs" abre pasta de logs
- Logs nunca contêm anon key

### 9.3 Backup e Migração

**Migração EF Core:**
- `dotnet ef migrations add v1_1_add_config` no projeto
- No startup: `await _db.Database.MigrateAsync();` automático
- Versão salva em `SyncState.VersaoSchemaLocal`

**Backup:**
- Botão "Configurações -> Exportar Backup" zipa `kolora.db` + `assets/` para `Documentos/Kolora/Backups/`
- Opcional: 1x por semana, SyncService faz upload do backup para Supabase Storage bucket `backups/{grafica_id}/` (se online)

### 9.4 Instalação e Atualização Fase 1
- Distribuição: .exe único via WhatsApp / Google Drive
- Instalação: usuário coloca .exe na Área de Trabalho e roda (portable). Primeira execução cria %AppData%
- Atualização: usuário baixa novo .exe e substitui. SQLite e config são preservados (fora do exe)
- Futuro: avaliar Squirrel.Windows ou MSIX na Fase 2

## 10. Módulos Funcionais (recapitulando)

1. **Cadastro de Produtos** - genérico, tipo cálculo configurável; 5 pré-cadastrados
2. **Cadastro de Materiais** - nome, unidade, stock atual/mínimo
3. **Calculadora Universal** - produto, dimensões/qtd, acabamento → custo + margem → preço
4. **Stock** - abate automático ao fechar orçamento; alerta visual
5. **Orçamento PDF** - QuestPDF offline com logo local
6. **Clientes** - nome, telefone, histórico
7. **Pedidos Recebidos (Inbox)** - pull do Supabase, badge, converter em orçamento
8. **Sync Service** - push outbox + pull catálogo + pull pedidos + resiliência
9. **Configurações** - dados gráfica, logo, margem global, validade, backup, logs

## 11. Critérios de Aceite da Fase 1

- [ ] Instalar em PC novo sem internet, cadastrar produto e material em <5 min e gerar PDF com logo
- [ ] Transação atômica: desligar PC no meio de "Salvar Orçamento" não deixa Orçamento sem Outbox
- [ ] Desligar internet, criar 10 orçamentos, religar - todos sincronizam sozinhos sem intervenção
- [ ] Apagar cabo no meio da sync - app não trava, retoma com backoff
- [ ] Ver no Supabase Studio os registros com grafica_id correto
- [ ] Stock abate e alerta abaixo do mínimo
- [ ] Inserir manualmente em pedidos_orcamento no Supabase e ver badge "novo" em até 30s sem reiniciar
- [ ] Converter pedido em orçamento e ver OrcamentoId + Status no Supabase após sync
- [ ] Deletar pasta %AppData%/Kolora/logs e ver logs sendo recriados
- [ ] Rodar em PC com 4GB, gerar PDF <2s
- [ ] Substituir .exe por versão nova e ver dados antigos preservados + migração automática

## 12. Roteiro Ajustado (com best practices)

| Dias | Entrega |
|---|---|
| 1 | Projeto WPF + EF Core + SQLite WAL + Serilog + estrutura %AppData%/Kolora |
| 2 | Entidades + Migrations + cadastro Produtos/Materiais + ConfiguracoesGrafica + assets logo |
| 3 | Calculadora Universal + regra de transação atômica Outbox |
| 4 | Abate de stock + alertas + Clientes |
| 5 | OutboxEvents completo + SyncBackgroundService push com retry/backoff + rpc_criar_grafica no Supabase |
| 6 | Pull catálogo + last-write-wins + indicador sync rodapé |
| 7 | QuestPDF com logo local + salvamento em Documentos/Kolora |
| 8 | Tabela pedidos_orcamento Supabase + RLS + pull dedicado + tela Pedidos Recebidos com badge |
| 9 | Fluxo Converter em Orçamento + backup manual + logs viewer |
| 10 | Build Single File Publish + teste offline/online completo + instalação piloto primeira gráfica KOLORA |
| 11 | Buffer para bugs + gravar vídeo demo para outras gráficas |

## 13. Pontos em Aberto Decididos

- Projeto Supabase: **único projeto multi-tenant com coluna grafica_id** (recomendado para marketplace Fase 2)
- Intervalo sync: 30s push/pull catálogo, 15-30s pull pedidos quando online, imediato ao detectar reconexão
- GraficaId: gerado no primeiro start, salvo em config.json
- Quem insere pedidos_orcamento na Fase 1: Supabase Studio manual para teste + formulário web simples na Cloudflare Pages (ou Worker AI Fase 2) - desktop só puxa
- Expiração pedidos: 3 dias (alinhado à validade do orçamento)
- RLS Fase 1: anon key + header x-grafica-id + função SECURITY DEFINER, JWT custom na Fase 2
- Logo: armazenamento local obrigatório, sync opcional via Storage
