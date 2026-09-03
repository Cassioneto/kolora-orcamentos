$appData = [Environment]::GetFolderPath('ApplicationData')
$dbPath = Join-Path $appData "Kolora\kolora.db"

if (-not (Test-Path $dbPath)) {
    Write-Host "Banco de dados não encontrado em: $dbPath" -ForegroundColor Red
    exit 1
}

Write-Host "Banco de dados encontrado: $dbPath" -ForegroundColor Green
Write-Host ""

# Usar sqlite3 ou dotnet para ler o banco
$query = "SELECT Id, GraficaId, NomeExibicaoPdf, MensagemRodapePdf FROM ConfiguracoesGrafica;"

Write-Host "Executando consulta SQL..." -ForegroundColor Yellow
Write-Host $query
Write-Host ""

# Tentar usar sqlite3 se estiver disponível
if (Get-Command sqlite3 -ErrorAction SilentlyContinue) {
    sqlite3 $dbPath $query
} else {
    Write-Host "sqlite3 não encontrado. Use o seguinte comando manualmente:" -ForegroundColor Yellow
    Write-Host "sqlite3 '$dbPath' '$query'" -ForegroundColor Cyan
    Write-Host ""
    Write-Host "Ou abra a aplicação, vá em Configurações e clique em Salvar novamente." -ForegroundColor Yellow
}
