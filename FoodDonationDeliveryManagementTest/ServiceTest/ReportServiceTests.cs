using BusinessLogic.Services.Implements;
using DataAccess.Entities;
using DataAccess.EntityEnums;
using DataAccess.Repositories;
using Microsoft.Extensions.Configuration;
using Moq;

namespace FoodDonationDeliveryManagementTest.ServiceTest
{
    public class ReportServiceTests
    {
        private Mock<IReportRepository> _mockReportRepository;
        private Mock<IConfiguration> _mockConfiguration;
        private ReportService _service;
        private Mock<IUserRepository> _mockUserRepository;

        [SetUp]
        public void Setup()
        {
            _mockReportRepository = new Mock<IReportRepository>();
            _mockConfiguration = new Mock<IConfiguration>();
            _mockUserRepository = new Mock<IUserRepository>();
            _service = new ReportService(
                _mockReportRepository.Object,
                _mockConfiguration.Object,
                _mockUserRepository.Object
            );
        }

        [Test]
        public async Task GetReportAsync_WithData_ShouldReturnPaginatedResponse()
        {
            // Arrange

            var mockReports = new List<Report>
            {
                new Report
                {
                    Id = Guid.NewGuid(),
                    UserId = Guid.NewGuid(),
                    User = new User
                    {
                        Name = "User1",
                        Email = "user1@example.com",
                        Phone = "1234567890"
                    },
                    CreatedDate = DateTime.Now,
                    Content = "Report Content 1",
                    Type = ReportType.MISSING_ITEMS_FROM_CONTRIBUTOR,
                },
                // Add more mock Report objects
            };
            _mockReportRepository
                .Setup(
                    repo =>
                        repo.GetReportsAsync(
                            It.IsAny<Guid?>(),
                            It.IsAny<string>(),
                            It.IsAny<ReportType>()
                        )
                )
                .ReturnsAsync(mockReports);

            // _mockConfiguration.Setup(c => c["ResponseMessages:AuthenticationMsg:InternalServerErrorMsg"]).Returns("Internal Server Error");



            int? page = 1;
            int? pageSize = 10;
            Guid? userId = Guid.NewGuid();

            string? keyWord = null; // or a specific keyword
            var result = await _service.GetReportAsync(page, pageSize, userId, keyWord, null);
            Assert.That(result.Status, Is.EqualTo(200));
            Assert.IsNotNull(result.Data);
        }
    }
}
