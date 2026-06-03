# Revisão dos Blocos — Ferri CT Demo

Guia para testar localmente cada bloco entregue na branch `demo/sem-mp-simulado`.
Blocos 1–4 foram implementados em 2026-05-29 / 2026-05-30.

---

## Como rodar o projeto localmente

1. **Clonar ou atualizar a branch correta**

   ```powershell
   git clone <URL> SistemaWebAgendamentoFerriCT
   cd SistemaWebAgendamentoFerriCT
   git checkout demo/sem-mp-simulado
   ```

   Ou, se já clonou:

   ```powershell
   git pull origin demo/sem-mp-simulado
   ```

2. **Abrir no Visual Studio**
   - Abra `SistemaWebAgendamentoFerriCT.sln`
   - Visual Studio vai restaurar pacotes NuGet automaticamente
   - Pressione **F5** (ou Debug → Start Debugging)

3. **Banco de dados**
   - Nenhuma configuração necessária. O `Global.asax.cs` roda as migrations e o seed na primeira execução
   - Seed cria: 2 clientes demo, professores, turmas e horários

4. **Credenciais de acesso**

   | Tipo | Campo | Valor |
   |------|-------|-------|
   | Admin | URL de login | `/Admin/Login` |
   | Admin | Senha | `123` |
   | Cliente demo 1 | E-mail | `demo@ferrict.com.br` |
   | Cliente demo 1 | CPF (senha) | `529.982.247-25` |
   | Cliente demo 2 | E-mail | `maria@ferrict.com.br` |
   | Cliente demo 2 | CPF (senha) | `111.444.777-35` |

---

## Bloco 1 — Admin coerente sem Mercado Pago

**Commit:** `c0168c6`

### O que foi alterado

| Arquivo | Mudança |
|---------|---------|
| `Views/Admin/DetalhesAgendamento.cshtml` | Botão "Registrar Pagamento Manual" adicionado |
| `Views/Admin/RegistrarPagamentoManual.cshtml` | Copy ajustada — "fora do sistema digital" (antes dizia "fora do Mercado Pago") |
| `MercadoPago/` (pasta) | Deletada (código morto) |
| `Controllers/PagamentoController.cs` | Deletado (webhook MP que não era chamado) |

### Como navegar para testar

1. Faça login como **Admin** em `/Admin/Login`
2. Acesse **Agendamentos** no menu lateral → clique em **Ver detalhes** de qualquer agendamento com status `PendentePagamento`
3. Na tela de detalhes, verifique que o botão **"Registrar Pagamento Manual"** aparece no rodapé da página

### O que verificar

- [ ] Botão "Registrar Pagamento Manual" visível em `DetalhesAgendamento`
- [ ] Ao clicar, abre a tela `/Admin/RegistrarPagamentoManual/{id}` sem erro 404
- [ ] Título da tela diz "**fora do sistema digital**", não "fora do Mercado Pago"
- [ ] Após confirmar o pagamento, o agendamento muda para status `Confirmado`

---

## Bloco 2 — Segurança: ownership + whitelist de status

**Commit:** `608b668`

### O que foi alterado

| Arquivo | Mudança |
|---------|---------|
| `Controllers/AgendamentoController.cs` | `Confirmacao(int id)` — adicionado ownership check (retorna 403 se `ClienteId` não bate) |
| `Controllers/AdminController.cs` | `EditarAgendamento` POST — whitelist de status válidos (`PendentePagamento`, `EmAnalise`, `Confirmado`, `Cancelado`); qualquer outra string retorna 400 |

### Como navegar para testar

**Ownership (Confirmacao):**
1. Faça login como **Cliente demo 1** (`demo@ferrict.com.br`)
2. Crie um agendamento e copie o ID da URL na tela de confirmação
3. Em outra aba / modo incógnito, faça login como **Cliente demo 2** (`maria@ferrict.com.br`)
4. Tente acessar `/Agendamento/Confirmacao/{ID_do_cliente_1}` diretamente pela URL

