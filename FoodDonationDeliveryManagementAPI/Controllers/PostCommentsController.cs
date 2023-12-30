using BusinessLogic.Services;
using BusinessLogic.Utils.SecurityServices;
using DataAccess.Models.Requests;
using DataAccess.Models.Responses;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FoodDonationDeliveryManagementAPI.Controllers
{
    [Route("post-comments")]
    [ApiController]
    public class PostCommentsController : ControllerBase
    {
        private readonly IPostCommentService _postCommentService;
        private readonly ILogger<ActivitiesController> _logger;
        private readonly IConfiguration _config;
        private readonly IJwtService _jwtService;

        public PostCommentsController(
            IPostCommentService postCommentService,
            ILogger<ActivitiesController> logger,
            IConfiguration config,
            IJwtService jwtService
        )
        {
            _postCommentService = postCommentService;
            _logger = logger;
            _config = config;
            _jwtService = jwtService;
        }

        /// <summary>
        /// Use for get post: User
        /// </summary>
        /// <remarks>
        /// Parameters:
        /// </remarks>
        /// <response code="200">If success.</response>
        /// <response code="400">If validation error.</response>
        /// <response code="500">Internal server error.</response>

        [HttpGet("")]
        public async Task<IActionResult> GetPostComment(int? page, int? pageSize, Guid postId)
        {
            CommonResponse commonResponse = new CommonResponse();
            string internalServerErrorMsg = _config[
                "ResponseMessages:UserMsg:InternalServerErrorMsg"
            ];
            try
            {
                commonResponse = await _postCommentService.GetComments(postId, page, pageSize);
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
        /// Use for get post: User
        /// </summary>
        /// <remarks>
        /// Parameters:
        /// </remarks>
        /// <response code="200">If success.</response>
        /// <response code="400">If validation error.</response>
        /// <response code="500">Internal server error.</response>

        [HttpPost()]
        [Authorize]
        public async Task<IActionResult> CreateComment(CommentCreatingRequest request)
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
                commonResponse = await _postCommentService.CreateComment(
                    request,
                    Guid.Parse(userSub!)
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
    }
}
