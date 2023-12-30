using BusinessLogic.Utils.FirebaseService;
using DataAccess.Entities;
using DataAccess.EntityEnums;
using DataAccess.Models.Requests;
using DataAccess.Models.Requests.ModelBinders;
using DataAccess.Models.Responses;
using DataAccess.ModelsEnum;
using DataAccess.Repositories;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System.Transactions;

namespace BusinessLogic.Services.Implements
{
    public class BranchService : IBranchService
    {
        private readonly IBranchRepository _branchRepository;
        private readonly IConfiguration _config;
        private readonly ILogger<BranchService> _logger;
        private readonly IUserRepository _userRepository;
        private readonly IRoleRepository _roleRepository;
        private readonly IFirebaseStorageService _firebaseStorageService;
        private readonly IRolePermissionRepository _rolePermissionRepository;
        private readonly IUserPermissionRepository _userPermissionRepository;

        public BranchService(
            IBranchRepository branchRepository,
            IConfiguration config,
            ILogger<BranchService> logger,
            IUserRepository userRepository,
            IRoleRepository roleRepository,
            IFirebaseStorageService firebaseStorageService,
            IRolePermissionRepository rolePermissionRepository,
            IUserPermissionRepository userPermissionRepository
        )
        {
            _branchRepository = branchRepository;
            _config = config;
            _logger = logger;
            _userRepository = userRepository;
            _roleRepository = roleRepository;
            _firebaseStorageService = firebaseStorageService;
            _rolePermissionRepository = rolePermissionRepository;
            _userPermissionRepository = userPermissionRepository;
        }

        public async Task<CommonResponse> GetBranchesAsync(
            string? name,
            BranchStatus? status,
            string? address,
            int? pageSize,
            int? page,
            string? orderBy,
            SortType? sortType,
            string? userRoleName
        )
        {
            List<Branch> branches = new List<Branch>();
            if (
                status == BranchStatus.INACTIVE
                && userRoleName != RoleEnum.SYSTEM_ADMIN.ToString()
                && userRoleName != RoleEnum.BRANCH_ADMIN.ToString()
            )
                return new CommonResponse
                {
                    Status = 403,
                    Message = _config[
                        "ResponseMessages:BranchMsg:UserNotAllowToGetInactiveBranchMsg"
                    ]
                };
            else
            {
                if (
                    userRoleName != RoleEnum.SYSTEM_ADMIN.ToString()
                    && userRoleName != RoleEnum.BRANCH_ADMIN.ToString()
                )
                    status = BranchStatus.ACTIVE;
                branches = await _branchRepository.GetBranchesAsync(name, status, address);
            }

            if (
                orderBy != null
                && sortType != null
                && (sortType == SortType.ASC || sortType == SortType.DES)
            )
            {
                try
                {
                    if (sortType == SortType.ASC)
                        branches = branches
                            .OrderBy(branch => GetPropertyValue(branch, orderBy))
                            .ToList();
                    else
                        branches = branches
                            .OrderByDescending(branch => GetPropertyValue(branch, orderBy))
                            .ToList();
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "An exception occurred in service method GetBranches.");
                    return new CommonResponse
                    {
                        Status = 400,
                        Message = _config["ResponseMessages:CommonMsg:SortedFieldNotFoundMsg"]
                    };
                }
            }
            Pagination pagination = new Pagination();
            pagination.PageSize = pageSize == null ? 10 : pageSize.Value;
            pagination.CurrentPage = page == null ? 1 : page.Value;
            pagination.Total = branches.Count;
            branches = branches
                .Skip((pagination.CurrentPage - 1) * pagination.PageSize)
                .Take(pagination.PageSize)
                .ToList();

            List<BranchResponse> branchResponses = new List<BranchResponse>();

            foreach (Branch branch in branches)
            {
                try
                {
                    branchResponses.Add(
                        new BranchResponse
                        {
                            Id = branch.Id,
                            Name = branch.Name,
                            Address = branch.Address,
                            Location = branch.Location
                                .Split(",")
                                .Select(l => double.Parse(l))
                                .ToList(),
                            Image = branch.Image,
                            CreatedDate = branch.CreatedDate,
                            Status = branch.Status.ToString()
                        }
                    );
                }
                catch { }
            }

            return new CommonResponse
            {
                Status = 200,
                Data = branchResponses,
                Pagination = pagination,
                Message = _config["ResponseMessages:BranchMsg:GetBranchesSuccessMsg"]
            };
        }

        static object? GetPropertyValue(object obj, string propertyName)
        {
            var propertyInfo = obj.GetType().GetProperty(propertyName);
            if (propertyInfo != null)
                return propertyInfo.GetValue(obj);
            else
                throw new Exception(
                    $"Property {propertyName} not found in object type {obj.GetType}."
                );
        }

