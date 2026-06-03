namespace SistemaWebAgendamentoFerriCT.Migrations
{
    using System;
    using System.Data.Entity.Migrations;
    
    public partial class AddMercadoPagoFields : DbMigration
    {
        public override void Up()
        {
            AddColumn("dbo.Pagamentoes", "PreferenceId", c => c.String(maxLength: 100));
            AddColumn("dbo.Pagamentoes", "WebhookEventoId", c => c.String(maxLength: 100));
            AddColumn("dbo.Pagamentoes", "DataCriacao", c => c.DateTime(nullable: false, defaultValueSql: "GETDATE()"));
            AddColumn("dbo.Pagamentoes", "DataAtualizacao", c => c.DateTime());
            AlterColumn("dbo.Pagamentoes", "CodigoTransacao", c => c.String(maxLength: 100));

            // Índices únicos filtrados — únicos apenas entre valores não-nulos.
            // EF6 não suporta filtered indexes via CreateIndex; usa Sql() puro.
            Sql("CREATE UNIQUE INDEX [IX_Pagamentoes_PreferenceId] ON [dbo].[Pagamentoes]([PreferenceId]) WHERE [PreferenceId] IS NOT NULL");
            Sql("CREATE UNIQUE INDEX [IX_Pagamentoes_CodigoTransacao] ON [dbo].[Pagamentoes]([CodigoTransacao]) WHERE [CodigoTransacao] IS NOT NULL");
            Sql("CREATE UNIQUE INDEX [IX_Pagamentoes_WebhookEventoId] ON [dbo].[Pagamentoes]([WebhookEventoId]) WHERE [WebhookEventoId] IS NOT NULL");
        }

        public override void Down()
        {
            Sql("DROP INDEX [IX_Pagamentoes_WebhookEventoId] ON [dbo].[Pagamentoes]");
            Sql("DROP INDEX [IX_Pagamentoes_CodigoTransacao] ON [dbo].[Pagamentoes]");
            Sql("DROP INDEX [IX_Pagamentoes_PreferenceId] ON [dbo].[Pagamentoes]");

            AlterColumn("dbo.Pagamentoes", "CodigoTransacao", c => c.String());
            DropColumn("dbo.Pagamentoes", "DataAtualizacao");
            DropColumn("dbo.Pagamentoes", "DataCriacao");
            DropColumn("dbo.Pagamentoes", "WebhookEventoId");
            DropColumn("dbo.Pagamentoes", "PreferenceId");
        }
    }
}