**Whitelist de status:**
1. Faça login como **Admin**
2. Abra as DevTools do browser (F12) → aba Network
3. Acesse **Agendamentos** e clique em **Editar** em qualquer agendamento
4. Observe que o select de status só exibe as 4 opções válidas

### O que verificar

- [ ] Tentar `/Agendamento/Confirmacao/{id_de_outro_cliente}` retorna **403 Forbidden** (tela de erro, não os dados do agendamento alheio)
- [ ] Admin consegue editar status para `Confirmado`, `Cancelado`, `PendentePagamento`, `EmAnalise` normalmente
- [ ] Status inválido rejeitado pelo servidor (não aparece na lista de opções do select)

---

## Bloco 3 — Cancelamento de agendamento pelo cliente

**Commit:** `9593eae`

### O que foi alterado

| Arquivo | Mudança |
|---------|---------|
| `Controllers/AgendamentoController.cs` | Actions `GET Cancelar` + `POST CancelarConfirmado` adicionadas |
| `Views/Agendamento/Cancelar.cshtml` | View nova — tela de confirmação antes de cancelar |
| `Views/Cliente/Perfil.cshtml` | Botão de cancelar adicionado ao lado de "Efetuar Pagamento" na tabela de agendamentos |

### Como navegar para testar

1. Faça login como **Cliente demo 1** em `/Cliente/Login`
2. Crie um agendamento novo em `/Agendamento/Create`
3. Na tela de pagamento, **não pague** — volte para o Perfil em `/Cliente/Perfil`
4. Na tabela "Meus Agendamentos", localize o agendamento com status **Pendente Pagamento**
5. Clique no ícone de **X** (cancelar) ao lado do botão "Efetuar Pagamento"

### O que verificar

- [ ] Botão de cancelar (ícone X vermelho) visível nos agendamentos com status `PendentePagamento`
- [ ] Clicar abre a tela `/Agendamento/Cancelar/{id}` com resumo do agendamento (tipo, data, horário, turma)
- [ ] Botão "Manter agendamento" volta para o Perfil sem alterar nada
- [ ] Botão "Cancelar" (vermelho) confirma o cancelamento, redireciona para o Perfil com toast de sucesso
- [ ] O agendamento cancelado aparece com status `Cancelado` (ou some da lista, dependendo do filtro)
- [ ] Tentar cancelar um agendamento já `Confirmado` diretamente pela URL (`/Agendamento/Cancelar/{id_confirmado}`) redireciona com mensagem de erro, sem cancelar
- [ ] Tentar cancelar agendamento de outro cliente retorna **403 Forbidden**

---

## Bloco 4 — Mobile: cards de Alunos + hero da Home

**Commit:** `8fef7a1`

### O que foi alterado

| Arquivo | Mudança |
|---------|---------|
| `Views/Admin/Alunos.cshtml` | Botão "Detalhes" (azul) adicionado aos cards como ação primária; Editar vira ícone-only; CSS `.action-btn-view` + `.card-btn-view` adicionados |
| `Views/Home/Index.cshtml` | Stats do hero em grade 2×2 no mobile (antes 4 items em coluna); overlay mais escuro no mobile; padding reduzido em `≤768px`; novo breakpoint `≤480px` (botões full-width, fonte menor) |

### Como navegar para testar — Alunos (cards mobile)

1. Faça login como **Admin**
2. Acesse `/Admin/Alunos`
3. **Redimensione o browser para menos de 768px** (DevTools → ícone de celular, ou arraste a janela)
4. A tabela deve desaparecer e os cards devem aparecer automaticamente

### O que verificar — Alunos

