using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Web.Mvc;
using SistemaWebAgendamentoFerriCT.MercadoPago;
using SistemaWebAgendamentoFerriCT.Models;
using SistemaWebAgendamentoFerriCT.ViewModels;

namespace SistemaWebAgendamentoFerriCT.Controllers
{
    public class AgendamentoController : Controller
    {
        private readonly SistemaContext db = new SistemaContext();
        private readonly IMercadoPagoService mp = new MercadoPagoService();

        // Valores das aulas (futuramente podem vir do banco)
        private const decimal ValorExperimental = 50.00m;
        private const decimal ValorMatricula    = 50.00m;

        // Feriados fixos: nacionais + municipais de Presidente Prudente (formato MM-dd)
        private readonly HashSet<string> FeriadosFixos = new HashSet<string>
        {
            "01-01", // Ano Novo
            "01-28", // Aniversário de Presidente Prudente
            "04-21", // Tiradentes
            "05-01", // Dia do Trabalho
            "06-13", // Santo Antônio (padroeiro municipal)
            "09-07", // Independência
            "10-12", // Nossa Senhora Aparecida
            "11-02", // Finados
            "11-15", // Proclamação da República
            "12-25"  // Natal
        };

        // Calcula a data da Páscoa (algoritmo Meeus/Jones/Butcher)
        private static DateTime CalcularPascoa(int ano)
        {
            int a = ano % 19, b = ano / 100, c = ano % 100;
            int d = b / 4,    e = b % 4,    f = (b + 8) / 25;
            int g = (b - f + 1) / 3;
            int h = (19 * a + b - d - g + 15) % 30;
            int i = c / 4,   k = c % 4;
            int l = (32 + 2 * e + 2 * i - h - k) % 7;
            int m = (a + 11 * h + 22 * l) / 451;
            int mes = (h + l - 7 * m + 114) / 31;
            int dia = ((h + l - 7 * m + 114) % 31) + 1;
            return new DateTime(ano, mes, dia);
        }

        // Feriados móveis calculados dinamicamente por ano
        private static HashSet<DateTime> FeriadosMoveis(int ano)
        {
            var pascoa = CalcularPascoa(ano);
            return new HashSet<DateTime>
            {
                pascoa.AddDays(-48), // Carnaval — Segunda-feira
                pascoa.AddDays(-47), // Carnaval — Terça-feira
                pascoa.AddDays(-2),  // Sexta-feira Santa
                pascoa.AddDays(60),  // Corpus Christi
            };
        }

        // ─── Auxiliares ─────────────────────────────────────────────────

        private bool ClienteLogado()
        {
            return Session["ClienteId"] != null;
        }

        private bool EFeriado(DateTime data)
        {
            if (FeriadosFixos.Contains(data.ToString("MM-dd"))) return true;
            return FeriadosMoveis(data.Year).Contains(data.Date);
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
                    return Json(new { fechado = true, motivo = "Não há turmas em feriados." }, JsonRequestBehavior.AllowGet);

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
                    ModelState.AddModelError("DataAula", "Não há turmas em feriados.");

                // ── Validação: aula experimental só para quem nunca agendou
                if (vm.TipoAula == "Experimental" && jaAgendou)
                    ModelState.AddModelError("TipoAula", "A aula experimental é exclusiva para novos alunos que ainda não realizaram nenhum agendamento.");

                // ── Validação: cliente só pode ter 1 agendamento aguardando pagamento ao mesmo tempo
                bool temPendente = db.Agendamentos.Any(a =>
                    a.ClienteId == clienteId &&
                    (a.Status == "PendentePagamento" || a.Status == "EmAnalise"));

                if (temPendente)
                    ModelState.AddModelError("", "Você possui um agendamento aguardando pagamento. Finalize-o ou aguarde o vencimento (1h) antes de criar outro.");

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

                    // Lista de espera não gera pagamento (admin promove quando vaga abre)
                    agendamento.Status = "AguardandoVaga";
                    db.SaveChanges();

                    return RedirectToAction("ListaEspera", new { id = agendamento.AgendamentoId });
                }

