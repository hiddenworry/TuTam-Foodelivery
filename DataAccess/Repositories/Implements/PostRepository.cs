using DataAccess.DbContextData;
using DataAccess.Entities;
using DataAccess.EntityEnums;
using Microsoft.EntityFrameworkCore;

namespace DataAccess.Repositories.Implements
{
    public class PostRepository : IPostRepository
    {
        private readonly FoodDonationDeliveryDbContext _context;

        public PostRepository(FoodDonationDeliveryDbContext context)
        {
            _context = context;
        }

        public int countPostByUserId(Guid userId)
        {
            return _context.Posts.Count(p => p.CreaterId == userId);
        }

        public async Task<int> CreatePostAsync(Post post)
        {
            await _context.Posts.AddAsync(post);
            return await _context.SaveChangesAsync();
        }

        public async Task<int> UpdatePostAsync(Post post)
        {
            try
            {
                _context.Posts.Update(post);
                return await _context.SaveChangesAsync();
            }
            catch
            {
                return 0;
            }
        }

        public async Task<List<Post>?> GetPosts(PostStatus? status, Guid? userId)
        {
            var query = _context.Posts
                .Include(p => p.PostComments)
                .ThenInclude(pm => pm.User)
                .AsQueryable();
            if (status != null)
            {
                query = query.Where(p => p.Status == status);
            }
            if (userId != null)
            {
                query = query.Where(p => p.CreaterId == userId);
            }
            return await query.OrderByDescending(p => p.CreatedDate).ToListAsync();
        }

        public async Task<Post?> GetPostById(Guid Id)
        {
            return await _context.Posts.FirstOrDefaultAsync(a => a.Id == Id);
        }

        public async Task<int> DeletePostAsync(Post post)
        {
            try
            {
                _context.Posts.Remove(post);
                return await _context.SaveChangesAsync();
            }
            catch
            {
                return 0;
            }
        }
    }
}
