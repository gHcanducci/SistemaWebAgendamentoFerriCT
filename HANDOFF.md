# Handoff — Sistema Ferri CT

Documento de contexto operacional para Claude e para o usuário. Pensado pra **reta final de projeto** (demo 03/06/2026). Histórico de sessões vive no `git log` + commits — aqui só fica o que tem **utilidade contínua**.

---

## 🎯 Estado atual

- **Branch ativa:** `demo/sem-mp-simulado`
- **Demo:** 03/06/2026 (PC do professor, `git clone` + F5)
- **Último commit (2026-05-29):** Bloco 2 fechado (ownership em `Confirmacao` + whitelist em `EditarAgendamento` POST). Working tree limpo.
- **Próximo passo:** Bloco 3 (Cancelamento pelo cliente). Ver "Cronograma reta final" abaixo.

Para reabrir contexto numa nova sessão, basta dizer ao Claude: **"continua do HANDOFF.md"** ou apontar a etapa do plano.

---

## 🚀 Como rodar o projeto

```powershell
# Visual Studio: F5 (StartAction = Project → abre Home/Index)
```
Banco LocalDB sobe via migration automática no `Application_Start`. Seed cria 2 clientes demo, professores, turmas e horários.

### Credenciais de demo

| Tipo | Login | Senha |
|---|---|---|
| Admin | (URL `/Admin/Login`) | `123` (SHA-256 hardcoded) |
| Cliente demo | `demo@ferrict.com.br` | CPF `529.982.247-25` |
| Cliente 2 | `maria@ferrict.com.br` | CPF `111.444.777-35` |

### URLs principais
- `/` → Home (público)
- `/Cliente/Login`, `/Cliente/Cadastro`, `/Cliente/Perfil`
- `/Agendamento/Create`, `/Agendamento/Pagamento/{id}`, `/Agendamento/Confirmacao/{id}`
- `/Admin/Login`, `/Admin/Index`, `/Admin/Agendamentos`, `/Admin/Alunos`, `/Admin/RegistrarPagamentoManual/{id}`

---

## 📁 Estrutura do projeto

Stack: **ASP.NET MVC 5 + EF 6 Code First + SQL Server LocalDB + Razor**. Detalhes em `CLAUDE.md` — abaixo só o que ele não cobre:

- **`App_Start/RouteConfig.cs`** — rota default `Home/Index`
- **`App_Start/BundleConfig.cs`** — existe mas hoje subutilizado; CSS vem inline nos `.cshtml` + Bootstrap via CDN no `_Layout`
- **`Tasks/AgendamentoCleanupJob.cs`** — timer estático iniciado em `Global.asax`. Cancela pendentes > 1h
- **`.csproj.user`** — não trackeado pelo git; `<StartAction>Project</StartAction>` é local

---

## ⚠️ Regras críticas pra editar este projeto

Erros silenciosos que custam tempo se ignorados.

### Razor `@@` em CSS dentro de `.cshtml`
Toda diretiva CSS com `@` precisa de `@@`: `@@media`, `@@keyframes`, `@@font-face`. Sem isso, o Razor tenta interpretar como C# e o CSS é **silenciosamente quebrado**. Não dá warning de compilação.

### UTF-8 com BOM obrigatório em `.cshtml`
ASP.NET MVC 5 deste projeto interpreta arquivo sem BOM como Windows-1252 → mojibake nos acentos (`HORÁRIO` vira `HORÃ¡RIO`, em-dash vira `ã€"`).
- `Edit`/`replace_all` **preserva** o BOM existente — seguro
- `Write` (reescrita completa) grava **sem** BOM — quebra. Após `Write` em `.cshtml` com acentos, regravar:
  ```powershell
  $path = "Views/.../Arquivo.cshtml"
  $content = [System.IO.File]::ReadAllText($path, [System.Text.Encoding]::UTF8)
  $utf8Bom = New-Object System.Text.UTF8Encoding($true)
  [System.IO.File]::WriteAllText($path, $content, $utf8Bom)
  ```
- Verificar BOM: `[System.IO.File]::ReadAllBytes($path)[0..2]` deve ser `239 187 191`

### Mudança em `Web.config`
IIS Express recicla automaticamente — não precisa reiniciar VS, mas a primeira request pós-edit é lenta (recompila views).

