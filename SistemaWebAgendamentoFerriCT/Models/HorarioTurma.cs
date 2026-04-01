using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SistemaWebAgendamentoFerriCT.Models
{
    public class HorarioTurma
    {
        [Key]
        public int HorarioTurmaId { get; set; }

        [Required]
        public DayOfWeek DiaSemana { get; set; }

        [Required]
        public TimeSpan HoraInicio { get; set; }

        [Required]
        public TimeSpan HoraFim { get; set; }

        [ForeignKey("Turma")]
        public int TurmaId { get; set; }
        public virtual Turma Turma { get; set; }

        public virtual ICollection<Agendamento> Agendamentos { get; set; }
    }
}