using BusinessLogic.Services.Implements;
using DataAccess.Entities;
using DataAccess.Repositories;
using Microsoft.Extensions.Configuration;
using Moq;

namespace FoodDonationDeliveryManagementTest.ServiceTest
{
    public class ItemUnitServiceTests
    {
        private Mock<IItemUnitRepostitory> _mockItemUnitRepository;
        private Mock<IConfiguration> _mockConfig;
        private ItemUnitService _service;

        [SetUp]
        public void Setup()
        {
            _mockItemUnitRepository = new Mock<IItemUnitRepostitory>();
            _mockConfig = new Mock<IConfiguration>();

            _service = new ItemUnitService(_mockItemUnitRepository.Object, _mockConfig.Object);
        }

        [Test]
        public async Task GetItemUnitListAsync_ShouldReturnSuccess_WhenDataIsAvailable()
        {
            // Arrange
            var fakeItemUnits = new List<ItemUnit>
            { /* populate with test data */
            };
            _mockItemUnitRepository
                .Setup(repo => repo.GetListItemUnitAsync())
                .ReturnsAsync(fakeItemUnits);
            _mockConfig
                .Setup(c => c["ResponseMessages:CommonMsg:InternalServerErrorMsg"])
                .Returns("Internal Server Error");

            // Act
            var result = await _service.GetItemUnitListAsync();

            // Assert
            Assert.That(result.Status, Is.EqualTo(200));
            Assert.That(result.Data, Is.EqualTo(fakeItemUnits));
        }

        [Test]
        public async Task GetItemUnitListAsync_ShouldReturnInternalServerError_WhenExceptionOccurs()
        {
            // Arrange
            _mockItemUnitRepository
                .Setup(repo => repo.GetListItemUnitAsync())
                .Throws(new Exception());
            _mockConfig
                .Setup(c => c["ResponseMessages:CommonMsg:InternalServerErrorMsg"])
                .Returns("Internal Server Error");

            // Act
            var result = await _service.GetItemUnitListAsync();

            // Assert
            Assert.That(result.Status, Is.EqualTo(500));
            Assert.That(result.Message, Is.EqualTo("Internal Server Error"));
        }
    }
}
