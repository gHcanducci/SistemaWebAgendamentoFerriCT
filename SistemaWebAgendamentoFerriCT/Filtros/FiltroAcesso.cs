using System.Web.Mvc;
using System.Web.Routing;

namespace SistemaWebAgendamentoFerriCT.Filtros
{
    // Herdamos de ActionFilterAttribute para transformar essa classe em um Filtro do MVC
    public class FiltroAcesso : ActionFilterAttribute
    {
        // Esse método roda ANTES de qualquer Action do Controller abrir
        public override void OnActionExecuting(ActionExecutingContext filterContext)
        {
            // Verifica se a sessão "AdminLogado" está vazia (nula)
            if (filterContext.HttpContext.Session["AdminLogado"] == null)
            {
                // Se estiver vazia, cancela a requisição e redireciona para o Login
                filterContext.Result = new RedirectToRouteResult(
                    new RouteValueDictionary(new { controller = "Admin", action = "Login" })
                );
            }

            base.OnActionExecuting(filterContext);
        }
    }
}
