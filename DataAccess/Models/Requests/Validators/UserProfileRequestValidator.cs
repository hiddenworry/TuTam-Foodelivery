using FluentValidation;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;

namespace DataAccess.Models.Requests.Validators
{
    public class UserProfileRequestValidator : AbstractValidator<UserProfileRequest>
    {
        private IConfiguration _config;

        public UserProfileRequestValidator(IConfiguration configuration)
        {
            _config = configuration;

            RuleFor(x => x.Name).Length(5, 50).WithMessage("Tên đầy đú phải có từ 5 đến 50 kí tự.");

            RuleFor(x => x.Address)
                .Length(10, 250)
                .WithMessage("Địa chỉ phải có từ 10 đến 250 kí tự.");

            RuleFor(x => x.Phone)
                .Length(9, 14)
                .WithMessage("Số điện thoại sai định dạng.")
                .Matches(@"^\+?\d{1,3}[-. \(\)]?\d{1,14}$")
                .WithMessage("Số điện thoại chỉ được chứa chữ số 0-9.");

            RuleFor(x => x.Location)
                .Must(locations => locations == null || locations.Length == 2)
                .WithMessage("Location phải chứa đúng 2 giá trị (latitude và longitude).");

            RuleFor(x => x.Avatar)
                .Must(HaveValidImageExtension!)
                .WithMessage(
                    "Avatar phải là một tệp hình ảnh hợp lệ (jpg, jpeg, png, gif) và có kích thước nhỏ hơn 10MB."
                );
        }

        private bool HaveValidImageExtension(IFormFile file)
        {
            if (file == null)
            {
                return true;
            }
            string[] allowedImageExtensions = _config
                .GetSection("FileUpload:AllowedImageExtensions")
                .Get<string[]>();
            string fileExtension = Path.GetExtension(file.FileName).ToLower();
            if (!allowedImageExtensions.Contains(fileExtension))
            {
                return false;
            }

            int maxFileSizeMegaBytes = _config.GetValue<int>("FileUpload:MaxFileSizeMegaBytes");
            if (file.Length > maxFileSizeMegaBytes * 1024 * 1024)
            {
                return false;
            }
            return true;
        }
    }
}
