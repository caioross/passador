# Copia o release mais novo do GitHub para o espelho da b75 (serviço "atualizacoes", porta 3114), de onde o Batuta
# instalado se atualiza. Roda no PC da casa depois de cada tag (o GitHub Actions ainda não alcança esse serviço:
# o host público só passa para a 3114 na virada M4 do espelho, no plano do Núcleo; aí o espelho.yml faz isto sozinho).
#
#   powershell -File tools\espelhar.ps1
#
# A chave do GitHub vai pela entrada padrão direto para o comando na b75 e morre com ele: não fica em disco, em log
# nem na tela.
$ErrorActionPreference = "Stop"
$cmd = "cd /srv/nucleo/apps/atualizacoes && sudo -u nucleo env ATUALIZACOES_DADOS=/srv/nucleo/atualizacoes ./.venv/bin/python app.py --espelhar batuta --token-stdin"
gh auth token | ssh b75 $cmd
if ($LASTEXITCODE -ne 0) { throw "a cópia falhou (veja a saída acima)" }
$feed = Invoke-RestMethod "http://atualizacoes.caiorossi.com/batuta/releases.win.json" -TimeoutSec 15
$feed.Assets | ForEach-Object { "{0} {1} {2}" -f $_.Version, $_.Type, $_.FileName }
