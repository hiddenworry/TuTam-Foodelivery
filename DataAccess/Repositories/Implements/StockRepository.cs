using DataAccess.DbContextData;
using DataAccess.Entities;
using DataAccess.EntityEnums;
using Microsoft.EntityFrameworkCore;

namespace DataAccess.Repositories.Implements
{
    public class StockRepository : IStockRepository
    {
        private readonly FoodDonationDeliveryDbContext _context;

        public StockRepository(FoodDonationDeliveryDbContext context)
        {
            _context = context;
        }

        public async Task<int> AddStockAsync(Stock stock)
        {
            Item? item = _context.Items
                .Include(a => a.ItemTemplate)
                .FirstOrDefault(a => a.Id == stock.ItemId);
            if (item != null)
            {
                stock.StockCode = GenerateStockCode(
                    item.ItemTemplate.Name.Trim(),
                    stock.ExpirationDate
                );
            }
            else
            {
                stock.StockCode = GenerateStockCode("UNKNOWN", stock.ExpirationDate);
            }

            await _context.Stocks.AddAsync(stock);
            return await _context.SaveChangesAsync() > 0 ? 1 : 0;
        }

        public async Task<Stock?> FindStockByItemIdAndExpirationDateAndBranchIdAndUserIdAndActivityId(
            Guid itemId,
            DateTime expirationDate,
            Guid branchId,
            Guid? userId,
            Guid? activityId
        )
        {
            return await _context.Stocks.FirstOrDefaultAsync(
                s =>
                    s.ItemId == itemId
                    && s.ExpirationDate == expirationDate
                    && s.BranchId == branchId
                    && s.Status == StockStatus.VALID
                    && s.UserId == userId
                    && s.ActivityId == activityId
            );
        }

        public async Task<List<Stock>> GetCurrentValidStocksByItemIdAndBranchId(
            Guid itemId,
            Guid branchId
        )
        {
            return await _context.Stocks
                .Include(s => s.StockUpdatedHistoryDetails)
                .Where(
                    s =>
                        s.BranchId == branchId
                        && s.ItemId == itemId
                        && s.Status == StockStatus.VALID
                        && s.Quantity > 0
                )
                .ToListAsync();
        }

        public async Task<int> UpdateStockAsync(Stock stock)
        {
            _context.Stocks.Update(stock);
            return await _context.SaveChangesAsync() > 0 ? 1 : 0;
        }

        public async Task<int> UpdateStocksAsync(List<Stock> stocks)
        {
            int rs = 0;
            foreach (Stock item in stocks)
            {
                rs += await UpdateStockAsync(item);
            }
            return rs;
        }

        //public async Task<Stock?> GetCurrentValidStocksById(Guid Id)
        //{
        //    return await _context.Stocks.Where(s => s.Id == Id).FirstOrDefaultAsync();
        //}

        public async Task<List<Stock>> GetStocksByItemIdAndBranchId(Guid itemId, Guid branchId)
        {
            return await _context.Stocks
                .Where(s => s.BranchId == branchId && s.ItemId == itemId && s.Quantity > 0)
                .ToListAsync();
        }

        public async Task<Stock?> GetExpiredStocksByIdAndBranchId(Guid stockId, Guid branchId)
        {
            return await _context.Stocks.FirstOrDefaultAsync(
                s => s.BranchId == branchId && s.Id == stockId && s.Status == StockStatus.EXPIRED
            );
        }

        public async Task<Stock?> GetStocksByItemIdAndBranchIdAndExpirationDate(
            Guid itemId,
            Guid branchId,
            DateTime expirationDate
        )
        {
            return await _context.Stocks
                .Where(
                    s =>
                        s.BranchId == branchId
                        && s.ItemId == itemId
                        && s.Status == StockStatus.VALID
                        && s.ExpirationDate == expirationDate
                )
                .FirstOrDefaultAsync();
        }

