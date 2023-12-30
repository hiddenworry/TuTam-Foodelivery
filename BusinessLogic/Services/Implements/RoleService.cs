using DataAccess.Models.Responses;
using DataAccess.Repositories;
using Microsoft.Extensions.Configuration;

namespace BusinessLogic.Services.Implements
{
    public class RoleService : IRoleService
    {
        private readonly IRoleRepository _roleRepository;
        private readonly IConfiguration _config;

        public RoleService(IRoleRepository roleRepository, IConfiguration configuration)
        {
            _roleRepository = roleRepository;
            _config = configuration;
        }

        public async Task<CommonResponse> GetAllRolesAsync()
        {
            string errorMsg = _config["ResponseMessages:CommonMsg:InternalServerErrorMsg"];
            var list = await _roleRepository.GetAllRolesAsync();
            CommonResponse commonResponse = new CommonResponse();
            commonResponse.Status = 200;
            commonResponse.Data = list;
            return commonResponse;
        }
    }
}
