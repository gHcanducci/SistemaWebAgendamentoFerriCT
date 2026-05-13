using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Data.Entity.Migrations;

namespace SistemaWebAgendamentoFerriCT.Models
{
    public class InicializadorBD : CreateDatabaseIfNotExists<SistemaContext>
    {
        protected override void Seed(SistemaContext context)
        {
            var professores = new List<Professor>
            {
                new Professor { Nome = "Carlos Silva",    Telefone = "(11) 99000-0001", Email = "carlos@ferri.ct",  Especialidade = "Musculação" },
                new Professor { Nome = "Ana Oliveira",   Telefone = "(11) 99000-0002", Email = "ana@ferri.ct",     Especialidade = "Funcional"  },
            };
            context.Professores.AddRange(professores);
            context.SaveChanges();

            var turmas = new List<Turma>
            {
                new Turma { NomeTurma = "Musculação Manhã",   CapacidadeMaxima = 20, ProfessorId = professores[0].ProfessorId },
                new Turma { NomeTurma = "Funcional Tarde",   CapacidadeMaxima = 15, ProfessorId = professores[1].ProfessorId },
            };
            context.Turmas.AddRange(turmas);
            context.SaveChanges();

            var horarios = new List<HorarioTurma>
            {
                new HorarioTurma { TurmaId = turmas[0].TurmaId, DiaSemana = DayOfWeek.Monday,    HoraInicio = new TimeSpan(7, 0, 0),  HoraFim = new TimeSpan(8, 0, 0)  },
                new HorarioTurma { TurmaId = turmas[0].TurmaId, DiaSemana = DayOfWeek.Wednesday, HoraInicio = new TimeSpan(7, 0, 0),  HoraFim = new TimeSpan(8, 0, 0)  },
                new HorarioTurma { TurmaId = turmas[1].TurmaId, DiaSemana = DayOfWeek.Tuesday,   HoraInicio = new TimeSpan(17, 0, 0), HoraFim = new TimeSpan(18, 0, 0) },
                new HorarioTurma { TurmaId = turmas[1].TurmaId, DiaSemana = DayOfWeek.Thursday,  HoraInicio = new TimeSpan(17, 0, 0), HoraFim = new TimeSpan(18, 0, 0) },
            };
            context.HorariosTurma.AddRange(horarios);
            context.SaveChanges();
        }
    }
}
