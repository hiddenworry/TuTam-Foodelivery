using BusinessLogic.Utils.SecurityServices;
using DataAccess.Repositories;
using System.Security.Claims;

namespace FoodDonationDeliveryManagementAPI.Security.MiddleWare
{
    public class PermissionMiddleware : IMiddleware
    {
        private readonly IPermissionRepository _permissionRepository;
        private readonly IUserPermissionRepository _userPermissionRepository;
        private readonly ILogger<PermissionMiddleware> _logger;
        private readonly IJwtService _jwtService;

        public PermissionMiddleware(
            IPermissionRepository permissionRepository,
            IUserPermissionRepository userPermissionRepository,
            ILogger<PermissionMiddleware> logger,
            IJwtService jwtService
        )
        {
            this._permissionRepository = permissionRepository;
            this._userPermissionRepository = userPermissionRepository;
            _logger = logger;
            _jwtService = jwtService;
        }

        public async Task InvokeAsync(HttpContext context, RequestDelegate _next)
        {
            string? path = null;

            var token = context.Request.Headers["Authorization"]
                .FirstOrDefault()
                ?.Split(" ")
                .Last();
            if (context.Request.Path.Value != null)
            {
                path = context.Request.Path.Value.ToLower();
            }
            if (
                token == null
                || path != null
                    && (
                        path.StartsWith("/authenticate")
                        || path.StartsWith("/logout")
                        || path.StartsWith("/refresh-access-token")
                    )
            )
            {
                await _next(context);
                return;
            }

            var decodedToken = _jwtService.GetClaimsPrincipal(token);
            var userSub = "";
            if (decodedToken != null)
            {
                userSub = decodedToken.Claims.FirstOrDefault(c => c.Type == "Id")?.Value;
            }
            if (string.IsNullOrEmpty(userSub))
            {
                context.Response.StatusCode = StatusCodes.Status401Unauthorized;
                await context.Response.WriteAsync("Đăng nhập thất bại");
                return;
            }
            try
            {
                var cancellationToken = context.RequestAborted;
                var permissionsIdentity = await GetUserPermissionsIdentity(userSub);
                if (permissionsIdentity == null)
                {
                    _logger.LogWarning("User {} does not have permissions", userSub);

                    context.Response.StatusCode = StatusCodes.Status403Forbidden;
                    await context.Response.WriteAsync("Bạn không có quyền để thực hiện việc này.");
                    return;
                }

                context.User.AddIdentity(permissionsIdentity);
                await _next(context);
            }
            catch
            {
                context.Response.StatusCode = StatusCodes.Status500InternalServerError;
                await context.Response.WriteAsync("Lỗi hệ thống");
                return;
            }
        }

        public async ValueTask<ClaimsIdentity?> GetUserPermissionsIdentity(string UserId)
        {
            var userPermissions = await _userPermissionRepository.GetClaimsByUserIdAsync(
                Guid.Parse(UserId)
            );
            return CreatePermissionsIdentity(userPermissions);
        }

        private static ClaimsIdentity? CreatePermissionsIdentity(
            IReadOnlyCollection<Claim> claimPermissions
        )
        {
            if (!claimPermissions.Any())
                return null;

            var permissionsIdentity = new ClaimsIdentity(
                nameof(PermissionMiddleware),
                "name",
                "role"
            );
            permissionsIdentity.AddClaims(claimPermissions);

            return permissionsIdentity;
        }
    }
}
