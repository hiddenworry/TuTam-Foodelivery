using BusinessLogic.Services.Implements;
using BusinessLogic.Utils.ExcelService;
using BusinessLogic.Utils.Notification.Implements;
using BusinessLogic.Utils.SecurityServices;
using DataAccess.Repositories;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Moq;

namespace FoodDonationDeliveryManagementTest.ServiceTest
{
    public class StockUpdatedHistoryServiceTest
    {
        private Mock<IStockUpdatedHistoryRepository> _mockStockUpdatedHistoryRepository;
        private Mock<ILogger<StockUpdatedHistoryDetailService>> _mockLogger;
        private Mock<IConfiguration> _mockConfig;
        private Mock<IItemRepository> _mockItemRepository;
        private Mock<IExcelService> _mockExcelService;
        private Mock<IStockRepository> _mockStockRepository;
        private Mock<IUserRepository> _mockUserRepository;
        private Mock<IActivityRepository> _mockActivityRepository;
        private Mock<IBranchRepository> _mockBranchRepository;
        private Mock<IStockUpdatedHistoryDetailRepository> _mockStockUpdatedHistoryDetailRepository;
        private Mock<IRoleRepository> _mockRoleRepository;
        private Mock<IPasswordHasher> _mockPasswordHasher;
        private Mock<IActivityBranchRepository> _mockActivityBranchRepository;
        private Mock<IHubContext<NotificationSignalSender>> _mockHubContext;
        private Mock<INotificationRepository> _mockNotificationRepository;
        private Mock<IDeliveryItemRepository> _mockDeliveryItemRepository;
        private Mock<IAidRequestRepository> _mockAidRequestRepository;

        [SetUp]
        public void Setup()
        {
            _mockStockUpdatedHistoryRepository = new Mock<IStockUpdatedHistoryRepository>();
            _mockLogger = new Mock<ILogger<StockUpdatedHistoryDetailService>>();
            _mockConfig = new Mock<IConfiguration>();
            _mockItemRepository = new Mock<IItemRepository>();
            _mockExcelService = new Mock<IExcelService>();
            _mockStockRepository = new Mock<IStockRepository>();
            _mockUserRepository = new Mock<IUserRepository>();
            _mockActivityRepository = new Mock<IActivityRepository>();
            _mockBranchRepository = new Mock<IBranchRepository>();
            _mockStockUpdatedHistoryDetailRepository =
                new Mock<IStockUpdatedHistoryDetailRepository>();
            _mockRoleRepository = new Mock<IRoleRepository>();
            _mockPasswordHasher = new Mock<IPasswordHasher>();
            _mockActivityBranchRepository = new Mock<IActivityBranchRepository>();
            _mockHubContext = new Mock<IHubContext<NotificationSignalSender>>();
            _mockNotificationRepository = new Mock<INotificationRepository>();
            _mockDeliveryItemRepository = new Mock<IDeliveryItemRepository>();
            _mockAidRequestRepository = new Mock<IAidRequestRepository>();
        }
    }
}
