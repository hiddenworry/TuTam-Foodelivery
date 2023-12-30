using FluentValidation;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;

namespace DataAccess.Models.Requests.Validators
{
    public class CharityUnitCreatingRequestValidation
        : AbstractValidator<CharityUnitCreatingRequest>
    {
        private IConfiguration _config;

        public CharityUnitCreatingRequestValidation(IConfiguration configuration)
        {
            _config = configuration;

            RuleFor(x => x.Email)
                .NotNull()
                .EmailAddress()
                .WithMessage("Email không đúng định dạng.")
                .Length(5, 100)
                .WithMessage("Email phải từ 5 tới 100 kí tự");

            RuleFor(x => x.Name)
                .NotNull()
                .WithMessage("Tên không được để trống.")
                .MinimumLength(5)
                .WithMessage("Tên phải từ 5 đến 100 kí tự.")
                .MaximumLength(100)
                .WithMessage("Tên phải từ 5 đến 100 kí tự.");
            RuleFor(x => x.Phone)
                .NotEmpty()
                .WithMessage("Số điện thoại không được để trống.")
                .Length(9, 14)
                .WithMessage("Số điện thoại sai định dạng.")
                .Matches(@"^\+?\d{1,3}[-. \(\)]?\d{1,14}$")
                .WithMessage("Số điện thoại chỉ được chứa chữ số 0-9.");
            RuleFor(x => x.Address)
                .Length(10, 250)
                .NotNull()
                .WithMessage("Địa chỉ phải có từ 10 đến 250 kí tự.");

            RuleFor(x => x.Location)
                .NotNull()
                .Must(locations => locations == null || locations.Length == 2)
                .WithMessage("Location phải chứa đúng 2 giá trị (latitude và longitude).");
            RuleFor(x => x.Image)
                .NotNull()
                .Must(HaveValidImageExtension)
                .WithMessage(
                    "Logo phải là một tệp hình ảnh hợp lệ (jpg, jpeg, png, gif) và có kích thước nhỏ hơn 10MB."
                );

            RuleFor(x => x.Description).MinimumLength(50).WithMessage("Ghi chú phải từ 50 kí tự.");

            RuleFor(x => x.LegalDocument)
                .NotNull()
                .Must(HaveValidDocxAndPdfExtension)
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
