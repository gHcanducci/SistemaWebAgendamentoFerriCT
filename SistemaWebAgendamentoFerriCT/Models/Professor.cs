using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace SistemaWebAgendamentoFerriCT.Models
{
    public class Professor
    {
        [Key]
        public int ProfessorId { get; set; }

        [Required]
        [StringLength(100)]
        public string Nome { get; set; }

        [StringLength(20)]
        public string Telefone { get; set; }

        [StringLength(100)]
        public string Email { get; set; }

        [StringLength(100)]
        public string Especialidade { get; set; }

        public virtual ICollection<Turma> Turmas { get; set; }
    }
}