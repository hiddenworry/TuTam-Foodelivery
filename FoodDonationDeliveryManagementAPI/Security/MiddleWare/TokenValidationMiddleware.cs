using BusinessLogic.Utils.SecurityServices;

namespace FoodDonationDeliveryManagementAPI.Security.MiddleWare
{
    public class TokenValidationMiddleware : IMiddleware
    {
        private readonly ITokenBlacklistService _tokenBlacklist;

        public TokenValidationMiddleware(ITokenBlacklistService tokenBlacklist)
        {
            _tokenBlacklist = tokenBlacklist;
        }

        public async Task InvokeAsync(HttpContext context, RequestDelegate next)
        {
            string? path = null;
            if (context.Request.Path.Value != null)
            {
                path = context.Request.Path.Value.ToLower();
            }
            // Exclude specific endpoints from authentication check
            if (
                path != null
                && (
                    path.StartsWith("/authenticate")
                    || path.StartsWith("/logout")
                    || path.StartsWith("/refresh-access-token")
                )
            )
            {
                await next(context);
                return;
            }

            var token = context.Request.Headers["Authorization"]
                .FirstOrDefault()
                ?.Split(" ")
                .Last();
            if (!string.IsNullOrEmpty(token) && _tokenBlacklist.IsTokenBlacklisted(token))
            {
                context.Response.StatusCode = StatusCodes.Status401Unauthorized;
                await context.Response.WriteAsync("Unauthorized");
                return;
            }
            await next(context);
        }
    }
}
