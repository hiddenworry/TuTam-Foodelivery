namespace DataAccess.EntityEnums
{
    public enum ScheduledRouteStatus
    {
        //thành phần delivery request bị cancel thông qua hết hạn hoặc bị lập sang scheduled route khác
        PENDING,
        ACCEPTED,
        PROCESSING,
        FINISHED,
        CANCELED,
        LATE
    }
}
