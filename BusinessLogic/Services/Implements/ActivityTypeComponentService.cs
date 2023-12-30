using DataAccess.Repositories;

namespace BusinessLogic.Services.Implements
{
    public class ActivityTypeComponentService : IActivityTypeComponentService
    {
        private readonly IActivityTypeComponentRepository _activityTypeComponentRepository;

        public ActivityTypeComponentService(
            IActivityTypeComponentRepository activityTypeComponentRepository
        )
        {
            _activityTypeComponentRepository = activityTypeComponentRepository;
        }
    }
}
