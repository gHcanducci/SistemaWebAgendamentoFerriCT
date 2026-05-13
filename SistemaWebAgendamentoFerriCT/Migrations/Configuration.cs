namespace SistemaWebAgendamentoFerriCT.Migrations
{
    using System;
    using System.Data.Entity;
    using System.Data.Entity.Migrations;
    using System.Linq;

    internal sealed class Configuration : DbMigrationsConfiguration<SistemaWebAgendamentoFerriCT.Models.SistemaContext>
    {
        public Configuration()
        {
            AutomaticMigrationsEnabled = false;
        }

        protected override void Seed(SistemaWebAgendamentoFerriCT.Models.SistemaContext context)
        {
            context.Professores.AddOrUpdate(
                p => p.Email,
                new Models.Professor { Nome = "Carlos Silva",  Telefone = "(11) 99000-0001", Email = "carlos@ferri.ct", Especialidade = "Musculação" },
                new Models.Professor { Nome = "Ana Oliveira",  Telefone = "(11) 99000-0002", Email = "ana@ferri.ct",    Especialidade = "Funcional"  }
            );
            context.SaveChanges();
        }
    }
}
