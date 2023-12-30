using DataAccess.Models.Requests;
using DataAccess.Models.Responses;

namespace BusinessLogic.Services
{
    public interface IPhaseService
    {
        Task<CommonResponse> CreatePharse(PharseCreatingRequest request);
        Task<CommonResponse> DeletePharse(Guid phaseId);

        // Task<CommonResponse> EndPhase(Guid phaseId);
        Task<CommonResponse> GetPhaseByActivityId(Guid activityId);

        // Task<CommonResponse> StartPhase(Guid phaseId);
        Task<CommonResponse> UpdatePharse(List<PhaseUpdatingRequest> phaseUpdatingRequest);
    }
}
