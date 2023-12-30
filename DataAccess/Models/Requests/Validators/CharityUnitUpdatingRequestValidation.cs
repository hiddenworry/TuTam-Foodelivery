using FluentValidation;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;

namespace DataAccess.Models.Requests.Validators
{
    public class CharityUnitUpdatingRequestValidation
        : AbstractValidator<CharityUnitUpdatingRequest>
    {
        private IConfiguration _config;

        public CharityUnitUpdatingRequestValidation(IConfiguration configuration)
        {
            _config = configuration;

            RuleFor(x => x.Name)
                .MinimumLength(5)
                .MaximumLength(100)
                .WithMessage("Tên phải từ 5 đến 100 kí tự.");

            RuleFor(x => x.Address)
                .Length(10, 250)
                .WithMessage("Địa chỉ phải có từ 10 đến 250 kí tự.");

            RuleFor(x => x.Location)
                .Must(locations => locations == null || locations.Length == 2)
                .WithMessage("Location phải chứa đúng 2 giá trị (latitude và longitude).");
            RuleFor(x => x.Image)
                .Must(HaveValidImageExtension!)
                .WithMessage(
                    "Logo phải là một tệp hình ảnh hợp lệ (jpg, jpeg, png, gif) và có kích thước nhỏ hơn 10MB."
                );

            RuleFor(x => x.Description).MinimumLength(50).WithMessage("Ghi chú phải từ 50 kí tự.");

            RuleFor(x => x.LegalDocument)
                .Must(HaveValidDocxAndPdfExtension!)
                .WithMessage(
                    "Logo phải là một tệp hình ảnh hợp lệ (pdf,docx) và có kích thước nhỏ hơn 10MB."
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

        private bool HaveValidDocxAndPdfExtension(IFormFile file)
        {
            if (file == null)
            {
                return true;
            }
            string[] allowedImageExtensions = _config
                .GetSection("FileUpload:AllowedDocumentExtensions")
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
