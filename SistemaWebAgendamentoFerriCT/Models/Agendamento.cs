// ─────────────────────────────────────────────────────────────────
// Agendamento.cs — com DataAnnotations e campo ListaEspera
// ─────────────────────────────────────────────────────────────────
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SistemaWebAgendamentoFerriCT.Models
{
    public class Agendamento
    {
        [Key]
        public int AgendamentoId { get; set; }

        [Required(ErrorMessage = "Selecione a data da aula.")]
        [Display(Name = "Data da Aula")]
        [DataType(DataType.Date)]
        public DateTime DataAula { get; set; }

        [Required(ErrorMessage = "Selecione o tipo de aula.")]
        [Display(Name = "Tipo de Aula")]
        public string TipoAula { get; set; }

        [Required]
        public string Status { get; set; }

        // Indica se o cliente entrou em lista de espera por turma lotada
        public bool ListaEspera { get; set; } = false;

        public DateTime DataSolicitacao { get; set; } = DateTime.Now;

        [ForeignKey("Cliente")]
        public int ClienteId { get; set; }
        public virtual Cliente Cliente { get; set; }

        [Required(ErrorMessage = "Selecione um horário disponível.")]
        [Display(Name = "Horário")]
        [ForeignKey("HorarioTurma")]
        public int HorarioTurmaId { get; set; }
        public virtual HorarioTurma HorarioTurma { get; set; }

        public virtual ICollection<Pagamento> Pagamentos { get; set; }
    }
}