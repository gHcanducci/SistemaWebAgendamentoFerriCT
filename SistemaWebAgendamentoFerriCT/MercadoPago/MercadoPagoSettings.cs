using System;
using System.Configuration;

namespace SistemaWebAgendamentoFerriCT.MercadoPago
{
    public static class MercadoPagoSettings
    {
        public static string AccessToken => Required("MercadoPago:AccessToken");
        public static string PublicKey => Required("MercadoPago:PublicKey");
        public static string WebhookSecret => Required("MercadoPago:WebhookSecret");
        public static string NotificationUrl => Required("MercadoPago:NotificationUrl");
        public static string BackUrlBase => Required("MercadoPago:BackUrlBase");

        public static bool IsSandbox =>
            AccessToken.StartsWith("TEST-", StringComparison.Ordinal);

        private static string Required(string key)
        {
            var value = ConfigurationManager.AppSettings[key];
            if (string.IsNullOrWhiteSpace(value))
                throw new InvalidOperationException(
                    $"Configuração ausente: '{key}'. Verifique Web.secrets.config.");
            return value;
        }
    }
}
