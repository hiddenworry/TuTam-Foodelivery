using BusinessLogic.Services;
using Hangfire;
using Microsoft.Extensions.DependencyInjection;

namespace BusinessLogic.Utils.HangfireService.Implements
{
    public class HangfireService : IHangfireService
    {
        private readonly IServiceProvider _serviceProvider;

        public HangfireService(IServiceProvider serviceProvider)
        {
            _serviceProvider = serviceProvider;
        }

        public void UpdateStockWhenStockOutDate()
        {
            using (var scope = _serviceProvider.CreateScope())
            {
                var _stockService = scope.ServiceProvider.GetRequiredService<IStockService>();
                BackgroundJob.Enqueue(() => _stockService.UpdateStockWhenOutDate());
            }
        }

        public void UpdateOutDateAidRequests()
        {
            using (var scope = _serviceProvider.CreateScope())
            {
                var _aidRequestService =
                    scope.ServiceProvider.GetRequiredService<IAidRequestService>();
                BackgroundJob.Enqueue(() => _aidRequestService.UpdateOutDateAidRequestsAsync());
            }
        }

        public void UpdateOutDateDonatedRequests()
        {
            using (var scope = _serviceProvider.CreateScope())
            {
                var _donatedRequestService =
                    scope.ServiceProvider.GetRequiredService<IDonatedRequestService>();
                BackgroundJob.Enqueue(
                    () => _donatedRequestService.UpdateOutDateDonatedRequestsAsync()
                );
            }
        }

        public void AutoUpdateAvailableAndLateScheduledRoute()
        {
            using (var scope = _serviceProvider.CreateScope())
            {
                var _scheduledRouteService =
                    scope.ServiceProvider.GetRequiredService<IScheduledRouteService>();
                BackgroundJob.Enqueue(
                    () => _scheduledRouteService.AutoUpdateAvailableAndLateScheduledRoute()
                );
            }
        }

        //public void AutoCheckLateScheduleRoute()
        //{
        //    using (var scope = _serviceProvider.CreateScope())
        //    {
        //        var _scheduledRouteService =
        //            scope.ServiceProvider.GetRequiredService<IScheduledRouteService>();
        //        BackgroundJob.Enqueue(() => _scheduledRouteService.AutoCheckLateScheduleRoute());
        //    }
        //}
    }
}