### Mudança em modelo EF
Sempre `Add-Migration NomeMigration` no Package Manager Console **antes** de F5. O `MigrateDatabaseToLatestVersion<>` no `Global.asax` aplica na inicialização. **NUNCA** usar `DropCreateDatabaseIfModelChanges` (quebra em produção).

### Inicializador do banco
Em `Global.asax.cs`: `SetInitializer(new MigrateDatabaseToLatestVersion<SistemaContext, Configuration>())`. Não trocar.

### Bug recorrente: `media (max-width)` sem `@@`
Já apareceu uma vez no `CriarAluno.cshtml` — não era `@media` sem `@@`, era literalmente `media` sem prefixo nenhum. Sempre dá grep antes de fechar trabalho em CSS.

---

## 🔄 Workflow de sessão

### Pré-sessão (Claude lê primeiro)
1. `git status` — saber o que está uncommitted
2. `git log --oneline -10` — últimos commits
3. `HANDOFF.md` (este arquivo) — contexto operacional
4. Plano ativo em `~/.claude/plans/` se mencionado pelo usuário
5. `MEMORY.md` é carregado automaticamente — não precisa ler

### Pós-sessão (encerramento)
1. Confirmar `git status` limpo (ou registrar uncommitted no HANDOFF se intencional)
2. Atualizar **só** a seção "Estado atual" deste arquivo — não inflar com histórico de sessão
3. Se houve aprendizado novo durável → salvar como memória, não como bullet no HANDOFF
4. Se plano avançou → atualizar plano (`~/.claude/plans/...`) com etapas concluídas
5. Perguntar antes de `git push` (afeta shared state — não é local)

### Quando o usuário pede pra "encerrar a sessão"
- Commits limpos, working tree limpo
- Estado atual + próximo passo escritos no HANDOFF
- Não pushar sem confirmação

---

## 🚧 Dívidas técnicas conhecidas

Lista mantida pra não esquecer — nenhuma bloqueia a demo.

1. **`SenhaAdminHash` hardcoded** (SHA-256 de "123") em `AdminController`. Em produção: secrets externos + bcrypt/Argon2
2. **`AgendamentoCleanupJob` usa Timer estático** — IIS app pool recycle pode perder ticks. Em produção: Hangfire
3. **Sem logging estruturado** — `System.Diagnostics.Trace`. Em produção: NLog/Serilog
4. **Sem testes automatizados** — sem cobertura mínima ainda
5. **Pagamento simulado** — essa branch não tem gateway real. O `CodigoTransacao = "DEMO-{Guid}"` distingue do `MANUAL-{Guid}` do balcão

### Nota: CPF em comparação exata
`ClienteController.Login` faz `c.CPF == vm.CPF` sem sanitizar. A view tem máscara JS formatando `529.982.247-25`. Seed precisa ter CPF formatado (já está).

---

## 🛡️ Defesas de segurança implementadas

| # | Defesa | Local |
|---|---|---|
| 1 | Senha do cliente em SHA-256 + salt + token de sessão | `ClienteController.Login` |
| 2 | `[ValidateAntiForgeryToken]` em todos os POST | controllers |
| 3 | `[FiltroAcesso]` herdado em todas as actions de Admin | `AdminController` |
| 4 | Ownership check (cliente A não acessa B) → 403 | `AgendamentoController.Pagamento/IniciarPagamento/Retorno` |
| 5 | Guard "1 PendentePagamento por cliente" | `AgendamentoController.Create` POST |
| 6 | Pagamentos pendentes cancelados ao reiniciar fluxo | `AgendamentoController.IniciarPagamento` |
| 7 | Valor recalculado server-side sempre | controllers |
| 8 | Forma de pagamento manual via whitelist | `AdminController.RegistrarPagamentoManual` POST |
| 9 | Forma de pagamento simulado via whitelist | `AgendamentoController.IniciarPagamento` POST |
| 10 | Cleanup automático de pendentes abandonados (>1h) | `Tasks/AgendamentoCleanupJob` |
| 11 | `CodigoTransacao` UNIQUE filtrado (anti-replay) | Migration |
| 12 | Botão `disabled` ao submeter (anti duplo-click) | `Pagamento.cshtml` JS |

---

## 🔍 Auditoria branch `demo/sem-mp-simulado` (atualizada 2026-05-29)

Punch list dos pontos falhos identificados na branch demo. Detalhes completos em `memory/audit-demo-branch-2026-05-27.md` (só nesta máquina). Snapshot — verificar contra código atual antes de implementar.

