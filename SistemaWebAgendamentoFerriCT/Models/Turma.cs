using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SistemaWebAgendamentoFerriCT.Models
{
    public class Turma
    {
        [Key]
        public int TurmaId { get; set; }

        [Required]
        [StringLength(100)]
        public string NomeTurma { get; set; }

        [ForeignKey("Professor")]
        public int ProfessorId { get; set; }
        public virtual Professor Professor { get; set; }

        public virtual ICollection<HorarioTurma> Horarios { get; set; }
    }
}