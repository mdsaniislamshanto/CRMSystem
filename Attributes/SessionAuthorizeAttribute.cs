using CRMSystem.Constants;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using static System.Collections.Specialized.BitVector32;

namespace CRMSystem.Attributes
{
    public class SessionAuthorizeAttribute : ActionFilterAttribute
    {
        public override void OnActionExecuting(ActionExecutingContext context)
        {
            var session = context.HttpContext.Session;

            var userId = session.GetString(SessionKeys.UserId);

            if (string.IsNullOrEmpty(userId))
            {
                context.Result = new RedirectToActionResult(
                    "Login",
                    "Auth",
                    null);

                return;
            }

            base.OnActionExecuting(context);
        }
    }
}