using DataAccess.Repositories;

namespace BusinessLogic.Services.Implements
{
    public class RoleMemberService : IRoleMemberService
    {
        private readonly IRoleMemberRepository _roleMemberRepository;

        public RoleMemberService(IRoleMemberRepository roleMemberRepository)
        {
            _roleMemberRepository = roleMemberRepository;
        }
    }
}
