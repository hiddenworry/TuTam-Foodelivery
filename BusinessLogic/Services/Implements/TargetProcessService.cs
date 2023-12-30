using DataAccess.Repositories;

namespace BusinessLogic.Services.Implements
{
    public class TargetProcessService : ITargetProcessService
    {
        private readonly ITargetProcessRepository _targetProcessRepository;

        public TargetProcessService(ITargetProcessRepository targetProcessRepository)
        {
            _targetProcessRepository = targetProcessRepository;
        }
    }
}