        public async Task<CommonResponse> CreateBranch(BranchCreatingRequest branchRequest)
        {
            CommonResponse commonResponse = new CommonResponse();
            string internalServerErrorMsg = _config[
                "ResponseMessages:CommonMsg:InternalServerErrorMsg"
            ];
            string createBranchesSuccessMsg = _config[
                "ResponseMessages:BranchMsg:CreateBranchesSuccessMsg"
            ];
            string userAlreadyIsBranchAdminMsg = _config[
                "ResponseMessages:BranchMsg:UserAlreadyIsBranchAdminMsg"
            ];
            string userIsNotBranchAdminRoleMsg = _config[
                "ResponseMessages:BranchMsg:UserIsNotBranchAdminRoleMsg"
            ];
            string userNotFoundMsg = _config["ResponseMessages:UserMsg:UserNotFoundMsg"];
            try
            {
                Role? roleBranchAdmin = await _roleRepository.GetRoleByName(
                    RoleEnum.BRANCH_ADMIN.ToString()
                );
                Branch branch = new Branch();
                branch.Address = branchRequest.Address;

                if (branchRequest.Location != null && branchRequest.Location!.Length >= 2)
                {
                    branch.Location = string.Join(",", branchRequest.Location!);
                }

                branch.CreatedDate = SettedUpDateTime.GetCurrentVietNamTime();
                branch.Name = branchRequest.Name;
                branch.Status = branchRequest.Status;
                branch.Description = branchRequest.Description;
                User? branchAdmin = await _userRepository.FindUserByIdInclueBranchAsync(
                    branchRequest.BranchAdminId
                );

                using (var stream = branchRequest.Image.OpenReadStream())
                {
                    string imageName =
                        Guid.NewGuid().ToString() + Path.GetExtension(branchRequest.Image.FileName);
                    string imageUrl = await _firebaseStorageService.UploadImageToFirebase(
                        stream,
                        imageName
                    );
                    branch.Image = imageUrl;
                }

                using (var scope = new TransactionScope(TransactionScopeAsyncFlowOption.Enabled))
                {
                    if (
                        branchAdmin != null
                        && branchAdmin.Role == roleBranchAdmin
                        && branchAdmin.Status == UserStatus.ACTIVE
                    )
                    {
                        if (branchAdmin.Branch != null)
                        {
                            commonResponse.Message = branchAdmin.Name + userAlreadyIsBranchAdminMsg;
                            commonResponse.Status = 400;
                            return commonResponse;
                        }
                        var rolePermissions =
                            await _rolePermissionRepository.GetPermissionsByRoleIdAsync(
                                roleBranchAdmin.Id
                            );
                        if (rolePermissions != null && rolePermissions.Count > 0)
                        {
                            foreach (var u in rolePermissions)
                            {
                                await _userPermissionRepository.UpdateUserPermissionAsync(
                                    branchAdmin.Id,
                                    u!.Id,
                                    UserPermissionStatus.PERMITTED
                                );
                            }
                        }
                        branch.BranchAdmin = branchAdmin;
                    }
                    else
                    {
                        commonResponse.Message = userIsNotBranchAdminRoleMsg;
                        commonResponse.Status = 400;
                        return commonResponse;
                    }
                    Branch? rs = await _branchRepository.CreateBranchAsync(branch)!;

                    if (rs != null)
                    {
                        commonResponse.Message = createBranchesSuccessMsg;
                        commonResponse.Status = 200;
                        scope.Complete();
                    }
                    else
                        throw new Exception();
                }
            }
            catch (Exception ex)
            {
                string className = nameof(BranchService);
                string methodName = nameof(CreateBranch);
                _logger.LogError(
                    ex,
                    "An error occurred in {ClassName}.{MethodName}: {ErrorMessage}",
                    className,
                    methodName,
                    ex.Message
                );
                commonResponse.Status = 500;
                commonResponse.Message = internalServerErrorMsg;
            }
            return commonResponse;
        }

