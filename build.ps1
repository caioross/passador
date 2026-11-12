# Gera (e opcionalmente publica) uma versão do Passador com o Velopack.
#
#   powershell -File build.ps1                          -> testes + pacote local, versão do Directory.Build.props
#   powershell -File build.ps1 -Versao 0.1.1 -Publicar  -> também publica no GitHub Releases (precisa de token)
#
# O GitHub Actions (.github/workflows/release.yml) roda este mesmo script quando uma tag v* é enviada; o
# espelho.yml leva o release para a b75, de onde os PCs instalados se atualizam sozinhos.
param(
    [string]$Versao = "",
    [switch]$Publicar,
    [string]$Token = $env:GITHUB_TOKEN
)
$ErrorActionPreference = "Stop"
$raiz = $PSScriptRoot
$repo = "https://github.com/caioross/passador"
$env:DOTNET_CLI_TELEMETRY_OPTOUT = "1"

# Onde fica o peso: no PC de desenvolvimento, E:\Cache; em qualquer outro lugar, .build\ ao lado do código.
$saida = if ((Test-Path "E:\Cache") -and -not $env:CI) { "E:\Cache\build\Passador" } else { Join-Path $raiz ".build" }
if (Test-Path "E:\Cache\nuget") { $env:NUGET_PACKAGES = "E:\Cache\nuget" }
$publicado = Join-Path $saida "publish"
$releases = Join-Path $saida "Releases"
$ferramentas = Join-Path $saida "tools"

if (-not $Versao) {
    [xml]$props = Get-Content (Join-Path $raiz "Directory.Build.props")
    $Versao = $props.Project.PropertyGroup.Version
}
$Versao = $Versao.TrimStart("v")
Write-Host "== Passador $Versao"

$vpk = Join-Path $ferramentas "vpk.exe"
if (-not (Test-Path $vpk)) {
    Write-Host "== instalando vpk"
    dotnet tool install vpk --version 1.2.161 --tool-path $ferramentas | Out-Null
}

Write-Host "== gancho nativo"
& (Join-Path $raiz "native\compilar.ps1")
if ($LASTEXITCODE -ne 0) { throw "o gancho nativo não compilou" }

Write-Host "== testes"
dotnet test (Join-Path $raiz "tests\Passador.Testes") -c Release --nologo -v q "-p:Version=$Versao"
if ($LASTEXITCODE -ne 0) { throw "testes falharam" }

Write-Host "== publish"
if (Test-Path $publicado) { Remove-Item -Recurse -Force $publicado }
dotnet publish (Join-Path $raiz "src\Passador.App") -c Release -r win-x64 --self-contained true -o $publicado --nologo -v q "-p:Version=$Versao"
if ($LASTEXITCODE -ne 0) { throw "publish falhou" }
if (-not (Test-Path (Join-Path $publicado "passador_gancho.dll"))) { throw "passador_gancho.dll ficou fora do pacote" }

if ($Publicar) {
    if (-not $Token) { throw "para publicar, passe -Token ou defina GITHUB_TOKEN" }
    Write-Host "== baixando a versão anterior do GitHub (para o delta)"
    & $vpk download github --repoUrl $repo --token $Token -o $releases
    # Até a 0.1.2 o pacote se chamava Batuta: delta só entre pacotes do mesmo nome (a 0.2.0 sai sem delta).
    $lista = Join-Path $releases "releases.win.json"
    if ((Test-Path $lista) -and ((Get-Content $lista -Raw | ConvertFrom-Json).Assets | Where-Object { $_.PackageId -ne "Passador" })) {
        Write-Host "== a anterior é de outro pacote (Batuta): sem delta desta vez"
        Get-ChildItem $releases -File | Remove-Item -Force
    }
}

Write-Host "== pacote (Velopack)"
$notas = Join-Path $raiz "docs\novidades\$Versao.md"
$extra = @()
if (Test-Path $notas) { $extra += @("--releaseNotes", $notas) }
& $vpk pack --packId Passador --packVersion $Versao --packDir $publicado --mainExe Passador.exe `
    --packTitle "Passador" --packAuthors "Caio Rossi" `
    --icon (Join-Path $raiz "assets\passador.ico") --splashImage (Join-Path $raiz "assets\instalador.png") `
    --shortcuts "StartMenuRoot,Desktop" -o $releases @extra
if ($LASTEXITCODE -ne 0) { throw "vpk pack falhou" }

if ($Publicar) {
    Write-Host "== publicando no GitHub"
    & $vpk upload github --repoUrl $repo --token $Token -o $releases --publish `
        --releaseName "Passador $Versao" --tag "v$Versao"
    if ($LASTEXITCODE -ne 0) { throw "vpk upload falhou" }
}

Get-ChildItem $releases -File | Where-Object { $_.Name -like "*$Versao*" -or $_.Name -like "*Setup*" -or $_.Name -like "releases.*" } |
  ForEach-Object { "{0,-45} {1,8:N1} MB" -f $_.Name, ($_.Length / 1MB) }
