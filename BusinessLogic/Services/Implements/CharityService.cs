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
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System.Transactions;

namespace BusinessLogic.Services.Implements
{
    public class CharityService : ICharityService
    {
        private readonly ICharityRepository _charityRepository;
        private readonly ICharityUnitRepository _charityUnitRepository;
        private readonly IConfiguration _config;
        private readonly ILogger<CharityService> _logger;
        private readonly IFirebaseStorageService _firebaseStorageService;
        private readonly IUserRepository _userRepository;
        private readonly IPasswordHasher _passwordHasher;
        private readonly IRoleRepository _roleRepository;
        private readonly IRolePermissionRepository _rolePermissionRepository;
        private readonly IUserPermissionRepository _userPermissionRepository;
        private readonly IEmailService _emailService;
        private readonly IPostRepository _postRepository;

        public CharityService(
            ICharityRepository charityRepository,
            IConfiguration configuration,
            ILogger<CharityService> logger,
            IFirebaseStorageService firebaseStorageService,
            IUserRepository userRepository,
            IPasswordHasher passwordHasher,
            IRoleRepository roleRepository,
            IRolePermissionRepository rolePermissionRepository,
            IUserPermissionRepository userPermissionRepository,
            IEmailService emailService,
            ICharityUnitRepository charityUnitRepository,
            IPostRepository postRepository
        )
        {
            _charityRepository = charityRepository;
            _config = configuration;
            _logger = logger;
            _firebaseStorageService = firebaseStorageService;
            _userRepository = userRepository;
            _passwordHasher = passwordHasher;
            _roleRepository = roleRepository;
            _rolePermissionRepository = rolePermissionRepository;
            _userPermissionRepository = userPermissionRepository;
            _emailService = emailService;
            _charityUnitRepository = charityUnitRepository;
            _postRepository = postRepository;
        }

