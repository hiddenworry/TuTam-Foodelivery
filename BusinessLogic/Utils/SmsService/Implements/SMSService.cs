using Microsoft.Extensions.Configuration;
using Twilio.Types;
using Twilio;
using Twilio.Rest.Api.V2010.Account;
using System.Text.RegularExpressions;

namespace BusinessLogic.Utils.SmsService.Implements
{
    public class SMSService : ISMSService
    {
        private readonly IConfiguration _configuration;
        private string _accountSid;
        private string _authToken;
        private string _fromPhoneNumber;
        private string _toPhoneNumber;

        public SMSService(IConfiguration configuration)
        {
            _configuration = configuration;
        }

        private string ConvertToInternationalFormat(string phoneNumber)
        {
            // Loại bỏ các ký tự không phải số từ số điện thoại
            string digits = Regex.Replace(phoneNumber, @"[^\d]", "");

            // Kiểm tra xem số điện thoại có bắt đầu bằng "0" không
            if (digits.StartsWith("0"))
            {
                // Chuyển đổi số "0" đầu tiên thành mã quốc gia "+84"
                return "+84" + digits.Substring(1);
            }
            else
            {
                // Nếu số không bắt đầu bằng "0", thì giữ nguyên số điện thoại
                return "+" + digits;
            }
        }

        public bool sendSMS(string toPhone, string code)
        {
            _accountSid = _configuration["TwilioSettings:AccountSid"];
            _authToken = _configuration["TwilioSettings:AuthToken"];
            _fromPhoneNumber = _configuration["TwilioSettings:FromPhoneNumber"];
            _toPhoneNumber = _configuration["TwilioSettings:ToPhoneNumber"];

            // _fromPhoneNumber = ConvertToInternationalFormat(_fromPhoneNumber);
            _toPhoneNumber = ConvertToInternationalFormat(_toPhoneNumber);

            TwilioClient.Init(_accountSid, _authToken);
            string msg = "Your otp is : " + code;

            var message = MessageResource.Create(
                body: msg,
                from: new PhoneNumber(_fromPhoneNumber),
                to: new PhoneNumber(_toPhoneNumber)
            );

            return true;
        }
    }
}
