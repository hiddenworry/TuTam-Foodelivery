using DataAccess.Models.Requests.ModelBinders;
using Microsoft.AspNetCore.Mvc;

namespace DataAccess.Models.Requests
{
    [ModelBinder(BinderType = typeof(MetadataValueModelBinder))]
    public class ScheduledTime
    {
        public string Day { get; set; }
        public string StartTime { get; set; }
        public string EndTime { get; set; }

        public bool Equals(ScheduledTime scheduledTime)
        {
            DateOnly day = DateOnly.Parse(scheduledTime.Day);
            TimeOnly startTime = TimeOnly.Parse(scheduledTime.StartTime);
            TimeOnly endTime = TimeOnly.Parse(scheduledTime.EndTime);

            DateOnly tDay = DateOnly.Parse(Day);
            TimeOnly tStartTime = TimeOnly.Parse(StartTime);
            TimeOnly tEndTime = TimeOnly.Parse(EndTime);

            return tDay == day || tDay == day && tStartTime == startTime && tEndTime == endTime;
        }
    }
}
