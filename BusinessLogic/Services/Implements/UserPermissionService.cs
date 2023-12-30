using DataAccess.Entities;
using DataAccess.Models.Requests;
using DataAccess.Models.Responses;
using DataAccess.ModelsEnum;
using DataAccess.Repositories;
using Microsoft.Extensions.Configuration;
using System.Transactions;

namespace BusinessLogic.Services.Implements
{
    public class UserPermissionService : IUserPermissionService
    {
        private readonly IUserPermissionRepository _userPermissionRepository;
        private readonly IConfiguration _config;

        public UserPermissionService(
            IUserPermissionRepository userPermissionRepository,
            IConfiguration config
        )
        {
            _userPermissionRepository = userPermissionRepository;
            _config = config;
        }

        public async Task<CommonResponse> UpdateUserPermissionAsync(UserPermissionRequest request)
        {
            string internalServerErrorMsg = _config[
                "ResponseMessages:UserPermissionMsg:InternalServerErrorMsg"
            ];
            string updatePermissonSuccessMsg = _config[
                "ResponseMessages:UserPermissionMsg:UpdatePermissonSuccessMsg"
            ];
            string userNotFoundMsg = _config["ResponseMessages:UserPermissionMsg:UserNotFoundMsg"];
            CommonResponse commonResponse = new CommonResponse();
            try
            {
                using (var scope = new TransactionScope(TransactionScopeAsyncFlowOption.Enabled))
                {
                    foreach (var item in request.PermissionRequests)
                    {
                        UserPermission? userPermission =
                            await _userPermissionRepository.UpdateUserPermissionAsync(
                                request.UserId,
                                item.PermissionId,
                                item.Status
                            );
                        if (userPermission == null)
                        {
                            {
                                commonResponse.Status = 400;
                                commonResponse.Message = userNotFoundMsg;
                            }
                        }
                    }
                    scope.Complete();
                    commonResponse.Status = 200;
                    commonResponse.Message = updatePermissonSuccessMsg;
                }
            }
            catch
            {
                commonResponse.Message = internalServerErrorMsg;
                commonResponse.Status = 500;
            }
            return commonResponse;
        }

        public async Task<CommonResponse> GetPermissionsByUserAsync(
            Guid userId,
            int? page,
            int? pageSize,
            SortType? sortType
        )
        {
            CommonResponse commonResponse = new CommonResponse();
            try
            {
                List<UserPermission>? rs =
                    await _userPermissionRepository.GetPermissionsByUserIdAsync(userId);
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
                            p =>
                                new UserPermissionResponse
                                {
                                    Name = p.Permission.Name,
                                    DisplayName = p.Permission.DisplayName,
                                    UserId = userId,
                                    PermissionId = p.PermissionId,
                                    Status = p.Status.ToString()
                                }
                        )
                        .Distinct()
                        .ToList();
                    commonResponse.Status = 200;
                    commonResponse.Data = res;
                    commonResponse.Pagination = pagination;
                }
            }
            catch
            {
                commonResponse.Status = 500;
                commonResponse.Message = "";
            }
            return commonResponse;
        }
    }
}
