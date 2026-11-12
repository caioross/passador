# Compila native\gancho\gancho.c em passador_gancho.dll (x64) com o MSVC do Visual Studio (vswhere acha qual).
#   powershell -File native\compilar.ps1 [-Saida <pasta>]
# Só recompila se o .c for mais novo que a DLL. Sem CRT, sem dependência: o Windows carrega em todo programa.
param([string]$Saida = (Join-Path $PSScriptRoot "saida"))
$ErrorActionPreference = "Stop"
$fonte = Join-Path $PSScriptRoot "gancho\gancho.c"
$dll = Join-Path $Saida "passador_gancho.dll"
if ((Test-Path $dll) -and (Get-Item $dll).LastWriteTime -ge (Get-Item $fonte).LastWriteTime) { Write-Host "gancho em dia: $dll"; exit 0 }
New-Item -ItemType Directory -Force $Saida | Out-Null

$vswhere = Join-Path ${env:ProgramFiles(x86)} "Microsoft Visual Studio\Installer\vswhere.exe"
$vs = & $vswhere -latest -products * -requires Microsoft.VisualStudio.Component.VC.Tools.x86.x64 -property installationPath
if (-not $vs) { throw "MSVC não encontrado (instale as Ferramentas de Build do Visual Studio com C++)" }
$vcvars = Join-Path $vs "VC\Auxiliary\Build\vcvars64.bat"
$obj = Join-Path $Saida "gancho.obj"
$cmd = "`"$vcvars`" >nul && cl /nologo /O1 /GS- /W4 /LD /Fo`"$obj`" /Fe`"$dll`" `"$fonte`" /link /NODEFAULTLIB /ENTRY:DllMain kernel32.lib user32.lib"
cmd /c $cmd
if ($LASTEXITCODE -ne 0) { throw "falhou ao compilar o gancho" }
Remove-Item (Join-Path $Saida "*.obj"), (Join-Path $Saida "*.exp"), (Join-Path $Saida "*.lib") -ErrorAction SilentlyContinue
Write-Host "gancho compilado: $dll"
