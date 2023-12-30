using DataAccess.Entities;
using DataAccess.EntityEnums;
using DataAccess.Models.Requests;
using DataAccess.Models.Requests.ModelBinders;
using DataAccess.Models.Responses;
using DataAccess.Repositories;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace BusinessLogic.Services.Implements
{
    public class PostCommentService : IPostCommentService
    {
        private readonly IPostCommentRepository _postCommentRepository;
        private readonly IUserRepository _userRepository;
        private readonly ILogger<PostCommentService> _logger;
        private readonly IConfiguration _config;

        public PostCommentService(
            IPostCommentRepository postCommentRepository,
            IUserRepository userRepository,
            ILogger<PostCommentService> logger,
            IConfiguration configuration
        )
        {
            _postCommentRepository = postCommentRepository;
            _userRepository = userRepository;
            _logger = logger;
            _config = configuration;
        }

        public async Task<CommonResponse> CreateComment(CommentCreatingRequest request, Guid userId)
        {
            CommonResponse commonResponse = new CommonResponse();
            string internalServerErrorMsg = _config[
                "ResponseMessages:AuthenticationMsg:InternalServerErrorMsg"
            ];

            try
            {
                User? user = await _userRepository.FindUserByIdAsync(userId);
                if (user == null)
                {
                    commonResponse.Status = 400;
                    commonResponse.Data = "Người dùng không tìm thấy";
                    return commonResponse;
                }
                PostComment postComment = new PostComment();
                postComment.Status = PostCommentStatus.ACTIVE;
                postComment.Content = request.Content;
                postComment.CreatedDate = SettedUpDateTime.GetCurrentVietNamTime();
                postComment.UserId = userId;
                postComment.PostId = request.PostId;
                int rs = await _postCommentRepository.CreateCommnentAsync(postComment);
                if (rs < 0)
                    throw new Exception();
                commonResponse.Status = 200;
                commonResponse.Data = "Câp jnahatj thành công";
            }
            catch (Exception ex)
            {
                string className = nameof(PostService);
                string methodName = nameof(CreateComment);
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

        public async Task<CommonResponse> GetComments(Guid postId, int? page, int? pageSize)
        {
            CommonResponse commonResponse = new CommonResponse();
            string internalServerErrorMsg = _config[
                "ResponseMessages:AuthenticationMsg:InternalServerErrorMsg"
            ];
            try
            {
                List<PostComment>? postComments = await _postCommentRepository.GetCommnentAsync(
                    postId
                );
                if (postComments != null && postComments.Count > 0)
                {
                    Pagination pagination = new Pagination();
                    pagination.PageSize = pageSize == null ? 10 : pageSize.Value;
                    pagination.CurrentPage = page == null ? 1 : page.Value;
                    pagination.Total = postComments.Count;
                    postComments = postComments
                        .Skip((pagination.CurrentPage - 1) * pagination.PageSize)
                        .Take(pagination.PageSize)
                        .ToList();
                    var rs = postComments.Select(
                        a =>
                            new
                            {
                                a.Id,
                                a.User.Name,
                                a.CreatedDate,
                                a.Content,
                                Image = a.User.Avatar ?? string.Empty
                            }
                    );

                    commonResponse.Data = rs;
                }
                else
                {
                    commonResponse.Data = new List<PostComment>();
                }
                commonResponse.Status = 200;
            }
            catch (Exception ex)
            {
                string className = nameof(PostService);
                string methodName = nameof(CreateComment);
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
    }
}
