# Sistema Web de Agendamento — Ferri CT

Sistema web de agendamento da academia **Ferri CT** (Presidente Prudente/SP) com integração de pagamento via **Mercado Pago Checkout Pro** (PIX + Débito).

**Stack:** ASP.NET MVC 5 (.NET Framework 4.7.2) + Entity Framework 6 (Code First com Migrations) + SQL Server LocalDB + Razor Views + Bootstrap.

---

## Pré-requisitos

| Software | Versão mínima | Observação |
|---|---|---|
| **Windows** | 10 ou 11 | Necessário para .NET Framework |
| **Visual Studio** | 2019 ou 2022 | Community Edition serve. Com workload "ASP.NET e desenvolvimento web" |
| **.NET Framework** | 4.7.2 | Já vem no Windows 10/11 atualizado |
| **SQL Server LocalDB** | 2019+ | Instala junto com Visual Studio (workload de dados) |
| **ngrok** | 3.x | Necessário **só pra apresentar o fluxo de webhook do Mercado Pago**. Não é obrigatório pra usar o sistema. |

---

## Como rodar (passo a passo)

### 1. Clonar o repositório

```powershell
git clone https://github.com/<seu-usuario>/SistemaWebAgendamentoFerriCT.git
cd SistemaWebAgendamentoFerriCT
```

### 2. Restaurar pacotes NuGet

No Visual Studio, abra `SistemaWebAgendamentoFerriCT.sln`. O VS restaura os pacotes automaticamente ao abrir a solução. Se der erro, no menu: **Tools → NuGet Package Manager → Restore NuGet Packages**.

### 3. Compilar (Ctrl+Shift+B)

A primeira compilação demora um pouco (resolve dependências).

### 4. Rodar (F5)

Na primeira execução:
- O Entity Framework cria automaticamente o banco SQL LocalDB (`SistemaWebAgendamentoFerriCT`)
- Aplica todas as migrations
- Roda o seed (popula professores, turmas, horários e 2 clientes de demo)

A aplicação abre em `https://localhost:44358`.

---

## Credenciais de demo

### Cliente (login por Email + CPF)

| Nome | Email | CPF |
|---|---|---|
| Cliente Demo | `demo@ferrict.com.br` | `529.982.247-25` |
| Maria Aluna | `maria@ferrict.com.br` | `111.444.777-35` |

### Admin

- URL: `https://localhost:44358/Admin/Login`
- Usuário: `admin`
- Senha: `123`

⚠️ A senha do admin é hardcoded e só serve pra demo acadêmica. Em produção real, deveria estar em `Web.secrets.config` com hash bcrypt/Argon2.

---

## Apresentando o fluxo de pagamento (Mercado Pago)

Pra demonstrar o pagamento **end-to-end** o Mercado Pago precisa enviar um webhook pro seu computador. Como o MP roda na internet pública, ele não chega em `localhost` direto — precisa de um túnel.

Este projeto usa **ngrok com subdomínio reservado fixo** (`molehill-salvation-clothes.ngrok-free.dev`), já cadastrado no painel do Mercado Pago.

### Configurar ngrok no PC novo (uma vez só)

1. Instalar ngrok: baixar em `https://ngrok.com/download` e adicionar ao PATH.
2. Autenticar com o token (o token está na conta ngrok do dono do projeto):
   ```powershell
   ngrok config add-authtoken <TOKEN>
   ```

### No dia da apresentação

```powershell
.\start-demo.ps1
```

O script verifica o ngrok, sobe o túnel no subdomínio fixo e mostra a URL. Depois é só F5 no Visual Studio.

### Comprador de teste (test buyer)

Pra pagar no checkout do MP em modo sandbox, é necessário logar como **test buyer** (não a conta real do MP). Email/senha do test buyer estão no arquivo `contas teste MP.txt` (não versionado).

### Cartão de teste pra Débito

| Campo | Valor |
|---|---|
| Número | `5031 4332 1540 6351` |
| CVV | `123` |
| Validade | Qualquer data futura (ex: 12/30) |
| Nome do titular (aprova) | `APRO` |
| Nome do titular (recusa) | `OTHE` |

### PIX

PIX está implementado no código mas o **sandbox do MP tem um bug conhecido** com cadastro de chave PIX em contas de teste (erro `PKF03`). A validação completa do fluxo PIX só funciona em produção com transação real.

---

## Estrutura do projeto

