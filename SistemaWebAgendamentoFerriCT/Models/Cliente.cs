using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace SistemaWebAgendamentoFerriCT.Models
{
    public class Cliente
    {
        [Key]
        public int ClienteId { get; set; }

        [Required]
        [StringLength(100)]
        public string Nome { get; set; }

        [Required]
        [StringLength(14)]
        public string CPF { get; set; }

        [Required]
        [StringLength(20)]
        public string Telefone { get; set; }

        [Required]
        [StringLength(100)]
        public string Email { get; set; }

        public DateTime DataCadastro { get; set; } = DateTime.Now;

        public virtual ICollection<Agendamento> Agendamentos { get; set; }
    }
}