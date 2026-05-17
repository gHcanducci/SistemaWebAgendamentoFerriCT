using System;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using SistemaWebAgendamentoFerriCT.Models;

namespace SistemaWebAgendamentoFerriCT.MercadoPago
{
    public class MercadoPagoService : IMercadoPagoService
    {
        private const string BaseUrl = "https://api.mercadopago.com";

        private static readonly HttpClient Http = CriarHttpClient();

        private static HttpClient CriarHttpClient()
        {
            ServicePointManager.SecurityProtocol |= SecurityProtocolType.Tls12;

            var client = new HttpClient
            {
                BaseAddress = new Uri(BaseUrl),
                Timeout = TimeSpan.FromSeconds(30)
            };
            client.DefaultRequestHeaders.Accept.Add(
                new MediaTypeWithQualityHeaderValue("application/json"));
            return client;
        }

        public async Task<PreferenceCreatedResult> CriarPreferenceAsync(
            Agendamento agendamento, Cliente cliente, decimal valor, string idempotencyKey)
        {
            if (agendamento == null) throw new ArgumentNullException(nameof(agendamento));
            if (cliente == null) throw new ArgumentNullException(nameof(cliente));
            if (valor <= 0) throw new ArgumentOutOfRangeException(nameof(valor));
            if (string.IsNullOrWhiteSpace(idempotencyKey))
                throw new ArgumentException("Idempotency key obrigatória.", nameof(idempotencyKey));

            var backBase = MercadoPagoSettings.BackUrlBase.TrimEnd('/');
            var nomeCompleto = (cliente.Nome ?? string.Empty).Trim();
            var partes = nomeCompleto.Split(new[] { ' ' }, 2);
            var primeiroNome = partes.Length > 0 ? partes[0] : nomeCompleto;
            var sobrenome = partes.Length > 1 ? partes[1] : string.Empty;

            var body = new
            {
                items = new[]
                {
                    new
                    {
                        title = $"Aula {agendamento.TipoAula} - Ferri CT",
                        quantity = 1,
                        currency_id = "BRL",
                        unit_price = valor
                    }
                },
                payer = new
                {
                    name = primeiroNome,
                    surname = sobrenome,
                    email = cliente.Email,
                    identification = new
                    {
                        type = "CPF",
                        number = SomenteDigitos(cliente.CPF)
                    }
                },
                external_reference = agendamento.AgendamentoId.ToString(),
                notification_url = MercadoPagoSettings.NotificationUrl,
                back_urls = new
                {
                    success = $"{backBase}/Agendamento/Retorno?status=success&id={agendamento.AgendamentoId}",
                    failure = $"{backBase}/Agendamento/Retorno?status=failure&id={agendamento.AgendamentoId}",
                    pending = $"{backBase}/Agendamento/Retorno?status=pending&id={agendamento.AgendamentoId}"
                },
                auto_return = "approved",
                payment_methods = new
                {
                    excluded_payment_types = new[]
                    {
                        new { id = "credit_card" },
                        new { id = "ticket" },
                        new { id = "atm" },
                        new { id = "account_money" }
                    },
                    installments = 1
                },
                statement_descriptor = "FERRI CT",
                binary_mode = false
            };

            using (var req = new HttpRequestMessage(HttpMethod.Post, "/checkout/preferences"))
            {
                req.Headers.Authorization =
                    new AuthenticationHeaderValue("Bearer", MercadoPagoSettings.AccessToken);
                req.Headers.Add("X-Idempotency-Key", idempotencyKey);
                req.Content = new StringContent(
                    JsonConvert.SerializeObject(body), Encoding.UTF8, "application/json");

                using (var resp = await Http.SendAsync(req).ConfigureAwait(false))
                {
                    var responseBody = await resp.Content.ReadAsStringAsync().ConfigureAwait(false);
                    if (!resp.IsSuccessStatusCode)
                        throw new MercadoPagoException(
                            $"Falha ao criar preference no Mercado Pago (HTTP {(int)resp.StatusCode}).",
                            resp.StatusCode, responseBody);

                    var json = JObject.Parse(responseBody);
                    var initPoint = MercadoPagoSettings.IsSandbox
                        ? json.Value<string>("sandbox_init_point")
                        : json.Value<string>("init_point");

                    return new PreferenceCreatedResult
                    {
                        PreferenceId = json.Value<string>("id"),
                        InitPoint = initPoint
                    };
                }
            }
        }

        public async Task<PaymentInfo> ConsultarPagamentoAsync(string paymentId)
        {
            if (string.IsNullOrWhiteSpace(paymentId))
                throw new ArgumentException("PaymentId obrigatório.", nameof(paymentId));

            var path = $"/v1/payments/{Uri.EscapeDataString(paymentId)}";

            using (var req = new HttpRequestMessage(HttpMethod.Get, path))
            {
                req.Headers.Authorization =
                    new AuthenticationHeaderValue("Bearer", MercadoPagoSettings.AccessToken);

                using (var resp = await Http.SendAsync(req).ConfigureAwait(false))
                {
                    var responseBody = await resp.Content.ReadAsStringAsync().ConfigureAwait(false);
                    if (!resp.IsSuccessStatusCode)
                        throw new MercadoPagoException(
                            $"Falha ao consultar pagamento {paymentId} (HTTP {(int)resp.StatusCode}).",
                            resp.StatusCode, responseBody);

                    var json = JObject.Parse(responseBody);
                    return new PaymentInfo
                    {
                        Id = json.Value<string>("id"),
                        Status = json.Value<string>("status"),
                        StatusDetail = json.Value<string>("status_detail"),
                        TransactionAmount = json.Value<decimal?>("transaction_amount") ?? 0m,
                        ExternalReference = json.Value<string>("external_reference"),
                        PaymentMethodId = json.Value<string>("payment_method_id"),
                        PaymentTypeId = json.Value<string>("payment_type_id"),
                        DateApproved = json.Value<DateTime?>("date_approved"),
                        DateCreated = json.Value<DateTime?>("date_created")
                    };
                }
            }
        }

        private static string SomenteDigitos(string s)
        {
            if (string.IsNullOrEmpty(s)) return s;
            var sb = new StringBuilder(s.Length);
            foreach (var c in s)
                if (char.IsDigit(c)) sb.Append(c);
            return sb.ToString();
        }
    }
}
