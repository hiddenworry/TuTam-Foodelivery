using DataAccess.EntityEnums;
using DataAccess.Models.Requests;
using DataAccess.Models.Responses;
using DataAccess.ModelsEnum;
using DataAccess.Repositories;
using Microsoft.Extensions.Configuration;
using System.Transactions;

namespace BusinessLogic.Services.Implements
{
    public class RolePermissionService : IRolePermissionService
    {
        private readonly IRolePermissionRepository _rolePermissionRepository;
        private readonly IConfiguration _config;
        private readonly IUserPermissionRepository _userPermissionRepository;

        public RolePermissionService(
            IRolePermissionRepository rolePermissionRepository,
            IConfiguration configuration,
            IUserPermissionRepository userPermissionRepository
        )
        {
            _rolePermissionRepository = rolePermissionRepository;
            _config = configuration;
            _userPermissionRepository = userPermissionRepository;
        }

        public async Task<CommonResponse> GetPermissionsByRoleAsync(
            Guid roleId,
            int? page,
            int? pageSize,
            SortType sortType
        )
        {
            CommonResponse commonResponse = new CommonResponse();
            try
            {
                var rs = await _rolePermissionRepository.GetRolePermissionsByRoleIdAsync(roleId);
                if (rs != null)
                {
                    Pagination pagination = new Pagination();
                    pagination.PageSize = pageSize == null ? 10 : pageSize.Value;
                    pagination.CurrentPage = page == null ? 1 : page.Value;
                    pagination.Total = rs.Count;
                    rs = rs.Skip((pagination.CurrentPage - 1) * pagination.PageSize)
                        .Take(pagination.PageSize)
                        .ToList();
                    var res = rs.Select(
                        r =>
                            new
                            {
                                r.Permission.Name,
                                Id = r.PermissionId,
                                Status = r.Status.ToString(),
                                r.Permission.DisplayName
                            }
                    );
                    if (sortType == SortType.ASC)
                    {
                        res = res.OrderBy(u => u.Name).ToList();
                    }
                    else
                    {
                        res = res.OrderByDescending(u => u.Name).ToList();
                    }
                    commonResponse.Status = 200;
                    commonResponse.Data = res;
                    commonResponse.Pagination = pagination;
                }
                else
                {
                    commonResponse.Status = 400;
                    commonResponse.Message = "";
                }
            }
            catch
            {
                commonResponse.Status = 500;
                commonResponse.Message = "";
            }
            return commonResponse;
        }

        public async Task<CommonResponse> UpdatePermissionsByRoleAsync(
            RolePermissionUpdatingRequest request
        )
        {
            CommonResponse commonResponse = new CommonResponse();
            string createSuccessMsg = _config[
                "ResponseMessages:RolePermissionMsg:CreateSuccessMsg"
            ];
            string roleNotFoundMsg = _config["ResponseMessages:RolePermissionMsg:RoleNotFoundMsg"];
            string internalServerErrorMsg = _config[
                "ResponseMessages:CommonMsg:InternalServerErrorMsg"
            ];
            try
            {
                using (var scope = new TransactionScope(TransactionScopeAsyncFlowOption.Enabled))
                {
                    int rs = await _rolePermissionRepository.UpdateRolePermissionsByRoleIdAsync(
                        request.RoleId,
                        request.PermissionId,
                        request.Status
                    );
                    int rs2 = await _userPermissionRepository.UpdateUserPermissionsByRoleIdAsync(
                        request.RoleId,
                        request.PermissionId,
                        (UserPermissionStatus)request.Status
                    );
                    if (rs > 0)
                    {
                        commonResponse.Status = 200;
                        commonResponse.Message = createSuccessMsg;
                        scope.Complete();
                    }
                    else
                    {
                        commonResponse.Status = 400;
                        commonResponse.Message = roleNotFoundMsg;
                    }
                }
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
