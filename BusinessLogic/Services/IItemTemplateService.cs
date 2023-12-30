using DataAccess.Models.Requests;
using DataAccess.Models.Responses;
using DataAccess.ModelsEnum;

namespace BusinessLogic.Services
{
    public interface IItemTemplateService
    {
        Task<CommonResponse> CreateItemTemplate(ItemTemplateRequest request, Guid userId);
        Task<CommonResponse> GetItemTemplateById(Guid id, Guid userId);
        Task<CommonResponse> GetItemTemplateByIdForUser(Guid id);
        Task<CommonResponse> GetItemTemplatesAsync(
            Guid userId,
            ItemFilterRequest itemFilterRequest,
            int? pageSize,
            int? page,
            SortType? sortType = SortType.DES
        );

        Task<CommonResponse> SearchItemTemplateByKeyWord(
            string searchKeyWord,
            int? pageSize,
            int? page
        );
        Task<CommonResponse> UpdateItemTemplate(
            ItemTemplateRequest request,
            Guid itemId,
            Guid userId
        );
    }
}
