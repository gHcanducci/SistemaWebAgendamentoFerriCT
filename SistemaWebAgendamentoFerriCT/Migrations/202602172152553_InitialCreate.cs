namespace SistemaWebAgendamentoFerriCT.Migrations
{
    using System;
    using System.Data.Entity.Migrations;
    
    public partial class InitialCreate : DbMigration
    {
        public override void Up()
        {
            CreateTable(
                "dbo.Agendamentoes",
                c => new
                    {
                        AgendamentoId = c.Int(nullable: false, identity: true),
                        DataAula = c.DateTime(nullable: false),
                        TipoAula = c.String(nullable: false),
                        Status = c.String(nullable: false),
                        DataSolicitacao = c.DateTime(nullable: false),
                        ClienteId = c.Int(nullable: false),
                        HorarioTurmaId = c.Int(nullable: false),
                    })
                .PrimaryKey(t => t.AgendamentoId)
                .ForeignKey("dbo.Clientes", t => t.ClienteId, cascadeDelete: true)
                .ForeignKey("dbo.HorarioTurmas", t => t.HorarioTurmaId, cascadeDelete: true)
                .Index(t => t.ClienteId)
                .Index(t => t.HorarioTurmaId);
            
            CreateTable(
                "dbo.Clientes",
                c => new
                    {
                        ClienteId = c.Int(nullable: false, identity: true),
                        Nome = c.String(nullable: false, maxLength: 100),
                        CPF = c.String(nullable: false, maxLength: 14),
                        Telefone = c.String(nullable: false, maxLength: 20),
                        Email = c.String(nullable: false, maxLength: 100),
                        DataCadastro = c.DateTime(nullable: false),
                    })
                .PrimaryKey(t => t.ClienteId);
            
            CreateTable(
                "dbo.HorarioTurmas",
                c => new
                    {
                        HorarioTurmaId = c.Int(nullable: false, identity: true),
                        DiaSemana = c.Int(nullable: false),
                        HoraInicio = c.Time(nullable: false, precision: 7),
                        HoraFim = c.Time(nullable: false, precision: 7),
                        TurmaId = c.Int(nullable: false),
                    })
                .PrimaryKey(t => t.HorarioTurmaId)
                .ForeignKey("dbo.Turmas", t => t.TurmaId, cascadeDelete: true)
                .Index(t => t.TurmaId);
            
            CreateTable(
                "dbo.Turmas",
                c => new
                    {
                        TurmaId = c.Int(nullable: false, identity: true),
                        NomeTurma = c.String(nullable: false, maxLength: 100),
                        CapacidadeMaxima = c.Int(nullable: false),
                        ProfessorId = c.Int(nullable: false),
                    })
                .PrimaryKey(t => t.TurmaId)
                .ForeignKey("dbo.Professors", t => t.ProfessorId, cascadeDelete: true)
                .Index(t => t.ProfessorId);
            
            CreateTable(
                "dbo.Professors",
                c => new
                    {
                        ProfessorId = c.Int(nullable: false, identity: true),
                        Nome = c.String(nullable: false, maxLength: 100),
                        Telefone = c.String(maxLength: 20),
                        Email = c.String(maxLength: 100),
                        Especialidade = c.String(maxLength: 100),
                    })
                .PrimaryKey(t => t.ProfessorId);
            
            CreateTable(
                "dbo.Pagamentoes",
                c => new
                    {
                        PagamentoId = c.Int(nullable: false, identity: true),
                        Valor = c.Decimal(nullable: false, precision: 18, scale: 2),
                        DataPagamento = c.DateTime(),
                        FormaPagamento = c.String(nullable: false),
                        StatusPagamento = c.String(nullable: false),
                        AgendamentoId = c.Int(nullable: false),
                        CentroTreinamento_CentroTreinamentoId = c.Int(),
                    })
                .PrimaryKey(t => t.PagamentoId)
                .ForeignKey("dbo.Agendamentoes", t => t.AgendamentoId, cascadeDelete: true)
                .ForeignKey("dbo.CentroTreinamentoes", t => t.CentroTreinamento_CentroTreinamentoId)
                .Index(t => t.AgendamentoId)
                .Index(t => t.CentroTreinamento_CentroTreinamentoId);
            
            CreateTable(
                "dbo.CentroTreinamentoes",
                c => new
                    {
                        CentroTreinamentoId = c.Int(nullable: false, identity: true),
                        Nome = c.String(nullable: false),
                        CNPJ = c.String(),
                        Endereco = c.String(),
                        Telefone = c.String(),
                    })
                .PrimaryKey(t => t.CentroTreinamentoId);
            
        }
        
        public override void Down()
        {
            DropForeignKey("dbo.Pagamentoes", "CentroTreinamento_CentroTreinamentoId", "dbo.CentroTreinamentoes");
            DropForeignKey("dbo.Pagamentoes", "AgendamentoId", "dbo.Agendamentoes");
            DropForeignKey("dbo.Agendamentoes", "HorarioTurmaId", "dbo.HorarioTurmas");
            DropForeignKey("dbo.HorarioTurmas", "TurmaId", "dbo.Turmas");
            DropForeignKey("dbo.Turmas", "ProfessorId", "dbo.Professors");
            DropForeignKey("dbo.Agendamentoes", "ClienteId", "dbo.Clientes");
            DropIndex("dbo.Pagamentoes", new[] { "CentroTreinamento_CentroTreinamentoId" });
            DropIndex("dbo.Pagamentoes", new[] { "AgendamentoId" });
            DropIndex("dbo.Turmas", new[] { "ProfessorId" });
            DropIndex("dbo.HorarioTurmas", new[] { "TurmaId" });
            DropIndex("dbo.Agendamentoes", new[] { "HorarioTurmaId" });
            DropIndex("dbo.Agendamentoes", new[] { "ClienteId" });
            DropTable("dbo.CentroTreinamentoes");
            DropTable("dbo.Pagamentoes");
            DropTable("dbo.Professors");
            DropTable("dbo.Turmas");
            DropTable("dbo.HorarioTurmas");
            DropTable("dbo.Clientes");
            DropTable("dbo.Agendamentoes");
        }
    }
}
