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

        [Required]
        public DateTime DataAula { get; set; }

        [Required]
        public string TipoAula { get; set; }

        [Required]
        public string Status { get; set; }

        public DateTime DataSolicitacao { get; set; } = DateTime.Now;

        [ForeignKey("Cliente")]
        public int ClienteId { get; set; }
        public virtual Cliente Cliente { get; set; }

        [ForeignKey("HorarioTurma")]
        public int HorarioTurmaId { get; set; }
        public virtual HorarioTurma HorarioTurma { get; set; }

        public virtual ICollection<Pagamento> Pagamentos { get; set; }
    }
}