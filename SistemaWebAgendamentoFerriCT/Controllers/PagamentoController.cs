using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using System.Web.Mvc;
using Newtonsoft.Json.Linq;
using SistemaWebAgendamentoFerriCT.MercadoPago;
using SistemaWebAgendamentoFerriCT.Models;

namespace SistemaWebAgendamentoFerriCT.Controllers
{
    // Controller dedicado para o webhook do Mercado Pago.
    // O webhook NUNCA recebe sessão de cliente — defesa é apenas via HMAC.
    public class PagamentoController : Controller
    {
        private readonly SistemaContext db = new SistemaContext();
        private readonly IMercadoPagoService mp = new MercadoPagoService();

        // Valores autoritativos por tipo de aula. Devem bater com AgendamentoController.
        private const decimal ValorExperimental = 50.00m;
        private const decimal ValorMatricula = 50.00m;

        // ─── POST: /Pagamento/Webhook ───────────────────────────────────
        // Recebe notificações do Mercado Pago. Validação:
        //   1. HMAC-SHA256 do x-signature em constant time
        //   2. Tolerância de 5 minutos no timestamp (anti-replay)
        //   3. Re-busca do pagamento na API do MP (não confia no payload)
        //   4. Comparação de transaction_amount com valor server-side (anti-tampering)
        //   5. Idempotência por WebhookEventoId UNIQUE
        [HttpPost]
        public async Task<ActionResult> Webhook()
        {
            // ── 1. Extrair dados da requisição ─────────────────────────
            var signatureHeader = Request.Headers["x-signature"];
            var requestId = Request.Headers["x-request-id"];

            // data.id vem da query string (MP usa esse formato).
            // Pode também vir no body — query é canônico para assinatura.
            var dataId = Request.QueryString["data.id"]
                         ?? Request.QueryString["id"];

            string body = null;
            try
            {
                Request.InputStream.Position = 0;
                using (var reader = new StreamReader(Request.InputStream))
                    body = await reader.ReadToEndAsync();
            }
            catch (Exception ex)
            {
                Trace.TraceError("Webhook MP: erro lendo body. " + ex.Message);
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest, "Cannot read body");
            }

            // Fallback: se data.id não veio em query, tenta extrair do body
            string eventoIdBody = null;
            string tipoEvento = null;
            if (!string.IsNullOrEmpty(body))
            {
                try
                {
                    var json = JObject.Parse(body);
                    if (string.IsNullOrEmpty(dataId))
                        dataId = json.SelectToken("data.id")?.Value<string>();
                    eventoIdBody = json.Value<string>("id");
                    tipoEvento = json.Value<string>("type");
                }
                catch
                {
                    // Body não é JSON válido — ignora, continua com o que veio da query
                }
            }

            // ── 2. Validar assinatura HMAC ─────────────────────────────
            var validation = WebhookSignatureValidator.Validate(
                signatureHeader, requestId, dataId,
                MercadoPagoSettings.WebhookSecret,
                DateTimeOffset.UtcNow);

            if (!validation.IsValid)
            {
                Trace.TraceWarning("Webhook MP: assinatura inválida. Motivo: " + validation.Reason);
                return new HttpStatusCodeResult(HttpStatusCode.Unauthorized, "Invalid signature");
            }

            // ── 3. Filtrar tipo de evento ──────────────────────────────
            if (!string.IsNullOrEmpty(tipoEvento) && tipoEvento != "payment")
            {
                // Outros tipos (merchant_order, etc.) — ack mas não processa
                return Content("Ignored: tipo " + tipoEvento);
            }

            // ── 4. Idempotência: já processamos esse evento? ───────────
            // Usa o id do delivery do webhook (body.id) se disponível,
            // senão usa "ts:dataId" como fallback.
            var eventoId = eventoIdBody ?? $"{validation.Timestamp}:{dataId}";

            var jaProcessado = db.Pagamentos.Any(p => p.WebhookEventoId == eventoId);
            if (jaProcessado)
                return Content("Already processed");

            // ── 5. Re-buscar pagamento na API do MP ────────────────────
            PaymentInfo payment;
            try
            {
                payment = await mp.ConsultarPagamentoAsync(dataId);
            }
            catch (MercadoPagoException ex)
            {
                Trace.TraceError($"Webhook MP: falha ao consultar payment {dataId}. {ex.Message}");
                // 500 → MP vai retentar
                return new HttpStatusCodeResult(HttpStatusCode.InternalServerError, "MP fetch failed");
            }