**Atualização 2026-05-29:**
- Lista de Espera + Capacidade de turma removidas (commit `695074f`). Itens #1, #2, #7, #9, #11 obsoletos.
- Bloco 1: Pagamento Manual linkado, copy "MP" trocada, código morto MP deletado. Itens #3, #12, #13 fechados.
- Bloco 2: ownership em `Confirmacao` + whitelist no `EditarAgendamento` POST. Itens #6 e #8 fechados.

### 🟠 Alto (UX quebrada / ação órfã)
4. **Card mobile de Alunos sem botão "Ver detalhes"** — tabela tem 3 botões (Ver/Editar/Excluir), cards só têm Editar/Excluir. Mobile esconde tabela → detalhes inacessíveis no celular.
5. **Cliente não consegue cancelar agendamento** — não há `CancelarAgendamento` no `AgendamentoController` nem no `ClienteController`. Cliente fica preso até cleanup de 1h (e só pra `PendentePagamento`).

### 🟢 Baixo (cosmético)
10. **`Alunos.cshtml:376` "Novos este mês"** compara só `.Month`, ignora ano. Janeiro/2024 conta em janeiro/2026.

### Ordem sugerida pra implementação
1. #5 (cancelamento pelo cliente)
2. #4 (botão "Ver detalhes" no card mobile)

#10 é extra se sobrar tempo.

---

## 📌 Cronograma reta final (definido 2026-05-29)

8 blocos pra fechar até 03/06. Demo é quarta — sobram sex/sáb/dom/seg/ter úteis.

| Quando | Bloco | Itens |
|---|---|---|
| **Sex 29 (hoje)** | **1** | Admin coerente sem MP — #3 botão "Registrar Pagamento Manual" em `DetalhesAgendamento` + #12 copy "MP" → "fora do sistema" em `RegistrarPagamentoManual.cshtml` + #13 deletar pasta `MercadoPago/`, `PagamentoController` (webhook) e refs no `.csproj`/`Web.config` |
| **Sex 29** | **2** | Segurança — #6 ownership check em `AgendamentoController.Confirmacao` + #8 whitelist de status em `AdminController.EditarAgendamento` POST |
| **Sáb 30 / Dom 31** | **3** | Cancelamento pelo cliente — action `CancelarAgendamento` no `AgendamentoController` + botão na lista "Meus Agendamentos" do `Perfil.cshtml` + tela de confirmação |
| **Sáb 30 / Dom 31** | **4** | Mobile — #4 botão "Ver detalhes" no card mobile de Alunos + hero da Home no mobile (`Views/Home/Index.cshtml`) |
| **Seg 01** | **5** | Acessibilidade — `aria-label` nos ícones funcionais, `aria-hidden` nos decorativos, `alt` em imagens, `role` semântico onde faltar |
| **Seg 01** | **6** | `@media print` — global (oculta nav/footer, força fundo claro+texto escuro) + dedicado em `Agendamento/Retorno.cshtml` (vira comprovante) |
| **Ter 02 manhã** | **7** | CSS externo parcial — extrair design tokens (`--accent`, `--bg-*`, `--text-*`) e CSS global pra arquivo `.css` externo + converter 1-2 views como demonstração (não refactor total — risco de regressão visual perto da demo) |
| **Ter 02 tarde** | **8** | Véspera — `git clone` num diretório novo + F5 do zero. Pegar surpresas antes da demo, não durante |
| **Qua 03** | — | Demo |

### Itens cortados (não fazer)
- Etapa do plano antigo: refactor de CSS completo (escopo grande, risco regressão a poucos dias da demo). Bloco 7 já cobre "um pouco" disso.
- #10 `Alunos.cshtml:376 .Month` ignora ano — bug real mas só aparece em 2027.
- Cenários de teste MP (cartão recusado, webhook forjado etc.) — não se aplicam nessa branch sem MP.

### Punch list ativo (referência cruzada — ver "Auditoria" acima)
- 🟠 #3, #4, #5 (em blocos 1, 4, 3)
- 🟡 #6, #8 (em bloco 2)
- 🟢 #12, #13 (em bloco 1)
- Obsoletos: #1, #2, #7, #9, #11 (removidos junto com Lista de Espera)
- Adiados: #10 (não bloqueia demo)
