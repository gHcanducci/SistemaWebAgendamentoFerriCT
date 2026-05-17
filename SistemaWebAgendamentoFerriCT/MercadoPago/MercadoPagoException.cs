using System;
using System.Net;

namespace SistemaWebAgendamentoFerriCT.MercadoPago
{
    public class MercadoPagoException : Exception
    {
        public HttpStatusCode? StatusCode { get; }
        public string ResponseBody { get; }

        public MercadoPagoException(
            string message,
            HttpStatusCode? statusCode = null,
            string responseBody = null,
            Exception inner = null)
            : base(message, inner)
        {
            StatusCode = statusCode;
            ResponseBody = responseBody;
        }
    }
}
