using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

using System.Data.Entity;
namespace SistemaWebAgendamentoFerriCT.Models
{
    public class SistemaContext : DbContext
    {
        // Define que usaremos uma conexão chamada "webProdConn" (criada no LocalDB automaticamente)
        public SistemaContext() : base("name=SistemaWebAgendamentoFerriCT") { }
        public DbSet<Cliente> Clientes { get; set; }
        public DbSet<Professor> Professores { get; set; }
        public DbSet<Turma> Turmas { get; set; }
        public DbSet<HorarioTurma> HorariosTurma { get; set; }
        public DbSet<Agendamento> Agendamentos { get; set; }
        public DbSet<Pagamento> Pagamentos { get; set; }
        public DbSet<CentroTreinamento> CentrosTreinamento { get; set; }
    }
}