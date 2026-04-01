using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace SistemaWebAgendamentoFerriCT.Models
{
    public class CentroTreinamento
    {
        [Key]
        public int CentroTreinamentoId { get; set; }

        [Required]
        public string Nome { get; set; }

        public string CNPJ { get; set; }

        public string Endereco { get; set; }

        public string Telefone { get; set; }

        public virtual ICollection<Pagamento> Pagamentos { get; set; }
    }
}