using DataAccess.Models.Requests;
using DataAccess.Models.Responses;

namespace BusinessLogic.Services
{
    public interface IAcceptableDonatedRequestService
    {
        Task<CommonResponse> ConfirmDonatedRequestAsync(
            DonatedRequestConfirmingRequest donatedRequestConfirmingRequest,
            Guid userId
        );
    }
}
