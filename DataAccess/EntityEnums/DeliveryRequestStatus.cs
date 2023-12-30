namespace DataAccess.EntityEnums
{
    public enum DeliveryRequestStatus
    {
        //khi finish
        PENDING,
        ACCEPTED, //về peding và cancel với scheduled route
        SHIPPING, //về peding và cancel với scheduled route
        ARRIVED_PICKUP, //về peding và cancel với scheduled route
        REPORTED, // giữ nguyên
        COLLECTED, //về peding và cancel với scheduled route
        ARRIVED_DELIVERY, //
        DELIVERED, // lên finish
        FINISHED,
        EXPIRED,
        CANCELED
    }
}
