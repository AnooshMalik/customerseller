using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Mvc;

namespace customerseller.Filters
{
    public class TempBlockFilter : IActionFilter
    {
        public void OnActionExecuting(ActionExecutingContext context)
        {
            var session = context.HttpContext.Session;
            var isTempBlocked = session.GetString("TempBlocked");

            if (isTempBlocked == "true")
            {
                var controllerName = context.RouteData.Values["controller"]?.ToString();
                var actionName = context.RouteData.Values["action"]?.ToString();

                bool isAllowed = controllerName == "Chat"
                    || (controllerName == "Account" && actionName == "Logout");

                if (!isAllowed)
                {
                    context.Result = new RedirectToActionResult("Messages", "Chat", null);
                }
            }
        }

        public void OnActionExecuted(ActionExecutedContext context) { }
    }
}