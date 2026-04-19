// ─────────────────────────────────────────────────────────────────
// Cliente.cs — com DataAnnotations completas (Aula 10)
// ─────────────────────────────────────────────────────────────────
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace SistemaWebAgendamentoFerriCT.Models
{
    public class Cliente
    {
        [Key]
        public int ClienteId { get; set; }

        [Required(ErrorMessage = "O nome é de preenchimento obrigatório.")]
        [StringLength(100, MinimumLength = 3, ErrorMessage = "O nome deve ter entre 3 e 100 caracteres.")]
        [Display(Name = "Nome Completo")]
        public string Nome { get; set; }

        [Required(ErrorMessage = "O CPF é obrigatório.")]
        [StringLength(14, MinimumLength = 11, ErrorMessage = "CPF inválido.")]
        [Display(Name = "CPF")]
        public string CPF { get; set; }

        [Required(ErrorMessage = "O telefone é obrigatório.")]
        [StringLength(20, MinimumLength = 10, ErrorMessage = "Telefone inválido.")]
        [Display(Name = "Telefone")]
        public string Telefone { get; set; }

        [Required(ErrorMessage = "O e-mail é obrigatório.")]
        [EmailAddress(ErrorMessage = "Digite um endereço de e-mail válido (ex: nome@email.com).")]
        [StringLength(100)]
        [Display(Name = "E-mail")]
        public string Email { get; set; }

        public DateTime DataCadastro { get; set; } = DateTime.Now;

        public virtual ICollection<Agendamento> Agendamentos { get; set; }
    }
}