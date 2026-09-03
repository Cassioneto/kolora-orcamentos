# ROADMAP KOLORA — Fases

> Spec Fase 1: `spec.md` (v1.1). Este roadmap mapeia o que foi entregue na Fase 1 e o que vem nas próximas.

---

## Fase 1 — KOLORA Gestor (Offline-first Desktop) — ENTREGUE (2026-09)

**Objetivo:** gráfica gera orçamento em minutos, 100% offline, com sync assíncrono para Supabase.

**Entregues:**
- [x] Projeto WPF .NET 8 + EF Core 8 + SQLite (WAL + busy_timeout) + Serilog + QuestPDF + Supabase C# Client
- [x] 10 entidades + DbContext + migrations (`Data/Migrations/InitialCreate`) + seed 5 produtos (DTF, Lona 440g, Vinil, Cartão, Sublimação)
- [x] Transação atômica Outbox (REGRA DE OURO) + fila `OutboxEvents` + `SyncState`
- [x] `GraficaIdService` (%AppData%/Kolora/config.json, UUID v4, single-user)
- [x] `SyncBackgroundService` (push 30s batch 50 + backoff 30s/2m/10m/1h + pull pedidos 15-30s + expiração 3 dias + last-write-wins)
- [x] `NetworkMonitorService` (NetworkChange + ping Supabase)
- [x] `CalculadoraService` (M2/Unidade/MetroLinear, custo + margem → preço)
- [x] `StockService` (abate automático + alerta StockAtual <= StockMinimo)
- [x] `PdfService` (QuestPDF offline, logo local %AppData%/Kolora/assets/logo.png ou fallback KOLORA, salva em Documentos/Kolora/Orcamentos/{ano}/{mes}/ORC-{id}.pdf, <2s)
- [x] `BackupService` (zipa kolora.db + assets + config para Documentos/Kolora/Backups)
- [x] 10 Views + ViewModels (Dashboard, Produtos, Materiais, Calculadora, Stock, Orcamentos, Clientes, PedidosRecebidos com badge, Configuracoes, Sobre) + MainWindow com sidebar #1A1A2E/#FF6B35 e indicador sync rodapé
- [x] DI via Host.CreateDefaultBuilder + Serilog file sink + appsettings.json (Supabase URL/AnonKey)
- [x] Supabase SQL (`supabase/migrations/001_initial_schema.sql`: tabelas, indexes, RLS permissiva Fase 1 + `rpc_criar_grafica` SECURITY DEFINER + triggers `atualizado_em`)
- [x] Single File Publish: `KoloraGestor.exe` (227 MB self-contained; alvo <80MB requer trimming/framework-dependent — ver débito técnico)
- [x] Critérios de aceite Fase 1 validados via build + estrutura de pastas + smoke check offline

**Entregue no polimento A (2026-09-02):**
- [x] **Produtos/Materiais/Clientes CRUD** com `DataGrid` + formulário inline + `BeginTransactionAsync` + `OutboxEvents` atômico (`ProdutosViewModel.cs:25`, `MateriaisViewModel.cs:18`, `ClientesViewModel.cs:22`)
- [x] **Calculadora reativa** `CalculadoraViewModel.cs:18` (live `M2/Unidade/MetroLinear` via `CalculadoraService.cs:8`, `OnProdutoSelecionadoChanged` + `Recalcular()`)
- [x] **Orçamentos** `OrcamentosViewModel.cs:18` (add itens → `Itens` + `Total` + `SalvarOrcamentoAsync` com PDF `PdfService.cs:18` + abate stock + Outbox na mesma transação, abre PDF)
- [x] **Dashboard** `DashboardViewModel.cs:12` (KPIs: produtos/clientes/orçamentos/faturação mês/stock crítico/pendentes/pedidos novos)
- [x] **Stock** `StockViewModel.cs:10` + `StockView.xaml:1` (lista críticos) + **Config** `ConfiguracoesViewModel.cs:15` (logo picker → `assets/logo.png`, margem/validade, backup zip, abrir logs/pasta)
- [x] Build 0 erros (6 warnings `CS1998` intencionais) + `MateriaisView.xaml:26` fix (removido `StockAlertConverter` inexistente)
- [x] Publish validado: **227 MB self-contained** vs **35 MB framework-dependent** (`--self-contained false`, testado `2026-09-02`, requer .NET 8 Runtime) — alvo <80MB atingido no modo framework-dependent

