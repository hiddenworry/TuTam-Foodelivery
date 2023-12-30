using DataAccess.Repositories;
using Microsoft.Extensions.Configuration;

namespace BusinessLogic.Services.Implements
{
    public class PermissionService : IPermissionService
    {
        private readonly IPermissionRepository _permissionRepository;
        private readonly IRolePermissionRepository _rolePermissionRepository;
        private readonly IConfiguration _config;

        public PermissionService(
            IPermissionRepository permissionRepository,
            IRolePermissionRepository rolePermissionRepository,
            IConfiguration configuration
        )
        {
            _permissionRepository = permissionRepository;
            _rolePermissionRepository = rolePermissionRepository;
            _config = configuration;
        }
    }
}
