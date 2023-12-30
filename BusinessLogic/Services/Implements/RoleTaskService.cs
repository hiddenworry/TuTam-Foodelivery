using DataAccess.Repositories;

namespace BusinessLogic.Services.Implements
{
    public class RoleTaskService : IRoleTaskService
    {
        private readonly IRoleTaskRepository _roleTaskRepository;

        public RoleTaskService(IRoleTaskRepository roleTaskRepository)
        {
            _roleTaskRepository = roleTaskRepository;
        }
    }
}
