using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.Extensions.DependencyInjection;
using System.Security.Claims;
using System.Threading.Tasks;
using System.Collections.Generic;
using Api.Services;

namespace Api.Authorization
{
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, AllowMultiple = true)]
    public class RequirePermissionAttribute : TypeFilterAttribute
    {
        public RequirePermissionAttribute(string permission) : base(typeof(PermissionAuthorizationFilter))
        {
            Arguments = new object[] { permission };
        }
    }

    public class PermissionAuthorizationFilter : IAuthorizationFilter
    {
        private readonly string _permission;
        private readonly IServiceProvider _serviceProvider;

        public PermissionAuthorizationFilter(string permission, IServiceProvider serviceProvider)
        {
            _permission = permission;
            _serviceProvider = serviceProvider;
        }

        public void OnAuthorization(AuthorizationFilterContext context)
        {
            // Verificar si el usuario está autenticado
            if (!context.HttpContext.User.Identity?.IsAuthenticated ?? true)
            {
                context.Result = new UnauthorizedResult();
                return;
            }

            // Obtener el rol del usuario
            var role = context.HttpContext.User.Claims
                .FirstOrDefault(c => c.Type == ClaimTypes.Role)?.Value;

            if (string.IsNullOrEmpty(role))
            {
                context.Result = new ForbidResult();
                return;
            }

            // Obtener el servicio de usuario usando DI
            var userService = _serviceProvider.GetRequiredService<UserService>();
            
            // Obtener los permisos del rol
            var permissions = Task.Run(async () => {
                return await userService.GetPermissionsByRoleAsync(role);
            }).Result;

            // Verificar si el usuario tiene el permiso requerido
            if (!permissions.Contains(_permission))
            {
                context.Result = new ForbidResult();
            }
        }
    }
}