using Hangfire.Dashboard;

namespace BusinessLogic.Utils.HangfireService
{
    public class MyAuthorizationFilter : IDashboardAuthorizationFilter
    {
        public bool Authorize(DashboardContext context)
        {
            return true;
            //return context.GetHttpContext().User.IsInRole("SYSTEM_ADMIN");
        }
    }
}