                // ── Redireciona para tela de pagamento (sem valor na URL — server recalcula)
                return RedirectToAction("Pagamento", new { id = agendamento.AgendamentoId });
            }
            catch (Exception)
            {
                ModelState.AddModelError("", "Erro interno ao realizar agendamento. Tente novamente.");
                return View(vm);
            }
        }

        // ─── GET: Agendamento/Pagamento ──────────────────────────────────
        // Tela de resumo + botão "Pagar com Mercado Pago".
        // Valor não vem da URL: é recalculado server-side a partir do TipoAula.

        public ActionResult Pagamento(int id)
        {
            if (!ClienteLogado())
                return RedirectToAction("Login", "Cliente");

            try
            {
                int clienteId = (int)Session["ClienteId"];

                var agendamento = db.Agendamentos
                    .Include("HorarioTurma")
                    .Include("HorarioTurma.Turma")
                    .FirstOrDefault(a => a.AgendamentoId == id);

                if (agendamento == null)
                    return HttpNotFound();

                // Não permite cliente acessar pagamento de outro cliente
                if (agendamento.ClienteId != clienteId)
                    return new HttpStatusCodeResult(System.Net.HttpStatusCode.Forbidden);

                decimal valor = agendamento.TipoAula == "Experimental" ? ValorExperimental : ValorMatricula;

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

        // ─── POST: Agendamento/IniciarPagamento ──────────────────────────
        // Cria Preference no Mercado Pago e redireciona para a página hospedada do MP.

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> IniciarPagamento(int agendamentoId)
        {
            if (!ClienteLogado())
                return RedirectToAction("Login", "Cliente");

            int clienteId = (int)Session["ClienteId"];

            var agendamento = db.Agendamentos
                .Include("Cliente")
                .FirstOrDefault(a => a.AgendamentoId == agendamentoId);

            if (agendamento == null)
                return HttpNotFound();

            if (agendamento.ClienteId != clienteId)
                return new HttpStatusCodeResult(System.Net.HttpStatusCode.Forbidden);

            // Só permite iniciar pagamento se agendamento estiver pendente
            if (agendamento.Status != "PendentePagamento")
            {
                TempData["Erro"] = "Este agendamento não está aguardando pagamento.";
                return RedirectToAction("Pagamento", new { id = agendamentoId });
            }

            // Valor sempre recalculado server-side
            decimal valor = agendamento.TipoAula == "Experimental" ? ValorExperimental : ValorMatricula;

            try
            {
                // Cancela tentativas anteriores deste agendamento (cliente recriando preference)
                var pendentesAntigos = db.Pagamentos
                    .Where(p => p.AgendamentoId == agendamentoId && p.StatusPagamento == "Pendente")
                    .ToList();

                foreach (var p in pendentesAntigos)
                {
                    p.StatusPagamento = "Cancelado";
                    p.DataAtualizacao = DateTime.Now;
                }

                // Idempotency key novo a cada tentativa (MP cria preference distinta)
                var idempotencyKey = Guid.NewGuid().ToString();

                var preference = await mp.CriarPreferenceAsync(
                    agendamento, agendamento.Cliente, valor, idempotencyKey);

                // Cria registro de Pagamento em estado Pendente.
                // FormaPagamento fica "Aguardando" até o webhook confirmar o método real.
                var pagamento = new Pagamento
                {
                    AgendamentoId = agendamento.AgendamentoId,
                    Valor = valor,
                    FormaPagamento = "Aguardando",
                    StatusPagamento = "Pendente",
                    PreferenceId = preference.PreferenceId,
                    DataCriacao = DateTime.Now
                };

                db.Pagamentos.Add(pagamento);
                db.SaveChanges();

                // Redireciona para a URL hospedada do Mercado Pago (init_point ou sandbox_init_point)
                return Redirect(preference.InitPoint);
            }
            catch (MercadoPagoException)
            {
                TempData["Erro"] = "Não foi possível iniciar o pagamento agora. Tente novamente em instantes.";
                return RedirectToAction("Pagamento", new { id = agendamentoId });
            }
            catch (Exception)
            {
                TempData["Erro"] = "Erro inesperado ao iniciar o pagamento.";
                return RedirectToAction("Pagamento", new { id = agendamentoId });
            }
        }

        // ─── GET: Agendamento/Retorno ────────────────────────────────────
        // Página visual após o cliente sair do Mercado Pago. A verdade do
        // pagamento vem do webhook; aqui só mostramos o estado atual do agendamento.
        // Não confiamos no parâmetro 'status' da URL para confirmar pagamento.

        public ActionResult Retorno(int id, string status)
        {
            if (!ClienteLogado())
                return RedirectToAction("Login", "Cliente");

            int clienteId = (int)Session["ClienteId"];

            var agendamento = db.Agendamentos
                .Include("HorarioTurma")
                .Include("HorarioTurma.Turma")
                .FirstOrDefault(a => a.AgendamentoId == id);

            if (agendamento == null)
                return HttpNotFound();

            if (agendamento.ClienteId != clienteId)
                return new HttpStatusCodeResult(System.Net.HttpStatusCode.Forbidden);

            // Se já confirmado, manda direto pra tela final
            if (agendamento.Status == "Confirmado")
                return RedirectToAction("Confirmacao", new { id });

            ViewBag.StatusMP = status;
            return View(agendamento);
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
