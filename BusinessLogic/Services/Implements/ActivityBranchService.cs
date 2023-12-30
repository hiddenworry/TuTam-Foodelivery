using DataAccess.Repositories;

namespace BusinessLogic.Services.Implements
{
    public class ActivityBranchService : IActivityBranchService
    {
        private readonly IActivityBranchRepository _activityBranchRepository;

        public ActivityBranchService(IActivityBranchRepository activityBranchRepository)
        {
            _activityBranchRepository = activityBranchRepository;
        }
    }
}
