<#
  Publica o Ferri CT no MonsterASP.NET (plano Free) via Web Deploy.

  Uso (na raiz do repo):
    .\deploy\publicar.ps1                 # compila, publica e roda o smoke test
    .\deploy\publicar.ps1 -MostrarErros   # idem, com customErrors Off (diagnóstico do 1º deploy)
    .\deploy\publicar.ps1 -SoPreparar     # só gera deploy\_publish, sem enviar nada

  Os dados do servidor ficam em deploy\segredos.local.psd1 (ignorado pelo git).
#>
param(
    [switch] $MostrarErros,
    [switch] $SoPreparar
)

$ErrorActionPreference = 'Stop'

$raiz    = Split-Path $PSScriptRoot -Parent
$projDir = Join-Path $raiz 'SistemaWebAgendamentoFerriCT'
$csproj  = Join-Path $projDir 'SistemaWebAgendamentoFerriCT.csproj'
$saida   = Join-Path $PSScriptRoot '_publish'

function Etapa($texto)   { Write-Host "`n== $texto" -ForegroundColor Cyan }
function Abortar($texto) { Write-Host "`nERRO: $texto" -ForegroundColor Red; exit 1 }

# ── 0. Ferramentas e segredos ────────────────────────────────────────
$vswhere = "${env:ProgramFiles(x86)}\Microsoft Visual Studio\Installer\vswhere.exe"
$msbuild = & $vswhere -latest -requires Microsoft.Component.MSBuild -find 'MSBuild\**\Bin\MSBuild.exe' | Select-Object -First 1
if (-not $msbuild) { Abortar 'MSBuild não encontrado (instale o Visual Studio com a workload ASP.NET).' }

$msdeploy = @("$env:ProgramFiles\IIS\Microsoft Web Deploy V3\msdeploy.exe",
              "${env:ProgramFiles(x86)}\IIS\Microsoft Web Deploy V3\msdeploy.exe") |
            Where-Object { Test-Path $_ } | Select-Object -First 1

if (-not $SoPreparar) {
    if (-not $msdeploy) { Abortar 'Web Deploy (msdeploy.exe) não encontrado.' }
    $arqSegredos = Join-Path $PSScriptRoot 'segredos.local.psd1'
    if (-not (Test-Path $arqSegredos)) { Abortar "Crie $arqSegredos a partir de segredos.exemplo.psd1." }
    $s = Import-PowerShellDataFile $arqSegredos
    foreach ($chave in 'SiteId', 'WebDeployUrl', 'WebDeployLogin', 'WebDeploySenha', 'SiteUrl', 'ConnectionString') {
        if ([string]::IsNullOrWhiteSpace($s[$chave]) -or $s[$chave] -match 'XXXX|SEU-SITE|=SENHA') {
            Abortar "Preencha '$chave' em $arqSegredos."
        }
    }

    # O app usa SQL Server (System.Data.SqlClient). String de MySQL (SslMode, etc.) quebra o site
    # em silêncio, porque os controllers capturam a exceção — então valida antes de publicar.
    Etapa 'Validando a connection string (precisa ser de SQL Server)'
    foreach ($chave in 'ConnectionString', 'ConnectionStringRemota') {
        if ([string]::IsNullOrWhiteSpace($s[$chave])) { continue }
        try { $null = New-Object System.Data.SqlClient.SqlConnectionStringBuilder $s[$chave] }
        catch { Abortar "$chave inválida para SQL Server: $($_.Exception.InnerException.Message) Use a string do banco MSSQL (não MySQL) do painel." }
    }

    # Com acesso remoto ligado no painel, testa o login no banco antes de enviar
    if (-not [string]::IsNullOrWhiteSpace($s['ConnectionStringRemota'])) {
        try {
            $teste = New-Object System.Data.SqlClient.SqlConnection ($s.ConnectionStringRemota.Trim().TrimEnd(';') + '; Connect Timeout=30')
            $teste.Open(); $teste.Close()
            Write-Host '  OK, login no banco funcionou.'
        } catch {
            $erroSql = $_.Exception.InnerException
            if ($erroSql -is [System.Data.SqlClient.SqlException] -and $erroSql.Number -eq 18456) {
                Abortar "Usuário/senha do banco recusados (Login failed). Confira as connection strings no painel."
            }
            # Banco inacessível de fora (acesso remoto desligado): não impede o deploy
            Write-Host "  Aviso: não deu para testar o login remoto ($($erroSql.Message)). Seguindo." -ForegroundColor Yellow
        }
    } else {
        Write-Host '  (sem ConnectionStringRemota: login no banco só será testado pelo smoke test)' -ForegroundColor DarkGray
    }
}

