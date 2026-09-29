como compilar sem o visual studio?
Compila 100% via terminal com o dotnet CLI — Visual Studio não é necessário.

1. Pré-requisito único: .NET 8 SDK
# Opção recomendada (instala global, entra no PATH)
winget install Microsoft.DotNet.SDK.8

# verificar
dotnet --list-sdks   # deve mostrar 8.0.x
No teu PC atual: o SDK global é o 6.0.428 e o .NET 8 que instalei está portável em C:\Users\cassi\AppData\Local\Temp\opencode\dotnet8\. Para usá-lo sem instalar, prefixe os comandos:

& "C:\Users\cassi\AppData\Local\Temp\opencode\dotnet8\dotnet.exe" build ...
(Instalar via winget é melhor — resolve o PATH permanentemente.)

2. Comandos (na raiz do repo)
# build
dotnet build src/Kolora.Orcamentos/Kolora.Orcamentos.csproj

# rodar (abre a janela WPF)
dotnet run --project src/Kolora.Orcamentos/Kolora.Orcamentos.csproj

# .exe único para distribuir (WhatsApp) — 35 MB, requer .NET 8 Runtime no PC destino
dotnet publish src/Kolora.Orcamentos/Kolora.Orcamentos.csproj -c Release -r win-x64 --self-contained false /p:PublishSingleFile=true

# .exe único autossuficiente — 227 MB, funciona em qualquer Windows sem instalar nada
dotnet publish src/Kolora.Orcamentos/Kolora.Orcamentos.csproj -c Release -r win-x64 --self-contained true /p:PublishSingleFile=true
dotnet publish src/Kolora.Orcamentos/Kolora.Orcamentos.csproj -c Release -r win-x64 --self-contained true /p:PublishSingleFile=true /p:IncludeNativeLibrariesForSelfExtract=true /p:DebugType=none /p:DebugSymbols=false /p:PublishDir="bin\Release\sc1\"
Saída do publish: src\Kolora.Orcamentos\bin\Release\net8.0-windows\win-x64\publish\KoloraGestor.exe

3. Editor (opcional, gratuito)
VS Code + extensões C# Dev Kit (da Microsoft) — IntelliSense, debug, terminal integrado. É o suficiente.
Ou Rider (JetBrains, pago) / qualquer editor — o build é sempre o comando acima.
4. Extras úteis
# migrations (se mudar entidades)
dotnet tool install --global dotnet-ef --version 8.0.0
dotnet ef migrations add NomeDaMigracao --project src/Kolora.Orcamentos/Kolora.Orcamentos.csproj

# limpar bin/obj se algo estranhar
dotnet clean src/Kolora.Orcamentos/Kolora.Orcamentos.csproj
Resumo: winget install Microsoft.DotNet.SDK.8 → dotnet run --project src/Kolora.Orcamentos/Kolora.Orcamentos.csproj — é tudo que precisa.