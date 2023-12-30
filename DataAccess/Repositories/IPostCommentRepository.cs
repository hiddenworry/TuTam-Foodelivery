using DataAccess.Entities;

namespace DataAccess.Repositories
{
    public interface IPostCommentRepository
    {
        Task<int> CreateCommnentAsync(PostComment post);
        Task<List<PostComment>?> GetCommnentAsync(Guid postId);
    }
}
