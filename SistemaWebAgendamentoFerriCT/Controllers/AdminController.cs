using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Web.Mvc;
using SistemaWebAgendamentoFerriCT.Filtros;
using SistemaWebAgendamentoFerriCT.Models;
using SistemaWebAgendamentoFerriCT.ViewModels;

namespace SistemaWebAgendamentoFerriCT.Controllers
{
    // [FiltroAcesso] protege TODAS as actions — exceto Login e Logout
    // que ficam fora do filtro por herança direta
    [FiltroAcesso]
    public class AdminController : Controller
    {
        private const string UsuarioAdmin = "admin";
        // SHA-256 de "123" — troque em produção
        private const string SenhaAdminHash = "a665a45920422f9d417e4867efdc4fb8a04a1f3fff1fa07e998e86f7f7a27ae3";

        private readonly SistemaContext db = new SistemaContext();

        // ─── Autenticação (sem FiltroAcesso — são públicas) ─────────────

        [OverrideActionFilters] // Remove o FiltroAcesso só nessas actions
        public ActionResult Login()
        {
            if (Session["AdminLogado"] != null)
                return RedirectToAction("Index");

            return View(new AdminLoginViewModel());
        }

        [HttpPost]
        [OverrideActionFilters]
        [ValidateAntiForgeryToken]
        public ActionResult Login(AdminLoginViewModel vm)
        {
            try
            {
                if (!ModelState.IsValid)
                    return View(vm);

                if (vm.Usuario == UsuarioAdmin && HashPassword(vm.Senha) == SenhaAdminHash)
                {
                    // Token de sessão único para segurança adicional
                    Session["AdminLogado"] = true;
                    Session["AdminNome"] = "Administrador";
                    return RedirectToAction("Index");
                }

                ViewBag.Erro = "Usuário ou senha incorretos.";
                return View(vm);
            }
            catch (Exception)
            {
                ViewBag.Erro = "Erro ao realizar login. Tente novamente.";
                return View(vm);
            }
        }

        [OverrideActionFilters]
        public ActionResult Logout()
        {
            Session.Clear();
            Session.Abandon(); // Destrói a sessão no servidor (Aula 11)
            return RedirectToAction("Login");
        }

        // ─── Dashboard ──────────────────────────────────────────────────

        public ActionResult Index()
        {
            try
            {
                ViewBag.TotalAlunos = db.Clientes.Count();
                ViewBag.AgendamentosHoje = db.Agendamentos.Count(a => a.DataAula == DateTime.Today);
                ViewBag.PendentePagamento = db.Agendamentos.Count(a => a.Status == "PendentePagamento");
                ViewBag.TotalTurmas = db.Turmas.Count();
                ViewBag.ConfirmadosHoje = db.Agendamentos.Count(a => a.DataAula == DateTime.Today && a.Status == "Confirmado");

                return View();
            }
            catch (Exception)
            {
                ViewBag.Erro = "Erro ao carregar dados do painel.";
                return View();
            }
        }

        // ─── Alunos ─────────────────────────────────────────────────────

        public ActionResult Alunos()
        {
            try
            {
                var alunos = db.Clientes.OrderByDescending(c => c.DataCadastro).ToList();
                return View(alunos);
            }
            catch (Exception)
            {
                ViewBag.Erro = "Erro ao carregar lista de alunos.";
                return View(new List<Cliente>());
            }
        }

        public ActionResult DetalhesAluno(int id)
        {
            try
            {
                var aluno = db.Clientes
                    .Include("Agendamentos")
                    .Include("Agendamentos.HorarioTurma")
                    .Include("Agendamentos.HorarioTurma.Turma")
                    .FirstOrDefault(c => c.ClienteId == id);

                if (aluno == null) return HttpNotFound();
                return View(aluno);
            }
            catch (Exception)
            {
                TempData["Erro"] = "Erro ao carregar detalhes do aluno.";
                return RedirectToAction("Alunos");
            }
        }

