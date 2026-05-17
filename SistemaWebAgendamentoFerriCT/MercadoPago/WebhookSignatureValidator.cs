using System;
using System.Security.Cryptography;
using System.Text;

namespace SistemaWebAgendamentoFerriCT.MercadoPago
{
    public static class WebhookSignatureValidator
    {
        // Janela de tolerância: rejeita timestamps com diferença maior que 5 minutos
        // do relógio do servidor. Mitiga replay attacks com webhooks capturados.
        private static readonly TimeSpan MaxClockSkew = TimeSpan.FromMinutes(5);

        public static ValidationResult Validate(
            string signatureHeader,
            string requestIdHeader,
            string dataId,
            string secret,
            DateTimeOffset now)
        {
            if (string.IsNullOrWhiteSpace(signatureHeader))
                return ValidationResult.Fail("Header x-signature ausente.");
            if (string.IsNullOrWhiteSpace(dataId))
                return ValidationResult.Fail("data.id ausente.");
            if (string.IsNullOrWhiteSpace(secret))
                return ValidationResult.Fail("WebhookSecret não configurado.");

            // x-signature: "ts=1234567890,v1=abc123..."
            string ts = null, v1 = null;
            foreach (var part in signatureHeader.Split(','))
            {
                var kv = part.Trim().Split(new[] { '=' }, 2);
                if (kv.Length != 2) continue;
                if (kv[0] == "ts") ts = kv[1];
                else if (kv[0] == "v1") v1 = kv[1];
            }

            if (string.IsNullOrEmpty(ts) || string.IsNullOrEmpty(v1))
                return ValidationResult.Fail("x-signature mal formado.");

            if (!long.TryParse(ts, out var tsUnix))
                return ValidationResult.Fail("ts não numérico.");

            var tsTime = DateTimeOffset.FromUnixTimeSeconds(tsUnix);
            var diff = now - tsTime;
            if (diff.Duration() > MaxClockSkew)
                return ValidationResult.Fail("ts fora da janela de tolerância.");

            // Manifest oficial do MP: id:<dataId>;request-id:<requestId>;ts:<ts>;
            // requestId é "" quando o header não vem; manifest continua válido.
            var manifest = $"id:{dataId};request-id:{requestIdHeader ?? string.Empty};ts:{ts};";

            string computedHex;
            using (var hmac = new HMACSHA256(Encoding.UTF8.GetBytes(secret)))
            {
                var hash = hmac.ComputeHash(Encoding.UTF8.GetBytes(manifest));
                computedHex = ToHexLower(hash);
            }

            if (!ConstantTimeEquals(Encoding.UTF8.GetBytes(computedHex), Encoding.UTF8.GetBytes(v1)))
                return ValidationResult.Fail("Assinatura inválida.");

            return ValidationResult.Ok(tsUnix);
        }

        // Comparação em tempo constante: imune a timing attacks que vazariam
        // prefixos corretos da assinatura por diferença de tempo de resposta.
        private static bool ConstantTimeEquals(byte[] a, byte[] b)
        {
            if (a == null || b == null) return false;
            if (a.Length != b.Length) return false;
            int diff = 0;
            for (int i = 0; i < a.Length; i++) diff |= a[i] ^ b[i];
            return diff == 0;
        }

        private static string ToHexLower(byte[] bytes)
        {
            var sb = new StringBuilder(bytes.Length * 2);
            foreach (var b in bytes) sb.Append(b.ToString("x2"));
            return sb.ToString();
        }

        public class ValidationResult
        {
            public bool IsValid { get; private set; }
            public string Reason { get; private set; }
            public long? Timestamp { get; private set; }

            public static ValidationResult Ok(long ts) =>
                new ValidationResult { IsValid = true, Timestamp = ts };

            public static ValidationResult Fail(string reason) =>
                new ValidationResult { IsValid = false, Reason = reason };
        }
    }
}
