using DataAccess.Repositories;

namespace FoodDonationDeliveryManagementAPI.Security.Authourization.config
{
    public static class AuthorizationConfig
    {
        public static void LoadPermissionsFromDatabase(IServiceCollection services)
        {
            var serviceProvider = services.BuildServiceProvider();
            var permissionRepository = serviceProvider.GetRequiredService<IPermissionRepository>();
            var allPermissions = permissionRepository
                .GetListPermissionAsync()
                .GetAwaiter()
                .GetResult();
            if (allPermissions != null)
            {
                foreach (var permission in allPermissions)
                {
                    if (permission != null && permission.Name != null)
                    {
                        services.AddAuthorization(options =>
                        {
                            options.AddPolicy(
                                permission.Name,
                                policy => policy.RequireClaim("Permissions", permission.Name)
                            );
                        });
                    }
                }
            }
        }
    }
}
