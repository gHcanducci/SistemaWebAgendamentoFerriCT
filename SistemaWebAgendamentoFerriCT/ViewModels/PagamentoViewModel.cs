using System;

namespace SistemaWebAgendamentoFerriCT.ViewModels
{
    // Apresentação do resumo antes de redirecionar para o Mercado Pago.
    // O cliente escolhe a forma de pagamento (PIX ou Débito) na página hospedada do MP.
    public class PagamentoViewModel
    {
        public int AgendamentoId { get; set; }
        public decimal Valor { get; set; }
        public string TipoAula { get; set; }
        public DateTime DataAula { get; set; }
        public string HorarioFormatado { get; set; }
    }
}
