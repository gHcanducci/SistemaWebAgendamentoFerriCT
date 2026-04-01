using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SistemaWebAgendamentoFerriCT.Models
{
    public class Pagamento
    {
        [Key]
        public int PagamentoId { get; set; }

        [Required]
        public decimal Valor { get; set; }

        public DateTime? DataPagamento { get; set; }

        [Required]
        public string FormaPagamento { get; set; }

        [Required]
        public string StatusPagamento { get; set; }

        [ForeignKey("Agendamento")]
        public int AgendamentoId { get; set; }
        public virtual Agendamento Agendamento { get; set; }
    }
}