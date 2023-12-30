using DataAccess.Models.Responses;

namespace BusinessLogic.Services
{
    public interface IActivityTypeService
    {
        Task<CommonResponse> GetAllActivityTypesAsync();
    }
}
