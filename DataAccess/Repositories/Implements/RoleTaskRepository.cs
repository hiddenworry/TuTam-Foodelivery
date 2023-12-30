using DataAccess.DbContextData;

namespace DataAccess.Repositories.Implements
{
    public class RoleTaskRepository : IRoleTaskRepository
    {
        private readonly FoodDonationDeliveryDbContext _context;

        public RoleTaskRepository(FoodDonationDeliveryDbContext context)
        {
            _context = context;
        }
    }
}
