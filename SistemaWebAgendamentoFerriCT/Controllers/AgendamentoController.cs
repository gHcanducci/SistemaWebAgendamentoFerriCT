using System;
using System.Collections.Generic;
using System.Linq;
using System.Web.Mvc;
using SistemaWebAgendamentoFerriCT.Models;
using SistemaWebAgendamentoFerriCT.ViewModels;

namespace SistemaWebAgendamentoFerriCT.Controllers
{
    public class AgendamentoController : Controller
    {
        private readonly SistemaContext db = new SistemaContext();

        // Valores das aulas (futuramente podem vir do banco)
        private const decimal ValorExperimental = 50.00m;
        private const decimal ValorMatricula = 120.00m;

        // Feriados nacionais fixos (formato MM-dd)
        // FUTURO: mover para tabela no banco para facilitar manutenção
        private readonly HashSet<string> FeriadosNacionais = new HashSet<string>
        {
            "01-01", // Ano Novo
            "04-21", // Tiradentes
            "05-01", // Dia do Trabalho
            "09-07", // Independência
            "10-12", // Nossa Senhora
            "11-02", // Finados
            "11-15", // Proclamação da República
            "12-25"  // Natal
        };

        // ─── Auxiliares ─────────────────────────────────────────────────

        private bool ClienteLogado()
        {
            return Session["ClienteId"] != null;
        }

        private bool EFeriado(DateTime data)
        {
            return FeriadosNacionais.Contains(data.ToString("MM-dd"));
        }

        private bool EDomingo(DateTime data)
        {
            return data.DayOfWeek == DayOfWeek.Sunday;
        }

        // Carrega horários filtrados pelo dia da semana da data escolhida
        private List<object> CarregarHorariosPorDia(DateTime data)
        {
            return db.HorariosTurma
                .Where(h => h.DiaSemana == data.DayOfWeek)
                .ToList()
                .Select(h => (object)new
                {
                    h.HorarioTurmaId,
                    HorarioFormatado = $"{h.HoraInicio:hh\\:mm} - {h.HoraFim:hh\\:mm} ({h.Turma.NomeTurma})"
                })
                .ToList();
        }

        // Verifica se a turma tem vagas disponíveis na data
        private bool TurmaTemVaga(int horarioTurmaId, DateTime data)
        {
            var horario = db.HorariosTurma
                .Include("Turma")
                .FirstOrDefault(h => h.HorarioTurmaId == horarioTurmaId);

            if (horario == null) return false;

            int agendadosConfirmados = db.Agendamentos.Count(a =>
                a.HorarioTurmaId == horarioTurmaId &&
                a.DataAula == data &&
                a.Status != "Cancelado" &&
                !a.ListaEspera);

            return agendadosConfirmados < horario.Turma.CapacidadeMaxima;
        }

        // Verifica se o cliente já fez algum agendamento (para controle de aula experimental)
        private bool ClienteJaAgendou(int clienteId)
        {
            return db.Agendamentos.Any(a =>
                a.ClienteId == clienteId &&
                a.Status != "Cancelado");
        }

        // ─── GET: Horários por data (chamado via AJAX na view) ───────────

        public JsonResult HorariosPorData(string data)
        {
            if (!ClienteLogado())
                return Json(new { erro = "Não autorizado." }, JsonRequestBehavior.AllowGet);

            try
            {
                if (!DateTime.TryParse(data, out DateTime dataSelecionada))
                    return Json(new { erro = "Data inválida." }, JsonRequestBehavior.AllowGet);

                // Domingo — academia fechada
                if (EDomingo(dataSelecionada))
                    return Json(new { fechado = true, motivo = "A academia não tem turmas aos domingos." }, JsonRequestBehavior.AllowGet);

                // Feriado — academia fechada
                if (EFeriado(dataSelecionada))
                    return Json(new { fechado = true, motivo = "Não há turmas em feriados nacionais." }, JsonRequestBehavior.AllowGet);

                var horarios = CarregarHorariosPorDia(dataSelecionada);

                if (!horarios.Any())
                    return Json(new { fechado = true, motivo = "Não há turmas disponíveis neste dia." }, JsonRequestBehavior.AllowGet);

                return Json(new { horarios }, JsonRequestBehavior.AllowGet);
            }
            catch (Exception)
            {
                return Json(new { erro = "Erro ao carregar horários." }, JsonRequestBehavior.AllowGet);
            }
        }

        // ─── GET: Agendamento/Create ─────────────────────────────────────