- [ ] Em `≤768px` (mobile): tabela sumiu, cards aparecem em coluna única
- [ ] Cada card tem **3 botões**: "Detalhes" (azul, largo), lápis (amarelo), lixeira (vermelho)
- [ ] "Detalhes" leva para `/Admin/DetalhesAluno/{id}` — a página abre sem erro
- [ ] Lápis leva para `/Admin/EditarAluno/{id}`
- [ ] Lixeira leva para `/Admin/ExcluirAluno/{id}`
- [ ] Em `≥769px` (desktop): view-toggle (ícones tabela/cards no canto) funciona normalmente; cards no modo desktop também têm os 3 botões

### Como navegar para testar — Hero da Home

1. Acesse `/` (home pública — sem login)
2. **Redimensione para menos de 768px** (DevTools → device mode)
3. Observe a seção de estatísticas no final do hero ("8+ Anos", "500+ Alunos", etc.)

### O que verificar — Hero

- [ ] Em `≤768px`: estatísticas aparecem em **grade 2×2** (dois itens por linha), não em coluna única
- [ ] Números ("8+", "500+", "100%", "4") legíveis sobre fundo escuro — sem foto de fundo visível por baixo
- [ ] Em `≤480px`: botões "Agendar Aula Grátis" e "Saiba mais" ficam **empilhados verticalmente** e ocupam a largura total
- [ ] Sem scroll horizontal em nenhum dos breakpoints
- [ ] Hero continua bonito em desktop (1280px+) — sem regressão

---

## Bloco 5 — Acessibilidade (alt + ARIA)

**Commit:** `6f96c28`

### O que foi alterado

| Arquivo | Mudança |
|---------|---------|
| `Views/Shared/_Layout.cshtml` | `aria-label` na `<nav>`, no link da marca, no botão mobile; `role="menu"` + `role="menuitem"` no dropdown do usuário; `aria-expanded` sincronizado via JS |
| `Views/Shared/_LayoutAdmin.cshtml` | `aria-label` na `<nav>` lateral e nos botões da topbar; `aria-expanded` no toggle mobile via `toggleSidebar()` |
| `Views/Home/Index.cshtml` | `aria-label` nas sections hero, diferenciais, "como funciona" e CTA; `aria-hidden` nos elementos decorativos (hero-bg, glow, watermark, pulse); `role="img"` + `aria-label` no `hero-bg` |
| `Views/Admin/Alunos.cshtml` | `role="status"`/`role="alert"` + `aria-live` nos toasts; `aria-label` no campo de busca; `aria-pressed` nos botões de alternância de visualização; `aria-label` contextual em cada botão de ação (tabela e cards) |
| `Views/Admin/Agendamentos.cshtml` | `aria-label` no campo de busca e na tabela; `aria-pressed` nos chips de filtro; `aria-label` contextual nos botões de ação; JS sincroniza `aria-pressed` ao clicar |
| `Views/Cliente/Perfil.cshtml` | `role="status"` + `aria-live="polite"` no toast; `aria-label="Meus agendamentos"` na tabela; `aria-label` contextual no botão de cancelar |
| `Views/Agendamento/Cancelar.cshtml` | `role="alertdialog"` + `aria-labelledby` no card de confirmação; `aria-hidden="true"` no ícone decorativo |

### Como navegar para testar

**Teste com leitor de tela (NVDA ou Narrator):**

1. **Navbar pública** — Acesse `/`
   - Ligue o Narrator (`Win + Ctrl + Enter`) ou o NVDA
   - Navegue pelo teclado (`Tab`) na barra de navegação
   - Verifique que o botão hambúrguer anuncia "Abrir menu de navegação"
   - Faça login como cliente e abra o dropdown de perfil; verifique que anuncia "expandido/recolhido"

2. **Hero decorativo** — Ainda em `/`
   - Navegue pela página; o fundo do hero deve ser anunciado como imagem ("Fachada da academia Ferri CT")
   - Elementos decorativos (glow, watermark, ponto pulsante) não devem ser lidos pelo leitor

3. **Sections** — Role a página `/`
   - Cada `<section>` tem label próprio; o leitor anuncia o título ao entrar nela

