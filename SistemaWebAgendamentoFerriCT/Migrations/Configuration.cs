namespace SistemaWebAgendamentoFerriCT.Migrations
{
    using System;
    using System.Collections.Generic;
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
            // ─── 1. Professores ───────────────────────────────────────────
            context.Professores.AddOrUpdate(p => p.Email,
                new Models.Professor { Nome = "Instrutor Boxe",     Email = "boxe@ferrict.com.br",     Especialidade = "Boxe" },
                new Models.Professor { Nome = "Instrutor Funcional", Email = "funcional@ferrict.com.br", Especialidade = "Funcional" }
            );
            context.SaveChanges();

            var profBoxe      = context.Professores.First(p => p.Email == "boxe@ferrict.com.br");
            var profFuncional = context.Professores.First(p => p.Email == "funcional@ferrict.com.br");

            // ─── 2. Turmas ────────────────────────────────────────────────
            context.Turmas.AddOrUpdate(t => t.NomeTurma,
                new Models.Turma { NomeTurma = "Boxe Misto",    CapacidadeMaxima = 20, ProfessorId = profBoxe.ProfessorId },
                new Models.Turma { NomeTurma = "Boxe Feminino", CapacidadeMaxima = 20, ProfessorId = profBoxe.ProfessorId },
                new Models.Turma { NomeTurma = "Boxe KIDS",     CapacidadeMaxima = 15, ProfessorId = profBoxe.ProfessorId },
                new Models.Turma { NomeTurma = "Funcional",     CapacidadeMaxima = 15, ProfessorId = profFuncional.ProfessorId }
            );
            context.SaveChanges();

            // ─── 3. Horários (idempotente — usa AddOrUpdate por TurmaId+DiaSemana+HoraInicio) ────────
            var misto    = context.Turmas.First(t => t.NomeTurma == "Boxe Misto");
            var feminino = context.Turmas.First(t => t.NomeTurma == "Boxe Feminino");
            var kids     = context.Turmas.First(t => t.NomeTurma == "Boxe KIDS");
            var func     = context.Turmas.First(t => t.NomeTurma == "Funcional");

            context.HorariosTurma.AddOrUpdate(
                h => new { h.TurmaId, h.DiaSemana, h.HoraInicio },
                // ── Boxe Misto – Segunda ────────────────────────────────
                new Models.HorarioTurma { TurmaId = misto.TurmaId, DiaSemana = DayOfWeek.Monday, HoraInicio = new TimeSpan( 9,  0, 0), HoraFim = new TimeSpan(10,  0, 0) },
                new Models.HorarioTurma { TurmaId = misto.TurmaId, DiaSemana = DayOfWeek.Monday, HoraInicio = new TimeSpan(16,  0, 0), HoraFim = new TimeSpan(17,  0, 0) },
                new Models.HorarioTurma { TurmaId = misto.TurmaId, DiaSemana = DayOfWeek.Monday, HoraInicio = new TimeSpan(17,  0, 0), HoraFim = new TimeSpan(18,  0, 0) },
                new Models.HorarioTurma { TurmaId = misto.TurmaId, DiaSemana = DayOfWeek.Monday, HoraInicio = new TimeSpan(18, 20, 0), HoraFim = new TimeSpan(19, 20, 0) },
                new Models.HorarioTurma { TurmaId = misto.TurmaId, DiaSemana = DayOfWeek.Monday, HoraInicio = new TimeSpan(19, 30, 0), HoraFim = new TimeSpan(20, 30, 0) },
                new Models.HorarioTurma { TurmaId = misto.TurmaId, DiaSemana = DayOfWeek.Monday, HoraInicio = new TimeSpan(20, 40, 0), HoraFim = new TimeSpan(21, 40, 0) },
                // ── Boxe Misto – Terça ──────────────────────────────────
                new Models.HorarioTurma { TurmaId = misto.TurmaId, DiaSemana = DayOfWeek.Tuesday, HoraInicio = new TimeSpan( 7, 20, 0), HoraFim = new TimeSpan( 8, 20, 0) },
                new Models.HorarioTurma { TurmaId = misto.TurmaId, DiaSemana = DayOfWeek.Tuesday, HoraInicio = new TimeSpan( 9,  0, 0), HoraFim = new TimeSpan(10,  0, 0) },
                new Models.HorarioTurma { TurmaId = misto.TurmaId, DiaSemana = DayOfWeek.Tuesday, HoraInicio = new TimeSpan(16,  0, 0), HoraFim = new TimeSpan(17,  0, 0) },
                new Models.HorarioTurma { TurmaId = misto.TurmaId, DiaSemana = DayOfWeek.Tuesday, HoraInicio = new TimeSpan(18, 20, 0), HoraFim = new TimeSpan(19, 20, 0) },
                new Models.HorarioTurma { TurmaId = misto.TurmaId, DiaSemana = DayOfWeek.Tuesday, HoraInicio = new TimeSpan(19, 30, 0), HoraFim = new TimeSpan(20, 30, 0) },
                new Models.HorarioTurma { TurmaId = misto.TurmaId, DiaSemana = DayOfWeek.Tuesday, HoraInicio = new TimeSpan(20, 40, 0), HoraFim = new TimeSpan(21, 40, 0) },
                // ── Boxe Misto – Quarta ─────────────────────────────────
                new Models.HorarioTurma { TurmaId = misto.TurmaId, DiaSemana = DayOfWeek.Wednesday, HoraInicio = new TimeSpan( 9,  0, 0), HoraFim = new TimeSpan(10,  0, 0) },
                new Models.HorarioTurma { TurmaId = misto.TurmaId, DiaSemana = DayOfWeek.Wednesday, HoraInicio = new TimeSpan(16,  0, 0), HoraFim = new TimeSpan(17,  0, 0) },
                new Models.HorarioTurma { TurmaId = misto.TurmaId, DiaSemana = DayOfWeek.Wednesday, HoraInicio = new TimeSpan(17,  0, 0), HoraFim = new TimeSpan(18,  0, 0) },
                new Models.HorarioTurma { TurmaId = misto.TurmaId, DiaSemana = DayOfWeek.Wednesday, HoraInicio = new TimeSpan(18, 20, 0), HoraFim = new TimeSpan(19, 20, 0) },
                new Models.HorarioTurma { TurmaId = misto.TurmaId, DiaSemana = DayOfWeek.Wednesday, HoraInicio = new TimeSpan(19, 30, 0), HoraFim = new TimeSpan(20, 30, 0) },
                new Models.HorarioTurma { TurmaId = misto.TurmaId, DiaSemana = DayOfWeek.Wednesday, HoraInicio = new TimeSpan(20, 40, 0), HoraFim = new TimeSpan(21, 40, 0) },
                // ── Boxe Misto – Quinta ─────────────────────────────────
                new Models.HorarioTurma { TurmaId = misto.TurmaId, DiaSemana = DayOfWeek.Thursday, HoraInicio = new TimeSpan( 7, 20, 0), HoraFim = new TimeSpan( 8, 20, 0) },
                new Models.HorarioTurma { TurmaId = misto.TurmaId, DiaSemana = DayOfWeek.Thursday, HoraInicio = new TimeSpan( 9,  0, 0), HoraFim = new TimeSpan(10,  0, 0) },
                new Models.HorarioTurma { TurmaId = misto.TurmaId, DiaSemana = DayOfWeek.Thursday, HoraInicio = new TimeSpan(16,  0, 0), HoraFim = new TimeSpan(17,  0, 0) },
                new Models.HorarioTurma { TurmaId = misto.TurmaId, DiaSemana = DayOfWeek.Thursday, HoraInicio = new TimeSpan(18, 20, 0), HoraFim = new TimeSpan(19, 20, 0) },
                new Models.HorarioTurma { TurmaId = misto.TurmaId, DiaSemana = DayOfWeek.Thursday, HoraInicio = new TimeSpan(19, 30, 0), HoraFim = new TimeSpan(20, 30, 0) },
                new Models.HorarioTurma { TurmaId = misto.TurmaId, DiaSemana = DayOfWeek.Thursday, HoraInicio = new TimeSpan(20, 40, 0), HoraFim = new TimeSpan(21, 40, 0) },
                // ── Boxe Misto – Sexta ──────────────────────────────────
                new Models.HorarioTurma { TurmaId = misto.TurmaId, DiaSemana = DayOfWeek.Friday, HoraInicio = new TimeSpan( 9,  0, 0), HoraFim = new TimeSpan(10,  0, 0) },
                new Models.HorarioTurma { TurmaId = misto.TurmaId, DiaSemana = DayOfWeek.Friday, HoraInicio = new TimeSpan(16,  0, 0), HoraFim = new TimeSpan(17,  0, 0) },
                new Models.HorarioTurma { TurmaId = misto.TurmaId, DiaSemana = DayOfWeek.Friday, HoraInicio = new TimeSpan(17,  0, 0), HoraFim = new TimeSpan(18,  0, 0) },
                new Models.HorarioTurma { TurmaId = misto.TurmaId, DiaSemana = DayOfWeek.Friday, HoraInicio = new TimeSpan(18, 20, 0), HoraFim = new TimeSpan(19, 20, 0) },
                new Models.HorarioTurma { TurmaId = misto.TurmaId, DiaSemana = DayOfWeek.Friday, HoraInicio = new TimeSpan(19, 30, 0), HoraFim = new TimeSpan(20, 30, 0) },
                // ── Boxe Misto – Sábado (2h) ────────────────────────────
                new Models.HorarioTurma { TurmaId = misto.TurmaId, DiaSemana = DayOfWeek.Saturday, HoraInicio = new TimeSpan(14, 0, 0), HoraFim = new TimeSpan(16, 0, 0) },

                // ── Boxe Feminino – Seg/Qua/Sex 07h10 ──────────────────
                new Models.HorarioTurma { TurmaId = feminino.TurmaId, DiaSemana = DayOfWeek.Monday,    HoraInicio = new TimeSpan(7, 10, 0), HoraFim = new TimeSpan(8, 10, 0) },
                new Models.HorarioTurma { TurmaId = feminino.TurmaId, DiaSemana = DayOfWeek.Wednesday, HoraInicio = new TimeSpan(7, 10, 0), HoraFim = new TimeSpan(8, 10, 0) },
                new Models.HorarioTurma { TurmaId = feminino.TurmaId, DiaSemana = DayOfWeek.Friday,    HoraInicio = new TimeSpan(7, 10, 0), HoraFim = new TimeSpan(8, 10, 0) },
                // ── Boxe Feminino – Ter/Qui 17h ─────────────────────────
                new Models.HorarioTurma { TurmaId = feminino.TurmaId, DiaSemana = DayOfWeek.Tuesday,  HoraInicio = new TimeSpan(17, 0, 0), HoraFim = new TimeSpan(18, 0, 0) },
                new Models.HorarioTurma { TurmaId = feminino.TurmaId, DiaSemana = DayOfWeek.Thursday, HoraInicio = new TimeSpan(17, 0, 0), HoraFim = new TimeSpan(18, 0, 0) },

                // ── Boxe KIDS – Sábado 10h ──────────────────────────────
                new Models.HorarioTurma { TurmaId = kids.TurmaId, DiaSemana = DayOfWeek.Saturday, HoraInicio = new TimeSpan(10, 0, 0), HoraFim = new TimeSpan(11, 0, 0) },

                // ── Funcional – Segunda ─────────────────────────────────
                new Models.HorarioTurma { TurmaId = func.TurmaId, DiaSemana = DayOfWeek.Monday, HoraInicio = new TimeSpan( 6,  0, 0), HoraFim = new TimeSpan( 7,  0, 0) },
                new Models.HorarioTurma { TurmaId = func.TurmaId, DiaSemana = DayOfWeek.Monday, HoraInicio = new TimeSpan(19, 30, 0), HoraFim = new TimeSpan(20, 30, 0) },
                // ── Funcional – Terça ───────────────────────────────────
                new Models.HorarioTurma { TurmaId = func.TurmaId, DiaSemana = DayOfWeek.Tuesday, HoraInicio = new TimeSpan( 9,  0, 0), HoraFim = new TimeSpan(10,  0, 0) },
                new Models.HorarioTurma { TurmaId = func.TurmaId, DiaSemana = DayOfWeek.Tuesday, HoraInicio = new TimeSpan(18, 20, 0), HoraFim = new TimeSpan(19, 20, 0) },
                // ── Funcional – Quarta ──────────────────────────────────
                new Models.HorarioTurma { TurmaId = func.TurmaId, DiaSemana = DayOfWeek.Wednesday, HoraInicio = new TimeSpan( 6,  0, 0), HoraFim = new TimeSpan( 7,  0, 0) },
                new Models.HorarioTurma { TurmaId = func.TurmaId, DiaSemana = DayOfWeek.Wednesday, HoraInicio = new TimeSpan(19, 30, 0), HoraFim = new TimeSpan(20, 30, 0) },
                // ── Funcional – Quinta ──────────────────────────────────
                new Models.HorarioTurma { TurmaId = func.TurmaId, DiaSemana = DayOfWeek.Thursday, HoraInicio = new TimeSpan( 9,  0, 0), HoraFim = new TimeSpan(10,  0, 0) },
                new Models.HorarioTurma { TurmaId = func.TurmaId, DiaSemana = DayOfWeek.Thursday, HoraInicio = new TimeSpan(18, 20, 0), HoraFim = new TimeSpan(19, 20, 0) },
                // ── Funcional – Sexta ───────────────────────────────────
                new Models.HorarioTurma { TurmaId = func.TurmaId, DiaSemana = DayOfWeek.Friday, HoraInicio = new TimeSpan(6, 0, 0), HoraFim = new TimeSpan(7, 0, 0) }
            );
            context.SaveChanges();
        }
    }
}
