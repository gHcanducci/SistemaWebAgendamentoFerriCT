using System;
using System.Linq;
using System.Web.Mvc;
using SistemaWebAgendamentoFerriCT.Models;

namespace SistemaWebAgendamentoFerriCT.Controllers
{
    public class ClienteController : Controller
    {
        private readonly SistemaContext db = new SistemaContext();

        // ─── Autenticação ───────────────────────────────────────────────

        private bool ClienteLogado()
        {
            return Session["ClienteId"] != null;
        }

        public ActionResult Login()
        {
            if (ClienteLogado())
                return RedirectToAction("Index", "Home");

            return View();
        }

        [HttpPost]
        public ActionResult Login(string email, string cpf)
        {
            try
            {
                var cliente = db.Clientes
                    .FirstOrDefault(c => c.Email == email && c.CPF == cpf);

                if (cliente != null)
                {
                    Session["ClienteId"] = cliente.ClienteId;
                    Session["ClienteNome"] = cliente.Nome;
                    return RedirectToAction("Index", "Home");
                }

                ViewBag.Erro = "E-mail ou CPF inválidos. Verifique e tente novamente.";
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
            try
            {
                Session.Clear();
                return RedirectToAction("Login");
            }
            catch (Exception)
            {
                return RedirectToAction("Login");
            }
        }

        // ─── Cadastro ───────────────────────────────────────────────────

        public ActionResult Create()
        {
            if (ClienteLogado())
                return RedirectToAction("Index", "Home");

            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Create(Cliente cliente)
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

                    if (db.Clientes.Any(c => c.Email == cliente.Email))
                    {
                        ModelState.AddModelError("Email", "E-mail já cadastrado.");
                        return View(cliente);
                    }

                    cliente.DataCadastro = DateTime.Now;
                    db.Clientes.Add(cliente);
                    db.SaveChanges();

                    Session["ClienteId"] = cliente.ClienteId;
                    Session["ClienteNome"] = cliente.Nome;
                    return RedirectToAction("Index", "Home");
                }
            }
            catch (Exception)
            {
                ModelState.AddModelError("", "Erro ao cadastrar. Tente novamente.");
            }

            return View(cliente);
        }

        // ─── Perfil ─────────────────────────────────────────────────────

        public ActionResult Perfil()
        {
            if (!ClienteLogado())
                return RedirectToAction("Login");

            try
            {
                int id = (int)Session["ClienteId"];

                var cliente = db.Clientes
                    .Include("Agendamentos")
                    .Include("Agendamentos.HorarioTurma")
                    .FirstOrDefault(c => c.ClienteId == id);

                if (cliente == null)
                    return RedirectToAction("Login");

                return View(cliente);
            }
            catch (Exception)
            {
                ViewBag.Erro = "Erro ao carregar perfil.";
                return RedirectToAction("Login");
            }
        }

        // ─── Editar ─────────────────────────────────────────────────────

        public ActionResult Editar()
        {
            if (!ClienteLogado())
                return RedirectToAction("Login");

            try
            {
                int id = (int)Session["ClienteId"];
                var cliente = db.Clientes.Find(id);

                if (cliente == null)
                    return RedirectToAction("Login");

                return View(cliente);
            }
            catch (Exception)
            {
                ViewBag.Erro = "Erro ao carregar dados para edição.";
                return RedirectToAction("Login");
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Editar(Cliente clienteAtualizado)
        {
            if (!ClienteLogado())
                return RedirectToAction("Login");

            try
            {
                if (ModelState.IsValid)
                {
                    int id = (int)Session["ClienteId"];
                    var cliente = db.Clientes.Find(id);

                    if (cliente == null)
                        return RedirectToAction("Login");

                    if (db.Clientes.Any(c => c.Email == clienteAtualizado.Email && c.ClienteId != id))
                    {
                        ModelState.AddModelError("Email", "Este e-mail já está em uso.");
                        return View(clienteAtualizado);
                    }

                    cliente.Nome = clienteAtualizado.Nome;
                    cliente.Email = clienteAtualizado.Email;
                    cliente.Telefone = clienteAtualizado.Telefone;

                    db.SaveChanges();

                    Session["ClienteNome"] = cliente.Nome;

                    TempData["Sucesso"] = "Dados atualizados com sucesso!";
                    return RedirectToAction("Perfil");
                }
            }
            catch (Exception)
            {
                ModelState.AddModelError("", "Erro ao atualizar dados. Tente novamente.");
            }

            return View(clienteAtualizado);
        }

        // ─── Excluir ─────────────────────────────────────────────────────

        public ActionResult Excluir()
        {
            if (!ClienteLogado())
                return RedirectToAction("Login");

            try
            {
                int id = (int)Session["ClienteId"];
                var cliente = db.Clientes.Find(id);

                if (cliente == null)
                    return RedirectToAction("Login");

                return View(cliente);
            }
            catch (Exception)
            {
                ViewBag.Erro = "Erro ao carregar dados.";
                return RedirectToAction("Login");
            }
        }

        [HttpPost, ActionName("Excluir")]
        [ValidateAntiForgeryToken]
        public ActionResult ExcluirConfirmado()
        {
            if (!ClienteLogado())
                return RedirectToAction("Login");

            try
            {
                int id = (int)Session["ClienteId"];
                var cliente = db.Clientes.Find(id);

                if (cliente == null)
                    return RedirectToAction("Login");

                db.Clientes.Remove(cliente);
                db.SaveChanges();

                Session.Clear();
                TempData["Mensagem"] = "Sua conta foi excluída com sucesso.";
                return RedirectToAction("Login");
            }
            catch (Exception)
            {
                TempData["Erro"] = "Erro ao excluir conta. Tente novamente.";
                return RedirectToAction("Perfil");
            }
        }

        // ─── Dispose ────────────────────────────────────────────────────

        protected override void Dispose(bool disposing)
        {
            if (disposing)
                db.Dispose();

            base.Dispose(disposing);
        }
    }
}