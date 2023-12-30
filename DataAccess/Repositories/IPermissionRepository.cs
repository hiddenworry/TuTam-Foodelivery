using DataAccess.Entities;

namespace DataAccess.Repositories
{
    public interface IPermissionRepository
    {
        Task<Permission?> AddPermissionAsync(Permission permission);
        Task<List<Permission>?> GetListPermissionAsync();
        Task<Permission?> GetPermissionByIdAsync(Guid id);
        Task<Permission?> GetPermissionByNameAsync(string name);
    }
}
