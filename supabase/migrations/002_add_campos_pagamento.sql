-- KOLORA - Supabase Migration 002: campos de pagamento no orcamento
-- Executar no SQL Editor do Supabase Studio (apos o 001_initial_schema.sql)

-- IBAN e Multicaixa Express na configuracao da grafica
alter table configuracoes_grafica
  add column if not exists iban text,
  add column if not exists multicaixa_express_numero text;
