using DataAccess.Models.Responses;

namespace BusinessLogic.Services
{
    public interface IItemUnitService
    {
        Task<CommonResponse> GetItemUnitListAsync();
    }
}
