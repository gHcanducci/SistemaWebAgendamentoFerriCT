using System;
using System.ComponentModel.DataAnnotations;

namespace SistemaWebAgendamentoFerriCT.ViewModels
{
    // ─────────────────────────────────────────────────────────────────
    // LoginViewModel — usado na tela de login do cliente
    // ─────────────────────────────────────────────────────────────────
    public class LoginViewModel
    {
        [Required(ErrorMessage = "O e-mail é obrigatório.")]
        [EmailAddress(ErrorMessage = "Digite um e-mail válido.")]
        [Display(Name = "E-mail")]
        public string Email { get; set; }

        [Required(ErrorMessage = "O CPF é obrigatório.")]
        [StringLength(14, MinimumLength = 11, ErrorMessage = "CPF inválido.")]
        [Display(Name = "CPF")]
        public string CPF { get; set; }
    }
}