# ── 1. Restore + publish Release em pasta ───────────────────────────
Etapa 'Restaurando pacotes NuGet'
& $msbuild $csproj /t:Restore /p:RestorePackagesConfig=true "/p:SolutionDir=$raiz\" /nologo /v:q
if ($LASTEXITCODE -ne 0) { Abortar 'Restore falhou.' }

Etapa 'Compilando (Release) e publicando em deploy\_publish'
& $msbuild $csproj /p:Configuration=Release /p:DeployOnBuild=true /p:DeployDefaultTarget=WebPublish `
    /p:WebPublishMethod=FileSystem "/p:PublishUrl=$saida" /p:DeleteExistingFiles=true `
    "/p:SolutionDir=$raiz\" /nologo /v:m
if ($LASTEXITCODE -ne 0) { Abortar 'Build/publish falhou.' }

# ── 2. Paridade: tudo de Views/Content/Scripts precisa ter ido ──────
Etapa 'Conferindo se todos os arquivos estáticos e views foram publicados'
$faltando = foreach ($pasta in 'Views', 'Content', 'Scripts') {
    Get-ChildItem (Join-Path $projDir $pasta) -Recurse -File |
        Where-Object { $_.Name -notmatch 'intellisense\.js$|vsdoc\.js$' } |
        ForEach-Object { $_.FullName.Substring($projDir.Length + 1) } |
        Where-Object { -not (Test-Path (Join-Path $saida $_)) }
}
if ($faltando) {
    $faltando | ForEach-Object { Write-Host "  falta: $_" -ForegroundColor Yellow }
    Abortar 'Arquivos fora do .csproj não seriam publicados. Inclua-os no projeto (Content Include).'
}
Write-Host '  OK, nenhum arquivo faltando.'

# ── 3. Web.config publicado (connection string só aqui, nunca no git) ─
$webConfig = Join-Path $saida 'Web.config'
[xml] $cfg = Get-Content $webConfig -Raw -Encoding UTF8

if (-not $SoPreparar) {
    Etapa 'Aplicando connection string do MonsterASP no Web.config publicado'
    $conexao = $cfg.configuration.connectionStrings.add | Where-Object { $_.name -eq 'SistemaWebAgendamentoFerriCT' }
    $valor = $s.ConnectionString.Trim().TrimEnd(';')
    if ($valor -notmatch 'Connect(ion)? Timeout') { $valor += '; Connect Timeout=60' }
    $conexao.connectionString = $valor
}

if ($MostrarErros) {
    Etapa 'Ligando customErrors Off (diagnóstico)'
    $sysWeb = $cfg.configuration.'system.web'
    $ce = $sysWeb.SelectSingleNode('customErrors')
    if (-not $ce) { $ce = $sysWeb.AppendChild($cfg.CreateElement('customErrors')) }
    $ce.SetAttribute('mode', 'Off')
}
$cfg.Save($webConfig)

