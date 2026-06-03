namespace SistemaWebAgendamentoFerriCT.Migrations
{
    using System;
    using System.Data.Entity.Migrations;
    
    public partial class RemoveListaEsperaECapacidade : DbMigration
    {
        public override void Up()
        {
            DropColumn("dbo.Agendamentoes", "ListaEspera");
            DropColumn("dbo.Turmas", "CapacidadeMaxima");
        }
        
        public override void Down()
        {
            AddColumn("dbo.Turmas", "CapacidadeMaxima", c => c.Int(nullable: false));
            AddColumn("dbo.Agendamentoes", "ListaEspera", c => c.Boolean(nullable: false));
        }
    }
}
