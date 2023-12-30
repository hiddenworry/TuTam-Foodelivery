using DataAccess.Entities;

namespace DataAccess.Repositories
{
    public interface IRoleRepository
    {
        Task<Role> AddRole(Role role);
        Task<List<Role>?> GetAllRolesAsync();
        Task<Role?> GetRoleByName(string name);
    }
}
