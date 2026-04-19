using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Web;

namespace SistemaWebAgendamentoFerriCT.ViewModels
{
    // ─────────────────────────────────────────────────────────────────
    // AdminLoginViewModel — usado na tela de login do admin
    // ─────────────────────────────────────────────────────────────────
    public class AdminLoginViewModel
    {
        [Required(ErrorMessage = "O usuário é obrigatório.")]
        [Display(Name = "Usuário")]
        public string Usuario { get; set; }

        [Required(ErrorMessage = "A senha é obrigatória.")]
        [StringLength(20, MinimumLength = 3, ErrorMessage = "A senha deve ter no mínimo 3 caracteres.")]
        [DataType(DataType.Password)]
        [Display(Name = "Senha")]
        public string Senha { get; set; }
    }
}