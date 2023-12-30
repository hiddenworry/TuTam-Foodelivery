using BusinessLogic.Services;
using BusinessLogic.Utils.Notification.Implements;
using BusinessLogic.Utils.SecurityServices;
using DataAccess.EntityEnums;
using DataAccess.Models.Requests;
using DataAccess.Models.Responses;
using DataAccess.ModelsEnum;
using FoodDonationDeliveryManagementAPI.Security.Authourization.PolicyProvider;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;

namespace FoodDonationDeliveryManagementAPI.Controllers
{
    [Route("collaborators")]
    [ApiController]
    public class CollaboratorsController : ControllerBase
    {
        private readonly ICollaboratorService _collaboratorService;
        private readonly ILogger<CollaboratorsController> _logger;
        private readonly IConfiguration _config;
        private readonly IJwtService _jwtService;
        private readonly IHubContext<NotificationSignalSender> _hubContext;

        public CollaboratorsController(
            ICollaboratorService collaboratorService,
            ILogger<CollaboratorsController> logger,
            IConfiguration config,
            IJwtService jwtService,
            IHubContext<NotificationSignalSender> hubContext
        )
        {
            _collaboratorService = collaboratorService;
            _logger = logger;
            _config = config;
            _jwtService = jwtService;
            _hubContext = hubContext;
        }

