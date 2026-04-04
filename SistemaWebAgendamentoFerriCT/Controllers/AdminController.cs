using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Web.Mvc;
using SistemaWebAgendamentoFerriCT.Models;

namespace SistemaWebAgendamentoFerriCT.Controllers
{
    public class AdminController : Controller
    {
        private const string UsuarioAdmin = "admin";
        // SHA-256 de "123" — troque em produção
        private const string SenhaAdminHash = "a665a45920422f9d417e4867efdc4fb8a04a1f3fff1fa07e998e86f7f7a27ae3";

        private readonly SistemaContext db = new SistemaContext();

        // ─── Autenticação ───────────────────────────────────────────────

        private bool AdminLogado()
        {
            return Session["AdminLogado"] != null && (bool)Session["AdminLogado"];
        }

        public ActionResult Login()
        {
            if (AdminLogado())
                return RedirectToAction("Index");

            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Login(string usuario, string senha)
        {
            try
            {
                if (usuario == UsuarioAdmin && HashPassword(senha) == SenhaAdminHash)
                {
                    // Token de sessão único para segurança adicional
                    Session["AdminLogado"] = true;
                    Session["AdminToken"] = Guid.NewGuid().ToString("N");
                    return RedirectToAction("Index");
                }

                ViewBag.Erro = "Usuário ou senha incorretos.";
                return View();
            }
            catch (Exception)
            {
                ViewBag.Erro = "Erro ao realizar login. Tente novamente.";
                return View();
            }
        }

        public ActionResult Logout()
        {
            Session.Clear();
            return RedirectToAction("Login");
        }

        // ─── Dashboard ──────────────────────────────────────────────────

        public ActionResult Index()
        {
            if (!AdminLogado())
                return RedirectToAction("Login");

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
            if (!AdminLogado())
                return RedirectToAction("Login");

            try
            {
                var alunos = db.Clientes
                    .OrderByDescending(c => c.DataCadastro)
                    .ToList();

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
            if (!AdminLogado())
                return RedirectToAction("Login");

            try
            {
                var aluno = db.Clientes
                    .Include("Agendamentos")
                    .Include("Agendamentos.HorarioTurma")
                    .FirstOrDefault(c => c.ClienteId == id);

                if (aluno == null)
                    return HttpNotFound();

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
            if (!AdminLogado())
                return RedirectToAction("Login");

            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult CriarAluno(Cliente cliente)
        {
            if (!AdminLogado())
                return RedirectToAction("Login");

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
            if (!AdminLogado())
                return RedirectToAction("Login");

            try
            {
                var aluno = db.Clientes.Find(id);

                if (aluno == null)
                    return HttpNotFound();

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
            if (!AdminLogado())
                return RedirectToAction("Login");

            try
            {
                if (ModelState.IsValid)
                {
                    var aluno = db.Clientes.Find(cliente.ClienteId);

                    if (aluno == null)
                        return HttpNotFound();

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
            if (!AdminLogado())
                return RedirectToAction("Login");

            try
            {
                var aluno = db.Clientes.Find(id);

                if (aluno == null)
                    return HttpNotFound();

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
            if (!AdminLogado())
                return RedirectToAction("Login");

            try
            {
                var aluno = db.Clientes.Find(id);

                if (aluno == null)
                    return HttpNotFound();

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
            if (!AdminLogado())
                return RedirectToAction("Login");

            try
            {
                var agendamentos = db.Agendamentos
                    .Include("Cliente")
                    .Include("HorarioTurma")
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
            if (!AdminLogado())
                return RedirectToAction("Login");

            try
            {
                var agendamento = db.Agendamentos
                    .Include("Cliente")
                    .Include("HorarioTurma")
                    .FirstOrDefault(a => a.AgendamentoId == id);

                if (agendamento == null)
                    return HttpNotFound();

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
            if (!AdminLogado())
                return RedirectToAction("Login");

            try
            {
                var agendamento = db.Agendamentos
                    .Include("Cliente")
                    .Include("HorarioTurma")
                    .FirstOrDefault(a => a.AgendamentoId == id);

                if (agendamento == null)
                    return HttpNotFound();

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

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult EditarAgendamento(int agendamentoId, string status)
        {
            if (!AdminLogado())
                return RedirectToAction("Login");

            try
            {
                var agendamento = db.Agendamentos.Find(agendamentoId);

                if (agendamento == null)
                    return HttpNotFound();

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
            if (!AdminLogado())
                return RedirectToAction("Login");

            try
            {
                var agendamento = db.Agendamentos
                    .Include("Cliente")
                    .Include("HorarioTurma")
                    .FirstOrDefault(a => a.AgendamentoId == id);

                if (agendamento == null)
                    return HttpNotFound();

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
            if (!AdminLogado())
                return RedirectToAction("Login");

            try
            {
                var agendamento = db.Agendamentos.Find(id);

                if (agendamento == null)
                    return HttpNotFound();

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
