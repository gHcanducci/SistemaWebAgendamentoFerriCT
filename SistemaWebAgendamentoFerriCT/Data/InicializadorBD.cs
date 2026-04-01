using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

using System.Data.Entity;
namespace SistemaWebAgendamentoFerriCT.Models
{
    public class InicializadorBD :
    DropCreateDatabaseIfModelChanges<SistemaContext>
    {
        protected override void Seed(SistemaContext context)
        {
            var clientes = new List<Cliente>();
            // Loop para criar 12 produtos fictícios
            for (int i = 1; i <= 12; i++)
            {
                clientes.Add(new Cliente
                {
                     
                });
            }
            // Adiciona ao contexto e salva
            context.Clientes.AddRange(clientes);
            context.SaveChanges();
        }
    }
}