        public async Task<List<Stock>?> GetStocksAsync(
            Guid? itemId,
            Guid? branchId,
            DateTime? expirationDate
        )
        {
            var query = _context.Stocks
                .Include(i => i.Item.ItemTemplate)
                .Include(a => a.StockUpdatedHistoryDetails)
                .AsQueryable();
            if (itemId != null)
            {
                query = query.Where(a => a.ItemId == itemId);
            }
            if (branchId != null)
            {
                query = query.Where(a => a.BranchId == branchId);
            }
            if (expirationDate != null)
            {
                DateTime endDate = expirationDate.Value.Date.AddDays(1).AddTicks(-1);
                query = query.Where(
                    a => a.ExpirationDate >= expirationDate && a.ExpirationDate <= endDate
                );
            }
            query = query.Where(a => a.Status == StockStatus.VALID && a.Quantity > 0);
            return await query.ToListAsync();
        }

        private string GenerateStockCode(string name, DateTime expiryDate)
        {
            // Lấy 8 ký tự đầu tiên của tên và chuyển thành chữ hoa
            string formattedName = RemoveUnicode(new string(name.Take(8).ToArray()).ToUpper())
                .Replace(" ", "");
            ;

            string formattedDate = expiryDate.ToString("yyyyMMdd"); // Định dạng ngày
            string randomString = GetRandomString(5); // Tạo chuỗi ngẫu nhiên gồm 5 ký tự

            return $"{formattedName}-{formattedDate}-{randomString}";
        }

        private string GetRandomString(int length)
        {
            Random _random = new();
            const string chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789";
            return new string(
                Enumerable.Repeat(chars, length).Select(s => s[_random.Next(s.Length)]).ToArray()
            );
        }

        private string RemoveUnicode(string text)
        {
            string[] arr1 = new string[]
            {
                "á",
                "à",
                "ả",
                "ã",
                "ạ",
                "â",
                "ấ",
                "ầ",
                "ẩ",
                "ẫ",
                "ậ",
                "ă",
                "ắ",
                "ằ",
                "ẳ",
                "ẵ",
                "ặ",
                "đ",
                "é",
                "è",
                "ẻ",
                "ẽ",
                "ẹ",
                "ê",
                "ế",
                "ề",
                "ể",
                "ễ",
                "ệ",
                "í",
                "ì",
                "ỉ",
                "ĩ",
                "ị",
                "ó",
                "ò",
                "ỏ",
                "õ",
                "ọ",
                "ô",
                "ố",
                "ồ",
                "ổ",
                "ỗ",
                "ộ",
                "ơ",
                "ớ",
                "ờ",
                "ở",
                "ỡ",
                "ợ",
                "ú",
                "ù",
                "ủ",
                "ũ",
                "ụ",
                "ư",
                "ứ",
                "ừ",
                "ử",
                "ữ",
                "ự",
                "ý",
                "ỳ",
                "ỷ",
                "ỹ",
                "ỵ",
            };
            string[] arr2 = new string[]
            {
                "a",
                "a",
                "a",
                "a",
                "a",
                "a",
                "a",
                "a",
                "a",
                "a",
                "a",
                "a",
                "a",
                "a",
                "a",
                "a",
                "a",
                "d",
                "e",
                "e",
                "e",
                "e",
                "e",
                "e",
                "e",
                "e",
                "e",
                "e",
                "e",
                "i",
                "i",
                "i",
                "i",
                "i",
                "o",
                "o",
                "o",
                "o",
                "o",
                "o",
                "o",
                "o",
                "o",
                "o",
                "o",
                "o",
                "o",
                "o",
                "o",
                "o",
                "o",
                "u",
                "u",
                "u",
                "u",
                "u",
                "u",
                "u",
                "u",
                "u",
                "u",
                "u",
                "y",
                "y",
                "y",
                "y",
                "y",
            };
            for (int i = 0; i < arr1.Length; i++)
            {
                text = text.Replace(arr1[i], arr2[i]);
                text = text.Replace(arr1[i].ToUpper(), arr2[i].ToUpper());
            }
            return text;
        }
    }
}
