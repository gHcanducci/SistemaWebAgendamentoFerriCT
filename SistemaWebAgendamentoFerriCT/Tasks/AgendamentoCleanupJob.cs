using System;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using SistemaWebAgendamentoFerriCT.Models;

namespace SistemaWebAgendamentoFerriCT.Tasks
{
    // Job que cancela agendamentos parados em PendentePagamento por mais de 1h.
    // Roda em Timer estático iniciado em Application_Start.
    public static class AgendamentoCleanupJob
    {
        // Janela de tolerância antes de cancelar
        private static readonly TimeSpan TempoMaximoPendente = TimeSpan.FromHours(1);

        // Frequência do tick (varredura completa)
        private static readonly TimeSpan IntervaloVerificacao = TimeSpan.FromMinutes(5);

        // Timer estático para sobreviver enquanto o app está vivo
        private static Timer _timer;
        private static int _executando; // 0 = idle, 1 = rodando (evita reentrância)

        public static void Iniciar()
        {
            if (_timer != null) return; // proteção contra dupla inicialização

            _timer = new Timer(
                callback: _ => SafeExecutar(),
                state: null,
                dueTime: TimeSpan.FromMinutes(1),    // primeira execução após 1 minuto
                period: IntervaloVerificacao);

            Trace.TraceInformation("AgendamentoCleanupJob iniciado.");
        }

        private static void SafeExecutar()
        {
            // Garante que não há duas execuções concorrentes (CompareExchange é atômico)
            if (Interlocked.CompareExchange(ref _executando, 1, 0) == 1) return;

            try
            {
                Executar();
            }
            catch (Exception ex)
            {
                // Nunca deixa o timer morrer por uma exception
                Trace.TraceError("AgendamentoCleanupJob falhou: " + ex);
            }
            finally
            {
                Interlocked.Exchange(ref _executando, 0);
            }
        }

        private static void Executar()
        {
            var limite = DateTime.Now - TempoMaximoPendente;

            using (var db = new SistemaContext())
            {
                // Busca agendamentos abandonados em PendentePagamento
                var expirados = db.Agendamentos
                    .Where(a => a.Status == "PendentePagamento" && a.DataSolicitacao < limite)
                    .ToList();

                if (expirados.Count == 0) return;

                foreach (var ag in expirados)
                {
                    ag.Status = "Cancelado";

                    // Cancela todos os Pagamentos pendentes associados
                    var pagamentos = db.Pagamentos
                        .Where(p => p.AgendamentoId == ag.AgendamentoId && p.StatusPagamento == "Pendente")
                        .ToList();

                    foreach (var p in pagamentos)
                    {
                        p.StatusPagamento = "Cancelado";
                        p.DataAtualizacao = DateTime.Now;
                    }
                }

                db.SaveChanges();

                Trace.TraceInformation(
                    $"AgendamentoCleanupJob: {expirados.Count} agendamento(s) cancelado(s) por timeout.");
            }
        }
    }
}