            // ── 6. Validar external_reference ──────────────────────────
            if (!int.TryParse(payment.ExternalReference, out var agendamentoId))
            {
                Trace.TraceWarning("Webhook MP: external_reference não numérico: " + payment.ExternalReference);
                return Content("Invalid external_reference"); // 200 — não retentar
            }

            var agendamento = db.Agendamentos.Find(agendamentoId);
            if (agendamento == null)
            {
                Trace.TraceWarning($"Webhook MP: Agendamento {agendamentoId} não encontrado.");
                return Content("Agendamento not found");
            }

            // ── 7. Validar valor (anti-tampering) ──────────────────────
            decimal valorEsperado = agendamento.TipoAula == "Experimental"
                ? ValorExperimental : ValorMatricula;
            if (payment.TransactionAmount != valorEsperado)
            {
                Trace.TraceError(
                    $"Webhook MP: valor divergente. Agendamento={agendamentoId} " +
                    $"esperado={valorEsperado} recebido={payment.TransactionAmount}");
                return Content("Amount mismatch");
            }

            // ── 8. Localizar Pagamento alvo ────────────────────────────
            // Estratégia: o Pagamento mais recente deste Agendamento que ainda não
            // está em estado final (Pendente ou EmAnalise). Pode também já ter sido
            // marcado com este CodigoTransacao (idempotência adicional).
            var pagamento = db.Pagamentos
                .Where(p => p.AgendamentoId == agendamentoId)
                .OrderByDescending(p => p.DataCriacao)
                .FirstOrDefault(p =>
                    p.CodigoTransacao == dataId ||
                    p.StatusPagamento == "Pendente" ||
                    p.StatusPagamento == "EmAnalise");

            if (pagamento == null)
            {
                Trace.TraceWarning($"Webhook MP: nenhum Pagamento elegível para Agendamento {agendamentoId}");
                return Content("No matching Pagamento");
            }

            // ── 9. Atualizar status conforme retorno do MP ─────────────
            pagamento.CodigoTransacao = dataId;
            pagamento.WebhookEventoId = eventoId;
            pagamento.DataAtualizacao = DateTime.Now;
            pagamento.FormaPagamento = MapearFormaPagamento(payment.PaymentTypeId, payment.PaymentMethodId);

            switch ((payment.Status ?? string.Empty).ToLowerInvariant())
            {
                case "approved":
                    pagamento.StatusPagamento = "Aprovado";
                    pagamento.DataPagamento = payment.DateApproved ?? DateTime.Now;
                    agendamento.Status = "Confirmado";
                    break;

                case "in_process":
                case "in_mediation":
                    pagamento.StatusPagamento = "EmAnalise";
                    agendamento.Status = "EmAnalise";
                    break;

                case "rejected":
                case "cancelled":
                    pagamento.StatusPagamento = "Cancelado";
                    agendamento.Status = "Cancelado";
                    break;

                case "refunded":
                case "charged_back":
                    pagamento.StatusPagamento = "Estornado";
                    agendamento.Status = "Cancelado";
                    break;

                case "pending":
                case "authorized":
                    // Mantém Pendente — webhook posterior trará atualização final
                    pagamento.StatusPagamento = "Pendente";
                    break;

                default:
                    Trace.TraceWarning("Webhook MP: status desconhecido: " + payment.Status);
                    break;
            }

            try
            {
                db.SaveChanges();
            }
            catch (System.Data.Entity.Infrastructure.DbUpdateException ex)
            {
                // Pode acontecer se duas execuções concorrentes do webhook tentarem
                // gravar o mesmo WebhookEventoId (índice UNIQUE rejeita o segundo).
                // Trata como já processado.
                Trace.TraceWarning("Webhook MP: race em SaveChanges (provável duplicata). " + ex.Message);
                return Content("Concurrent duplicate");
            }

            return Content("OK");
        }

        // Traduz payment_type_id + payment_method_id do MP para nosso enum interno.
        private static string MapearFormaPagamento(string paymentTypeId, string paymentMethodId)
        {
            if (string.Equals(paymentMethodId, "pix", StringComparison.OrdinalIgnoreCase))
                return "Pix";
            if (string.Equals(paymentTypeId, "bank_transfer", StringComparison.OrdinalIgnoreCase))
                return "Pix";
            if (string.Equals(paymentTypeId, "debit_card", StringComparison.OrdinalIgnoreCase))
                return "Debito";
            return "Aguardando";
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing) db.Dispose();
            base.Dispose(disposing);
        }
    }
}
