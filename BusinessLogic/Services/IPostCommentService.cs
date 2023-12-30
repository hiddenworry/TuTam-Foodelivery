using DataAccess.Models.Requests;
using DataAccess.Models.Responses;

namespace BusinessLogic.Services
{
    public interface IPostCommentService
    {
        Task<CommonResponse> CreateComment(CommentCreatingRequest request, Guid userId);
        Task<CommonResponse> GetComments(Guid postId, int? page, int? pageSize);
    }
}
