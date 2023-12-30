using DataAccess.Entities;

namespace DataAccess.Repositories
{
    public interface IActivityTypeRepository
    {
        Task<ActivityType?> FindActivityTypeByIdAsync(Guid id);
        Task<List<ActivityType>> GetAllActivityTypesAsync();
    }
}
