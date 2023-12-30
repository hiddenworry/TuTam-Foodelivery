using BusinessLogic.Services.Implements;
using DataAccess.Entities;
using DataAccess.Repositories;
using Microsoft.Extensions.Configuration;
using Moq;

namespace FoodDonationDeliveryManagementTest.ServiceTest
{
    public class RoleServiceTests
    {
        private Mock<IRoleRepository> _mockRoleRepository;
        private Mock<IConfiguration> _mockConfig;
        private RoleService _service;

        [SetUp]
        public void Setup()
        {
            _mockRoleRepository = new Mock<IRoleRepository>();
            _mockConfig = new Mock<IConfiguration>();

            _service = new RoleService(_mockRoleRepository.Object, _mockConfig.Object);
        }

        [Test]
        public async Task GetAllRolesAsync_ShouldReturnRoles_WhenRolesAreAvailable()
        {
            // Arrange
            var fakeRoles = new List<Role>
            { /* populate with test data */
            };
            _mockRoleRepository.Setup(repo => repo.GetAllRolesAsync()).ReturnsAsync(fakeRoles);
            _mockConfig
                .Setup(c => c["ResponseMessages:CommonMsg:InternalServerErrorMsg"])
                .Returns("Internal Server Error");

            // Act
            var result = await _service.GetAllRolesAsync();

            // Assert
            Assert.That(result.Status, Is.EqualTo(200));
            Assert.That(result.Data, Is.EqualTo(fakeRoles));
        }
    }
}
