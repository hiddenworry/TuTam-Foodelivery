namespace BusinessLogic.Utils.HangfireService
{
    public interface IHangfireService
    {
        void AutoUpdateAvailableAndLateScheduledRoute();

        void UpdateOutDateDonatedRequests();

        void UpdateOutDateAidRequests();

        void UpdateStockWhenStockOutDate();

        //void AutoCheckLateScheduleRoute();
    }
}
