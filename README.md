# Sistema Web de Agendamento — Ferri CT

Sistema web de agendamento da academia **Ferri CT** (Presidente Prudente/SP), desenvolvido como projeto acadêmico. Permite que alunos agendem aulas online e que o administrador gerencie agendamentos e alunos pelo painel.

**Stack:** ASP.NET MVC 5 (.NET Framework 4.7.2) · Entity Framework 6 (Code First + Migrations) · SQL Server LocalDB · Razor Views · Bootstrap 5

---

## Screenshots

| Home | Agendamento |
|------|-------------|
| ![Home](SistemaWebAgendamentoFerriCT/screenshots/home.png) | ![Agendamento](SistemaWebAgendamentoFerriCT/screenshots/agendamento.png) |

| Confirmação | Painel Admin |
|-------------|-------------|
| ![Confirmação](SistemaWebAgendamentoFerriCT/screenshots/confirmacao.png) | ![Admin](SistemaWebAgendamentoFerriCT/screenshots/admin-agendamentos.png) |

---

## Pré-requisitos

| Software | Versão mínima | Observação |
|---|---|---|
| **Windows** | 10 ou 11 | Necessário para .NET Framework |
| **Visual Studio** | 2019 ou superior | Community Edition serve. Instalar workload "ASP.NET e desenvolvimento web" |
| **.NET Framework** | 4.7.2 | Já incluso no Windows 10/11 atualizado |
| **SQL Server LocalDB** | 2019+ | Instalado junto com o Visual Studio (workload de dados) |

---

## Como rodar

### 1. Clonar o repositório

```powershell
git clone https://github.com/gHcanducci/SistemaWebAgendamentoFerriCT.git
cd SistemaWebAgendamentoFerriCT
```

### 2. Restaurar pacotes NuGet

Abra `SistemaWebAgendamentoFerriCT.sln` no Visual Studio. Os pacotes são restaurados automaticamente. Se houver erro: **Tools → NuGet Package Manager → Restore NuGet Packages**.

### 3. Compilar

`Ctrl + Shift + B`

### 4. Rodar

`F5` — na primeira execução o Entity Framework cria o banco, aplica as migrations e roda o seed (professores, turmas, horários e 2 clientes de demo).

A aplicação abre em `https://localhost:44358`.

---

## Credenciais de demo

### Cliente (login por e-mail + CPF)

| Nome | E-mail | CPF |
|---|---|---|
| Cliente Demo | `demo@ferrict.com.br` | `529.982.247-25` |
| Maria Aluna | `maria@ferrict.com.br` | `111.444.777-35` |

### Admin

- URL: `https://localhost:44358/Admin/Login`
- Usuário: `admin`
- Senha: `123`

> ⚠️ As credenciais de admin são fixas e servem apenas para demonstração acadêmica. Em produção deveriam estar em configuração externa com hash bcrypt/Argon2.

---

## Fluxo de pagamento

O sistema **não possui integração com gateway de pagamento real**. O fluxo foi implementado de forma simulada para fins acadêmicos — a integração com a API do Mercado Pago estava planejada, mas ficou como evolução futura.

**Como funciona na prática:**

- **Cliente:** escolhe a forma de pagamento (PIX ou Débito) na tela de pagamento. Ao confirmar, o sistema registra um `Pagamento` com status `Aprovado` e `CodigoTransacao = "DEMO-{Guid}"`. Nenhuma cobrança real é realizada.
- **Admin:** pode registrar pagamento recebido presencialmente (Dinheiro, PIX ou Débito) pelo painel em `Detalhes do Agendamento → Registrar Pagamento Manual`, gerando `CodigoTransacao = "MANUAL-{Guid}"`.
- **Valores:** sempre recalculados server-side a partir do `TipoAula`. O cliente nunca informa o valor diretamente.
- **Timeout:** agendamentos parados em `PendentePagamento` por mais de 1h são cancelados automaticamente pelo `AgendamentoCleanupJob`.

### Estados do agendamento

```
PendentePagamento ─┬─► Confirmado  (confirmação simulada pelo cliente)
                   ├─► Confirmado  (pagamento manual registrado pelo admin)
                   └─► Cancelado   (timeout de 1h ou cancelamento pelo admin)
```

---

## Estrutura do projeto

```
SistemaWebAgendamentoFerriCT/
├── Controllers/
│   ├── HomeController.cs
│   ├── ClienteController.cs        (cadastro, login, perfil)
│   ├── AgendamentoController.cs    (agendamento e pagamento simulado)
│   └── AdminController.cs          (painel administrativo)
├── Models/                         (entidades EF Code First)
├── ViewModels/                     (DTOs para as views)
├── Views/                          (Razor Views)
├── Migrations/                     (EF migrations + seed em Configuration.cs)
├── Filtros/
│   └── FiltroAcesso.cs             (action filter de autenticação do admin)
└── Tasks/
    └── AgendamentoCleanupJob.cs    (cancela agendamentos com timeout de 1h)
```

---

## Regras de negócio

### Agendamento

- Academia **fechada aos domingos** e em feriados nacionais e municipais de Presidente Prudente.
- Feriados móveis (Carnaval, Sexta Santa, Corpus Christi) calculados dinamicamente via algoritmo de Páscoa.
- Cliente só pode ter **1 agendamento ativo** por (data, horário).
- Aula **Experimental** é exclusiva para clientes sem agendamento prévio.
- Máximo de **1 agendamento em `PendentePagamento`** por cliente ao mesmo tempo.

### Pagamento

- Formas aceitas no fluxo simulado: **PIX** e **Débito**.
- Admin pode registrar pagamento presencial: **Dinheiro, PIX ou Débito**.
- Timeout de pendente: **1 hora** → cancela automaticamente.
- Valor recalculado sempre server-side — nunca confiado ao formulário ou à URL.

---

## Defesas de segurança implementadas

- Senha do cliente armazenada com **SHA-256 + salt**
- Todas as actions POST protegidas por `[ValidateAntiForgeryToken]`
- **Ownership check** em endpoints de pagamento — cliente A não acessa agendamento de B
- Guard de 1 `PendentePagamento` por cliente
- Valor recalculado server-side a partir do `TipoAula` (URL e formulário não são confiáveis)
- **Whitelist** explícita de formas de pagamento aceitas
- Cleanup automático de agendamentos abandonados após 1h via `AgendamentoCleanupJob` com controle de reentrância (`Interlocked.CompareExchange`)

---

## Comandos úteis

```powershell
# No Package Manager Console do Visual Studio:
Update-Database              # aplica migrations e roda o seed
Add-Migration NomeMigration  # cria uma nova migration
```

---

## Solução de problemas

| Sintoma | Causa provável | Solução |
|---|---|---|
| Erro de build "metadata file not found" | Pacotes NuGet não restaurados | Restore manual: **Tools → NuGet Package Manager → Restore** |
| "Cannot open database" | LocalDB não instalado | Reinstalar SQL Server LocalDB pelo Visual Studio Installer |
| `AutomaticMigrationsDisabledException` | Modelo EF divergente do snapshot | Rodar `Add-Migration NomeDescritivo` no Package Manager Console e pressionar F5 |
| Caracteres acentuados aparecem como `Ã¡` | Arquivo `.cshtml` salvo sem BOM UTF-8 | Reabrir no VS e salvar como "UTF-8 with BOM" |

---

## Licença

Projeto acadêmico — sem licença pública definida.
