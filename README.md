# KOLORA Gestor — Orçamentos Offline-First para Gráficas (Luanda)

> **Stack:** .NET 8 + WPF + SQLite (WAL) + EF Core 8 + QuestPDF + Supabase (Postgres) + Serilog  
> **Marca:** KOLORA — `KOLORA Gestor` (desktop, Fase 1) + `KOLORA Market` (Fase 2)

Sistema desktop **offline-first** para gráficas gerarem orçamentos (DTF, vinil, lona, cartão, sublimação) em minutos: cálculo automático custo/margem, controlo de stock, PDF com logo local, e sync assíncrono com Supabase quando houver rede — sem nunca bloquear a UI.

Spec completo: [`spec.md`](spec.md) · Roadmap: [`ROADMAP.md`](ROADMAP.md)

---

## O que foi entregue (Fase 1)

- WPF .NET 8 com 10 Views/ViewModels (Dashboard, Produtos, Materiais, Calculadora, Stock, Orçamentos, Clientes, Pedidos Recebidos com badge, Configurações, Sobre)
- 10 entidades + EF Core Code First + migrations + WAL mode + transação atômica Outbox (REGRA DE OURO)
- `GraficaId` gerado no 1º start (`%AppData%/Kolora/config.json`), multi-tenant via `grafica_id`
- Sync engine: push Outbox (30s, batch 50, backoff exponencial) + pull pedidos (15-30s) + expiração 3 dias + indicador `● Sincronizado / N pendentes / Offline`
- Calculadora universal (M2/Unidade/MetroLinear), abate de stock, alerta de mínimo
- PDF offline com QuestPDF (logo local, QR WhatsApp, validade configurável) → `Documentos/Kolora/Orcamentos/{ano}/{mes}/ORC-*.pdf`
- Supabase SQL pronto (`supabase/migrations/001_initial_schema.sql`) com RLS Fase 1 + `rpc_criar_grafica`
- Single-file publish: `KoloraGestor.exe` (self-contained win-x64)

```
src/Kolora.Orcamentos/          # App WPF
supabase/migrations/            # SQL para criar projeto Supabase
spec.md                         # Spec Fase 1 (fonte da verdade)
ROADMAP.md                      # Fases 2, 3, 4
```

---

## Como rodar

### Pré-requisitos

- Windows 10 64-bit, 4 GB RAM, resolução 1366×768
- .NET 8 SDK (o repo já traz um bootstrap em `dotnet8/` se necessário)
- Supabase (opcional para modo offline; obrigatório para sync): criar projeto em https://supabase.com

### 1. Supabase (opcional para testar offline; obrigatório para sync)

1. Crie um projeto Supabase.
2. No **SQL Editor**, execute `supabase/migrations/001_initial_schema.sql`.
3. Em **Project Settings → API**, copie `URL` e `anon key`.
4. Cole em `src/Kolora.Orcamentos/appsettings.json`:

```json
{
  "Supabase": { "Url": "https://xxx.supabase.co", "AnonKey": "eyJ..." }
}
```

Se deixar vazio, o app funciona 100% offline (fila Outbox acumula até reconectar).

### 2. Build & Run

```powershell
# Build
dotnet build src/Kolora.Orcamentos/Kolora.Orcamentos.csproj

# Run (cria %AppData%/Kolora/{config.json, kolora.db, assets/, logs/})
dotnet run --project src/Kolora.Orcamentos/Kolora.Orcamentos.csproj
```

Primeira execução cria:
- `%AppData%/Kolora/config.json` com `grafica_id` (UUID v4, nunca regenera)
- `%AppData%/Kolora/kolora.db` (SQLite WAL + `busy_timeout=5000`, migrations automáticas)
- `%AppData%/Kolora/logs/kolora-*.log` (Serilog)
- Seed de 5 produtos se DB vazio

### 3. Publish (.exe único via WhatsApp)

```powershell
dotnet publish src/Kolora.Orcamentos/Kolora.Orcamentos.csproj -c Release -r win-x64 --self-contained true /p:PublishSingleFile=true
# Saída: src/Kolora.Orcamentos/bin/Release/net8.0-windows/win-x64/publish/KoloraGestor.exe
```

> **Nota tamanho:** self-contained atual ≈ 227 MB. Alvo do spec é <80 MB. Para atingir: `framework-dependent` (`--self-contained false`, requer .NET 8 Runtime no PC, ~30 MB) ou `PublishTrimmed=true` + `InvariantGlobalization` (ver `ROADMAP.md`).

Instalação Fase 1 (portable): coloque o `.exe` na Área de Trabalho e execute. Atualização: substitua o `.exe`, o `%AppData%/Kolora` é preservado.

---

## Arquitetura (offline-first)

```
WPF UI → SQLite (fonte de verdade) → Outbox (tabela) ──background──→ SyncService → Supabase
          WAL mode, busy_timeout 5s       Pendente/Enviado/Erro      Timer 30s, batch 50, retry/backoff
```

Toda escrita de negócio está na **mesma transação** que o `OutboxEvents` (se o PC desligar no meio, ou grava tudo ou nada) — `spec.md:7.1`.

---

## Próximas fases

Ver [`ROADMAP.md`](ROADMAP.md):

- **Fase 2 — KOLORA Market:** site (Cloudflare Pages) + Worker AI (parse de pedidos) + Supabase Auth + Realtime + Storage de logo + Squirrel/MSIX auto-updater.
- **Fase 3 — Operação:** multi-PC por loja, fatura AGT (SAF-T), relatórios, backup cloud automático, telemetria.
- **Fase 4 — Ecossistema:** app mobile, API pública, IA de precificação, marketplace com checkout.

---

## Critérios de aceite Fase 1 (checklist)

- [ ] Instalar sem internet, cadastrar produto/material e gerar PDF com logo em <5 min
- [ ] Transação atômica: desligar no meio de "Salvar Orçamento" não deixa órfão sem Outbox
- [ ] 10 orçamentos offline → reconectar → sync automático
- [ ] Puxar cabo no meio da sync → app não trava, retoma com backoff
- [ ] Registros no Supabase com `grafica_id` correto
- [ ] Stock abate e alerta abaixo do mínimo
- [ ] Inserir `pedidos_orcamento` no Supabase → badge "novo" em ≤30s
- [ ] Converter pedido em orçamento → `orcamento_id` + `RespondidoLocal` no Supabase após sync
- [ ] Deletar `%AppData%/Kolora/logs` → logs recriados
- [ ] Gerar PDF <2s em PC 4 GB
- [ ] Substituir `.exe` por versão nova → dados preservados + migração automática

---

## Licença

MIT — ver [`LICENSE`](LICENSE). QuestPDF Community License.
