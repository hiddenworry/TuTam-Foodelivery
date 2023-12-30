namespace DataAccess.Models.Requests.ModelBinders
{
    public static class SettedUpDateTime
    {
        public static DateTime GetCurrentVietNamTime()
        {
            TimeZoneInfo vietnamTimeZone = TimeZoneInfo.FindSystemTimeZoneById(
                "SE Asia Standard Time"
            );
            DateTime currentUtcTime = DateTime.UtcNow;
            return TimeZoneInfo.ConvertTimeFromUtc(currentUtcTime, vietnamTimeZone);
        }

        public static DateTime GetCurrentVietNamTimeWithDateOnly()
        {
            TimeZoneInfo vietnamTimeZone = TimeZoneInfo.FindSystemTimeZoneById(
                "SE Asia Standard Time"
            );
            DateTime currentUtcTime = DateTime.UtcNow;
            DateTime dateTime = TimeZoneInfo.ConvertTimeFromUtc(currentUtcTime, vietnamTimeZone);
            return new DateTime(dateTime.Year, dateTime.Month, dateTime.Day);
        }
    }
}
