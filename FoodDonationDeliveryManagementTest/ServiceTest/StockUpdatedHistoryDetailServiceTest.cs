using BusinessLogic.Services.Implements;
using BusinessLogic.Utils.ExcelService;
using DataAccess.Entities;
using DataAccess.Repositories;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Moq;

namespace FoodDonationDeliveryManagementTest.ServiceTest
{
    public class StockUpdatedHistoryDetailServiceTest
    {
        private Mock<IStockUpdatedHistoryDetailRepository> _mockStockUpdatedHistoryDetailRepository;
        private Mock<IConfiguration> _mockConfiguration;
        private Mock<ILogger<StockUpdatedHistoryDetailService>> _mockLogger;
        private Mock<IItemRepository> _mockItemRepository;
        private Mock<IExcelService> _mockExcelService;
        private Mock<IStockRepository> _mockStockRepository;
        private Mock<IStockUpdatedHistoryRepository> _mockStockUpdatedHistoryRepository;
        private Mock<IUserRepository> _mockUserRepository;
        private Mock<IActivityRepository> _mockActivityRepository;
        private Mock<IBranchRepository> _mockBranchRepository;

        private StockUpdatedHistoryDetailService _service;

        [SetUp]
        public void Setup()
        {
            _mockStockUpdatedHistoryDetailRepository =
                new Mock<IStockUpdatedHistoryDetailRepository>();
            _mockConfiguration = new Mock<IConfiguration>();
            _mockLogger = new Mock<ILogger<StockUpdatedHistoryDetailService>>();
            _mockItemRepository = new Mock<IItemRepository>();
            _mockExcelService = new Mock<IExcelService>();
            _mockStockRepository = new Mock<IStockRepository>();
            _mockStockUpdatedHistoryRepository = new Mock<IStockUpdatedHistoryRepository>();
            _mockUserRepository = new Mock<IUserRepository>();
            _mockActivityRepository = new Mock<IActivityRepository>();
            _mockBranchRepository = new Mock<IBranchRepository>();

            _service = new StockUpdatedHistoryDetailService(
                _mockStockUpdatedHistoryDetailRepository.Object,
                _mockConfiguration.Object,
                _mockLogger.Object,
                _mockItemRepository.Object,
                _mockExcelService.Object,
                _mockStockRepository.Object,
                _mockStockUpdatedHistoryRepository.Object,
                _mockUserRepository.Object,
                _mockActivityRepository.Object,
                _mockBranchRepository.Object
            );
        }

        [Test]
        public async Task GetStockUpdateHistoryDetailsOfBranch_WithData_ShouldReturnPaginatedResponse()
        {
            // Arrange
            var mockStockUpdatedHistoryDetails = new List<StockUpdatedHistoryDetail>
            {
                // Populate with test data
            };

            _mockStockUpdatedHistoryDetailRepository
                .Setup(
                    repo =>
                        repo.GetStockUpdateHistoryDetailsByBranchId(
                            It.IsAny<Guid>(),
                            It.IsAny<DateTime?>(),
                            It.IsAny<DateTime?>()
                        )
                )
                .ReturnsAsync(mockStockUpdatedHistoryDetails);

            int? page = 1;
            int? pageSize = 10;
            Guid branchId = Guid.NewGuid();

            // Act
            var result = await _service.GetStockUpdateHistoryDetailsOfBranch(
                page,
                pageSize,
                branchId,
                null,
                null
            );

            // Assert
            Assert.That(result.Status, Is.EqualTo(200));
        }

        //[Test]
        //public async Task GetStockUpdateHistoryDetailsOfActivity_WithData_ShouldReturnPaginatedResponse()
        //{
        //    // Arrange
        //    var mockStockUpdatedHistoryDetails = new List<StockUpdatedHistoryDetail>
        //    {
        //        // Populate with test data
        //    };

        //    _mockStockUpdatedHistoryDetailRepository
        //        .Setup(
        //            repo =>
        //                repo.GetStockUpdateHistoryDetailsByBranchId(
        //                    It.IsAny<Guid>(),
        //                    It.IsAny<DateTime?>(),
        //                    It.IsAny<DateTime?>()
        //                )
        //        )
        //        .ReturnsAsync(mockStockUpdatedHistoryDetails);

        //    int? page = 1;
        //    int? pageSize = 10;
        //    Guid activityId = Guid.NewGuid();

        //    // Act
        //    var result = await _service.GetStockUpdateHistoryDetailsOfActivity(
        //        page,
        //        pageSize,
        //        activityId,
        //        null,
        //        null
        //    );

        //    // Assert
        //    Assert.That(result.Status, Is.EqualTo(200));
        //}

        //[Test]
        //public async Task GetStockUpdateHistoryDetailsOfCharityUnit_WithData_ShouldReturnPaginatedResponse()
        //{
        //    // Arrange
        //    var mockStockUpdatedHistoryDetails = new List<StockUpdatedHistoryDetail>
        //    {
        //        // Populate with test data
        //    };

        //    _mockStockUpdatedHistoryDetailRepository
        //        .Setup(
        //            repo =>
        //                repo.GetStockUpdateHistoryDetailsByBranchId(
        //                    It.IsAny<Guid>(),
        //                    It.IsAny<DateTime?>(),
        //                    It.IsAny<DateTime?>()
        //                )
        //        )
        //        .ReturnsAsync(mockStockUpdatedHistoryDetails);

        //    int? page = 1;
        //    int? pageSize = 10;
        //    Guid charityUnitId = Guid.NewGuid();

        //    // Act
        //    var result = await _service.GetStockUpdateHistoryByCharityUnit(
        //        page,
        //        pageSize,
        //        charityUnitId,
        //        null,
        //        null
        //    );

        //    // Assert
        //    Assert.That(result.Status, Is.EqualTo(200));
        //}
    }
}
