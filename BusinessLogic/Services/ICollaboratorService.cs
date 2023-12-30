using DataAccess.EntityEnums;
using DataAccess.Models.Requests;
using DataAccess.Models.Responses;
using DataAccess.ModelsEnum;

namespace BusinessLogic.Services
{
    public interface ICollaboratorService
    {
        Task<CommonResponse> checkCollaborator(Guid userId);
        Task<CommonResponse> ConfirmCollaborator(
            Guid collaboratorId,
            ConfirmCollaboratorRequest request
        );
        Task<CommonResponse> DeleteCollaborator(Guid collaboratorId);
        Task<CommonResponse> GetDetailsCollaborator(Guid collaboratorId);

        Task<CommonResponse> GetListUnVerifyCollaborator(
            CollaboratorStatus? status,
            int? page,
            int? pageSize,
            SortType? sortType = SortType.ASC
        );
        Task<CommonResponse> RegisterToBecomeCollaborator(
            Guid userId,
            CollaboratorCreatingRequest request
        );
        //Task<CommonResponse> UpdateCollaboratorForUserAsync(
        //    Guid userId,
        //    CollaboratorUpdatingRequest request
        //);
    }
}
