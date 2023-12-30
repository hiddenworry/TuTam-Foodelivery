using DataAccess.DbContextData;
using DataAccess.Entities;
using Microsoft.EntityFrameworkCore;

namespace DataAccess.Repositories.Implements
{
    public class PostCommentRepository : IPostCommentRepository
    {
        private readonly FoodDonationDeliveryDbContext _context;

        public PostCommentRepository(FoodDonationDeliveryDbContext context)
        {
            _context = context;
        }

        public async Task<int> CreateCommnentAsync(PostComment post)
        {
            await _context.PostComments.AddAsync(post);
            return await _context.SaveChangesAsync();
        }

        public async Task<int> UpdateCommentAsync(PostComment post)
        {
            try
            {
                _context.PostComments.Update(post);
                return await _context.SaveChangesAsync();
            }
            catch
            {
                return 0;
            }
        }

        public async Task<List<PostComment>?> GetCommnentAsync(Guid postId)
        {
            var query = _context.PostComments.Where(a => a.PostId == postId).Include(a => a.User);

            return await query.OrderByDescending(a => a.CreatedDate).ToListAsync();
        }
    }
}
