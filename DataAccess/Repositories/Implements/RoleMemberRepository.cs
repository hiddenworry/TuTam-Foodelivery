using DataAccess.DbContextData;

namespace DataAccess.Repositories.Implements
{
    public class RoleMemberRepository : IRoleMemberRepository
    {
        private readonly FoodDonationDeliveryDbContext _context;

        public RoleMemberRepository(FoodDonationDeliveryDbContext context)
        {
            _context = context;
        }
    }
}
