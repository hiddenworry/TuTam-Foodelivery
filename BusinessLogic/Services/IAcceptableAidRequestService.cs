using DataAccess.Models.Requests;
using DataAccess.Models.Responses;

namespace BusinessLogic.Services
{
    public interface IAcceptableAidRequestService
    {
        Task<CommonResponse> ConfirmAidRequestAsync(
            AidRequestComfirmingRequest aidRequestComfirmingRequest,
            Guid userId
        );
    }
}