        /// <summary>
        /// Register to become collaborator
        /// </summary>
        /// <br/>
        /// <remarks>
        /// Body:
        /// - **FullName**: String (8-60 characters) (Require)
        /// - **Date of birth**: DateTime (From 1900 to now) (Require)
        /// - **Genter**: int(Require) 0(Man), 1(Woman)
        /// - **Avatar**: link(Require)
        /// - **frontOfIDCard**: link(Require)
        /// - **backOfIDCart**: link(Require)
        /// - **Note**: String (0-500 kí tự characters) (Require)
        /// </remarks>
        /// <br />
        /// <response code="200">
        /// ```
        /// {
        ///     "status": 200,
        ///     "data": null,
        ///     "pagination": null,
        ///     "message": "Cập nhật thành công."
        /// }
        /// ```
        ///</response>
        /// <response code="400">
        /// ```
        /// {
        ///     "status": 400,
        ///     "data": null,
        ///     "pagination": null,
        ///     "message": "User không tồn tại"
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
        [PermissionAuthorize("REGISTER-COLLABORATOR")]
        [Authorize()]
        [HttpPost]
        public async Task<IActionResult> RegiterToBecomeCollaborator(
            [FromForm] CollaboratorCreatingRequest request
        )
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
                string? userSub = "";
                if (token != null)
                {
                    var decodedToken = _jwtService.GetClaimsPrincipal(token);

                    if (decodedToken != null)
                    {
                        userSub = decodedToken.Claims.FirstOrDefault(c => c.Type == "Id")?.Value;
                    }
                }
                commonResponse = await _collaboratorService.RegisterToBecomeCollaborator(
                    Guid.Parse(userSub!),
                    request
                );

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
                commonResponse.Message = internalServerErrorMsg;
                commonResponse.Status = 500;
                return StatusCode(500, commonResponse);
            }
        }

        ///// <summary>
        ///// Collaborator update their status
        ///// </summary>
        ///// <br/>
        ///// <remarks>
        ///// Body:
        ///// - **isActive**: true(Active, false(Inactive)(Require)
        ///// </remarks>
        ///// <br />
        ///// <response code="200">
        ///// ```
        ///// {
        /////     "status": 200,
        /////     "data": null,
        /////     "pagination": null,
        /////     "message": "Cập nhật thành công."
        ///// }
        ///// ```
        /////</response>
        ///// <response code="400">
        ///// ```
        ///// {
        /////     "status": 400,
        /////     "data": null,
        /////     "pagination": null,
        /////     "message": "User không tồn tại"
        ///// }
        ///// ```
        /////</response>
        ///// <response code="500">
        ///// Internal server error, return message:
        ///// ```
        ///// {
        /////     "status": 500,
        /////     "data": null,
        /////     "pagination": null,
        /////     "message": "Lỗi hệ thống."
        ///// }
        /////</response>
        ///// ```
        //[HttpPut()]
        //public async Task<IActionResult> UpdateCollaboratorForUserAsync(
        //    CollaboratorUpdatingRequest request
        //)
        //{
        //    CommonResponse commonResponse = new CommonResponse();
        //    string internalServerErrorMsg = _config[
        //        "ResponseMessages:UserMsg:InternalServerErrorMsg"
        //    ];
        //    try
        //    {
        //        var token = HttpContext.Request.Headers["Authorization"]
        //            .FirstOrDefault()
        //            ?.Split(" ")
        //            .Last();
        //        string? userSub = "";
        //        if (token != null)
        //        {
        //            var decodedToken = _jwtService.GetClaimsPrincipal(token);

        //            if (decodedToken != null)
        //            {
        //                userSub = decodedToken.Claims.FirstOrDefault(c => c.Type == "Id")?.Value;
        //            }
        //        }
        //        commonResponse = await _collaboratorService.UpdateCollaboratorForUserAsync(
        //            Guid.Parse(userSub!),
        //            request
        //        );

        //        switch (commonResponse.Status)
        //        {
        //            case 200:
        //                return Ok(commonResponse);
        //            case 400:
        //                return BadRequest(commonResponse);
        //            default:
        //                return StatusCode(500, commonResponse);
        //        }
        //    }
        //    catch
        //    {
        //        commonResponse.Message = internalServerErrorMsg;
        //        commonResponse.Status = 500;
        //        return StatusCode(500, commonResponse);
        //    }
        //}

        /// <summary>
        /// Admin confirm collaborator(Role SystemAdmin)
        /// </summary>
        /// <br/>
        /// <remarks>
        /// Body:
        /// - **isAccept**: true(Require)
        /// - **collaboratorId**: id
        /// - **reason**: when isAccepted = false, reason must be input to notify to user
        /// </remarks>
        /// <br />
        /// <response code="200">
        /// ```
        /// {
        ///     "status": 200,
        ///     "data": null,
        ///     "pagination": null,
        ///     "message": "Cập nhật thành công."
        /// }
        /// ```
        ///</response>
        /// <response code="400">
        /// ```
        /// {
        ///     "status": 400,
        ///     "data": null,
        ///     "pagination": null,
        ///     "message": "User không tồn tại"
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
        [PermissionAuthorize("UPDATE-COLLABORATOR")]
        [HttpPut("{collaboratorId}")]
        public async Task<IActionResult> ConfirmCollaborator(
            [FromRoute] Guid collaboratorId,
            [FromBody] ConfirmCollaboratorRequest request
        )
        {
            CommonResponse commonResponse = new CommonResponse();
            string internalServerErrorMsg = _config[
                "ResponseMessages:UserMsg:InternalServerErrorMsg"
            ];
            try
            {
                commonResponse = await _collaboratorService.ConfirmCollaborator(
                    collaboratorId,
                    request
                );

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
                commonResponse.Message = internalServerErrorMsg;
                commonResponse.Status = 500;
                return StatusCode(500, commonResponse);
            }
        }

        /// <summary>
        /// Admin delete collaborator(Role SystemAdmin)
        /// </summary>
        /// <br/>
        /// <remarks>
        /// Body:
        /// - **collaboratorId**: id
        /// </remarks>
        /// <br />
        /// <response code="200">
        /// ```
        /// {
        ///     "status": 200,
        ///     "data": null,
        ///     "pagination": null,
        ///     "message": "Cập nhật thành công."
        /// }
        /// ```
        ///</response>
        /// <response code="400">
        /// ```
        /// {
        ///     "status": 400,
        ///     "data": null,
        ///     "pagination": null,
        ///     "message": "User không tồn tại"
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
        [PermissionAuthorize("DELETE-COLLABORATOR")]
        [Authorize]
        [HttpDelete("{collaboratorId}")]
        public async Task<IActionResult> DeleteCollaboratorAsync([FromRoute] Guid collaboratorId)
        {
            CommonResponse commonResponse = new CommonResponse();
            string internalServerErrorMsg = _config[
                "ResponseMessages:UserMsg:InternalServerErrorMsg"
            ];
            try
            {
                commonResponse = await _collaboratorService.DeleteCollaborator(collaboratorId);

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
                commonResponse.Message = internalServerErrorMsg;
                commonResponse.Status = 500;
                return StatusCode(500, commonResponse);
            }
        }

        /// <summary>
        ///Get list collaborator(Role SystemAdmin)
        /// </summary>
        /// <br/>
        /// <remarks>
        /// - **page**: int
        /// - **pageSize**: int
        /// - **Status**: int 0(ACTIVE), 1(INACTIVE), 2(UNVERIFY)
        /// </remarks>
        /// <br />
        /// <response code="200">
        /// ```
        ///          {
        ///      "status": 200,
        ///     "data": [
        ///        {
        ///          "fullName": "sgrfdsgdfsg",
        ///          "createDate": "2023-10-16T20:15:07.3282119",
        ///         "status": ACTIVE,
        ///          "email": "datnhtse151251@fpt.edu.vn",
        ///          "phone": ""
        ///       }
        ///      ],
        ///      "pagination": {
        ///        "currentPage": 1,
        ///        "pageSize": 10,
        ///       "total": 1
        ///      },
        ///      "message": null
        ///    }
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
        [PermissionAuthorize("VIEW-COLLABORATOR")]
        [Authorize]
        [HttpGet()]
        public async Task<IActionResult> GetListUnverifyCollaborator(
            int? status,
            int? page,
            int? pageSize,
            SortType sortType
        )
        {
            CommonResponse commonResponse = new CommonResponse();
            string internalServerErrorMsg = _config[
                "ResponseMessages:UserMsg:InternalServerErrorMsg"
            ];
            CollaboratorStatus? inputStatus;
            switch (status)
            {
                case 0:
                    inputStatus = CollaboratorStatus.ACTIVE;
                    break;
                case 1:
                    inputStatus = CollaboratorStatus.INACTIVE;
                    break;
                case 2:
                    inputStatus = CollaboratorStatus.UNVERIFIED;
                    break;
                default:
                    inputStatus = null;
                    break;
            }
            try
            {
                commonResponse = await _collaboratorService.GetListUnVerifyCollaborator(
                    inputStatus,
                    page,
                    pageSize,
                    sortType
                );

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
                commonResponse.Message = internalServerErrorMsg;
                commonResponse.Status = 500;
                return StatusCode(500, commonResponse);
            }
        }

        /// <summary>
        ///Get detail collaborator(Role SystemAdmin)
        /// </summary>
        /// <br/>
        /// <remarks>
        /// - **page**: int
        /// - **pageSize**: int
        /// - **Status**: int 0(ACTIVE), 1(INACTIVE), 2(UNVERIFY)
        /// </remarks>
        /// <br />
        /// <response code="200">
        /// ```
        ///          {
        ///      "status": 200,
        ///      "data": {
        ///       "phone": "",
        ///         "email": "datnhtse151251@fpt.edu.vn",
        ///     "fullName": "sgrfdsgdfsg",
        ///        "avatar": "link",
        ///        "dateOfBirth": "2001-10-05T14:00:00",
        ///       "gender": "MALE",
        ///       "frontOfIdCard": "link",
        ///      "backOfIdCard": "link",
        ///    "note": "dfdssfg"
        /// },
        ///       "pagination": null,
        ///              "message": null
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
        /// ```
        ///</response>

        [PermissionAuthorize("VIEW-COLLABORATOR")]
        [Authorize]
        [HttpGet("{collaboratorId}")]
        public async Task<IActionResult> GetCollaboratorDetailsCollaborator(Guid collaboratorId)
        {
            CommonResponse commonResponse = new CommonResponse();
            string internalServerErrorMsg = _config[
                "ResponseMessages:UserMsg:InternalServerErrorMsg"
            ];
            try
            {
                commonResponse = await _collaboratorService.GetDetailsCollaborator(collaboratorId);

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
                commonResponse.Message = internalServerErrorMsg;
                commonResponse.Status = 500;
                return StatusCode(500, commonResponse);
            }
        }

        [Authorize]
        [HttpGet("is-collaborator")]
        public async Task<IActionResult> CheckCollaborator()
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
                string? userSub = "";
                if (token != null)
                {
                    var decodedToken = _jwtService.GetClaimsPrincipal(token);

                    if (decodedToken != null)
                    {
                        userSub = decodedToken.Claims.FirstOrDefault(c => c.Type == "Id")?.Value;
                    }
                }
                commonResponse = await _collaboratorService.checkCollaborator(Guid.Parse(userSub!));

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
                commonResponse.Message = internalServerErrorMsg;
                commonResponse.Status = 500;
                return StatusCode(500, commonResponse);
            }
        }
    }
}
