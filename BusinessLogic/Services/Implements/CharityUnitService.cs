using BusinessLogic.Utils.EmailService;
using BusinessLogic.Utils.FirebaseService;
using BusinessLogic.Utils.SecurityServices;
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
    public class CharityUnitService : ICharityUnitService
    {
        private readonly ICharityUnitRepository _charityUnitRepository;
        private readonly ILogger<CharityService> _logger;
        private readonly IConfiguration _config;
        private readonly IFirebaseStorageService _firebaseStorageService;
        private readonly IUserRepository _userRepository;
        private readonly IPasswordHasher _passwordHasher;
        private readonly IRoleRepository _roleRepository;
        private readonly IRolePermissionRepository _rolePermissionRepository;
        private readonly IUserPermissionRepository _userPermissionRepository;
        private readonly IEmailService _emailService;
        private readonly IPostRepository _postRepository;

        public CharityUnitService(
            ICharityUnitRepository charityUnitRepository,
            ILogger<CharityService> logger,
            IConfiguration configuration,
            IFirebaseStorageService firebaseStorageService,
            IUserRepository userRepository,
            IPasswordHasher passwordHasher,
            IRoleRepository roleRepository,
            IRolePermissionRepository rolePermissionRepository,
            IUserPermissionRepository userPermissionRepository,
            IEmailService emailService,
            IPostRepository postRepository
        )
        {
            _charityUnitRepository = charityUnitRepository;
            _logger = logger;
            _config = configuration;
            _firebaseStorageService = firebaseStorageService;
            _userRepository = userRepository;
            _passwordHasher = passwordHasher;
            _roleRepository = roleRepository;
            _rolePermissionRepository = rolePermissionRepository;
            _userPermissionRepository = userPermissionRepository;
            _emailService = emailService;
            _postRepository = postRepository;
        }

        public async Task<CommonResponse> GetCharityUnitById(Guid charityUnitId)
        {
            CommonResponse commonResponse = new();
            CharityUnit? charityUnit = await _charityUnitRepository.FindCharityUnitByIdAsync(
                charityUnitId
            );
            if (charityUnit != null)
            {
                CharityUnitResponse charityUnitResponse =
                    new()
                    {
                        Address = charityUnit.Address,
                        CharityLogo = charityUnit.Charity.Logo,
                        CharityName = charityUnit.Charity.Name,
                        Name = charityUnit.Name,
                        Image = charityUnit.Image,
                        Id = charityUnit.Id,
                        Status = charityUnit.Status.ToString()
                    };
                commonResponse.Data = charityUnitResponse;
            }
            commonResponse.Status = 200;
            return commonResponse;
        }

        public async Task<CommonResponse> CreateCharityUnit(
            CharityUnitCreatingRequest request,
            Guid userId
        )
        {
            CommonResponse commonResponse = new();
            string internalServerErrorMsg = _config[
                "ResponseMessages:CommonMsg:InternalServerErrorMsg"
            ];

            string charityUnitDoNotAllowMsg = _config[
                "ResponseMessages:CharityMsg:CharityUnitDoNotAllowMsg"
            ];
            string charityRegisterSuccessMsg = _config[
                "ResponseMessages:CharityUnit:CharityRegisterSuccessMsg"
            ];
            string emailOrPhoneAlreadyExistedMsg = _config[
                "ResponseMessages:CharityMsg:EmailOrPhoneAlreadyExistedMsg"
            ];
            try
            {
                bool check = await CheckUserAvalbleToCreateCharityUnit(userId);
                if (!check)
                {
                    commonResponse.Message =
                        "Chỉ có chi nhánh chính mới có quyền thực hiện hành động này";
                    commonResponse.Status = 400;
                    return commonResponse;
                }
                CharityUnit? headQuater = await _charityUnitRepository.FindCharityUnitByUserIdAsync(
                    userId
                );
                CharityUnit charityUnit = new();

                charityUnit.CharityId = headQuater!.CharityId;
                charityUnit.Address = request.Address;
                charityUnit.Name = request.Name;
                charityUnit.Description = request.Description;
                if (request.Location != null && request.Location.Length >= 2)
                {
                    charityUnit.Location =
                        request.Location[0].ToString() + ", " + request.Location[1].ToString();
                }
                charityUnit.Status = CharityUnitStatus.UNVERIFIED;
                charityUnit.CreatedDate = SettedUpDateTime.GetCurrentVietNamTime();
                using (var stream = request.LegalDocument.OpenReadStream())
                {
                    string imageName =
                        Guid.NewGuid().ToString()
                        + Path.GetExtension(request.LegalDocument.FileName);
                    string imageUrl = await _firebaseStorageService.UploadImageToFirebase(
                        stream,
                        imageName
                    );
                    charityUnit.LegalDocuments = imageUrl;
                }
                ;
                charityUnit.Status = CharityUnitStatus.UNVERIFIED;

                using (var stream = request.Image.OpenReadStream())
                {
                    string imageName =
                        Guid.NewGuid().ToString() + Path.GetExtension(request.Image.FileName);
                    string imageUrl = await _firebaseStorageService.UploadImageToFirebase(
                        stream,
                        imageName
                    );
                    charityUnit.Image = imageUrl;
                }

                User? userWithEmail = await _userRepository.FindUserByEmailOrPhoneAsync(
                    request.Email
                );
                User? userWithPhone = await _userRepository.FindUserByEmailOrPhoneAsync(
                    request.Phone
                );
                using (var scope = new TransactionScope(TransactionScopeAsyncFlowOption.Enabled))
                {
                    if (userWithEmail != null || userWithPhone != null)
                    {
                        commonResponse.Status = 400;
                        commonResponse.Message = emailOrPhoneAlreadyExistedMsg;
                        return commonResponse;
                    }
                    else
                    {
                        User? user = await _userRepository.CreateUserAsync(
                            await CreateAccountForCharityUnit(
                                request.Email,
                                request.Phone,
                                request.Name,
                                charityUnit.Image,
                                charityUnit.Location,
                                request.Address
                            )
                        );
                        if (user == null)
                            throw new Exception();
                        charityUnit.UserId = user.Id;
                    }
                    int rs = await _charityUnitRepository.UpdateCharityUnitAsync(charityUnit);
                    if (rs > 0)
                    {
                        commonResponse.Message = charityRegisterSuccessMsg;
                        commonResponse.Status = 200;
                        scope.Complete();
                    }
                }
            }
            catch (Exception ex)
            {
                string className = nameof(CollaboratorService);
                string methodName = nameof(UpdateCharityUnit);
                _logger.LogError(
                    ex,
                    "An error occurred in {ClassName}.{MethodName}: {ErrorMessage}",
                    className,
                    methodName,
                    ex.Message
                );
                commonResponse.Message = internalServerErrorMsg;
                commonResponse.Status = 500;
            }
            return commonResponse;
        }

        public async Task<CommonResponse> UpdateCharityUnit(
            CharityUnitUpdatingRequest request,
            Guid userId
        )
        {
            CommonResponse commonResponse = new();
            string internalServerErrorMsg = _config[
                "ResponseMessages:CommonMsg:InternalServerErrorMsg"
            ];

            string charityUnitDoNotAllowMsg = _config[
                "ResponseMessages:CharityMsg:CharityUnitDoNotAllowMsg"
            ];

            try
            {
                //bool check = await CheckUserAvalbleToCreateCharityUnit(userId);
                //if (!check)
                //{
                //    commonResponse.Status = 400;
                //    commonResponse.Message =
                //        "Chỉ có chi nhánh chính mới có quyền thực hiện hành động này.";
                //    return commonResponse;
                //}
                CharityUnit? charityUnit =
                    await _charityUnitRepository.FindCharityUnitByUserIdAsync(userId);
                if (charityUnit != null && charityUnit.Status == CharityUnitStatus.ACTIVE)
                {
                    //if (request.Email != null)
                    //{
                    //    User? userWithEmail = await _userRepository.FindUserByEmailOrPhoneAsync(
                    //        request.Email
                    //    );
                    //    if (userWithEmail != null && userWithEmail.Id != charityUnit.UserId)
                    //    {
                    //        commonResponse.Status = 400;
                    //        commonResponse.Message = "Email đã tồn tại.";
                    //        return commonResponse;
                    //    }
                    //}

                    //if (request.Phone != null)
                    //{
                    //    User? userWithPhone = await _userRepository.FindUserByEmailOrPhoneAsync(
                    //        request.Phone
                    //    );
                    //    if (userWithPhone != null && userWithPhone.Id != charityUnit.UserId)
                    //    {
                    //        commonResponse.Status = 400;
                    //        commonResponse.Message = "Số điện thoại đã tồn tại.";
                    //        return commonResponse;
                    //    }
                    //}
                    CharityUnit newCharityUnit = new() { };

                    newCharityUnit.Address = !string.IsNullOrEmpty(request.Address)
                        ? request.Address
                        : charityUnit.Address;
                    newCharityUnit.Name = !string.IsNullOrEmpty(request.Name)
                        ? request.Name
                        : charityUnit.Name;
                    newCharityUnit.Description = !string.IsNullOrEmpty(request.Description)
                        ? request.Description
                        : charityUnit.Description;
                    if (request.Location != null && request.Location.Length >= 2)
                    {
                        newCharityUnit.Location =
                            request.Location[0].ToString() + ", " + request.Location[1].ToString();
                    }
                    else
                    {
                        newCharityUnit.Location = charityUnit.Location;
                    }
                    if (request.LegalDocument != null)
                    {
                        using (var stream = request.LegalDocument.OpenReadStream())
                        {
                            string imageName =
                                Guid.NewGuid().ToString()
                                + Path.GetExtension(request.LegalDocument.FileName);
                            string imageUrl = await _firebaseStorageService.UploadImageToFirebase(
                                stream,
                                imageName
                            );
                            newCharityUnit.LegalDocuments = imageUrl;
                        }
                    }
                    else
                    {
                        newCharityUnit.LegalDocuments = charityUnit.LegalDocuments;
                    }
                    ;

                    if (request.Image != null)
                    {
                        using (var stream = request.Image.OpenReadStream())
                        {
                            string imageName =
                                Guid.NewGuid().ToString()
                                + Path.GetExtension(request.Image.FileName);
                            string imageUrl = await _firebaseStorageService.UploadImageToFirebase(
                                stream,
                                imageName
                            );
                            newCharityUnit.Image = imageUrl;
                        }
                    }
                    else
                    {
                        newCharityUnit.Image = charityUnit.Image;
                    }

                    newCharityUnit.Status = CharityUnitStatus.UNVERIFIED_UPDATE;
                    newCharityUnit.CreatedDate = SettedUpDateTime.GetCurrentVietNamTime();

                    newCharityUnit.CharityId = charityUnit.CharityId;
                    newCharityUnit.UserId = charityUnit.UserId;
                    newCharityUnit.IsHeadquarter = charityUnit.IsHeadquarter;

                    if (request.Address != null)
                    {
                        newCharityUnit.Address = request.Address;
                    }
                    else
                    {
                        newCharityUnit.Address = charityUnit.Address;
                    }

                    CharityUnit? check =
                        await _charityUnitRepository.FindUnverifyUpdateCharityUnitByUserIdAsync(
                            userId
                        );

                    if (check != null)
                    {
                        check.Address = newCharityUnit.Address;
                        check.LegalDocuments = newCharityUnit.LegalDocuments;
                        check.Status = newCharityUnit.Status;
                        check.Name = newCharityUnit.Name;
                        check.CharityId = newCharityUnit.CharityId;
                        check.UserId = newCharityUnit.UserId;
                        check.Name = newCharityUnit.Name;
                        check.CreatedDate = newCharityUnit.CreatedDate;
                        check.Description = newCharityUnit.Description;
                        check.Image = newCharityUnit.Image;
                        check.IsHeadquarter = newCharityUnit.IsHeadquarter;
                        check.IsHeadquarter = newCharityUnit.IsHeadquarter;

                        int rs = await _charityUnitRepository.UpdateCharityUnitAsync(check);

                        if (rs > 0)
                        {
                            commonResponse.Status = 200;
                        }
                    }
                    else
                    {
                        newCharityUnit.IsHeadquarter = false;
                        int rs = await _charityUnitRepository.CreateCharityUnitAsync(
                            newCharityUnit
                        );

                        if (rs > 0)
                        {
                            commonResponse.Status = 200;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                string className = nameof(CollaboratorService);
                string methodName = nameof(UpdateCharityUnit);
                _logger.LogError(
                    ex,
                    "An error occurred in {ClassName}.{MethodName}: {ErrorMessage}",
                    className,
                    methodName,
                    ex.Message
                );
                commonResponse.Message = internalServerErrorMsg;
                commonResponse.Status = 500;
            }
            return commonResponse;
        }

        public async Task<CommonResponse> ConfirmCharityUnit(
            Guid charityUnitId,
            ConfirmCharityUnitRequest request,
            Guid userId
        )
        {
            string internalServerErrorMsg = _config[
                "ResponseMessages:CommonMsg:InternalServerErrorMsg"
            ];
            CommonResponse commonResponse = new();
            try
            {
                CharityUnit? charityUnit =
                    await _charityUnitRepository.FindUnVerifyCharityUnitsByIdAsync(charityUnitId);
                if (charityUnit != null)
                {
                    using (
                        var scope = new TransactionScope(TransactionScopeAsyncFlowOption.Enabled)
                    )
                    {
                        if (
                            !request.isAccept
                            && request.reason != null
                            && charityUnit.Status == CharityUnitStatus.UNVERIFIED
                        )
                        {
                            int rs = await _charityUnitRepository.DeleteCharityUnitAsync(
                                charityUnit
                            );

                            //// gửi email
                            await _emailService.SendNotificationForDenyCharityUnitUpdateEmail(
                                charityUnit.User.Email,
                                charityUnit.Name,
                                request.reason
                            );
                            scope.Complete();
                            commonResponse.Status = 200;
                            commonResponse.Message = "Cập nhật thành công";
                        }
                        else if (
                            request.isAccept && charityUnit.Status == CharityUnitStatus.UNVERIFIED
                        )
                        {
                            charityUnit.Status = CharityUnitStatus.ACTIVE;
                            int rs = await _charityUnitRepository.UpdateCharityUnitAsync(
                                charityUnit
                            );
                            if (rs < 0)
                                throw new Exception();
                            await ActiveAccoutForCharity(charityUnit.UserId, userId);
                        }
                        else
                        {
                            commonResponse.Message = "Bạn không được phép thực hiện hành động này";
                            commonResponse.Status = 400;
                        }
                        scope.Complete();
                        commonResponse.Status = 200;
                        commonResponse.Message = "Cập nhật thành công";
                    }
                }
            }
            catch (Exception ex)
            {
                string className = nameof(CollaboratorService);
                string methodName = nameof(UpdateCharityUnit);
                _logger.LogError(
                    ex,
                    "An error occurred in {ClassName}.{MethodName}: {ErrorMessage}",
                    className,
                    methodName,
                    ex.Message
                );
                commonResponse.Message = internalServerErrorMsg;
                commonResponse.Status = 500;
            }
            return commonResponse;
        }

        public async Task<CommonResponse> ConfirmUpdateCharityUnit(
            Guid charityUnitId,
            ConfirmCharityUnitRequest request,
            Guid confirmById
        )
        {
            string internalServerErrorMsg = _config[
                "ResponseMessages:CommonMsg:InternalServerErrorMsg"
            ];
            CommonResponse commonResponse = new();
            try
            {
                CharityUnit? charityUnit = await _charityUnitRepository.FindCharityUnitByIdAsync(
                    charityUnitId
                );

                if (charityUnit != null)
                {
                    using (
                        var scope = new TransactionScope(TransactionScopeAsyncFlowOption.Enabled)
                    )
                    {
                        if (
                            !request.isAccept
                            && request.reason != null
                            && charityUnit.Status == CharityUnitStatus.UNVERIFIED_UPDATE
                        )
                        {
                            int rs = await _charityUnitRepository.DeleteCharityUnitAsync(
                                charityUnit
                            );

                            //// gửi email
                            await _emailService.SendNotificationForDenyCharityUnitUpdateEmail(
                                charityUnit.User.Email,
                                charityUnit.Name,
                                request.reason
                            );
                            scope.Complete();
                            commonResponse.Status = 200;
                            commonResponse.Message = "Cập nhật thành công";
                        }
                        else if (
                            request.isAccept
                            && charityUnit.Status == CharityUnitStatus.UNVERIFIED_UPDATE
                        )
                        {
                            CharityUnit? updatedCharityUnit =
                                await _charityUnitRepository.FindActiveCharityUnitsByUserIdAsync(
                                    charityUnit.UserId
                                );
                            if (updatedCharityUnit != null)
                            {
                                updatedCharityUnit.Status = CharityUnitStatus.ACTIVE;
                                updatedCharityUnit.Name = charityUnit.Name;
                                updatedCharityUnit.Address = charityUnit.Address;
                                updatedCharityUnit.CharityId = charityUnit.CharityId;
                                updatedCharityUnit.LegalDocuments = charityUnit.LegalDocuments;
                                updatedCharityUnit.UserId = charityUnit.UserId;
                                updatedCharityUnit.Description = charityUnit.Description;
                                updatedCharityUnit.CreatedDate = charityUnit.CreatedDate;
                                updatedCharityUnit.IsHeadquarter = charityUnit.IsHeadquarter;

                                int rs = await _charityUnitRepository.UpdateCharityUnitAsync(
                                    updatedCharityUnit
                                );
                                await _charityUnitRepository.DeleteCharityUnitAsync(charityUnit);
                                User? user = await _userRepository.FindUserByIdAsync(
                                    charityUnit.UserId
                                );

                                if (user != null)
                                {
                                    user.Address = charityUnit.Address;
                                    user.Location = charityUnit.Location;
                                    user.Name = charityUnit.Name;
                                    user.Avatar = charityUnit.Image;
                                    await _userRepository.UpdateUserAsync(user);
                                }
                                scope.Complete();
                                commonResponse.Status = 200;
                                commonResponse.Message = "Cập nhật thành công";
                            }
                            else
                            {
                                commonResponse.Status = 400;
                                commonResponse.Message = "Không tìm thấy charity unit.";
                                return commonResponse;
                            }
                        }
                    }
                }
                else
                {
                    commonResponse.Status = 400;
                    commonResponse.Message = "Không tìm thấy charity unit.";
                    return commonResponse;
                }
            }
            catch (Exception ex)
            {
                string className = nameof(CollaboratorService);
                string methodName = nameof(UpdateCharityUnit);
                _logger.LogError(
                    ex,
                    "An error occurred in {ClassName}.{MethodName}: {ErrorMessage}",
                    className,
                    methodName,
                    ex.Message
                );
                commonResponse.Message = internalServerErrorMsg;
                commonResponse.Status = 500;
            }
            return commonResponse;
        }

        private async Task<User> CreateAccountForCharityUnit(
            string email,
            string phone,
            string name,
            string avatarUrl,
            string loacation,
            string address
        )
        {
            User user = new User();
            user.Email = email;
            user.Phone = phone;

            user.Avatar = avatarUrl;
            user.Name = name;
            user.Location = loacation;
            user.Address = address;
            user.Password = _passwordHasher.GenerateNewPassword();
            user.Role = (await _roleRepository.GetRoleByName(RoleEnum.CHARITY.ToString()))!;
            user.Status = UserStatus.UNVERIFIED;

            return user;
        }

        private async Task ActiveAccoutForCharity(Guid userId, Guid creatorId)
        {
            User? user = await _userRepository.FindUserByIdAsync(userId);
            string password = _passwordHasher.GenerateNewPassword();
            if (user != null)
            {
                await UpdateUserPermissionForCharity(user, UserPermissionStatus.PERMITTED);
                user.Status = UserStatus.ACTIVE;
                user.Password = _passwordHasher.Hash(password);
                await _userRepository.UpdateUserAsync(user);
            }
            await _emailService.SendNotificationForCreatingAccountForCharityUnitEmail(
                user!.Email,
                user.Email,
                user.Phone,
                password
            );
        }

        private async Task UpdateUserPermissionForCharity(User user, UserPermissionStatus status)
        {
            Role? role = await _roleRepository.GetRoleByName(RoleEnum.CHARITY.ToString());

            if (role != null)
            {
                List<Permission>? rolePermissions =
                    await _rolePermissionRepository.GetPermissionsByRoleIdAsync(role.Id);
                if (user != null && rolePermissions!.Count > 0)
                {
                    foreach (var permission in rolePermissions)
                    {
                        UserPermission? tmp =
                            await _userPermissionRepository.UpdateOrCreateUserPermissionAsync(
                                user.Id,
                                permission!.Id,
                                status
                            );
                        if (tmp == null)
                            throw new Exception("Excution sql failed");
                    }
                }
            }
            else
            {
                throw new Exception("User or permission not found.");
            }
        }

        private async Task<bool> CheckUserAvalbleToCreateCharityUnit(Guid userId)
        {
            try
            {
                CharityUnit? charityUnit =
                    await _charityUnitRepository.FindCharityUnitByUserIdAsync(userId);

                if (charityUnit != null && charityUnit.IsHeadquarter == true)
                {
                    return true;
                }
            }
            catch (Exception ex)
            {
                string className = nameof(CollaboratorService);
                string methodName = nameof(CheckUserAvalbleToCreateCharityUnit);
                _logger.LogError(
                    ex,
                    "An error occurred in {ClassName}.{MethodName}: {ErrorMessage}",
                    className,
                    methodName,
                    ex.Message
                );
            }
            return false;
        }

        public async Task<CommonResponse> GetCharityUnitDetails(Guid charityUnitId)
        {
            CommonResponse commonResponse = new();
            string internalServerErrorMsg = _config[
                "ResponseMessages:CommonMsg:InternalServerErrorMsg"
            ];
            try
            {
                CharityUnit? charityUnit =
                    await _charityUnitRepository.FindCharityUnitsByIdForUserAsync(charityUnitId);

                if (charityUnit != null)
                {
                    int numberOfPost = _postRepository.countPostByUserId(charityUnit!.UserId);
                    var res = new
                    {
                        charityUnit.Id,
                        charityUnit.Name,
                        Status = charityUnit.Status.ToString(),
                        charityUnit.User.Email,
                        charityId = charityUnit.CharityId,
                        charityUnit.User.Phone,
                        charityUnit.Description,
                        charityUnit.Image,
                        charityUnit.LegalDocuments,
                        charityUnit.Location,
                        charityUnit.Address,
                        NumberOfPost = numberOfPost,
                    };

                    commonResponse.Data = res;
                }
                commonResponse.Status = 200;
            }
            catch (Exception ex)
            {
                string className = nameof(CollaboratorService);
                string methodName = nameof(UpdateCharityUnit);
                _logger.LogError(
                    ex,
                    "An error occurred in {ClassName}.{MethodName}: {ErrorMessage}",
                    className,
                    methodName,
                    ex.Message
                );
                commonResponse.Message = internalServerErrorMsg;
                commonResponse.Status = 500;
            }
            return commonResponse;
        }

        public async Task<CommonResponse> GetCharityUnitDetailsByUserIdAndStatusForAdmin(
            Guid userId,
            CharityUnitStatus status
        )
        {
            CommonResponse commonResponse = new();
            string internalServerErrorMsg = _config[
                "ResponseMessages:CommonMsg:InternalServerErrorMsg"
            ];
            try
            {
                CharityUnit? charityUnit =
                    await _charityUnitRepository.FindCharityUnitsByUserAndStatusIdAsync(
                        userId,
                        status
                    );

                if (charityUnit != null)
                {
                    var res = new
                    {
                        charityUnit.Id,
                        charityUnit.Name,
                        Status = charityUnit.Status.ToString(),
                        charityUnit.User.Email,
                        charityId = charityUnit.CharityId,
                        charityUnit.User.Phone,
                        charityUnit.Description,
                        charityUnit.Image,
                        charityUnit.LegalDocuments,
                        charityUnit.Location,
                        charityUnit.Address,
                        charityUnit.UserId
                    };

                    commonResponse.Data = res;
                }
                commonResponse.Status = 200;
            }
            catch (Exception ex)
            {
                string className = nameof(CollaboratorService);
                string methodName = nameof(UpdateCharityUnit);
                _logger.LogError(
                    ex,
                    "An error occurred in {ClassName}.{MethodName}: {ErrorMessage}",
                    className,
                    methodName,
                    ex.Message
                );
                commonResponse.Message = internalServerErrorMsg;
                commonResponse.Status = 500;
            }
            return commonResponse;
        }

        public async Task<CommonResponse> GetLatestCharityUnitUpdateVersion(Guid userId)
        {
            string internalServerErrorMsg = _config[
                "ResponseMessages:CommonMsg:InternalServerErrorMsg"
            ];
            CommonResponse commonResponse = new();
            try
            {
                CharityUnit? charityUnit =
                    await _charityUnitRepository.GetLatestUpdateCharityUnitByUserIdForAdminAsync(
                        userId
                    );
                //User? updatedByUser;
                if (charityUnit != null)
                {
                    var res = new
                    {
                        charityUnit.Name,
                        CreateDate = charityUnit.CreatedDate,
                        charityUnit.Location,
                        charityUnit.Address,
                        charityUnit.LegalDocuments
                    };
                    commonResponse.Data = res;
                }

                commonResponse.Status = 200;
            }
            catch (Exception ex)
            {
                string className = nameof(CollaboratorService);
                string methodName = nameof(UpdateCharityUnit);
                _logger.LogError(
                    ex,
                    "An error occurred in {ClassName}.{MethodName}: {ErrorMessage}",
                    className,
                    methodName,
                    ex.Message
                );
                commonResponse.Message = internalServerErrorMsg;
                commonResponse.Status = 500;
            }
            return commonResponse;
        }

        public async Task<CommonResponse> GetCharityUnit(
            string? searchKeyWord,
            CharityUnitStatus? status,
            Guid? charityId,
            int? page,
            int? pageSize,
            SortType? sortType
        )
        {
            string internalServerErrorMsg = _config[
                "ResponseMessages:CommonMsg:InternalServerErrorMsg"
            ];
            CommonResponse commonResponse = new();
            try
            {
                List<CharityUnit>? charityUnits = await _charityUnitRepository.GetCharityUnit(
                    searchKeyWord,
                    status,
                    charityId
                );

                if (charityUnits != null && charityUnits.Count > 0)
                {
                    Pagination pagination = new();
                    pagination.PageSize = pageSize == null ? 10 : pageSize.Value;
                    pagination.CurrentPage = page == null ? 1 : page.Value;
                    pagination.Total = charityUnits.Count;
                    charityUnits = charityUnits
                        .Skip((pagination.CurrentPage - 1) * pagination.PageSize)
                        .Take(pagination.PageSize)
                        .ToList();
                    commonResponse.Pagination = pagination;
                    var rs = charityUnits.Select(
                        cu =>
                            new
                            {
                                cu.Name,
                                Status = cu.Status.ToString(),
                                cu.User.Email,
                                cu.Id,
                                cu.User.Phone,
                                cu.Description,
                                cu.Image,
                                cu.LegalDocuments,
                                cu.Location,
                                cu.Address,
                                cu.CreatedDate,
                                isHeadQuater = cu.IsHeadquarter
                            }
                    );
                    if (sortType == SortType.ASC)
                    {
                        rs = rs.OrderBy(u => u.Name).ToList();
                    }
                    else
                    {
                        rs = rs.OrderByDescending(u => u.Name).ToList();
                    }
                    commonResponse.Data = rs;
                    commonResponse.Pagination = pagination;
                }
                commonResponse.Status = 200;
            }
            catch (Exception ex)
            {
                string className = nameof(CollaboratorService);
                string methodName = nameof(UpdateCharityUnit);
                _logger.LogError(
                    ex,
                    "An error occurred in {ClassName}.{MethodName}: {ErrorMessage}",
                    className,
                    methodName,
                    ex.Message
                );
                commonResponse.Message = internalServerErrorMsg;
                commonResponse.Status = 500;
            }
            return commonResponse;
        }

        public async Task<CommonResponse> DeleteCharityUnit(Guid charrityUnitId)
        {
            string internalServerErrorMsg = _config[
                "ResponseMessages:CommonMsg:InternalServerErrorMsg"
            ];
            string UpdateSuccessMsg = _config["ResponseMessages:CharityMsg:UpdateSuccessMsg"];
            string CharityNotFoundMsg = _config["ResponseMessages:CharityMsg:CharityNotFoundMsg"];
            CommonResponse commonResponse = new();
            try
            {
                using (var scope = new TransactionScope(TransactionScopeAsyncFlowOption.Enabled))
                {
                    CharityUnit? charityUnit =
                        await _charityUnitRepository.FindCharityUnitByIdAsync(charrityUnitId);
                    if (charityUnit != null)
                    {
                        charityUnit.Status = CharityUnitStatus.DELETED;
                        int rs = await _charityUnitRepository.UpdateCharityUnitAsync(charityUnit);
                        await BanAccount(charityUnit.User);
                        scope.Complete();
                        commonResponse.Status = 200;
                        commonResponse.Message = UpdateSuccessMsg;
                    }
                    else
                    {
                        commonResponse.Status = 400;
                        commonResponse.Message = CharityNotFoundMsg;
                    }
                }
            }
            catch (Exception ex)
            {
                string className = nameof(CollaboratorService);
                string methodName = nameof(DeleteCharityUnit);
                _logger.LogError(
                    ex,
                    "An error occurred in {ClassName}.{MethodName}: {ErrorMessage}",
                    className,
                    methodName,
                    ex.Message
                );
                commonResponse.Message = internalServerErrorMsg;
                commonResponse.Status = 500;
            }
            return commonResponse;
        }

        private async Task BanAccount(User user)
        {
            user.Status = UserStatus.INACTIVE;
            foreach (var up in user.UserPermissions)
            {
                up.Status = UserPermissionStatus.BANNED;
            }
            await _userRepository.UpdateUserAsync(user);
        }
    }
}