        public ActionResult CriarAluno()
        {
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult CriarAluno(Cliente cliente)
        {
            try
            {
                if (ModelState.IsValid)
                {
                    if (db.Clientes.Any(c => c.CPF == cliente.CPF))
                    {
                        ModelState.AddModelError("CPF", "CPF já cadastrado.");
                        return View(cliente);
                    }

                    cliente.DataCadastro = DateTime.Now;
                    db.Clientes.Add(cliente);
                    db.SaveChanges();

                    TempData["Sucesso"] = "Aluno cadastrado com sucesso!";
                    return RedirectToAction("Alunos");
                }
            }
            catch (Exception)
            {
                ModelState.AddModelError("", "Erro ao cadastrar aluno. Tente novamente.");
            }

            return View(cliente);
        }

        public ActionResult EditarAluno(int id)
        {
            try
            {
                var aluno = db.Clientes.Find(id);
                if (aluno == null) return HttpNotFound();
                return View(aluno);
            }
            catch (Exception)
            {
                TempData["Erro"] = "Erro ao carregar dados do aluno.";
                return RedirectToAction("Alunos");
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult EditarAluno(Cliente cliente)
        {
            try
            {
                if (ModelState.IsValid)
                {
                    var aluno = db.Clientes.Find(cliente.ClienteId);
                    if (aluno == null) return HttpNotFound();

                    aluno.Nome = cliente.Nome;
                    aluno.Email = cliente.Email;
                    aluno.Telefone = cliente.Telefone;
                    aluno.CPF = cliente.CPF;

                    db.SaveChanges();
                    TempData["Sucesso"] = "Aluno atualizado com sucesso!";
                    return RedirectToAction("Alunos");
                }
            }
            catch (Exception)
            {
                ModelState.AddModelError("", "Erro ao atualizar aluno. Tente novamente.");
            }

            return View(cliente);
        }

        public ActionResult ExcluirAluno(int id)
        {
            try
            {
                var aluno = db.Clientes.Find(id);
                if (aluno == null) return HttpNotFound();
                return View(aluno);
            }
            catch (Exception)
            {
                TempData["Erro"] = "Erro ao carregar dados do aluno.";
                return RedirectToAction("Alunos");
            }
        }

        [HttpPost, ActionName("ExcluirAluno")]
        [ValidateAntiForgeryToken]
        public ActionResult ExcluirAlunoConfirmado(int id)
        {
            try
            {
                var aluno = db.Clientes.Find(id);
                if (aluno == null) return HttpNotFound();

                db.Clientes.Remove(aluno);
                db.SaveChanges();

                TempData["Sucesso"] = "Aluno excluído com sucesso!";
                return RedirectToAction("Alunos");
            }
            catch (Exception)
            {
                TempData["Erro"] = "Erro ao excluir aluno. Verifique se não possui agendamentos ativos.";
                return RedirectToAction("Alunos");
            }
        }

        // ─── Agendamentos ────────────────────────────────────────────────

        public ActionResult Agendamentos()
        {
            try
            {
                var agendamentos = db.Agendamentos
                    .Include("Cliente")
                    .Include("HorarioTurma")
                    .Include("HorarioTurma.Turma")
                    .OrderByDescending(a => a.DataSolicitacao)
                    .ToList();

                return View(agendamentos);
            }
            catch (Exception)
            {
                ViewBag.Erro = "Erro ao carregar agendamentos.";
                return View(new List<Agendamento>());
            }
        }

        public ActionResult DetalhesAgendamento(int id)
        {
            try
            {
                var agendamento = db.Agendamentos
                    .Include("Cliente")
                    .Include("HorarioTurma")
                    .Include("HorarioTurma.Turma")
                    .Include("Pagamentos")
                    .FirstOrDefault(a => a.AgendamentoId == id);

                if (agendamento == null) return HttpNotFound();
                return View(agendamento);
            }
            catch (Exception)
            {
                TempData["Erro"] = "Erro ao carregar detalhes do agendamento.";
                return RedirectToAction("Agendamentos");
            }
        }

        public ActionResult EditarAgendamento(int id)
        {
            try
            {
                var agendamento = db.Agendamentos
                    .Include("Cliente")
                    .Include("HorarioTurma")
                    .Include("HorarioTurma.Turma")
                    .FirstOrDefault(a => a.AgendamentoId == id);

                if (agendamento == null) return HttpNotFound();

                ViewBag.StatusOpcoes = new SelectList(
                    new[] { "PendentePagamento", "Confirmado", "Cancelado" },
                    agendamento.Status
                );

                return View(agendamento);
            }
            catch (Exception)
            {
                TempData["Erro"] = "Erro ao carregar agendamento.";
                return RedirectToAction("Agendamentos");
            }
        }

        // Whitelist de status aceitos no POST de edição — protege a máquina de estados
        // contra POST forjado que poderia setar qualquer string.
        private static readonly HashSet<string> StatusValidos =
            new HashSet<string>(StringComparer.Ordinal) { "PendentePagamento", "Confirmado", "Cancelado" };

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult EditarAgendamento(int agendamentoId, string status)
        {
            if (string.IsNullOrWhiteSpace(status) || !StatusValidos.Contains(status))
            {
                TempData["Erro"] = "Status inválido.";
                return RedirectToAction("EditarAgendamento", new { id = agendamentoId });
            }

            try
            {
                var agendamento = db.Agendamentos.Find(agendamentoId);
                if (agendamento == null) return HttpNotFound();

                agendamento.Status = status;
                db.SaveChanges();

                TempData["Sucesso"] = "Status do agendamento atualizado!";
                return RedirectToAction("Agendamentos");
            }
            catch (Exception)
            {
                TempData["Erro"] = "Erro ao atualizar agendamento.";
                return RedirectToAction("Agendamentos");
            }
        }

        public ActionResult ExcluirAgendamento(int id)
        {
            try
            {
                var agendamento = db.Agendamentos
                    .Include("Cliente")
                    .Include("HorarioTurma")
                    .Include("HorarioTurma.Turma")
                    .FirstOrDefault(a => a.AgendamentoId == id);

                if (agendamento == null) return HttpNotFound();
                return View(agendamento);
            }
            catch (Exception)
            {
                TempData["Erro"] = "Erro ao carregar agendamento.";
                return RedirectToAction("Agendamentos");
            }
        }

        [HttpPost, ActionName("ExcluirAgendamento")]
        [ValidateAntiForgeryToken]
        public ActionResult ExcluirAgendamentoConfirmado(int id)
        {
            try
            {
                var agendamento = db.Agendamentos.Find(id);
                if (agendamento == null) return HttpNotFound();

                db.Agendamentos.Remove(agendamento);
                db.SaveChanges();

                TempData["Sucesso"] = "Agendamento excluído com sucesso!";
                return RedirectToAction("Agendamentos");
            }
            catch (Exception)
            {
                TempData["Erro"] = "Erro ao excluir agendamento.";
                return RedirectToAction("Agendamentos");
            }
        }

        // ─── Pagamento manual (balcão) ───────────────────────────────────
        // Permite ao admin registrar pagamento feito fora do MP (dinheiro, PIX direto).
        // CodigoTransacao = "MANUAL-{Guid}" para distinguir de pagamentos do MP.

        private const decimal ValorExperimental = 50.00m;
        private const decimal ValorMatricula = 50.00m;

        public ActionResult RegistrarPagamentoManual(int id)
        {
            try
            {
                var agendamento = db.Agendamentos
                    .Include("Cliente")
                    .Include("HorarioTurma")
                    .Include("HorarioTurma.Turma")
                    .FirstOrDefault(a => a.AgendamentoId == id);

                if (agendamento == null) return HttpNotFound();

                // Só faz sentido para agendamentos aguardando pagamento
                if (agendamento.Status != "PendentePagamento")
                {
                    TempData["Erro"] = $"Não é possível registrar pagamento manual para agendamento no status '{agendamento.Status}'.";
                    return RedirectToAction("DetalhesAgendamento", new { id });
                }

                return View(agendamento);
            }
            catch (Exception)
            {
                TempData["Erro"] = "Erro ao carregar agendamento.";
                return RedirectToAction("Agendamentos");
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult RegistrarPagamentoManual(int agendamentoId, string formaPagamento)
        {
            // Whitelist explícita das formas aceitas no balcão
            var formasValidas = new[] { "Dinheiro", "Pix", "Debito" };
            if (string.IsNullOrEmpty(formaPagamento) || !formasValidas.Contains(formaPagamento))
            {
                TempData["Erro"] = "Forma de pagamento inválida.";
                return RedirectToAction("RegistrarPagamentoManual", new { id = agendamentoId });
            }

            try
            {
                var agendamento = db.Agendamentos.Find(agendamentoId);
                if (agendamento == null) return HttpNotFound();

                if (agendamento.Status != "PendentePagamento")
                {
                    TempData["Erro"] = $"Agendamento não está aguardando pagamento (status atual: {agendamento.Status}).";
                    return RedirectToAction("DetalhesAgendamento", new { id = agendamentoId });
                }

                // Cancela tentativas pendentes (cliente que tinha iniciado fluxo MP)
                var pendentesAntigos = db.Pagamentos
                    .Where(p => p.AgendamentoId == agendamentoId && p.StatusPagamento == "Pendente")
                    .ToList();

                foreach (var p in pendentesAntigos)
                {
                    p.StatusPagamento = "Cancelado";
                    p.DataAtualizacao = DateTime.Now;
                }

                // Valor recalculado server-side — admin NÃO informa valor
                decimal valor = agendamento.TipoAula == "Experimental" ? ValorExperimental : ValorMatricula;

                var adminNome = (Session["AdminNome"] as string) ?? "Administrador";

                var pagamento = new Pagamento
                {
                    AgendamentoId = agendamentoId,
                    Valor = valor,
                    FormaPagamento = formaPagamento,
                    StatusPagamento = "Aprovado",
                    DataPagamento = DateTime.Now,
                    DataCriacao = DateTime.Now,
                    DataAtualizacao = DateTime.Now,
                    CodigoTransacao = "MANUAL-" + Guid.NewGuid().ToString("N").Substring(0, 16).ToUpperInvariant()
                };

                db.Pagamentos.Add(pagamento);
                agendamento.Status = "Confirmado";

                db.SaveChanges();

                System.Diagnostics.Trace.TraceInformation(
                    $"Pagamento manual registrado: Agendamento={agendamentoId} Forma={formaPagamento} Admin={adminNome} Codigo={pagamento.CodigoTransacao}");

                TempData["Sucesso"] = "Pagamento manual registrado e agendamento confirmado.";
                return RedirectToAction("DetalhesAgendamento", new { id = agendamentoId });
            }
            catch (Exception ex)
            {
                System.Diagnostics.Trace.TraceError("Erro ao registrar pagamento manual: " + ex);
                TempData["Erro"] = "Erro ao registrar pagamento manual.";
                return RedirectToAction("RegistrarPagamentoManual", new { id = agendamentoId });
            }
        }

        // ─── Dispose ────────────────────────────────────────────────────

        protected override void Dispose(bool disposing)
        {
            if (disposing)
                db.Dispose();

            base.Dispose(disposing);
        }


        // ─── Utilitário de hash ─────────────────────────────────────
        private static string HashPassword(string senha)
        {
            using (var sha256 = SHA256.Create())
            {
                var bytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(senha));
                var sb = new System.Text.StringBuilder();
                foreach (var b in bytes)
                    sb.Append(b.ToString("x2"));
                return sb.ToString();
            }
        }
    }
}
