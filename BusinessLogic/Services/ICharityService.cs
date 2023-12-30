using DataAccess.EntityEnums;
using DataAccess.Models.Requests;
using DataAccess.Models.Responses;
using DataAccess.ModelsEnum;

namespace BusinessLogic.Services
{
    public interface ICharityService
    {
        Task<CommonResponse> ConfirmCharity(
            Guid charityId,
            ConfirmCharityRequest request,
            Guid userId
        );
        Task<CommonResponse> DeleteCharity(Guid charrityId);
        Task<CommonResponse> GetCharitiesAsync(
            int? page,
            int? pageSize,
            CharityStatus? charityStatus,
            SortType? sortType,
            string? name,
            bool isWaitingToUpdate
        );
        Task<CommonResponse> GetCharityDetails(Guid charityId);
        Task<CommonResponse> GetCharityUnitListByCharityId(Guid charityId, Guid userId);
        Task<CommonResponse> GetCharityUnitListByCharityIdForGuess(Guid charityId);
        Task<CommonResponse> RegisterToBecomeCharity(CharityCreatingRequest request);
    }
}