**Hardening offline-first (2026-09-03) — validado com smoke test:**
- [x] **Migration real via dotnet-ef** (`Data/Migrations/20260903022251_InitialCreate.cs` + snapshot, via `DesignTimeFactory.cs`) — substituiu a migration manual que não registrava em `__EFMigrationsHistory` (causava `no such table` no 1º start)
- [x] **Auto-recuperação da gráfica** `GraficaIdService.cs:31` — mesmo com `config.json` existente, recria `Graficas`/`ConfiguracoesGrafica`/Outbox INSERT se o DB for novo (log: `Grafica recriada no banco local`)
- [x] **Sync nunca descarta offline** `SyncBackgroundService.cs:1` (reescrito) — eventos ficam `Pendente` para sempre sem rede/config; backoff por última tentativa (30s/2m/10m/1h); push/pull só roda se `IsSupabaseConfigured && IsOnline`; loops separados push 30s / pedidos 20s; evento `NovosPedidos` para badge
- [x] **Gate `IsSupabaseConfigured`** (`IConfigurationService.cs:1`) — app 100% funcional com `appsettings.json` vazio; `NetworkMonitorService.cs:26` não faz ping sem URL
- [x] **Payloads snake_case** `Helpers/OutboxJson.cs:1` (`SnakeCaseLower` + enums string) em todos os pontos de gravação Outbox — compatível com colunas Postgres
- [x] **Fix `Sum(decimal)` SQLite** `DashboardViewModel.cs:34` — agregação client-side
- [x] **0 warnings 0 erros** (repositórios `Ef*.cs` convertidos para `Task` síncrono)
- [x] **Evidência smoke test offline:** app rodou 22s sem Supabase (PID ativo, zero `[ERR]`/`[FTL]` no log), 11 tabelas + `__EFMigrationsHistory` criadas, WAL ativo (`.db-wal` presente), seed 5 produtos, `Graficas=1`, `ConfiguracoesGrafica=1`, `OutboxEvents=1 (Grafica, INSERT, Pendente)`, `SyncState=1`
- [x] Publish pós-fix: **34.2 MB framework-dependent** + **180 MB self-contained**

**Débitos restantes Fase 1 (para fechar piloto):**
- Validação de campos (nome obrigatório já, falta máscara telefone AO + margem 0-200% + custo >0)
- `ProdutoMateriais` consumo por produto ainda sem UI (hoje abate é por `ProdutoMateriais` seed; falta tela vincular material→produto com `ConsumoPorUnidade`)
- Pull catálogo last-write-wins implementado (`SyncBackgroundService.PullCatalogoAsync`) — validar contra Supabase real com RLS ativa antes do piloto multi-PC
- Testes xUnit pendentes (ver Fase C abaixo)
- **Aba Margem** | uma calculadora de Margem com explicação detalhada
---

## Fase 2 — KOLORA Market (Marketplace) — 6 a 10 semanas — MAPEADA

**Objetivo:** cliente final pede orçamento pelo site; gráfica recebe inbox sem fazer nada; Worker AI pré-preenche.

