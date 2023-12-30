using FluentValidation;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;

namespace DataAccess.Models.Requests.Validators
{
    public class CharityCreatingRequestValidation : AbstractValidator<CharityCreatingRequest>
    {
        private IConfiguration _config;

        public CharityCreatingRequestValidation(IConfiguration configuration)
        {
            _config = configuration;

            RuleFor(x => x.Email)
                .NotNull()
                .EmailAddress()
                .WithMessage("Email không đúng định dạng.")
                .MinimumLength(5)
                .WithMessage("Email phải từ 5 đến 100 kí tự.")
                .MaximumLength(100)
                .WithMessage("Email phải từ 5 đến 100 kí tự.");

            RuleFor(x => x.Name)
                .NotNull()
                .WithMessage("Tên không được để trống.")
                .MinimumLength(5)
                .MaximumLength(100)
                .WithMessage("Tên phải từ 5 đến 100 kí tự.");

            RuleFor(x => x.Logo)
                .NotNull()
                .Must(HaveValidImageExtension)
                .WithMessage(
                    "Logo phải là một tệp hình ảnh hợp lệ (jpg, jpeg, png, gif) và có kích thước nhỏ hơn 10MB."
                );

            RuleFor(x => x.Description)
                .MinimumLength(50)
                .WithMessage("Ghi chú phải có từ 50 kí tự.");

            RuleForEach(x => x.CharityUnits)
                .SetValidator(new CharityUnitCreatingRequestValidation(_config));
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
