# =====================================================================
#  start-demo.ps1 — Inicia o tunel publico para a demo do MercadoPago
# =====================================================================
#  Uso:
#    .\start-demo.ps1
#
#  O que faz:
#    1. Verifica se o ngrok esta instalado
#    2. Verifica se ja existe um ngrok rodando (porta 4040)
#    3. Se nao tiver, sobe o ngrok no subdominio fixo deste projeto
#    4. Mostra a URL publica e os proximos passos
#
#  Apos rodar este script, abra o Visual Studio e pressione F5.
# =====================================================================

$ErrorActionPreference = "Stop"

# --- Configuracao do projeto -----------------------------------------
$Subdominio   = "molehill-salvation-clothes.ngrok-free.dev"
$PortaLocal   = 44358
$UrlLocal     = "https://localhost:$PortaLocal"
$UrlPublica   = "https://$Subdominio"
$InterfaceWeb = "http://127.0.0.1:4040"
# ---------------------------------------------------------------------

function Escrever-Linha {
    param([string]$Texto, [string]$Cor = "White")
    Write-Host $Texto -ForegroundColor $Cor
}

Escrever-Linha ""
Escrever-Linha "=====================================================" "Cyan"
Escrever-Linha " Ferri CT - Demo do MercadoPago (start-demo.ps1)     " "Cyan"
Escrever-Linha "=====================================================" "Cyan"
Escrever-Linha ""

# --- 1. ngrok instalado? ---------------------------------------------
$ngrokCmd = Get-Command ngrok -ErrorAction SilentlyContinue
if ($null -eq $ngrokCmd) {
    Escrever-Linha "ERRO: ngrok nao encontrado no PATH." "Red"
    Escrever-Linha ""
    Escrever-Linha "Instale ngrok seguindo as instrucoes do README.md," "Yellow"
    Escrever-Linha "ou baixe direto em https://ngrok.com/download" "Yellow"
    Escrever-Linha ""
    exit 1
}
Escrever-Linha "[OK] ngrok encontrado em: $($ngrokCmd.Source)" "Green"

# --- 2. Ja tem ngrok rodando? ----------------------------------------
$portaUsada = Get-NetTCPConnection -LocalPort 4040 -ErrorAction SilentlyContinue
if ($null -ne $portaUsada) {
    Escrever-Linha "[OK] ngrok ja esta rodando (interface em $InterfaceWeb)." "Green"
    Escrever-Linha ""
    Escrever-Linha "URL publica do tunel:" "Cyan"
    Escrever-Linha "  $UrlPublica" "White"
    Escrever-Linha ""
    Escrever-Linha "Proximos passos:" "Cyan"
    Escrever-Linha "  1. Abra o Visual Studio (SistemaWebAgendamentoFerriCT.sln)" "White"
    Escrever-Linha "  2. Pressione F5" "White"
    Escrever-Linha "  3. Acesse a aplicacao em $UrlLocal" "White"
    Escrever-Linha ""
    exit 0
}

# --- 3. Subir ngrok em nova janela -----------------------------------
Escrever-Linha "Subindo ngrok no subdominio fixo..." "Yellow"
Escrever-Linha "  Subdominio : $Subdominio" "Gray"
Escrever-Linha "  Local      : $UrlLocal" "Gray"
Escrever-Linha ""

$argumentos = @(
    "http", $UrlLocal,
    "--domain=$Subdominio",
    # IIS Express rejeita requests com Host diferente de localhost (400 Invalid Hostname).
    # --host-header=rewrite faz o ngrok reescrever o Host header pra localhost:44358
    # antes de encaminhar pro backend.
    "--host-header=rewrite"
)

Start-Process -FilePath $ngrokCmd.Source `
              -ArgumentList $argumentos `
              -WindowStyle Normal

# Aguarda alguns segundos pro ngrok subir
Start-Sleep -Seconds 3

# --- 4. Mostrar status final -----------------------------------------
Escrever-Linha "[OK] ngrok iniciado em uma nova janela." "Green"
Escrever-Linha ""
Escrever-Linha "=====================================================" "Cyan"
Escrever-Linha " URL publica do tunel" "Cyan"
Escrever-Linha "=====================================================" "Cyan"
Escrever-Linha "  $UrlPublica" "White"
Escrever-Linha ""
Escrever-Linha "Interface web do ngrok (debug): $InterfaceWeb" "Gray"
Escrever-Linha ""
Escrever-Linha "Proximos passos:" "Cyan"
Escrever-Linha "  1. Abra o Visual Studio (SistemaWebAgendamentoFerriCT.sln)" "White"
Escrever-Linha "  2. Pressione F5" "White"
Escrever-Linha "  3. Acesse a aplicacao em $UrlLocal" "White"
Escrever-Linha ""
Escrever-Linha "Para parar o tunel: feche a janela do ngrok (Ctrl+C nela)." "Gray"
Escrever-Linha ""
