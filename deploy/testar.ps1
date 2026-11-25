<#
  Smoke test do Ferri CT — roda contra qualquer URL (local ou publicada).
  NÃO cria agendamentos nem altera dados: só faz logins e leituras.

  Uso:  .\deploy\testar.ps1 -BaseUrl https://ferrict.runasp.net
#>
param(
    [Parameter(Mandatory = $true)] [string] $BaseUrl,
    [int] $EsperaMaxSegundos = 180   # 1º acesso no servidor: compila views + migrations + seed
)

$ErrorActionPreference = 'Stop'
[Net.ServicePointManager]::SecurityProtocol = [Net.SecurityProtocolType]::Tls12
$BaseUrl = $BaseUrl.TrimEnd('/')
$script:falhas = 0

function Ok($msg)    { Write-Host "  [OK]    $msg" -ForegroundColor Green }
function Falha($msg) { Write-Host "  [FALHA] $msg" -ForegroundColor Red; $script:falhas++ }

function Get-Pagina($caminho, $sessao) {
    try {
        Invoke-WebRequest "$BaseUrl$caminho" -WebSession $sessao -UseBasicParsing -TimeoutSec 120
    } catch {
        if ($_.Exception.Response) { return [pscustomobject]@{ StatusCode = [int]$_.Exception.Response.StatusCode; Content = '' } }
        # Sem resposta HTTP (ex.: loop de redirecionamento enquanto o app reinicia): registra e segue
        return [pscustomobject]@{ StatusCode = -1; Content = $_.Exception.Message }
    }
}

function Test-Pagina($caminho, $sessao, $marcador) {
    $r = Get-Pagina $caminho $sessao
    $erro = $r.Content -match 'Server Error|Erro de Servidor|Ops! Algo deu errado'
    if ($r.StatusCode -eq 200 -and -not $erro -and (-not $marcador -or $r.Content -match [regex]::Escape($marcador))) {
        Ok "GET $caminho"
    } else {
        Falha "GET $caminho (status $($r.StatusCode)$(if ($erro) { ', página de erro' }))"
    }
    return $r
}

function Get-Token($html) {
    [regex]::Match($html, 'name="__RequestVerificationToken"[^>]*value="([^"]+)"').Groups[1].Value
}

function Invoke-Login($caminho, $campos, $sessao) {
    $form = Get-Pagina $caminho $sessao
    $campos['__RequestVerificationToken'] = Get-Token $form.Content
    $r = Invoke-WebRequest "$BaseUrl$caminho" -Method Post -Body $campos -WebSession $sessao `
            -UseBasicParsing -MaximumRedirection 0 -ErrorAction SilentlyContinue
    return [int]$r.StatusCode
}

$agoraBr = [TimeZoneInfo]::ConvertTimeBySystemTimeZoneId([DateTime]::UtcNow, 'E. South America Standard Time')

Write-Host "`nSmoke test em $BaseUrl" -ForegroundColor Cyan

# ── 1. Home (aguarda o cold start) ───────────────────────────────────
$limite = (Get-Date).AddSeconds($EsperaMaxSegundos)
$inicio = $null
while ((Get-Date) -lt $limite) {
    try { $inicio = Invoke-WebRequest "$BaseUrl/" -UseBasicParsing -TimeoutSec 120; break }
    catch { Write-Host "  ... aguardando o site subir ($($_.Exception.Message))" -ForegroundColor DarkGray; Start-Sleep -Seconds 10 }
}
if ($inicio -and $inicio.StatusCode -eq 200 -and $inicio.Content -notmatch 'Server Error|Ops! Algo deu errado') { Ok "GET / (Home)" }
else { Falha "GET / — site não respondeu em $EsperaMaxSegundos s"; exit 1 }

# ── 2. Cliente demo: login + horários vindos do banco ────────────────
$cli = New-Object Microsoft.PowerShell.Commands.WebRequestSession
$st = Invoke-Login '/Cliente/Login' @{ Email = 'demo@ferrict.com.br'; CPF = '529.982.247-25' } $cli
if ($st -eq 302) { Ok "Login cliente demo (302)" }
else { Falha "Login cliente demo (status $st) — cliente demo não existe no banco? (seed)" }

$achou = $false
$dia = $agoraBr.Date
for ($i = 0; $i -lt 21 -and -not $achou; $i++) {
    $dia = $dia.AddDays(1)
    if ($dia.DayOfWeek -eq 'Sunday') { continue }
    $j = Get-Pagina "/Agendamento/HorariosPorData?data=$($dia.ToString('yyyy-MM-dd'))" $cli
    if ($j.Content -match '"horarios"') { $achou = $true; Ok "HorariosPorData $($dia.ToString('dd/MM')) trouxe horários do banco" }
}
if (-not $achou) { Falha "HorariosPorData não trouxe horários (banco/seed?)" }

$null = Test-Pagina '/Agendamento/Create' $cli $null

# ── 3. Admin: telas alteradas + fuso + cultura ───────────────────────
$adm = New-Object Microsoft.PowerShell.Commands.WebRequestSession
$st = Invoke-Login '/Admin/Login' @{ Usuario = 'admin'; Senha = '123' } $adm
if ($st -eq 302) { Ok "Login admin (302)" } else { Falha "Login admin (status $st)" }

$null = Test-Pagina '/Admin/Index' $adm 'Sistema operacional'
$null = Test-Pagina '/Admin/Alunos' $adm $null

# Cultura pt-BR + fuso de Brasília: a data precisa sair como dd/MM/yyyy (com barras) no dia de Brasília
$criar = Test-Pagina '/Admin/CriarAluno' $adm $null
$dataBr = $agoraBr.ToString('dd\/MM\/yyyy')
if ($criar.Content -match [regex]::Escape($dataBr)) { Ok "Data em pt-BR e no dia de Brasília ($dataBr)" }
else { Falha "Data esperada $dataBr não encontrada em /Admin/CriarAluno (cultura ou fuso errado)" }

# Fuso: "Atualizado em HH:mm" deve bater com Brasília (tolerância de 5 min)
$ag = Test-Pagina '/Admin/Agendamentos' $adm 'Atualizado em'
$m = [regex]::Match($ag.Content, 'Atualizado em\s+(\d{2}):(\d{2})')
if ($m.Success) {
    $servidor = $agoraBr.Date.AddHours([int]$m.Groups[1].Value).AddMinutes([int]$m.Groups[2].Value)
    $dif = [Math]::Abs(($servidor - $agoraBr).TotalMinutes)
    if ($dif -gt 720) { $dif = 1440 - $dif }   # virada de meia-noite
    if ($dif -le 5) { Ok "Horário do sistema = Brasília ($($m.Groups[1].Value):$($m.Groups[2].Value))" }
    else { Falha "Horário do sistema $($m.Groups[1].Value):$($m.Groups[2].Value) difere de Brasília $($agoraBr.ToString('HH:mm'))" }
} else { Falha "Não achei 'Atualizado em HH:mm' em /Admin/Agendamentos" }

# ── Resultado ────────────────────────────────────────────────────────
if ($script:falhas -eq 0) { Write-Host "`nTUDO OK — $BaseUrl" -ForegroundColor Green; exit 0 }
else { Write-Host "`n$($script:falhas) FALHA(S)" -ForegroundColor Red; exit 1 }
