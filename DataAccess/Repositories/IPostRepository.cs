using DataAccess.Entities;
using DataAccess.EntityEnums;

namespace DataAccess.Repositories
{
    public interface IPostRepository
    {
        int countPostByUserId(Guid userId);
        Task<int> CreatePostAsync(Post post);
        Task<int> DeletePostAsync(Post post);
        Task<Post?> GetPostById(Guid Id);
        Task<List<Post>?> GetPosts(PostStatus? status, Guid? userId);

        Task<int> UpdatePostAsync(Post post);
    }
}
