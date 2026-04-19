using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Web;

namespace SistemaWebAgendamentoFerriCT.ViewModels
{
    // ─────────────────────────────────────────────────────────────────
    // PagamentoViewModel — usado na tela de pagamento simulado
    // ─────────────────────────────────────────────────────────────────
    public class PagamentoViewModel
    {
        public int AgendamentoId { get; set; }
        public decimal Valor { get; set; }
        public string TipoAula { get; set; }
        public DateTime DataAula { get; set; }
        public string HorarioFormatado { get; set; }

        [Required(ErrorMessage = "Selecione a forma de pagamento.")]
        [Display(Name = "Forma de Pagamento")]
        public string FormaPagamento { get; set; } // "Pix" ou "Cartao"
    }
}