| # | Módulo | Detalhe | Tarefa desktop | Dependência |
|---|---|---|---|---|
| 2.1 | **Site KOLORA Market** | Cloudflare Pages + Svelte/Next, lista gráficas por bairro, formulário "Descreve o que precisas" + upload foto | — | Supabase `pedidos_orcamento` |
| 2.2 | **Worker AI** | Cloudflare Worker + OpenAI: parseia `texto_original_cliente` → `produto_desejado`, `descricao_pedido`, `quantidade`; cria `pedidos_orcamento` com `grafica_id` broadcast | — | Supabase |
| 2.3 | **Auth por gráfica** | Supabase Auth + custom claim `grafica_id` (JWT), RLS `current_setting('request.jwt.claims') ->> grafica_id` (substitui política `allow_all` da Fase 1 `supabase/migrations/001_initial_schema.sql:35`) | `SupabaseService.cs:14` passa `Authorization: Bearer <jwt>` + remove `x-grafica-id` | Supabase Auth |
| 2.4 | **Realtime** | Supabase Realtime subscription no desktop (substitui poll 15-30s) + push notification Windows Toast | `SyncBackgroundService.cs:28` → `Supabase.Channel` + `OnInsert` → `PedidosOrcamento` + `BadgePedidos` | Supabase Realtime |
| 2.5 | **Converter em Orçamento** | Botão "Converter" pré-preenche Calculadora (produto/quantidade/medidas) com dados do pedido + grava `orcamento_id` e `status=RespondidoLocal` | `PedidosRecebidosViewModel.cs:18` → `OrcamentosViewModel` com `ProdutoDesejado` + `Quantidade` | Desktop |
| 2.6 | **Pagamentos (prep)** | Integração PayPay / Multicaixa Express / referência bancária (apenas UI + webhook esqueleto, sem cobrança automática ainda) | Nova `PagamentosView` + `SupabaseService` webhook | PSP Angola |
| 2.7 | **Storage logo sync** | Upload logo para Supabase Storage `assets/{grafica_id}/logo.png` + download no desktop se online | `ConfiguracoesViewModel.cs:25` → `Supabase.Storage` | Storage |
| 2.8 | **Auto-updater** | Squirrel.Windows ou MSIX (substitui distribuição via WhatsApp) | `Kolora.Orcamentos.csproj:9` → `PublishSingleFile` + `Squirrel` | CI/CD |


**Critérios de aceite Fase 2:**
- Cliente cria pedido no site → gráfica vê badge "🔔 Novo" em <10s sem reiniciar
- Worker AI acerta >80% dos parses (DTF/lona/vinil/cartão) em teste com 50 frases reais
- RLS impede gráfica A de ler `produtos` da gráfica B (teste com 2 anon keys)
- Realtime não aumenta uso de CPU/memória perceptível

---

## Fase 3 — Operação & Escala — 8 a 12 semanas após Fase 2 — MAPEADA

| # | Módulo | Detalhe | Esforço |
|---|---|---|---|
| 3.1 | **Multi-usuário por gráfica** | 2-3 PCs na mesma loja via LAN sync (SQLite + LiteFS ) ou Supabase( prerfencial) com `updated_at` + fila de conflitos campo-a-campo | Alto |, Deve criar utilizador no supabase
| 3.2 | **Fatura AGT** | Integração SAF-T (AO) — geração de fatura/recibo com numeração, QR AGT, assinatura | Alto (legal) |
| 3.3 | **Relatórios** | Faturação por período, margem real vs prevista, stock crítico, clientes recorrentes; export Excel (ClosedXML) | Médio |
| 3.4 | **Backup automático cloud** | Upload semanal `kolora.db.zip` (`BackupService.cs:13` → `Supabase.Storage` `backups/{grafica_id}/`) + restauro 1-clique | Médio |
| 3.5 | **Telemetria** | Sentry + PostHog (offline queue) para crash reporting sem vazar anon key | Baixo |
| 3.6 | **Onboarding** | Wizard primeira execução (nome gráfica, telefone, logo, margem global, 3 produtos exemplo) + vídeo demo 2 min | Médio |

Dependências: `SyncState.VersaoSchemaLocal` + migrations incrementais; `OutboxEvents` com `Entidade` estendido para `Fatura`.

---

## Fase 4 — Ecossistema — 6 meses+ — MAPEADA

| # | Módulo | Stack | Receita |
|---|---|---|---|
| 4.1 | **app web pwa** | para fazer orcamentos online
| 4.2 | **API pública KOLORA** | REST + API key por gráfica, catálogo/preços (ex: agências integram) | B2B |
| 4.3 | **IA precificação** | Sugere margem por produto com base em histórico + custo material real (`StockService.cs:15` + `Orcamentos.Total`) | Margem |
| 4.4 | **Marketplace completo** | Checkout, comissão KOLORA (10-15%), ranking gráficas, reviews | Marketplace |