        public async Task<CommonResponse> GetCharitiesAsync(
            int? page,
            int? pageSize,
            CharityStatus? charityStatus,
            SortType? sortType,
            string? name,
            bool isWaitingToUpdate
        )
        {
            CommonResponse commonResponse = new CommonResponse();
            string internalServerErrorMsg = _config[
                "ResponseMessages:CommonMsg:InternalServerErrorMsg"
            ];

            try
            {
                List<Charity> charities = await _charityRepository.GetCharitiesAsync(
                    charityStatus,
                    name,
                    isWaitingToUpdate
                );
                if (charities != null && charities.Count > 0)
                {
                    Pagination pagination = new Pagination();
                    pagination.PageSize = pageSize == null ? 10 : pageSize.Value;
                    pagination.CurrentPage = page == null ? 1 : page.Value;
                    pagination.Total = charities.Count;
                    charities = charities
                        .Skip((pagination.CurrentPage - 1) * pagination.PageSize)
                        .Take(pagination.PageSize)
                        .ToList();
                    var rs = charities.Select(
                        c =>
                            new CharityResponse
                            {
                                Id = c.Id,
                                Name = c.Name,
                                CreatedDate = c.CreatedDate,
                                Status = c.Status.ToString(),
                                Logo = c.Logo,
                                Description = c.Description,
                                NumberOfCharityUnits = c.CharityUnits
                                    .Where(p => p.Status == CharityUnitStatus.ACTIVE)
                                    .Count(),
                                isWattingToUpdate =
                                    c.CharityUnits
                                        .Where(p => p.Status == CharityUnitStatus.UNVERIFIED_UPDATE)
                                        .Count() > 0
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
                string className = nameof(CharityService);
                string methodName = nameof(GetCharitiesAsync);
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

        public async Task<CommonResponse> RegisterToBecomeCharity(CharityCreatingRequest request)
        {
            CommonResponse commonResponse = new CommonResponse();
            string internalServerErrorMsg = _config[
                "ResponseMessages:CommonMsg:InternalServerErrorMsg"
            ];
            string charityRegisterSuccessMsg = _config[
                "ResponseMessages:CharityMsg:CreateSuccessMsg"
            ];
            string emailOrPhoneAlreadyExistedMsg = _config[
                "ResponseMessages:CharityMsg:EmailOrPhoneAlreadyExistedMsg"
            ];
            string charityEmailExistedMsg = _config[
                "ResponseMessages:CharityMsg:CharityEmailExistedMsg"
            ];
            Role? branchAdminRole = await _roleRepository.GetRoleByName(
                RoleEnum.CHARITY.ToString()
            );

            try
            {
                Charity? tmp = await _charityRepository.GetCharityByEmail(request.Email);
                if (tmp != null)
                {
                    commonResponse.Status = 400;
                    commonResponse.Message = charityEmailExistedMsg;
                    return commonResponse;
                }

                Charity charity = new Charity();
                charity.Status = CharityStatus.UNVERIFIED;
                charity.Description = request.Description;
                charity.Name = request.Name;
                using (var stream = request.Logo.OpenReadStream())
                {
                    string imageName =
                        Guid.NewGuid().ToString() + Path.GetExtension(request.Logo.FileName);
                    string imageUrl = await _firebaseStorageService.UploadImageToFirebase(
                        stream,
                        imageName
                    );
                    charity.Logo = imageUrl;
                }
                charity.CreatedDate = SettedUpDateTime.GetCurrentVietNamTime();
                charity.Email = request.Email;
                using (var scope = new TransactionScope(TransactionScopeAsyncFlowOption.Enabled))
                {
                    List<CharityUnit>? charityUnits = await GetCharityUnitFromRequest(request);
                    if (charityUnits != null && charityUnits.Count > 0)
                    {
                        charity.CharityUnits = charityUnits;
                        int rs = await _charityRepository.CreateCharityAsync(charity);
                        if (rs > 0)
                        {
                            commonResponse.Status = 200;
                            commonResponse.Message = charityRegisterSuccessMsg;
                            scope.Complete();
                        }
                        else
                            throw new Exception("Error excute sql");
                    }
                    else if (charityUnits == null)
                    {
                        commonResponse.Status = 400;
                        commonResponse.Message = emailOrPhoneAlreadyExistedMsg;
                        return commonResponse;
                    }
                }
            }
            catch (Exception ex)
            {
                string className = nameof(CharityService);
                string methodName = nameof(RegisterToBecomeCharity);
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

        private async Task<List<CharityUnit>> GetCharityUnitFromRequest(
            CharityCreatingRequest request
        )
        {
            List<CharityUnit> charityUnits = new List<CharityUnit>();
            foreach (var cu in request.CharityUnits)
            {
                CharityUnit charityUnit = new CharityUnit();
                charityUnit.Address = cu.Address;
                if (cu.Location != null && cu.Location.Length >= 2)
                {
                    charityUnit.Location =
                        cu.Location[0].ToString() + ", " + cu.Location[1].ToString();
                }
                charityUnit.Description = cu.Description;
                charityUnit.Name = cu.Name;
                using (var stream = cu.LegalDocument.OpenReadStream())
                {
                    string imageName =
                        Guid.NewGuid().ToString() + Path.GetExtension(cu.LegalDocument.FileName);
                    string imageUrl = await _firebaseStorageService.UploadImageToFirebase(
                        stream,
                        imageName
                    );
                    charityUnit.LegalDocuments = imageUrl;
                }
                ;
                charityUnit.Status = CharityUnitStatus.UNVERIFIED;

                using (var stream = cu.Image.OpenReadStream())
                {
                    string imageName =
                        Guid.NewGuid().ToString() + Path.GetExtension(cu.Image.FileName);
                    string imageUrl = await _firebaseStorageService.UploadImageToFirebase(
                        stream,
                        imageName
                    );
                    charityUnit.Image = imageUrl;
                }
                ;
                User? userWithEmail = await _userRepository.FindUserByEmailOrPhoneAsync(cu.Email);
                User? userWithPhone = await _userRepository.FindUserByEmailOrPhoneAsync(cu.Phone);
                if (userWithEmail != null || userWithPhone != null)
                {
                    return charityUnits;
                }
                else
                {
                    charityUnit.User = await CreateAccountForCharityUnit(
                        cu.Email,
                        cu.Phone,
                        cu.Name,
                        charityUnit.Image,
                        charityUnit.Location,
                        cu.Address
                    );
                }
                if (cu.IsHeadquarter != null)
                {
                    charityUnit.IsHeadquarter = cu.IsHeadquarter;
                }
                else
                {
                    charityUnit.IsHeadquarter = false;
                }
                await _userRepository.CreateUserAsync(charityUnit.User);
                charityUnits.Add(charityUnit);
            }
            return charityUnits;
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

        private async Task<string> GetImageUrl(IFormFile image)
        {
            using (var stream = image.OpenReadStream())
            {
                string imageName = Guid.NewGuid().ToString() + Path.GetExtension(image.FileName);
                string imageUrl = await _firebaseStorageService.UploadImageToFirebase(
                    stream,
                    imageName
                );
                return imageUrl;
            }
        }

        private async Task<string> GetListLegalDocumentOfCharityUnit(List<IFormFile> formFiles)
        {
            List<string> imageUrls = new List<string>();
            foreach (IFormFile image in formFiles)
            {
                using (var stream = image.OpenReadStream())
                {
                    string imageName =
                        Guid.NewGuid().ToString() + Path.GetExtension(image.FileName);
                    string imageUrl = await _firebaseStorageService.UploadImageToFirebase(
                        stream,
                        imageName
                    );
                    imageUrls.Add(imageUrl);
                }
            }
            return string.Join(",", imageUrls);
        }

        private string GetListLegalDocumentOfCharityUnitV2(List<string> formFiles)
        {
            return string.Join(",", formFiles);
        }

        public async Task<CommonResponse> ConfirmCharity(
            Guid charityId,
            ConfirmCharityRequest request,
            Guid userId
        )
        {
            string UpdateSuccessMsg = _config["ResponseMessages:CharityMsg:UpdateSuccessMsg"];
            CommonResponse commonResponse = new CommonResponse();
            string internalServerErrorMsg = _config[
                "ResponseMessages:CommonMsg:InternalServerErrorMsg"
            ];

            try
            {
                Charity? charity = await _charityRepository.GetCharityById(charityId);
                if (charity != null)
                {
                    using (
                        var scope = new TransactionScope(TransactionScopeAsyncFlowOption.Enabled)
                    )
                    {
                        if (
                            !request.isAccept
                            && request.reason != null
                            && charity.Status == CharityStatus.UNVERIFIED
                        )
                        {
                            int rs = await _charityRepository.DeleteCharityAsync(charity);
                            await DeLeteAccoutForCharity(charity);
                            // gửi email
                            await _emailService.SendNotificationAboutDenyCharity(
                                charity.Email,
                                charity.Name,
                                request.reason
                            );
                        }
                        else if (request.isAccept && charity.Status == CharityStatus.UNVERIFIED)
                        {
                            int rs = await ActiveCharityUnit(charity, userId, userId);
                            if (rs < 0)
                                throw new Exception();
                            charity.Status = CharityStatus.ACTIVE;
                            await _charityRepository.UpdateCharityAsync(charity);
                            await ActiveAccoutForCharity(charity, userId);
                        }
                        else
                        {
                            commonResponse.Message = "Bạn không được phép thực hiện hành động này";
                            commonResponse.Status = 400;
                        }
                        scope.Complete();
                        commonResponse.Status = 200;
                        commonResponse.Message = UpdateSuccessMsg;
                    }
                }
            }
            catch (Exception ex)
            {
                string className = nameof(CollaboratorService);
                string methodName = nameof(ConfirmCharity);
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

        private async Task ActiveAccoutForCharity(Charity charity, Guid creatorId)
        {
            foreach (var u in charity.CharityUnits)
            {
                User? user = await _userRepository.FindUserByIdAsync(u.UserId);
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
        }

        private async Task<int> ActiveCharityUnit(Charity charity, Guid updateBy, Guid confirmBy)
        {
            foreach (CharityUnit cu in charity.CharityUnits)
            {
                cu.Status = CharityUnitStatus.ACTIVE;
                int rs = await _charityUnitRepository.UpdateCharityUnitAsync(cu);
                if (rs < 0)
                {
                    return 0;
                }
            }
            return 1;
        }

        private async Task DeLeteAccoutForCharity(Charity charity)
        {
            foreach (var u in charity.CharityUnits)
            {
                User? user = await _userRepository.FindUserByIdAsync(u.UserId);
                if (user != null)
                {
                    await _userRepository.DeleteUserAsync(user);
                }
            }
        }

        private async Task UpdateUserPermissionForCharity(User user, UserPermissionStatus status)
        {
            Role? role = await _roleRepository.GetRoleByName(RoleEnum.CHARITY.ToString());

            if (role != null)
            {
                List<Permission?>? rolePermissions = (
                    await _rolePermissionRepository.GetPermissionsByRoleIdAsync(role.Id)
                )!;
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

        //    public async Task<CommonResponse> SendUpdateCharityRequest(CharityUpdatingRequest request)
        //    {
        //        CommonResponse commonResponse = new CommonResponse();
        //        string internalServerErrorMsg = _config[
        //            "ResponseMessages:CommonMsg:InternalServerErrorMsg"
        //        ];
        //        string charityRegisterSuccessMsg = _config[
        //            "ResponseMessages:CharityMsg:CreateSuccessMsg"
        //        ];
        //        string emailOrPhoneAlreadyExistedMsg = _config[
        //            "ResponseMessages:CharityMsg:EmailOrPhoneAlreadyExistedMsg"
        //        ];
        //        string charityEmailExistedMsg = _config[
        //            "ResponseMessages:CharityMsg:CharityEmailExistedMsg"
        //        ];
        //        Role? branchAdminRole = await _roleRepository.GetRoleByName(
        //            RoleEnum.CHARITY_UNIT.ToString()
        //        );

        //        try
        //        {
        //            Charity? tmp = await _charityRepository.GetCharityByEmail(request.Email);
        //            if (tmp != null)
        //            {
        //                commonResponse.Status = 400;
        //                commonResponse.Message = charityEmailExistedMsg;
        //                return commonResponse;
        //            }

        //            Charity charity = new Charity();
        //            charity.Status = CharityStatus.UNVERIFIED;
        //            charity.Description = request.Description;
        //            charity.Name = request.Name;
        //            using (var stream = request.Logo.OpenReadStream())
        //            {
        //                string imageName =
        //                    Guid.NewGuid().ToString() + Path.GetExtension(request.Logo.FileName);
        //                string imageUrl = await _firebaseStorageService.UploadImageToFirebase(
        //                    stream,
        //                    imageName
        //                );
        //                charity.Logo = imageUrl;
        //            }
        //            charity.CreatedDate = SettedUpDateTime.GetCurrentVietNamTime();
        //            charity.Email = request.Email;
        //            using (var scope = new TransactionScope(TransactionScopeAsyncFlowOption.Enabled))
        //            {
        //                List<CharityUnit>? charityUnits = await GetCharityUnitFromRequest(request);
        //                if (charityUnits != null && charityUnits.Count > 0)
        //                {
        //                    charity.CharityUnits = charityUnits;
        //                    int rs = await _charityRepository.CreateCharityAsync(charity);
        //                    if (rs > 0)
        //                    {
        //                        commonResponse.Status = 200;
        //                        commonResponse.Message = charityRegisterSuccessMsg;
        //                        scope.Complete();
        //                    }
        //                    else
        //                        throw new Exception("Error excute sql");
        //                }
        //                else if (charityUnits == null)
        //                {
        //                    commonResponse.Status = 400;
        //                    commonResponse.Message = emailOrPhoneAlreadyExistedMsg;
        //                    return commonResponse;
        //                }
        //            }
        //        }
        //        catch (Exception ex)
        //        {
        //            string className = nameof(CharityService);
        //            string methodName = nameof(RegisterToBecomeCharity);
        //            _logger.LogError(
        //                ex,
        //                "An error occurred in {ClassName}.{MethodName}: {ErrorMessage}",
        //                className,
        //                methodName,
        //                ex.Message
        //            );
        //            commonResponse.Message = internalServerErrorMsg;
        //            commonResponse.Status = 500;
        //        }
        //        return commonResponse;
        //    }

        public async Task<CommonResponse> GetCharityDetails(Guid charityId)
        {
            CommonResponse commonResponse = new CommonResponse();
            string internalServerErrorMsg = _config[
                "ResponseMessages:CommonMsg:InternalServerErrorMsg"
            ];
            try
            {
                Charity? charity = await _charityRepository.GetCharityById(charityId);
                int numberOfPost = 0;

                if (charity != null)
                {
                    foreach (var cu in charity.CharityUnits)
                    {
                        numberOfPost += _postRepository.countPostByUserId(cu.UserId);
                    }
                    var res = new
                    {
                        charity.Name,
                        Status = charity.Status.ToString(),
                        charity.Email,
                        Id = charityId,
                        NumberOfPost = numberOfPost,
                        NumberOfCharityUnit = charity.CharityUnits
                            .Where(c => c.Status == CharityUnitStatus.ACTIVE)
                            .Count(),
                        charity.Description,
                        charity.Logo
                    };
                    commonResponse.Data = res;
                    commonResponse.Status = 200;
                }
            }
            catch (Exception ex)
            {
                string className = nameof(CollaboratorService);
                string methodName = nameof(GetCharityDetails);
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

        public async Task<CommonResponse> GetCharityUnitListByCharityId(Guid charityId, Guid userId)
        {
            CommonResponse commonResponse = new CommonResponse();
            string internalServerErrorMsg = _config[
                "ResponseMessages:CommonMsg:InternalServerErrorMsg"
            ];
            try
            {
                User? user = await _userRepository.FindUserByIdAsync(userId);
                List<CharityUnit> charityUnits = new List<CharityUnit>();
                if (user != null)
                {
                    if (user.Role.Name == RoleEnum.SYSTEM_ADMIN.ToString())
                    {
                        charityUnits =
                            await _charityUnitRepository.FindCharityUnitsByCharityIdAsync(
                                charityId
                            );
                    }
                    else
                    {
                        charityUnits =
                            await _charityUnitRepository.FindActiveCharityUnitsByCharityIdAsync(
                                charityId
                            );
                    }
                }

                if (charityUnits != null && charityUnits.Count > 0)
                {
                    List<CharityUnitForAdminResponse> rs = charityUnits
                        .GroupBy(cu => cu.UserId)
                        .SelectMany(
                            group =>
                                group.Count() > 1
                                    ? group.Where(
                                        cu => cu.Status != CharityUnitStatus.UNVERIFIED_UPDATE
                                    )
                                    : group
                        )
                        .Select(
                            cu =>
                                new CharityUnitForAdminResponse
                                {
                                    Name = cu.Name,
                                    Status = cu.Status.ToString(),
                                    Email = cu.User.Email,
                                    Id = cu.Id,
                                    UserId = cu.UserId,
                                    Phone = cu.User.Phone,
                                    Description = cu.Description,
                                    Image = cu.Image,
                                    LegalDocuments = cu.LegalDocuments,
                                    Location = cu.Location,
                                    Address = cu.Address,
                                    isHeadQuater = cu.IsHeadquarter
                                }
                        )
                        .ToList();

                    foreach (CharityUnitForAdminResponse cu in rs)
                    {
                        cu.isWatingToConfirmUpdate =
                            await _charityUnitRepository.FindUnverifyUpdateCharityUnitByUserIdAsync(
                                cu.UserId
                            ) != null;
                    }

                    commonResponse.Data = rs;
                    commonResponse.Status = 200;
                }
            }
            catch (Exception ex)
            {
                string className = nameof(CollaboratorService);
                string methodName = nameof(GetCharityDetails);
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

        public async Task<CommonResponse> GetCharityUnitListByCharityIdForGuess(Guid charityId)
        {
            CommonResponse commonResponse = new CommonResponse();
            string internalServerErrorMsg = _config[
                "ResponseMessages:CommonMsg:InternalServerErrorMsg"
            ];
            try
            {
                List<CharityUnit> charityUnits =
                    await _charityUnitRepository.FindActiveCharityUnitsByCharityIdAsync(charityId);

                if (charityUnits != null && charityUnits.Count > 0)
                {
                    var res = charityUnits.Select(
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
                            }
                    );
                    commonResponse.Data = res;
                    commonResponse.Status = 200;
                }
                else
                {
                    commonResponse.Status = 200;
                    commonResponse.Message = "Tổ chức hiện chưa có bất kì chi nhánh nào";
                }
            }
            catch (Exception ex)
            {
                string className = nameof(CollaboratorService);
                string methodName = nameof(GetCharityDetails);
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

        public async Task<CommonResponse> DeleteCharity(Guid charrityId)
        {
            string internalServerErrorMsg = _config[
                "ResponseMessages:CommonMsg:InternalServerErrorMsg"
            ];
            string UpdateSuccessMsg = _config["ResponseMessages:CharityMsg:UpdateSuccessMsg"];
            string CharityNotFoundMsg = _config["ResponseMessages:CharityMsg:CharityNotFoundMsg"];
            CommonResponse commonResponse = new CommonResponse();
            try
            {
                using (var scope = new TransactionScope(TransactionScopeAsyncFlowOption.Enabled))
                {
                    Charity? charity = await _charityRepository.GetCharityById(charrityId);
                    if (charity != null)
                    {
                        charity.Status = CharityStatus.DELETED;
                        int rs = await _charityRepository.UpdateCharityAsync(charity);
                        foreach (var charityUnit in charity.CharityUnits)
                        {
                            charityUnit.Status = CharityUnitStatus.DELETED;
                            await _charityUnitRepository.UpdateCharityUnitAsync(charityUnit);
                            await BanAccount(charityUnit.User);
                        }
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
                string methodName = nameof(DeleteCharity);
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
