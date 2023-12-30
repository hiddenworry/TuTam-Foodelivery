using BusinessLogic.Services;
using BusinessLogic.Utils.SecurityServices;
using DataAccess.EntityEnums;
using DataAccess.Models.Requests;
using DataAccess.Models.Responses;
using DataAccess.ModelsEnum;
using FoodDonationDeliveryManagementAPI.Security.Authourization.PolicyProvider;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FoodDonationDeliveryManagementAPI.Controllers
{
    [Route("branches")]
    [ApiController]
    public class BranchesController : ControllerBase
    {
        private readonly IBranchService _branchService;
        private readonly ILogger<ActivitiesController> _logger;
        private readonly IConfiguration _config;
        private readonly IJwtService _jwtService;

        public BranchesController(
            IBranchService branchService,
            ILogger<ActivitiesController> logger,
            IConfiguration config,
            IJwtService jwtService
        )
        {
            _branchService = branchService;
            _logger = logger;
            _config = config;
            _jwtService = jwtService;
        }

        /// <summary>
        /// Get paging list of branches by search and filter.
        /// </summary>
        /// <response code="200">
        /// Get branches success, return list:
        /// ```
        /// {
        ///     "status": 200,
        ///     "data": [
        ///         {
        ///             "id": "db324806-1a51-ee11-9f1b-c809a8bfd17d",
        ///             "name": "Từ Tâm Thủ Đức",
        ///             "address": "Thủ Đức",
        ///             "image": "image",
        ///             "createdDate": "2023-09-10T00:00:00",
        ///             "status": "ACTIVE"
        ///         },
        ///         {
        ///             "id": "0337cb91-1a51-ee11-9f1b-c809a8bfd17d",
        ///             "name": "Từ Tâm Quận 1",
        ///             "address": "Quận 1",
        ///             "image": "image",
        ///             "createdDate": "2023-09-11T00:00:00",
        ///             "status": "ACTIVE"
        ///         }
        ///     ],
        ///     "pagination": {
        ///         "currentPage": 1,
        ///         "pageSize": 10,
        ///         "total": 3
        ///     },
        ///     "message": "Lấy danh sách các chi nhánh thành công."
        /// }
        /// ```
        /// </response>
        /// <response code="400">
        /// Bad request in many ways, return a message of what's wrong:
        /// ```
        /// {
        ///     "status": 400,
        ///     "data": null,
        ///     "pagination": null,
        ///     "message": "Không tìm thấy trường dữ liệu cần sắp xếp."
        /// }
        /// ```
        /// </response>
        /// <response code="403">
        /// User is not allow to get inactive branches, return a message of what's wrong:
        /// ```
        /// {
        ///     "status": 403,
        ///     "data": null,
        ///     "pagination": null,
        ///     "message": "Chỉ quản trị viên mới xem được chi nhánh không còn hoạt động."
        /// }
        /// ```
        /// </response>
        /// <response code="500">
        /// Internal server error, return message:
        /// ```
        /// {
        ///     "status": 500,
        ///     "data": null,
        ///     "pagination": null,
        ///     "message": "Lỗi hệ thống."
        /// }
        /// ```
        /// </response>
        [HttpGet]
        public async Task<IActionResult> GetBranches(
            string? name,
            BranchStatus? status,
            string? address,
            int? pageSize,
            int? page,
            string? orderBy,
            SortType? sortType
        )
        {
            string internalServerErrorMsg = _config[
                "ResponseMessages:CommonMsg:InternalServerErrorMsg"
            ];
            try
            {
                CommonResponse commonResponse = new CommonResponse();
                string? jwtToken = Request.Headers["Authorization"]
                    .FirstOrDefault()
                    ?.Split(" ")
                    .Last();
                if (jwtToken == null)
                    commonResponse = await _branchService.GetBranchesAsync(
                        name,
                        status,
                        address,
                        pageSize,
                        page,
                        orderBy,
                        sortType,
                        null
                    );
                else
                {
                    Guid userId = _jwtService.GetUserIdByJwtToken(jwtToken);
                    string userRoleName = _jwtService.GetRoleNameByJwtToken(jwtToken);
                    commonResponse = await _branchService.GetBranchesAsync(
                        name,
                        status,
                        address,
                        pageSize,
                        page,
                        orderBy,
                        sortType,
                        userRoleName
                    );
                }
                switch (commonResponse.Status)
                {
                    case 200:
                        return Ok(commonResponse);
                    case 400:
                        return BadRequest(commonResponse);
                    case 403:
                        return StatusCode(403, commonResponse);
                    default:
                        return StatusCode(500, commonResponse);
                }
            }
            catch
            {
                return StatusCode(
                    500,
                    new CommonResponse { Status = 500, Message = internalServerErrorMsg }
                );
            }
        }

        /// <summary>
        /// Create branch,
        /// ROLE = SYSTEM ADMIN
        /// </summary>
        /// /// <remarks>
        /// Permission: CREATE-BRANCH
        /// <br/>
        /// Body:
        /// - **name**: Branch's name (8-40 characters).
        /// - **address**: Address (Not null).
        /// - **location**: Location (double array [x, y]) (Not null).
        /// - **status**:  0(Active) or 1(Inactive).
        /// - **Image**: Branch's images (not null, not empty).
        /// - **branchAdminId**: {Pass in 2 variables: userId and status (Active-Inactive). userId will be obtained when calling the GET:/branch-admin API with the searchStr parameter as "Name, email, or phone number"}. Example: 226abb80-972e-ee11-adee-105bad532eff:ACTIVE
        /// </remarks>
        /// <br />
        /// <response code="200">
        /// ```
        /// {
        ///     "status": 200,
        ///     "data": null,
        ///     "pagination": null,
        ///     "message": "Tạo chi nhánh thành công."
        /// }
        /// ```
        /// ```
        /// {
        ///     "status": 400,
        ///     "data": null,
        ///     "pagination": null,
        ///     "message": "User đã ở trong một chia nhánh khác."
        /// }
        /// ```
        ///</response>
        /// <response code="500">
        /// Internal server error, return message:
        /// ```
        /// {
        ///     "status": 500,
        ///     "data": null,
        ///     "pagination": null,
        ///     "message": "Lỗi hệ thống."
        /// }
        ///</response>
        /// ```

        [PermissionAuthorize("CREATE-BRANCH")]
        [HttpPost]
        public async Task<IActionResult> CreateBranch(
            [FromForm] BranchCreatingRequest branchRequest
        )
        {
            CommonResponse commonResponse = new CommonResponse();
            try
            {
                string internalServerErrorMsg = _config[
                    "ResponseMessages:UserMsg:InternalServerErrorMsg"
                ];
                string uploadImageFailedMsg = _config[
                    "ResponseMessages:UserMsg:UploadImageFailedMsg"
                ];
                int MaxFileSizeMegaBytes = _config.GetValue<int>("FileUpload:MaxFileSizeMegaBytes");
                if (branchRequest.Image.Length > MaxFileSizeMegaBytes * 1024 * 1024)
                {
                    return BadRequest(
                        new CommonResponse
                        {
                            Status = 400,
                            Message = $"Dung lượng tối đa phải <= {MaxFileSizeMegaBytes} MB"
                        }
                    );
                }

                // Kiểm tra định dạng tệp hình ảnh
                string[] allowedImageExtensions = _config
                    .GetSection("FileUpload:AllowedImageExtensions")
                    .Get<string[]>();
                string fileExtension = Path.GetExtension(branchRequest.Image.FileName).ToLower();
                if (!allowedImageExtensions.Contains(fileExtension))
                {
                    string errorMessage =
                        $"Hệ thống chỉ hỗ trợ các tệp như: {string.Join(", ", allowedImageExtensions)}";
                    return BadRequest(new CommonResponse { Status = 400, Message = errorMessage });
                }
                commonResponse = await _branchService.CreateBranch(branchRequest);

                switch (commonResponse.Status)
                {
                    case 200:
                        return Ok(commonResponse);
                    case 400:
                        return BadRequest(commonResponse);
                    default:
                        return StatusCode(500, commonResponse);
                }
            }
            catch
            {
                commonResponse.Status = 500;
                commonResponse.Message = "Lỗi hệ thống";
                return StatusCode(500, commonResponse);
            }
        }

        /// <summary>
        /// Update branch,
        /// ROLE = SYSTEM ADMIN
        /// </summary>
        /// /// <remarks>
        /// Permission: UPDATE-BRANCH
        /// <br/>
        /// Body:
        /// - **name**: Branch's name (6-40 characters).
        /// - **location**: Location (double array [x, y]) (Not null).
        /// - **address**: Address (Not null).
        /// - **status**: 0 (Active) or 1 (Inactive).
        /// - **images**: Branch's images (not null, not empty).
        /// - **branchAdminId**: UserId of branch admin when calling the GET:/branch-admin API with the searchStr parameter as "Name, email, or phone number"}. Example: 226abb80-972e-ee11-adee-105bad532eff:ACTIVE
        /// </remarks>
        /// <br />
        /// <response code="200">
        /// ```
        /// {
        ///     "status": 200,
        ///     "data": null,
        ///     "pagination": null,
        ///     "message": "Cập nhật chi nhánh thành công."
        /// }
        /// ```
        ///</response>
        /// <response code="400">
        /// ```
        /// {
        ///     "status": 400,
        ///     "data": null,
        ///     "pagination": null,
        ///     "message": "Bạn chỉ có thể bổ nhiệm người dùng với vai trò branch admin vào chi nhánh."
        /// }
        /// ```
        ///</response>
        /// <response code="400">
        /// ```
        /// {
        ///     "status": 400,
        ///     "data": null,
        ///     "pagination": null,
        ///     "message": "User đã ở trong một chia nhánh khác."
        /// }
        /// ```
        ///</response>
        /// <response code="500">
        /// Internal server error, return message:
        /// ```
        /// {
        ///     "status": 500,
        ///     "data": null,
        ///     "pagination": null,
        ///     "message": "Lỗi hệ thống."
        /// }
        ///</response>
        /// ```
        [Authorize]
        [PermissionAuthorize("UPDATE-BRANCH")]
        [HttpPut("{branchId}")]
        public async Task<IActionResult> UpdateBranch(
            [FromForm] BranchUpdatingRequest branchRequest,
            [FromRoute] Guid branchId
        )
        {
            CommonResponse commonResponse = new CommonResponse();
            try
            {
                if (branchRequest.Image != null)
                {
                    int MaxFileSizeMegaBytes = _config.GetValue<int>(
                        "FileUpload:MaxFileSizeMegaBytes"
                    );
                    if (branchRequest.Image.Length > MaxFileSizeMegaBytes * 1024 * 1024)
                    {
                        return BadRequest(
                            new CommonResponse
                            {
                                Status = 400,
                                Message = $"Dung lượng tối đa phải <= {MaxFileSizeMegaBytes} MB"
                            }
                        );
                    }

                    // Kiểm tra định dạng tệp hình ảnh
                    string[] allowedImageExtensions = _config
                        .GetSection("FileUpload:AllowedImageExtensions")
                        .Get<string[]>();
                    string fileExtension = Path.GetExtension(branchRequest.Image.FileName)
                        .ToLower();
                    if (!allowedImageExtensions.Contains(fileExtension))
                    {
                        string errorMessage =
                            $"Hệ thống chỉ hỗ trợ các tệp như: {string.Join(", ", allowedImageExtensions)}";
                        return BadRequest(
                            new CommonResponse { Status = 400, Message = errorMessage }
                        );
                    }
                }

                commonResponse = await _branchService.UpdateBranch(branchRequest, branchId);

                switch (commonResponse.Status)
                {
                    case 200:
                        return Ok(commonResponse);
                    case 400:
                        return BadRequest(commonResponse);
                    default:
                        return StatusCode(500, commonResponse);
                }
            }
            catch
            {
                commonResponse.Status = 500;
                commonResponse.Message = "Lỗi hệ thống";
                return StatusCode(500, commonResponse);
            }
        }

        /// <summary>
        /// Get branch details
        /// </summary>
        /// /// <remarks>
        /// <br/>
        /// - **branchId**: BranchId .
        /// </remarks>
        /// <br />
        /// <response code="200">
        /// ```
        ///
        /// For role System Admin
        ///       {
        ///  "status": 200,
        ///  "data": {
        ///    "id": "7473b0c0-d657-ee11-9f0b-f46add733d9c",
        ///    "name": "string",
        ///    "address": "string",
        ///    "image": "string",
        ///    "createdDate": "2023-09-20T23:57:11.4973486",
        ///    "status": "ACTIVE",
        ///    "branchMemberResponses": [
        ///      {
        ///        "userId": "ffc74bb9-922e-ee11-adee-105bad532efe",
        ///        "memberName": "ABC",
        ///        "email": "FreeVolunter@gmail.com",
        ///        "phone": "0123456789",
        ///        "status": "ACTIVE"
        ///      },
        ///      {
        ///        "userId": "a1fe47d6-922e-ee11-adee-105bad532efe",
        ///        "memberName": "ABC",
        ///        "email": "Volunteer@gmail.com",
        ///        "phone": "0123456789",
        ///        "status": "ACTIVE"
        ///      }
        ///    ]
        ///  },
        ///  "pagination": null,
        ///  "message": null
        ///}
        ///</response>
        /// <response code="200">
        /// ```
        ///
        /// For role User
        ///       {
        ///  "status": 200,
        ///  "data": {
        ///    "id": "7473b0c0-d657-ee11-9f0b-f46add733d9c",
        ///    "name": "string",
        ///    "address": "string",
        ///    "image": "string",
        ///    "createdDate": "2023-09-20T23:57:11.4973486",
        ///    "status": "ACTIVE"
        ///  },
        ///  "pagination": null,
        ///  "message": null
        ///}
        /// ```
        ///</response>
        /// <response code="500">
        /// Internal server error, return message:
        /// ```
        /// {
        ///     "status": 500,
        ///     "data": null,
        ///     "pagination": null,
        ///     "message": "Lỗi hệ thống."
        /// }
        ///</response>
        /// ```

        [HttpGet("{branchId}")]
        public async Task<IActionResult> GetById([FromRoute] Guid branchId)
        {
            CommonResponse commonResponse = new CommonResponse();

            string internalServerErrorMsg = _config[
                "ResponseMessages:UserMsg:InternalServerErrorMsg"
            ];
            try
            {
                var token = HttpContext.Request.Headers["Authorization"]
                    .FirstOrDefault()
                    ?.Split(" ")
                    .Last();

                string userRoleName = "";

                if (token != null)
                {
                    userRoleName = _jwtService.GetRoleNameByJwtToken(token);
                }

                if (
                    !string.IsNullOrEmpty(userRoleName)
                    && (
                        userRoleName == RoleEnum.SYSTEM_ADMIN.ToString()
                        || userRoleName == RoleEnum.BRANCH_ADMIN.ToString()
                    )
                )
                {
                    commonResponse = await _branchService.GetBranchDetailsForSystemAdmin(branchId);
                }
                else
                {
                    commonResponse = await _branchService.GetBranchDetailsForUser(branchId);
                }

                switch (commonResponse.Status)
                {
                    case 200:
                        return Ok(commonResponse);
                    case 400:
                        return BadRequest(commonResponse);
                    default:
                        return StatusCode(500, commonResponse);
                }
            }
            catch
            {
                commonResponse.Status = 500;
                commonResponse.Message = internalServerErrorMsg;
                return StatusCode(500, commonResponse);
            }
        }

        /// <summary>
        /// Get branch profile for branch admin
        /// </summary>
        [Authorize(Roles = "BRANCH_ADMIN")]
        [HttpGet("profile")]
        public async Task<IActionResult> GetBranchesProfile()
        {
            string internalServerErrorMsg = _config[
                "ResponseMessages:CommonMsg:InternalServerErrorMsg"
            ];
            try
            {
                CommonResponse commonResponse = new CommonResponse();
                string? jwtToken = Request.Headers["Authorization"]
                    .FirstOrDefault()
                    ?.Split(" ")
                    .Last();

                if (jwtToken != null)
                {
                    Guid userId = _jwtService.GetUserIdByJwtToken(jwtToken);
                    commonResponse = await _branchService.GetBranchDetailsForBranchAdmin(userId);
                }

                switch (commonResponse.Status)
                {
                    case 200:
                        return Ok(commonResponse);
                    case 400:
                        return BadRequest(commonResponse);
                    case 403:
                        return StatusCode(403, commonResponse);
                    default:
                        return StatusCode(500, commonResponse);
                }
            }
            catch
            {
                return StatusCode(
                    500,
                    new CommonResponse { Status = 500, Message = internalServerErrorMsg }
                );
            }
        }
    }
}
