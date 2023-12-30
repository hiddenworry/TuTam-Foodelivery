using DataAccess.Entities;
using DataAccess.EntityEnums;

namespace DataAccess.Repositories
{
    public interface ICharityUnitRepository
    {
        Task<int> CreateCharityUnitAsync(CharityUnit charityUnit);
        Task<int> DeleteCharityUnitAsync(CharityUnit charity);
        Task<List<CharityUnit>> FindActiveCharityUnitsByCharityIdAsync(Guid charityId);
        Task<CharityUnit?> FindCharityUnitByUserIdAsync(Guid userId);
        Task<List<CharityUnit>> FindCharityUnitsByCharityIdAsync(Guid charityId);

        Task<CharityUnit?> FindCharityUnitByIdAsync(Guid charityUnitId);
        Task<CharityUnit?> FindCharityUnitsByIdForUserAsync(Guid charityUnitId);
        Task<CharityUnit?> FindUnverifyCharityUnitByUserIdAsync(Guid userId);
        Task<CharityUnit?> FindUnVerifyCharityUnitsByIdAsync(Guid charityUnitId);
        Task<List<CharityUnit>?> GetCharityUnit(
            string? searchKeyWord,
            CharityUnitStatus? status,
            Guid? charityId
        );
        Task<CharityUnit?> GetLatestUpdateCharityUnitByUserIdForAdminAsync(Guid userId);
        Task<int> UpdateCharityUnitAsync(CharityUnit charity);
        Task<CharityUnit?> FindActiveCharityUnitsByUserIdAsync(Guid userId);
        Task<CharityUnit?> FindUnverifyUpdateCharityUnitByUserIdAsync(Guid userId);
        Task<CharityUnit?> FindCharityUnitsByUserAndStatusIdAsync(
            Guid userId,
            CharityUnitStatus status
        );
        Task<CharityUnit?> FindCharityUnitByUserIdOnlyAsync(Guid userId);
    }
}