```
SistemaWebAgendamentoFerriCT/
├── Controllers/
│   ├── HomeController.cs
│   ├── ClienteController.cs       (login, cadastro, perfil)
│   ├── AdminController.cs         (painel admin)
│   ├── AgendamentoController.cs   (agendar, listar, cancelar)
│   └── PagamentoController.cs     (webhook do MP, 9 etapas de validacao)
├── Models/                        (entidades EF Code First)
├── ViewModels/                    (DTOs pra views)
├── Views/                         (Razor)
├── Migrations/                    (EF migrations + Configuration.cs com seed)
├── MercadoPago/
│   ├── MercadoPagoService.cs      (cria Preferences + consulta payments)
│   ├── WebhookSignatureValidator.cs (HMAC-SHA256 em constant time)
│   └── MercadoPagoSettings.cs
├── Filtros/
│   └── FiltroAcesso.cs            (action filter pra admin)
├── Tasks/
│   └── AgendamentoCleanupJob.cs   (cancela agendamentos com timeout 1h)
└── Web.config                     (config + credenciais MP sandbox)
```

---

## Regras de negócio principais

### Agendamento

- Academia **fechada aos domingos** e em feriados (fixos + móveis via algoritmo de Páscoa).
- Cliente só pode ter 1 agendamento ativo por (data, horário).
- Aula **Experimental** é exclusiva para clientes sem agendamento prévio.
- Capacidade da turma é checada antes de confirmar. Excedente vai pra `ListaEspera`.

### Pagamento

- Gateway: **Mercado Pago Checkout Pro**
- Métodos: **PIX + Débito apenas** (crédito, prepago, cripto e boleto bloqueados via `excluded_payment_types`)
- Timeout de pendente: **1h** → cancela automaticamente e libera vaga
- Sem reembolso após pagamento aprovado
- Lista de espera não gera pagamento (admin promove)
- Pagamento manual pelo admin permitido (`CodigoTransacao = "MANUAL-{Guid}"`)
- Máximo 1 agendamento `PendentePagamento` por cliente

### Estados de agendamento

```
PendentePagamento ─┬─► EmAnalise ─┬─► Confirmado
                   │              └─► Cancelado (MP rejeitou)
                   ├─► Confirmado (PIX aprovou direto)
                   └─► Cancelado (timeout 1h)

AguardandoVaga ─► PendentePagamento (admin promove)
```

---

## Defesas de segurança da integração MP

O webhook de pagamento implementa 20 defesas documentadas. Ver `HANDOFF.md` pra lista completa.

Resumo dos itens críticos:
- HMAC-SHA256 do `x-signature` validado em **constant time** (resistente a timing attack)
- Tolerância de timestamp de 5min (anti-replay)
- `WebhookEventoId` UNIQUE no banco (idempotência)
- Re-busca de cada pagamento via `/v1/payments/{id}` antes de confirmar
- Validação de `transaction_amount` server-side contra valores hardcoded no controller
- Ownership check em todos endpoints de pagamento (cliente A não acessa pagamento de B)
- Guard "1 PendentePagamento por cliente"
- TLS 1.2 forçado no HttpClient

---

## Documentos relacionados

- **`HANDOFF.md`** — estado atual do trabalho, decisões técnicas e dívidas conhecidas
- **`DEMONSTRACAO-MERCADO-PAGO.md`** — roteiro de apresentação ao professor (script falado, perguntas prováveis)
- **`CLAUDE.md`** — instruções pro assistente de IA (Claude Code) que ajuda no desenvolvimento

---

## Comandos úteis

```powershell
# Restaurar pacotes NuGet (linha de comando)
nuget restore

# No Package Manager Console do Visual Studio:
Update-Database              # aplica migrations + roda seed
Add-Migration NomeMigration  # cria nova migration
```

---

## Solução de problemas

| Sintoma | Causa provável | Solução |
|---|---|---|
| Erro de build "metadata file not found" | Pacotes NuGet não restaurados | Restore manual no menu do VS |
| "Cannot open database" | LocalDB não instalado | Reinstalar SQL Server LocalDB |
| Webhook do MP retorna 401 | `WebhookSecret` errado no Web.config | Conferir no painel do MP → Webhooks → Chave Secreta |
| Webhook nunca chega | ngrok não está rodando | Rodar `.\start-demo.ps1` |
| Pagamento aprovado mas status não muda | Webhook caiu antes de chegar | Aguardar até 5min OU forçar reload da página (auto-refresh é 5s) |
| `account_money` aparece como opção | Comprador tem saldo na carteira MP | Aceitável — MP não permite excluir esse método via API |

---

## Licença

Projeto acadêmico — sem licença pública definida.
