using DataAccess.Models.Requests.ModelBinders;

namespace DataAccess.Models.Requests.Validators.Common
{
    public static class CommonValidator
    {
        public const int MIN_PERIOD_AS_HOUR = 1;

        public static bool IsScheduledTimesNotDuplicate(List<ScheduledTime> scheduledTimes)
        {
            int count = 0;
            foreach (ScheduledTime scheduledTime in scheduledTimes)
            {
                foreach (ScheduledTime tmp in scheduledTimes)
                {
                    if (scheduledTime.Equals(tmp))
                    {
                        count += 1;
                    }
                }
            }
            return count == scheduledTimes.Count;
        }

        public static bool IsScheduledTimeValid(ScheduledTime scheduledTime)
        {
            if (scheduledTime == null)
                return false;

            if (
                !(
                    DateOnly.TryParse(scheduledTime.Day, out _)
                    && TimeOnly.TryParse(scheduledTime.StartTime, out _)
                    && TimeOnly.TryParse(scheduledTime.EndTime, out _)
                )
            )
            {
                return false;
            }

            DateOnly day = DateOnly.Parse(scheduledTime.Day);
            TimeOnly startTime = TimeOnly.Parse(scheduledTime.StartTime);
            TimeOnly endTime = TimeOnly.Parse(scheduledTime.EndTime);

            if (day.ToDateTime(startTime) <= SettedUpDateTime.GetCurrentVietNamTime())
            {
                return false;
            }

            if (endTime - startTime < TimeSpan.FromHours(MIN_PERIOD_AS_HOUR))
            {
                return false;
            }

            return true;
        }

        public static bool IsScheduledTimeValidForDelivery(ScheduledTime scheduledTime)
        {
            if (scheduledTime == null)
                return false;

            if (
                !(
                    DateOnly.TryParse(scheduledTime.Day, out _)
                    && TimeOnly.TryParse(scheduledTime.StartTime, out _)
                    && TimeOnly.TryParse(scheduledTime.EndTime, out _)
                )
            )
            {
                return false;
            }

            DateOnly day = DateOnly.Parse(scheduledTime.Day);
            TimeOnly startTime = TimeOnly.Parse(scheduledTime.StartTime);
            TimeOnly endTime = TimeOnly.Parse(scheduledTime.EndTime);

            if (day.ToDateTime(endTime) <= SettedUpDateTime.GetCurrentVietNamTime())
            {
                return false;
            }

            if (endTime - startTime < TimeSpan.FromHours(MIN_PERIOD_AS_HOUR))
            {
                return false;
            }

            return true;
        }
    }
}