# ── 4. Checagens antes de enviar ─────────────────────────────────────
Etapa 'Checagens finais do pacote'
$texto = Get-Content $webConfig -Raw
$checagens = [ordered]@{
    'bin\roslyn\csc.exe (compilador das views)'      = Test-Path (Join-Path $saida 'bin\roslyn\csc.exe')
    'bin\SistemaWebAgendamentoFerriCT.dll'           = Test-Path (Join-Path $saida 'bin\SistemaWebAgendamentoFerriCT.dll')
    'Views\Agendamento\Retorno.cshtml'               = Test-Path (Join-Path $saida 'Views\Agendamento\Retorno.cshtml')
    'Views\Admin\RegistrarPagamentoManual.cshtml'    = Test-Path (Join-Path $saida 'Views\Admin\RegistrarPagamentoManual.cshtml')
    'Web.config sem debug="true"'                    = $texto -notmatch 'debug="true"'
    'Web.config com globalization pt-BR'             = $texto -match 'culture="pt-BR"'
}
if (-not $SoPreparar) { $checagens['Web.config sem (localdb)'] = $texto -notmatch '\(localdb\)' }
foreach ($c in $checagens.GetEnumerator()) {
    if ($c.Value) { Write-Host "  [OK]    $($c.Key)" -ForegroundColor Green }
    else          { Abortar "Checagem falhou: $($c.Key)" }
}

if ($SoPreparar) {
    Write-Host "`nPacote pronto em $saida (nada foi enviado: -SoPreparar)." -ForegroundColor Green
    exit 0
}

# ── 5. Envio via Web Deploy (mesmos parâmetros do guia do MonsterASP) ─
# --% repassa a linha crua ao msdeploy; os valores entram por variáveis de ambiente.
Etapa "Enviando para $($s.SiteId) via Web Deploy"
$env:FERRI_ORIGEM = $saida
$env:FERRI_SITE   = $s.SiteId
$env:FERRI_URL    = $s.WebDeployUrl
$env:FERRI_LOGIN  = $s.WebDeployLogin
$env:FERRI_SENHA  = $s.WebDeploySenha
try {
    & $msdeploy --% -verb:sync -source:contentPath="%FERRI_ORIGEM%" -dest:contentPath="%FERRI_SITE%",computerName="%FERRI_URL%",userName="%FERRI_LOGIN%",password="%FERRI_SENHA%",authtype="Basic",includeAcls="False" -allowUntrusted -disableLink:AppPoolExtension -disableLink:ContentExtension -disableLink:CertificateExtension -enableRule:AppOffline -retryAttempts:3
    if ($LASTEXITCODE -ne 0) { Abortar 'Web Deploy falhou (confira login/senha/URL no card "Visual Studio access").' }

    Etapa 'Conferindo arquivos no servidor'
    $remoto = & $msdeploy --% -verb:dump -source:contentPath="%FERRI_SITE%",computerName="%FERRI_URL%",userName="%FERRI_LOGIN%",password="%FERRI_SENHA%",authtype="Basic",includeAcls="False" -allowUntrusted -disableLink:AppPoolExtension -disableLink:ContentExtension -disableLink:CertificateExtension
    foreach ($arq in 'Views\Agendamento\Retorno.cshtml', 'Views\Admin\RegistrarPagamentoManual.cshtml', 'bin\roslyn\csc.exe') {
        if ($remoto -match [regex]::Escape($arq)) { Write-Host "  [OK]    no servidor: $arq" -ForegroundColor Green }
        else { Abortar "Não encontrado no servidor: $arq" }
    }
} finally {
    Remove-Item Env:FERRI_ORIGEM, Env:FERRI_SITE, Env:FERRI_URL, Env:FERRI_LOGIN, Env:FERRI_SENHA -ErrorAction SilentlyContinue
}

# ── 6. Aquecimento + smoke test ─────────────────────────────────────
Etapa 'Smoke test no site publicado (1º acesso cria o banco; pode levar ~1-2 min)'
& (Join-Path $PSScriptRoot 'testar.ps1') -BaseUrl $s.SiteUrl -EsperaMaxSegundos 240
if ($LASTEXITCODE -ne 0) {
    Abortar 'Smoke test falhou. Rode de novo com -MostrarErros para ver o erro detalhado no navegador.'
}

Write-Host "`nSite no ar: $($s.SiteUrl)" -ForegroundColor Green
Write-Host '  Cliente demo: demo@ferrict.com.br / CPF 529.982.247-25'
Write-Host '  Admin:        /Admin/Login  usuário admin / senha 123'
