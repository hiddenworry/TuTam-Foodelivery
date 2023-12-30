namespace BusinessLogic.Utils.SmsService
{
    public interface ISMSService
    {
        bool sendSMS(string toPhone, string code);
    }
}