---

## Fase B/C — Débitos mapeados (pós-piloto A)

### B — Sync Realtime + RLS (2-3 semanas,  apóspiloto A)
- [ ] `SupabaseService.cs:70` → `Supabase.Realtime` channel `pedidos_orcamento:grafica_id=eq.{id}` + fallback poll
- [ ] `supabase/migrations/002_rls_jwt.sql` — `create policy isolamento_por_grafica on produtos for all using (grafica_id::text = (current_setting('request.jwt.claims',true)::jsonb ->> 'grafica_id'))`
- [ ] `App.xaml.cs:33` → login Supabase Auth (magic link telefone) + persist `access_token` em `%AppData%/Kolora/auth.json`
- [ ] Teste RLS com 2 gráficas (anon key não deve vazar)

### C — Testes + Trim (1-2 semanas, paralelo a B)
- [ ] **xUnit** `tests/Kolora.Tests/` — `CalculadoraService` (M2 2×3×1500=9000, Unidade, MetroLinear), `Outbox` atomicidade (rollback deixa 0 `OutboxEvents`), WAL `database is locked` com 2 writers concorrentes
- [ ] **Trim exe:** `Kolora.Orcamentos.csproj:10` → `<PublishTrimmed>true</PublishTrimmed>` + `<InvariantGlobalization>true</InvariantGlobalization>` + `QuestPDF` trim-compat; validado: self-contained 227 MB → framework-dependent 35 MB (2026-09-02, `dotnet publish --self-contained false`); próximo passo: self-contained trimmed alvo ~60 MB
- [ ] **CI:** GitHub Actions `build → test → publish` + release `.exe` via WhatsApp/Google Drive

---

## Decisões de arquitetura a revisitar

| Tema | Fase 1 | Fase 2 | Fase 3 |
|---|---|---|---|
| RLS | `allow_all` + `x-grafica-id` (Worker valida) | JWT custom `grafica_id` | idem + `authenticated` role |
| Sync | Poll 30s/15s | Realtime + poll fallback | CRDT ou last-write-wins campo-a-campo |
| Distribuição | .exe WhatsApp | Squirrel/MSIX | Store + auto-update delta |
| Auth | sem login (UUID local) | Supabase Auth (email/telefone) | idem + OAuth |
| Tamanho exe | 227 MB self-contained | <60 MB trimmed ou ~30 MB framework-dependent | idem |

---

## Como rodar (Fase 1)

```powershell
# Requisitos: .NET 8 SDK + Windows 10 64-bit
# 1. Configurar Supabase (opcional para offline)
#    - Criar projeto em supabase.com
#    - Executar supabase/migrations/001_initial_schema.sql no SQL Editor
#    - Copiar URL + anon key para src/Kolora.Orcamentos/appsettings.json

# 2. Build
.\dotnet8\dotnet.exe build src/Kolora.Orcamentos/Kolora.Orcamentos.csproj

# 3. Run (cria %AppData%/Kolora/config.json + kolora.db + logs)
.\dotnet8\dotnet.exe run --project src/Kolora.Orcamentos/Kolora.Orcamentos.csproj

# 4. Publish single-file
.\dotnet8\dotnet.exe publish src/Kolora.Orcamentos/Kolora.Orcamentos.csproj -c Release -r win-x64 --self-contained true /p:PublishSingleFile=true
# Saída: src/Kolora.Orcamentos/bin/Release/net8.0-windows/win-x64/publish/KoloraGestor.exe
```

## Estrutura

```
Kolora.Orcamentos.sln
src/Kolora.Orcamentos/
  Models/Entities/ + Enums/
  Data/{KoloraDbContext, Configurations/, Repositories/, Migrations/}
  Services/{GraficaId, NetworkMonitor, Supabase, SyncBackground, Calculadora, Stock, Pdf, Backup}
  ViewModels/ + Views/ (WPF)
  Helpers/
  App.xaml(.cs) + appsettings.json
supabase/migrations/001_initial_schema.sql
spec.md
ROADMAP.md
```

---

*Última atualização: 2026-09-02 — Cássio / KOLORA*
