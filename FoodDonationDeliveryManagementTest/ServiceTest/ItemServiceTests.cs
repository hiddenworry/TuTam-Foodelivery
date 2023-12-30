using DataAccess.Repositories;
using Microsoft.Extensions.Logging;
using Moq;
using Microsoft.Extensions.Configuration;
using System.Collections;
using DataAccess.EntityEnums;
using BusinessLogic.Services;
using DataAccess.Entities;
using BusinessLogic.Services.Implements;

namespace FoodDonationDeliveryManagementTest.ServiceTest
{
    public class ItemServiceTests
    {
        private Mock<IItemRepository> _mockItemRepository;
        private Mock<IItemTemplateRepository> _mockItemTemplateRepository;
        private Mock<IConfiguration> _mockConfig;
        private Mock<ILogger<IItemTemplateService>> _mockLogger;
        private ItemService _service;

        [SetUp]
        public void Setup()
        {
            _mockItemRepository = new Mock<IItemRepository>();
            _mockItemTemplateRepository = new Mock<IItemTemplateRepository>();
            _mockConfig = new Mock<IConfiguration>();
            _mockLogger = new Mock<ILogger<IItemTemplateService>>();

            _service = new ItemService(
                _mockItemTemplateRepository.Object,
                _mockItemRepository.Object,
                _mockConfig.Object,
                _mockLogger.Object
            );
        }

        [Test]
        public async Task SearchItemForUser_ShouldReturnItems_WhenCalled()
        {
            // Arrange
            var fakeItems = new List<Item>
            { /* Populate with test data */
            };
            _mockItemRepository
                .Setup(
                    repo =>
                        repo.SelectRelevanceByKeyWordAsync(
                            It.IsAny<string>(),
                            It.IsAny<int?>(),
                            It.IsAny<ItemCategoryType?>(),
                            It.IsAny<Guid?>()
                        )
                )
                .ReturnsAsync(fakeItems);

            // Act
            var response = await _service.SearchItemForUser("searchTerm", null, null, 1, 10);

            // Assert
            Assert.That(response.Status, Is.EqualTo(200));
            Assert.IsNotNull(response.Data);
        }

        [Test]
        public async Task SearchItemForUser_ShouldReturnStatus200_WhenNoItemsAreFound()
        {
            // Arrange
            _mockItemRepository
                .Setup(
                    repo =>
                        repo.SelectRelevanceByKeyWordAsync(
                            It.IsAny<string>(),
                            It.IsAny<int?>(),
                            It.IsAny<ItemCategoryType?>(),
                            It.IsAny<Guid?>()
                        )
                )
                .ReturnsAsync(new List<Item>());

            // Act
            var response = await _service.SearchItemForUser("nonexistent", null, null, 1, 10);

            // Assert
            Assert.That(response.Status, Is.EqualTo(200));
        }

        [Test]
        public async Task SearchItemForUser_ShouldReturnInternalServerError_WhenExceptionOccurs()
        {
            // Arrange
            _mockItemRepository
                .Setup(
                    repo =>
                        repo.SelectRelevanceByKeyWordAsync(
                            It.IsAny<string>(),
                            It.IsAny<int?>(),
                            It.IsAny<ItemCategoryType?>(),
                            It.IsAny<Guid?>()
                        )
                )
                .ThrowsAsync(new Exception("Test exception"));

            // Act
            var response = await _service.SearchItemForUser("searchTerm", null, null, 1, 10);

            Assert.That(response.Status, Is.EqualTo(500));
        }

        [Test]
        public async Task GetItemById_ShouldReturnItem_WhenItemExists()
        {
            // Arrange
            var fakeItemId = Guid.NewGuid();
            var fakeItem = new Item
            {
                Id = Guid.NewGuid(),
                ItemAttributeValues = new List<ItemAttributeValue>
                {
                    new ItemAttributeValue
                    {
                        AttributeValue = new AttributeValue { Value = "AttributeValue1" }
                    },
                    // ... potentially more attribute values ...
                },
                ItemTemplate = new ItemTemplate
                {
                    Name = "ItemTemplateName",
                    Unit = new ItemUnit { Name = "UnitName" },
                    ItemCategory = new ItemCategory
                    {
                        Name = "CategoryName",
                        Type = ItemCategoryType.FOOD
                    },
                },
                MaximumTransportVolume = 10,
                EstimatedExpirationDays = 5,
                Note = "ItemNote",
                Image = "ImageUrl"
            };
            _mockItemRepository
                .Setup(repo => repo.FindItemByIdAsync(fakeItemId))
                .ReturnsAsync(fakeItem);
            _mockConfig
                .Setup(c => c["ResponseMessages:CommonMsg:InternalServerErrorMsg"])
                .Returns("Internal Server Error");

            // Act
            var response = await _service.GetItemById(fakeItemId);

            // Assert
            Assert.That(response.Status, Is.EqualTo(200));
            Assert.IsNotNull(response.Data);
            // Additional assertions for verifying the data content
        }

        [Test]
        public async Task GetItemById_ShouldReturnNull_WhenItemDoesNotExist()
        {
            // Arrange
            var nonExistentItemId = Guid.NewGuid();
            _mockItemRepository
                .Setup(repo => repo.FindItemByIdAsync(nonExistentItemId))
                .ReturnsAsync((Item)null!);
            _mockConfig
                .Setup(c => c["ResponseMessages:CommonMsg:InternalServerErrorMsg"])
                .Returns("Internal Server Error");

            // Act
            var response = await _service.GetItemById(nonExistentItemId);

            // Assert
            Assert.That(response.Status, Is.EqualTo(200));
            Assert.IsNull(response.Data);
        }

        [Test]
        public async Task GetItemById_ShouldReturnInternalServerError_WhenExceptionOccurs()
        {
            // Arrange
            var itemId = Guid.NewGuid();
            _mockItemRepository
                .Setup(repo => repo.FindItemByIdAsync(itemId))
                .ThrowsAsync(new Exception("Test exception"));
            _mockConfig
                .Setup(c => c["ResponseMessages:CommonMsg:InternalServerErrorMsg"])
                .Returns("Internal Server Error");

            // Act
            var response = await _service.GetItemById(itemId);

            // Assert
            Assert.That(response.Status, Is.EqualTo(500));
            Assert.That(response.Message, Is.EqualTo("Internal Server Error"));
        }
    }
}
