using System;

namespace SistemaWebAgendamentoFerriCT.Helpers
{
    // Relógio único do sistema, sempre no horário de Brasília.
    // DateTime.Now usa o fuso do servidor: na hospedagem (fora do Brasil) o "hoje"
    // viraria amanhã à noite e os horários exibidos ficariam adiantados.
    public static class Relogio
    {
        private static readonly TimeZoneInfo FusoBrasilia = CarregarFuso();

        // Data e hora atuais em Brasília (substitui DateTime.Now)
        public static DateTime Agora => TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, FusoBrasilia);

        // Data atual em Brasília, sem hora (substitui DateTime.Today)
        public static DateTime Hoje => Agora.Date;

        private static TimeZoneInfo CarregarFuso()
        {
            try
            {
                // ID do fuso de Brasília no Windows (sem horário de verão desde 2019)
                return TimeZoneInfo.FindSystemTimeZoneById("E. South America Standard Time");
            }
            catch (TimeZoneNotFoundException)
            {
                // Servidor sem o fuso cadastrado: usa UTC-3 fixo
                return TimeZoneInfo.CreateCustomTimeZone("Brasilia", TimeSpan.FromHours(-3), "Brasília", "Brasília");
            }
        }
    }
}
