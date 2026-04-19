namespace SistemaWebAgendamentoFerriCT.Migrations
{
    using System;
    using System.Data.Entity.Migrations;
    
    public partial class AdicionaCamposV2 : DbMigration
    {
        public override void Up()
        {
            AddColumn("dbo.Agendamentoes", "ListaEspera", c => c.Boolean(nullable: false));
            AddColumn("dbo.Pagamentoes", "CodigoTransacao", c => c.String());
        }
        
        public override void Down()
        {
            DropColumn("dbo.Pagamentoes", "CodigoTransacao");
            DropColumn("dbo.Agendamentoes", "ListaEspera");
        }
    }
}
