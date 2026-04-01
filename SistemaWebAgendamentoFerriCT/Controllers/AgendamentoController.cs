using System;
using System.Linq;
using System.Web.Mvc;
using SistemaWebAgendamentoFerriCT.Models;

namespace SistemaWebAgendamentoFerriCT.Controllers
{
    public class AgendamentoController : Controller
    {
        private readonly SistemaContext db = new SistemaContext();

        // ─── Auxiliar ───────────────────────────────────────────────────

        private bool ClienteLogado()
        {
            return Session["ClienteId"] != null;
        }

        private void CarregarHorarios()
        {
            ViewBag.Horarios = db.HorariosTurma
                .ToList()
                .Select(h => new
                {
                    h.HorarioTurmaId,
                    HorarioFormatado = h.HoraInicio.ToString(@"hh\:mm") + " - " + h.HoraFim.ToString(@"hh\:mm")
                })
                .ToList();
        }

        // ─── Create ─────────────────────────────────────────────────────

        public ActionResult Create()
        {
            if (!ClienteLogado())
                return RedirectToAction("Login", "Cliente");

            try
            {
                CarregarHorarios();
                return View();
            }
            catch (Exception)
            {
                ViewBag.Erro = "Erro ao carregar a página de agendamento.";
                CarregarHorarios();
                return View();
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Create(Agendamento agendamento)
        {
            if (!ClienteLogado())
                return RedirectToAction("Login", "Cliente");

            try
            {
                int clienteId = (int)Session["ClienteId"];
                agendamento.ClienteId = clienteId;

                if (agendamento.DataAula < DateTime.Today)
                    ModelState.AddModelError("DataAula", "Não é possível agendar para datas passadas.");

                if (string.IsNullOrEmpty(agendamento.TipoAula))
                    ModelState.AddModelError("TipoAula", "Selecione o tipo de aula.");

                if (agendamento.HorarioTurmaId == 0)
                    ModelState.AddModelError("HorarioTurmaId", "Selecione um horário.");

                bool horarioOcupado = db.Agendamentos.Any(a =>
                    a.DataAula == agendamento.DataAula &&
                    a.HorarioTurmaId == agendamento.HorarioTurmaId &&
                    a.Status != "Cancelado");

                if (horarioOcupado)
                    ModelState.AddModelError("", "Este horário já está ocupado.");

                bool clienteJaTem = db.Agendamentos.Any(a =>
                    a.ClienteId == clienteId &&
                    a.DataAula == agendamento.DataAula &&
                    a.HorarioTurmaId == agendamento.HorarioTurmaId &&
                    a.Status != "Cancelado");

                if (clienteJaTem)
                    ModelState.AddModelError("", "Você já possui um agendamento neste horário.");

                if (ModelState.IsValid)
                {
                    agendamento.Status = "PendentePagamento";
                    agendamento.DataSolicitacao = DateTime.Now;

                    db.Agendamentos.Add(agendamento);
                    db.SaveChanges();

                    TempData["Sucesso"] = "Agendamento realizado com sucesso!";
                    return RedirectToAction("Confirmacao");
                }

                CarregarHorarios();
                return View(agendamento);
            }
            catch (Exception)
            {
                ModelState.AddModelError("", "Erro interno ao realizar agendamento.");
                CarregarHorarios();
                return View(agendamento);
            }
        }

        // ─── Confirmação ────────────────────────────────────────────────

        public ActionResult Confirmacao()
        {
            if (!ClienteLogado())
                return RedirectToAction("Login", "Cliente");

            return View();
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