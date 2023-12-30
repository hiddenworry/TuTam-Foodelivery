using BusinessLogic.Utils.FirebaseService;
using DataAccess.Models.Responses;
using OfficeOpenXml;

namespace BusinessLogic.Utils.ExcelService.Implements
{
    public class ExcelService : IExcelService
    {
        private readonly IFirebaseStorageService _firebaseStorageService;

        public ExcelService(IFirebaseStorageService firebaseStorageService)
        {
            _firebaseStorageService = firebaseStorageService;
        }

        public async Task<string> CreateExcelFile(List<StockUpdateHistoryDetailResponse> items)
        {
            ExcelPackage.LicenseContext = LicenseContext.NonCommercial;
            string rs = string.Empty;
            using (var memoryStream = new MemoryStream())
            {
                // Create an Excel package with the memory stream
                using (var package = new ExcelPackage(memoryStream))
                {
                    var worksheet = package.Workbook.Worksheets.Add("Data");

                    // Define the headers
                    string[] headers = new string[]
                    {
                        "ID",
                        "Số lượng",
                        "Tên",
                        "Thuộc tính",
                        "Đơn vị",
                        "Điểm lấy hàng",
                        "Điểm nhận hàng",
                        "Người quyên góp",
                        "Ngày tạo",
                        "Loại(Xuất/Nhập)",
                        "Ghi chú",
                        "Tên hoạt động tương ứng",
                    };

                    for (int i = 0; i < headers.Length; i++)
                    {
                        worksheet.Cells[1, i + 1].Value = headers[i];
                    }

                    int row = 2;

                    foreach (var item in items)
                    {
                        worksheet.Cells[row, 1].Value = item.Id;
                        worksheet.Cells[row, 2].Value = item.Quantity;
                        worksheet.Cells[row, 3].Value = item.Name ?? string.Empty;
                        if (item.AttributeValues != null)
                        {
                            worksheet.Cells[row, 4].Value =
                                string.Join(", ", item.AttributeValues) ?? string.Empty;
                        }
                        else
                        {
                            worksheet.Cells[row, 4].Value = "";
                        }

                        worksheet.Cells[row, 5].Value = item.Unit ?? string.Empty;
                        worksheet.Cells[row, 6].Value = item.PickUpPoint ?? string.Empty;
                        worksheet.Cells[row, 7].Value = item.DeliveryPoint ?? string.Empty;
                        worksheet.Cells[row, 8].Value = item.DonorName ?? string.Empty;
                        worksheet.Cells[row, 9].Value = item.CreatedDate.ToString() ?? string.Empty;
                        if (item.Type == "EXPORT")
                        {
                            worksheet.Cells[row, 10].Value = "Xuất kho" ?? string.Empty;
                        }
                        else
                        {
                            worksheet.Cells[row, 10].Value = "Nhập kho" ?? string.Empty;
                        }

                        worksheet.Cells[row, 11].Value = item.Note ?? string.Empty;
                        worksheet.Cells[row, 12].Value = item.ActivityName ?? string.Empty;

                        // Move to the next row
                        row++;
                    }

                    package.Save();

                    var uniqueFileName = $"StockUpdate_{DateTime.Now:yyyyMMddHHmmss}.xlsx";
                    rs = await _firebaseStorageService.UploadImageToFirebase(
                        memoryStream,
                        uniqueFileName
                    );
                }
            }

            return rs;
        }
    }
}
