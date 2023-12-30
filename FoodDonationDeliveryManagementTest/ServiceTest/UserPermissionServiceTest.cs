using BusinessLogic.Services.Implements;
using DataAccess.Entities;
using DataAccess.EntityEnums;
using DataAccess.Models.Requests;
using DataAccess.Repositories;
using Microsoft.Extensions.Configuration;
using Moq;

namespace FoodDonationDeliveryManagementTest.ServiceTest
{
    public class UserPermissionServiceTest
    {
        private Mock<IUserPermissionRepository> _mockUserPermissionRepository;
        private Mock<IConfiguration> _configurationMock;
        private UserPermissionService _service;

        [SetUp]
        public void Setup()
        {
            _mockUserPermissionRepository = new Mock<IUserPermissionRepository>();
            _configurationMock = new Mock<IConfiguration>();
            _service = new UserPermissionService(
                _mockUserPermissionRepository.Object,
                _configurationMock.Object
            );
        }

        [Test]
        public async Task UpdateUserPermissionAsync_WhenUserNotFound_ShouldReturn400Status()
        {
            // Arrange
            var request = new UserPermissionRequest(); // Populate request as needed
            _mockUserPermissionRepository
                .Setup(
                    repo =>
                        repo.UpdateUserPermissionAsync(
                            It.IsAny<Guid>(),
                            It.IsAny<Guid>(),
                            It.IsAny<UserPermissionStatus>()
                        )
                )
                .ReturnsAsync((UserPermission)null!);

            // Act
            var response = await _service.UpdateUserPermissionAsync(request);

            // Assert
            Assert.That(response.Status, Is.EqualTo(500));
        }

        [Test]
        public async Task UpdateUserPermissionAsync_WhenSuccessful_ShouldReturn200Status()
        {
            // Arrange
            var request = new UserPermissionRequest
            {
                UserId = new Guid(),
                PermissionRequests = new List<PermissionRequest>()
            }; // Populate request as needed

            // Act
            var response = await _service.UpdateUserPermissionAsync(request);

            // Assert
            Assert.That(response.Status, Is.EqualTo(200));
        }

        // Add more tests for other scenarios in UpdateUserPermissionAsync

        [Test]
        public async Task GetPermissionsByUserAsync_WhenUserHasPermissions_ShouldReturnNonEmptyList()
        {
            // Arrange
            var userId = Guid.NewGuid();
            _mockUserPermissionRepository
                .Setup(repo => repo.GetPermissionsByUserIdAsync(userId))
                .ReturnsAsync(new List<UserPermission?> { new UserPermission() }!); // Return a list with one item
            var userPermissions = new List<UserPermission>
            {
                new UserPermission
                {
                    Permission = new Permission
                    {
                        Name = "Permission1",
                        DisplayName = "Display Permission 1"
                    },
                    PermissionId = Guid.NewGuid(),
                    UserId = Guid.NewGuid(), // Example UserId
                    Status = UserPermissionStatus.PERMITTED // Assuming an enum type for Status
                },
                // Add more UserPermission objects as needed
            };

            // Setup the mock to return this list
            _mockUserPermissionRepository
                .Setup(repo => repo.GetPermissionsByUserIdAsync(It.IsAny<Guid>()))
                .ReturnsAsync(userPermissions);
            _mockUserPermissionRepository
                .Setup(
                    repo =>
                        repo.UpdateUserPermissionAsync(
                            It.IsAny<Guid>(),
                            It.IsAny<Guid>(),
                            It.IsAny<UserPermissionStatus>()
                        )
                )
                .ReturnsAsync(new UserPermission()); // Return a valid object

            // Act
            var response = await _service.GetPermissionsByUserAsync(userId, null, null, null);

            // Assert
            Assert.IsNotNull(response.Data);
        }
    }
}
