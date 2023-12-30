using DataAccess.DbContextData;
using DataAccess.Entities;
using Microsoft.EntityFrameworkCore;

namespace DataAccess.Repositories.Implements
{
    public class PhaseRepository : IPhaseRepository
    {
        private readonly FoodDonationDeliveryDbContext _context;

        public PhaseRepository(FoodDonationDeliveryDbContext context)
        {
            _context = context;
        }

        public async Task<int> CreatePhaseAsync(Phase phase)
        {
            _context.Phases.Add(phase);
            return await _context.SaveChangesAsync();
        }

        public async Task<int> UpdatePhaseAsync(Phase phase)
        {
            _context.Phases.Update(phase);
            return await _context.SaveChangesAsync();
        }

        public async Task<int> DeletePhaseAsync(Phase phase)
        {
            var rs = _context.ActivityTasks
                .Include(a => a.RoleTasks)
                .Where(a => a.PhaseId == phase.Id);

            foreach (var a in rs)
            {
                foreach (var rt in a.RoleTasks)
                {
                    _context.RoleTasks.Remove(rt);
                }
                _context.ActivityTasks.Remove(a);
            }
            _context.Phases.Remove(phase);
            return await _context.SaveChangesAsync();
        }

        public async Task<List<Phase>?> GetPhaseByActivityIdAsync(Guid activityId)
        {
            return await _context.Phases.Where(p => p.ActivityId == activityId).ToListAsync();
        }

        public async Task<Phase?> GetPhaseByIdAsync(Guid phaseId)
        {
            return await _context.Phases.Where(p => p.Id == phaseId).FirstOrDefaultAsync();
        }

        public Phase? GetPhaseById(Guid phaseId)
        {
            return _context.Phases.Where(p => p.Id == phaseId).FirstOrDefault();
        }

        public int UpdatePhase(Phase phase)
        {
            _context.Phases.Update(phase);
            return _context.SaveChanges();
        }
    }
}
