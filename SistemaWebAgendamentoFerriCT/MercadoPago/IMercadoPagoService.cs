using System.Threading.Tasks;
using SistemaWebAgendamentoFerriCT.Models;

namespace SistemaWebAgendamentoFerriCT.MercadoPago
{
    public interface IMercadoPagoService
    {
        Task<PreferenceCreatedResult> CriarPreferenceAsync(
            Agendamento agendamento,
            Cliente cliente,
            decimal valor,
            string idempotencyKey);

        Task<PaymentInfo> ConsultarPagamentoAsync(string paymentId);
    }
}