        public async Task<CommonResponse> UpdateBranch(
            BranchUpdatingRequest branchRequest,
            Guid branchId
        )
        {
            CommonResponse commonResponse = new CommonResponse();
            string internalServerErrorMsg = _config[
                "ResponseMessages:CommonMsg:InternalServerErrorMsg"
            ];
            string updateBranchesSuccessMsg = _config[
                "ResponseMessages:BranchMsg:UpdateBranchesSuccessMsg"
            ];
            string userAlreadyIsBranchAdminMsg = _config[
                "ResponseMessages:BranchMsg:UserAlreadyIsBranchAdminMsg"
            ];
            string userIsNotBranchAdminRoleMsg = _config[
                "ResponseMessages:BranchMsg:UserIsNotBranchAdminRoleMsg"
            ];
            string userNotFoundMsg = _config["ResponseMessages:UserMsg:UserNotFoundMsg"];
            string branchNotFoundMsg = _config["ResponseMessages:BranchMsg:BranchNotFoundMsg"];
            try
            {
                Branch? branch = await _branchRepository.FindBranchByIdAsync(branchId);
                if (branch != null)
                {
                    var roleBranchAdmin = await _roleRepository.GetRoleByName(
                        RoleEnum.BRANCH_ADMIN.ToString()
                    );
                    if (roleBranchAdmin == null)
                    {
                        throw new Exception("Role branch admin do not exist.");
                    }

                    if (branchRequest.Location != null && branchRequest.Location.Count() >= 2)
                    {
                        branch.Location = string.Join(",", branchRequest.Location!);
                    }

                    branch.Address = !string.IsNullOrEmpty(branchRequest.Address)
                        ? branchRequest.Address
                        : branch.Address!;
                    branch.Name = !string.IsNullOrEmpty(branchRequest.Name)
                        ? branchRequest.Name
                        : branch.Name;
                    branch.Description = !string.IsNullOrEmpty(branchRequest.Description)
                        ? branchRequest.Description
                        : branch.Description;
                    if (branchRequest.Status != null)
                    {
                        branch.Status = branchRequest.Status.Value;
                    }

                    if (branchRequest.Image != null)
                    {
                        using (var stream = branchRequest.Image.OpenReadStream())
                        {
                            string imageName =
                                Guid.NewGuid().ToString()
                                + Path.GetExtension(branchRequest.Image.FileName);
                            string imageUrl = await _firebaseStorageService.UploadImageToFirebase(
                                stream,
                                imageName
                            );
                            branch.Image = imageUrl;
                        }
                    }

                    using (
                        var scope = new TransactionScope(TransactionScopeAsyncFlowOption.Enabled)
                    )
                    {
                        if (branchRequest.BranchAdminId != null)
                        {
                            User? branchAdmin = await _userRepository.FindUserByIdAsync(
                                (Guid)branchRequest.BranchAdminId
                            );
                            if (
                                branchAdmin != null
                                && branchAdmin.Role == roleBranchAdmin
                                && branchAdmin.Status == UserStatus.ACTIVE
                            )
                            {
                                if (branchAdmin.Branch != null)
                                {
                                    commonResponse.Message =
                                        branchAdmin.Name + userAlreadyIsBranchAdminMsg;
                                    commonResponse.Status = 400;
                                    return commonResponse;
                                }
                                else if (branchAdmin.Branch == null)
                                {
                                    var rolePermissions =
                                        await _rolePermissionRepository.GetPermissionsByRoleIdAsync(
                                            branchAdmin.Id
                                        );
                                    if (rolePermissions != null && rolePermissions.Count > 0)
                                    {
                                        foreach (var u in rolePermissions)
                                        {
                                            await _userPermissionRepository.UpdateUserPermissionAsync(
                                                branchAdmin.Id,
                                                u!.Id,
                                                UserPermissionStatus.PERMITTED
                                            );
                                            User? oldBranchAdmin = branch.BranchAdmin;
                                            if (oldBranchAdmin != null)
                                            {
                                                await _userPermissionRepository.UpdateUserPermissionAsync(
                                                    oldBranchAdmin.Id,
                                                    u!.Id,
                                                    UserPermissionStatus.BANNED
                                                );
                                            }
                                        }
                                    }
                                    branch.BranchAdmin = branchAdmin;
                                }
                            }
                            else
                            {
                                commonResponse.Status = 400;
                                commonResponse.Message = userIsNotBranchAdminRoleMsg;
                                return commonResponse;
                            }
                        }

                        Branch? rs = await _branchRepository.UpdateBranchAsync(branch)!;
                        if (rs != null)
                        {
                            commonResponse.Message = updateBranchesSuccessMsg;
                            commonResponse.Status = 200;
                            scope.Complete();
                        }
                        else
                            throw new Exception();
                    }
                }
                else
                {
                    commonResponse.Status = 400;
                    commonResponse.Message = branchNotFoundMsg;
                }
            }
            catch (Exception ex)
            {
                string className = nameof(BranchService);
                string methodName = nameof(UpdateBranch);
                _logger.LogError(
                    ex,
                    "An error occurred in {ClassName}.{MethodName}: {ErrorMessage}",
                    className,
                    methodName,
                    ex.Message
                );
                commonResponse.Status = 500;
                commonResponse.Message = internalServerErrorMsg;
            }
            return commonResponse;
        }

