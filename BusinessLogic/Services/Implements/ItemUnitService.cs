using DataAccess.Models.Responses;
using DataAccess.Repositories;
using Microsoft.Extensions.Configuration;

namespace BusinessLogic.Services.Implements
{
    public class ItemUnitService : IItemUnitService
    {
        private readonly IItemUnitRepostitory _itemUnitRepostitory;
        private readonly IConfiguration _config;

        public ItemUnitService(
            IItemUnitRepostitory itemUnitRepostitory,
            IConfiguration configuration
        )
        {
            _itemUnitRepostitory = itemUnitRepostitory;
            _config = configuration;
        }

        public async Task<CommonResponse> GetItemUnitListAsync()
        {
            CommonResponse commonResponse = new CommonResponse();
            string internalServerErrorMsg = _config[
                "ResponseMessages:CommonMsg:InternalServerErrorMsg"
            ];
            try
            {
                var rs = await _itemUnitRepostitory.GetListItemUnitAsync();
                commonResponse.Status = 200;
                commonResponse.Data = rs;
            }
            catch
            {
                commonResponse.Status = 500;
                commonResponse.Message = internalServerErrorMsg;
            }
            return commonResponse;
        }
    }
}
