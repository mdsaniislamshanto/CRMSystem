using CRMSystem.Constants;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace CRMSystem.Attributes
{
    public class RoleAuthorizeAttribute : ActionFilterAttribute
    {
        private readonly string _requiredRole;

        public RoleAuthorizeAttribute(string requiredRole)
        {
            _requiredRole = requiredRole;
        }

        public override void OnActionExecuting(ActionExecutingContext context)
        {
            var roleKey = context.HttpContext.Session.GetString(SessionKeys.RoleKey);

            if (string.IsNullOrEmpty(roleKey))
            {
                context.Result = new RedirectToActionResult(
                    "Login",
                    "Auth",
                    null);

                return;
            }

            if (roleKey != _requiredRole)
            {
                context.Result = new RedirectToActionResult(
                    "AccessDenied",
                    "Auth",
                    null);

                return;
            }

            base.OnActionExecuting(context);
        }
    }
}