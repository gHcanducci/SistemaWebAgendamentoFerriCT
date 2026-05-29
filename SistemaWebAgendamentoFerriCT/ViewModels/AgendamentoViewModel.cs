using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Web;

namespace SistemaWebAgendamentoFerriCT.ViewModels
{
    // ─────────────────────────────────────────────────────────────────
    // AgendamentoViewModel — usado no formulário de agendamento
    // ─────────────────────────────────────────────────────────────────
    public class AgendamentoViewModel
    {
        [Required(ErrorMessage = "Selecione a data da aula.")]
        [DataType(DataType.Date)]
        [Display(Name = "Data da Aula")]
        public DateTime DataAula { get; set; }

        [Required(ErrorMessage = "Selecione o tipo de aula.")]
        [Display(Name = "Tipo de Aula")]
        public string TipoAula { get; set; }

        [Required(ErrorMessage = "Selecione um horário disponível.")]
        [Display(Name = "Horário")]
        public int HorarioTurmaId { get; set; }

        // Informações exibidas na view (não enviadas pelo form)
        public string HorarioFormatado { get; set; }
        public decimal ValorAula { get; set; }
    }
}