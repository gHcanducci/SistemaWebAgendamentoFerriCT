using System;

namespace SistemaWebAgendamentoFerriCT.MercadoPago
{
    public class PaymentInfo
    {
        public string Id { get; set; }
        public string Status { get; set; }
        public string StatusDetail { get; set; }
        public decimal TransactionAmount { get; set; }
        public string ExternalReference { get; set; }
        public string PaymentMethodId { get; set; }
        public string PaymentTypeId { get; set; }
        public DateTime? DateApproved { get; set; }
        public DateTime? DateCreated { get; set; }
    }
}
