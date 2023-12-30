using FluentValidation;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;

namespace DataAccess.Models.Requests.Validators
{
    public class CollaboratorCreatingRequestValidator
        : AbstractValidator<CollaboratorCreatingRequest>
    {
        private IConfiguration _config;

        public CollaboratorCreatingRequestValidator(IConfiguration configuration)
        {
            _config = configuration;

            RuleFor(x => x.FullName)
                .NotNull()
                .WithMessage("Tên thật phải từ 5 đến 50 kí tự.")
                .Length(5, 50)
                .WithMessage("Tên thật phải từ 5 đến 50 kí tự.");

            RuleFor(x => x.DateOfBirth)
                .NotNull()
                .WithMessage("Ngày sinh không được để trống.")
                .Must(BeAValidDate)
                .WithMessage(
                    "Ngày sinh không hợp lệ. Bạn phải đủ 18 tuổi mới có thể tham gia vận chuyển."
                );

            RuleFor(x => x.Avatar)
                .NotNull()
                .Must(HaveValidImageExtension)
                .WithMessage(
                    "Hình đại diện phải là một tệp hình ảnh hợp lệ (jpg, jpeg, png, gif) và có kích thước nhỏ hơn 10MB."
                );
            RuleFor(x => x.FrontOfIdCard)
                .NotNull()
                .Must(HaveValidImageExtension)
                .WithMessage(
                    "Mặt trước thẻ căn cước phải là một tệp hình ảnh hợp lệ (jpg, jpeg, png, gif) và có kích thước nhỏ hơn 10MB."
                );

            RuleFor(x => x.BackOfIdCard)
                .NotNull()
                .Must(HaveValidImageExtension)
                .WithMessage(
                    "Avatar phải là một tệp hình ảnh hợp lệ (jpg, jpeg, png, gif) và có kích thước nhỏ hơn 10MB."
                );

            RuleFor(x => x.Note)
                .MaximumLength(500)
                .WithMessage("Ghi chú phải có tối đa 500 kí tự.");
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

        private bool BeAValidDate(DateTime dateOfBirth)
        {
            DateTime currentDate = DateTime.Now;
            DateTime eighteenYearsAgo = currentDate.AddYears(-18);
            return dateOfBirth.Year >= 1900 && dateOfBirth <= eighteenYearsAgo;
        }
    }
}