        public async Task<CommonResponse> GetBranchDetailsForSystemAdmin(Guid branchId)
        {
            CommonResponse commonResponse = new CommonResponse();
            string internalServerErrorMsg = _config[
                "ResponseMessages:CommonMsg:InternalServerErrorMsg"
            ];
            try
            {
                Branch? rs = await _branchRepository.FindBranchDetailsByIdAsync(branchId);
                if (rs != null)
                {
                    BranchDetailsResponse branchDetailsResponse = new BranchDetailsResponse
                    {
                        Address = rs.Address,
                        Location = rs.Location,
                        Id = rs.Id,
                        CreatedDate = rs.CreatedDate,
                        Image = rs.Image,
                        Name = rs.Name,
                        Status = rs.Status.ToString(),
                        Description = rs.Description
                    };
                    if (rs.BranchAdmin != null)
                    {
                        branchDetailsResponse.BranchAdminResponses = new BranchAdminResponse
                        {
                            Email = rs.BranchAdmin.Email,
                            Phone = rs.BranchAdmin.Phone,
                            MemberName = rs.BranchAdmin.Name,
                            Id = rs.BranchAdminId
                        };
                    }

                    commonResponse.Data = branchDetailsResponse;
                }
                commonResponse.Status = 200;
            }
            catch (Exception ex)
            {
                string className = nameof(BranchService);
                string methodName = nameof(CreateBranch);
                _logger.LogError(
                    ex,
                    "An error occurred in {ClassName}.{MethodName}: {ErrorMessage}",
                    className,
                    methodName,
                    ex.Message
                );
                commonResponse.Status = 500;
                commonResponse.Message = internalServerErrorMsg;
            }
            return commonResponse;
        }

        public async Task<CommonResponse> GetBranchDetailsForUser(Guid branchId)
        {
            CommonResponse commonResponse = new CommonResponse();
            string internalServerErrorMsg = _config[
                "ResponseMessages:CommonMsg:InternalServerErrorMsg"
            ];
            try
            {
                Branch? rs = await _branchRepository.FindBranchDetailsByIdAsync(branchId);
                if (rs != null)
                {
                    BranchDetailsResponse branchDetailsResponse = new BranchDetailsResponse
                    {
                        Address = rs.Address,
                        Location = rs.Location,
                        Id = rs.Id,
                        CreatedDate = rs.CreatedDate,
                        Image = rs.Image,
                        Name = rs.Name,
                        Status = rs.Status.ToString(),
                        Description = rs.Description
                    };
                    commonResponse.Data = branchDetailsResponse;
                }
                commonResponse.Status = 200;
            }
            catch (Exception ex)
            {
                string className = nameof(BranchService);
                string methodName = nameof(CreateBranch);
                _logger.LogError(
                    ex,
                    "An error occurred in {ClassName}.{MethodName}: {ErrorMessage}",
                    className,
                    methodName,
                    ex.Message
                );
                commonResponse.Status = 500;
                commonResponse.Message = internalServerErrorMsg;
            }
            return commonResponse;
        }

        public async Task<CommonResponse> GetBranchDetailsForBranchAdmin(Guid userId)
        {
            CommonResponse commonResponse = new CommonResponse();
            string internalServerErrorMsg = _config[
                "ResponseMessages:CommonMsg:InternalServerErrorMsg"
            ];
            try
            {
                Branch? rs = await _branchRepository.FindBranchByBranchAdminIdAsync(userId);
                if (rs != null)
                {
                    BranchDetailsResponse branchDetailsResponse = new BranchDetailsResponse
                    {
                        Address = rs.Address,
                        Location = rs.Location,
                        Id = rs.Id,
                        CreatedDate = rs.CreatedDate,
                        Image = rs.Image,
                        Name = rs.Name,
                        Status = rs.Status.ToString(),
                        Description = rs.Description
                    };
                    if (rs.BranchAdmin != null)
                    {
                        branchDetailsResponse.BranchAdminResponses = new BranchAdminResponse
                        {
                            Email = rs.BranchAdmin.Email,
                            Phone = rs.BranchAdmin.Phone,
                            MemberName = rs.BranchAdmin.Name,
                            Id = rs.BranchAdminId
                        };
                    }

                    commonResponse.Data = branchDetailsResponse;
                }
                commonResponse.Status = 200;
            }
            catch (Exception ex)
            {
                string className = nameof(BranchService);
                string methodName = nameof(CreateBranch);
                _logger.LogError(
                    ex,
                    "An error occurred in {ClassName}.{MethodName}: {ErrorMessage}",
                    className,
                    methodName,
                    ex.Message
                );
                commonResponse.Status = 500;
                commonResponse.Message = internalServerErrorMsg;
            }
            return commonResponse;
        }
    }
}
