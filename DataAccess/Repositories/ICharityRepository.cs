using DataAccess.Entities;
using DataAccess.EntityEnums;

namespace DataAccess.Repositories
{
    public interface ICharityRepository
    {
        Task<int> CreateCharityAsync(Charity charity);
        Task<int> DeleteCharityAsync(Charity charity);
        Task<List<Charity>> GetCharitiesAsync(
            CharityStatus? status,
            string? name,
            bool isWaitingToUpdate
        );
        Task<Charity?> GetCharityByEmail(string email);
        Task<Charity?> GetCharityById(Guid charityId);
        Task<int> UpdateCharityAsync(Charity charity);
    }
}