        public ActionResult Create()
        {
            if (!ClienteLogado())
                return RedirectToAction("Login", "Cliente");

            try
            {
                int clienteId = (int)Session["ClienteId"];

                // Verifica se cliente já agendou — controle de aula experimental
                bool jaAgendou = ClienteJaAgendou(clienteId);
                ViewBag.JaAgendou = jaAgendou;

                return View(new AgendamentoViewModel
                {
                    DataAula = DateTime.Today.AddDays(1)
                });
            }
            catch (Exception)
            {
                ViewBag.Erro = "Erro ao carregar a página de agendamento.";
                return View(new AgendamentoViewModel());
            }
        }

        // ─── POST: Agendamento/Create ────────────────────────────────────

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Create(AgendamentoViewModel vm)
        {
            if (!ClienteLogado())
                return RedirectToAction("Login", "Cliente");

            try
            {
                int clienteId = (int)Session["ClienteId"];
                bool jaAgendou = ClienteJaAgendou(clienteId);
                ViewBag.JaAgendou = jaAgendou;

                // ── Validação: data não pode ser passada
                if (vm.DataAula < DateTime.Today)
                    ModelState.AddModelError("DataAula", "Não é possível agendar para datas passadas.");

                // ── Validação: domingo sem turmas
                if (EDomingo(vm.DataAula))
                    ModelState.AddModelError("DataAula", "A academia não possui turmas aos domingos.");

                // ── Validação: feriado sem turmas
                if (EFeriado(vm.DataAula))
                    ModelState.AddModelError("DataAula", "Não há turmas em feriados nacionais.");

                // ── Validação: aula experimental só para quem nunca agendou
                if (vm.TipoAula == "Experimental" && jaAgendou)
                    ModelState.AddModelError("TipoAula", "A aula experimental é exclusiva para novos alunos que ainda não realizaram nenhum agendamento.");

                // ── Validação: cliente já tem agendamento neste horário e data
                bool clienteJaTem = db.Agendamentos.Any(a =>
                    a.ClienteId == clienteId &&
                    a.DataAula == vm.DataAula &&
                    a.HorarioTurmaId == vm.HorarioTurmaId &&
                    a.Status != "Cancelado");

                if (clienteJaTem)
                    ModelState.AddModelError("", "Você já possui um agendamento neste horário.");

                if (!ModelState.IsValid)
                    return View(vm);

                // ── Verificar capacidade da turma
                bool temVaga = TurmaTemVaga(vm.HorarioTurmaId, vm.DataAula);

                // ── Definir valor conforme tipo de aula
                decimal valor = vm.TipoAula == "Experimental" ? ValorExperimental : ValorMatricula;

                // ── Criar o agendamento
                var agendamento = new Agendamento
                {
                    ClienteId = clienteId,
                    DataAula = vm.DataAula,
                    TipoAula = vm.TipoAula,
                    HorarioTurmaId = vm.HorarioTurmaId,
                    DataSolicitacao = DateTime.Now,
                    ListaEspera = !temVaga,
                    Status = "PendentePagamento"
                };

                db.Agendamentos.Add(agendamento);
                db.SaveChanges();

                // ── Se turma lotada: avisa cliente e notifica admin
                if (!temVaga)
                {
                    // Registra notificação para o admin ver no painel
                    TempData["NotificacaoAdmin"] = $"Lista de espera: cliente {Session["ClienteNome"]} tentou agendar para {vm.DataAula:dd/MM/yyyy} — turma lotada.";
                    TempData["ListaEspera"] = true;

                    // Redireciona para confirmação informando lista de espera
                    return RedirectToAction("ListaEspera", new { id = agendamento.AgendamentoId });
                }

                // ── Redireciona para pagamento
                TempData["ValorAula"] = valor;
                return RedirectToAction("Pagamento", new { id = agendamento.AgendamentoId, valor });
            }
            catch (Exception)
            {
                ModelState.AddModelError("", "Erro interno ao realizar agendamento. Tente novamente.");
                return View(vm);
            }
        }

        // ─── GET: Agendamento/Pagamento ──────────────────────────────────

        public ActionResult Pagamento(int id, decimal valor)
        {
            if (!ClienteLogado())
                return RedirectToAction("Login", "Cliente");

            try
            {
                var agendamento = db.Agendamentos
                    .Include("HorarioTurma")
                    .Include("HorarioTurma.Turma")
                    .FirstOrDefault(a => a.AgendamentoId == id);

                if (agendamento == null)
                    return HttpNotFound();

                var vm = new PagamentoViewModel
                {
                    AgendamentoId = agendamento.AgendamentoId,
                    Valor = valor,
                    TipoAula = agendamento.TipoAula,
                    DataAula = agendamento.DataAula,
                    HorarioFormatado = $"{agendamento.HorarioTurma.HoraInicio:hh\\:mm} - {agendamento.HorarioTurma.HoraFim:hh\\:mm}"
                };

                return View(vm);
            }
            catch (Exception)
            {
                TempData["Erro"] = "Erro ao carregar a página de pagamento.";
                return RedirectToAction("Create");
            }
        }

