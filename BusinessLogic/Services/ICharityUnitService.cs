using DataAccess.EntityEnums;
using DataAccess.Models.Requests;
using DataAccess.Models.Responses;
using DataAccess.ModelsEnum;

namespace BusinessLogic.Services
{
    public interface ICharityUnitService
    {
        Task<CommonResponse> ConfirmCharityUnit(
            Guid charityUnitId,
            ConfirmCharityUnitRequest request,
            Guid userId
        );
        Task<CommonResponse> ConfirmUpdateCharityUnit(
            Guid charityUnitId,
            ConfirmCharityUnitRequest request,
            Guid confirmById
        );
        Task<CommonResponse> CreateCharityUnit(CharityUnitCreatingRequest request, Guid userId);
        Task<CommonResponse> DeleteCharityUnit(Guid charrityUnitId);
        Task<CommonResponse> GetCharityUnit(
            string? searchKeyWord,
            CharityUnitStatus? status,
            Guid? charityId,
            int? page,
            int? pageSize,
            SortType? sortType
        );
        Task<CommonResponse> GetCharityUnitDetails(Guid charityUnitId);
        Task<CommonResponse> GetCharityUnitDetailsByUserIdAndStatusForAdmin(
            Guid userId,
            CharityUnitStatus status
        );
        Task<CommonResponse> GetLatestCharityUnitUpdateVersion(Guid userId);

        Task<CommonResponse> UpdateCharityUnit(CharityUnitUpdatingRequest request, Guid userId);
    }
}