4. **Painel admin — view-toggle** — Acesse `/Admin/Alunos`
   - Foque os botões de alternância (tabela/cards)
   - Devem anunciar "pressionado" quando ativos

5. **Painel admin — filtro de agendamentos** — Acesse `/Admin/Agendamentos`
   - Clique nos chips de filtro; o leitor deve anunciar "pressionado" no chip ativo

6. **Botões de ação icon-only** — Em `/Admin/Alunos` e `/Admin/Agendamentos`
   - Foque os ícones de lápis, olho e lixeira; o leitor deve anunciar o nome completo do aluno/agendamento
   - Exemplo: "Ver detalhes de João Silva" em vez de apenas "link"

7. **Toast de confirmação** — Cancel um agendamento em `/Cliente/Perfil`
   - Ao cancelar, o toast de sucesso deve ser anunciado automaticamente pelo leitor sem precisar focar nele

8. **Tela de cancelamento** — Acesse `/Agendamento/Cancelar/{id}`
   - O card de confirmação é marcado como `alertdialog`; o leitor deve anunciá-lo ao entrar na página

### O que verificar (sem leitor de tela — inspeção manual)

Abra as DevTools → aba **Elements** e inspecione:

- [ ] `<nav class="ferri-nav">` tem `aria-label="Navegação principal"`
- [ ] `<a href="/" class="nav-brand">` tem `aria-label="Ferri CT — Página inicial"`
- [ ] `.dot` decorativo na marca tem `aria-hidden="true"`
- [ ] Botão hambúrguer mobile tem `aria-expanded="false"` (muda para `true` ao abrir)
- [ ] `<div class="nav-dropdown">` tem `role="menu"` e `id="nav-user-dropdown"`
- [ ] Cada `<a class="dropdown-item">` tem `role="menuitem"`
- [ ] `<nav class="sidebar-nav">` tem `aria-label="Navegação do painel administrativo"`
- [ ] `<div class="hero-bg">` tem `role="img"` e `aria-label="Fachada da academia Ferri CT"`
- [ ] `<div class="hero-glow">`, `hero-watermark` e `.pulse` têm `aria-hidden="true"`
- [ ] `<section id="diferenciais">` tem `aria-labelledby="section-diferenciais-title"`
- [ ] Em Alunos, o input de busca tem `aria-label`
- [ ] Em Alunos, os botões de toggle têm `aria-pressed` correto
- [ ] Em Agendamentos, os chips de filtro têm `aria-pressed` correto
- [ ] Em Perfil, a `<table class="ag-table">` tem `aria-label="Meus agendamentos"`
- [ ] Em Cancelar, o `.cancel-card` tem `role="alertdialog"` e `aria-labelledby`

---

## Verificação final rápida (smoke test geral)

Execute este roteiro de ponta a ponta antes da demo:

1. `/` → Home carrega com foto de fundo e hero
2. `/Cliente/Cadastro` → criar um novo cliente
3. `/Cliente/Login` → logar com o cliente criado
4. `/Agendamento/Create` → criar agendamento (não-experimental)
5. Tela de pagamento aparece → **não pague**
6. `/Cliente/Perfil` → agendamento em "Pendente Pagamento" + botão X visível
7. Clicar X → tela de cancelamento aparece → confirmar
8. Perfil mostra agendamento cancelado ou toast de sucesso
9. `/Admin/Login` (senha `123`) → painel admin carrega
10. `/Admin/Alunos` → tabela com o cliente criado aparece
11. Reduzir para mobile → cards aparecem com 3 botões
12. "Detalhes" abre a tela de DetalhesAluno sem erro
13. `/Admin/Agendamentos` → achar o agendamento cancelado → "Ver detalhes"
14. Botão "Registrar Pagamento Manual" visível → clicar → registrar → status vai pra Confirmado

---

*Documento atualizado em 2026-05-29 — branch `demo/sem-mp-simulado`, HEAD `6f96c28`*
