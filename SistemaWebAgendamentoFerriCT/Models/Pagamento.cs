// ─────────────────────────────────────────────────────────────────
// Pagamento.cs — com DataAnnotations e FormaPagamento enum-like
// ─────────────────────────────────────────────────────────────────
using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SistemaWebAgendamentoFerriCT.Models
{
    public class Pagamento
    {
        [Key]
        public int PagamentoId { get; set; }

        [Required(ErrorMessage = "O valor é obrigatório.")]
        [Range(0.01, 99999.99, ErrorMessage = "O valor deve ser maior que zero.")]
        [Display(Name = "Valor")]
        [DataType(DataType.Currency)]
        public decimal Valor { get; set; }

        [Display(Name = "Data do Pagamento")]
        [DataType(DataType.DateTime)]
        public DateTime? DataPagamento { get; set; }

        [Required(ErrorMessage = "Selecione a forma de pagamento.")]
        [Display(Name = "Forma de Pagamento")]
        public string FormaPagamento { get; set; } // "Pix" ou "Cartao"

        [Required]
        [Display(Name = "Status")]
        public string StatusPagamento { get; set; } // Aprovado, Pendente, Recusado

        // Código de transação retornado pelo gateway (futuro Mercado Pago)
        public string CodigoTransacao { get; set; }

        [ForeignKey("Agendamento")]
        public int AgendamentoId { get; set; }
        public virtual Agendamento Agendamento { get; set; }
    }
}
