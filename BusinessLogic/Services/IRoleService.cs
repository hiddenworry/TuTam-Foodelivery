using DataAccess.Models.Responses;

namespace BusinessLogic.Services
{
    public interface IRoleService
    {
        Task<CommonResponse> GetAllRolesAsync();
    }
}
