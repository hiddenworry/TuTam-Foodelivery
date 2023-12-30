using DataAccess.EntityEnums;
using DataAccess.Models.Requests;
using DataAccess.Models.Responses;

namespace BusinessLogic.Services
{
    public interface IPostService
    {
        Task<CommonResponse> ConfirmPost(Guid postId, ConfirmPostRequest request);
        Task<CommonResponse> CreatePost(PostCreatingRequest postCreatingRequest, Guid userId);
        Task<CommonResponse> GetPostByStatusAndUserId(
            int? page,
            int? pageSize,
            PostStatus postStatus,
            Guid userId
        );
        Task<CommonResponse> GetPostForUser(int? page, int? pageSize);

        Task<CommonResponse> UpdatePost(Guid postId, PostCreatingRequest request, Guid userId);
    }
}