        // ─── POST: Agendamento/ProcessarPagamento ────────────────────────
        
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult ProcessarPagamento(PagamentoViewModel vm)
        {
            if (!ClienteLogado())
                return RedirectToAction("Login", "Cliente");

            try
            {
                // Remove validação do Valor — será buscado do banco
                ModelState.Remove("Valor");

                if (!ModelState.IsValid)
                    return View("Pagamento", vm);

                var agendamento = db.Agendamentos.Find(vm.AgendamentoId);
                if (agendamento == null)
                    return HttpNotFound();

                // Busca o valor correto pelo tipo de aula — evita problema de conversão decimal
                decimal valor = agendamento.TipoAula == "Experimental" ? 50.00m : 120.00m;

                // ── SIMULAÇÃO DE PAGAMENTO ───────────────────────────────
                // TODO: Substituir por integração real com Mercado Pago
                // Endpoint: POST https://api.mercadopago.com/v1/payments
                // Documentação: https://www.mercadopago.com.br/developers
                // Requer: Access Token (produção) no Web.config como AppSetting
                // Formas suportadas: "Pix" (payment_method_id: "pix")
                //                    "Cartao" (payment_method_id: cartão de crédito)


                bool pagamentoAprovado = SimularPagamento(vm.FormaPagamento, valor);

                var pagamento = new Pagamento
                {
                    AgendamentoId = vm.AgendamentoId,
                    Valor = valor,
                    FormaPagamento = vm.FormaPagamento,
                    DataPagamento = DateTime.Now,
                    StatusPagamento = pagamentoAprovado ? "Aprovado" : "Recusado",
                    CodigoTransacao = Guid.NewGuid().ToString("N").Substring(0, 12).ToUpper()
                };

                db.Pagamentos.Add(pagamento);

                if (pagamentoAprovado)
                {
                    agendamento.Status = "Confirmado";
                    db.SaveChanges();

                    TempData["Sucesso"] = "Pagamento aprovado!";
                    return RedirectToAction("Confirmacao", new { id = vm.AgendamentoId });
                }
                else
                {
                    db.SaveChanges();
                    TempData["Erro"] = "Pagamento recusado. Tente novamente.";
                    return RedirectToAction("Pagamento", new { id = vm.AgendamentoId, valor });
                }
            }
            catch (Exception ex)
            {
                TempData["Erro"] = "Erro: " + ex.Message;
                return RedirectToAction("Pagamento", new { id = vm.AgendamentoId, valor = vm.Valor });
            }
        }

        // ─── Simulação de pagamento (remover quando integrar Mercado Pago) ─

        private bool SimularPagamento(string formaPagamento, decimal valor)
        {
            // Simulação: Pix sempre aprovado, Cartão aprovado 90% das vezes
            if (formaPagamento == "Pix") return true;
            return new Random().Next(1, 11) > 1; // 90% aprovação
        }

        // ─── GET: Agendamento/Confirmacao ────────────────────────────────

        public ActionResult Confirmacao(int id)
        {
            if (!ClienteLogado())
                return RedirectToAction("Login", "Cliente");

            try
            {
                var agendamento = db.Agendamentos
                    .Include("HorarioTurma")
                    .Include("HorarioTurma.Turma")
                    .Include("Pagamentos")
                    .FirstOrDefault(a => a.AgendamentoId == id);

                if (agendamento == null)
                    return HttpNotFound();

                return View(agendamento);
            }
            catch (Exception)
            {
                return RedirectToAction("Create");
            }
        }

        // ─── GET: Agendamento/ListaEspera ────────────────────────────────

        public ActionResult ListaEspera(int id)
        {
            if (!ClienteLogado())
                return RedirectToAction("Login", "Cliente");

            try
            {
                var agendamento = db.Agendamentos
                    .Include("HorarioTurma")
                    .Include("HorarioTurma.Turma")
                    .FirstOrDefault(a => a.AgendamentoId == id);

                if (agendamento == null)
                    return HttpNotFound();

                return View(agendamento);
            }
            catch (Exception)
            {
                return RedirectToAction("Create");
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
