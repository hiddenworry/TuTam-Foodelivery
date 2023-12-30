using BusinessLogic.Services;
using BusinessLogic.Utils.SecurityServices;
using DataAccess.Models.Requests;
using DataAccess.Models.Responses;
using FoodDonationDeliveryManagementAPI.Controllers;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Moq;

namespace FoodDonationDeliveryManagementTest.ControllerTests
{
    public class AuthenticationControllerTests
    {
        private AuthenticationController _controller;
        private Mock<IUserService> _userServiceMock;
        private Mock<ITokenBlacklistService> _tokenBlacklistServiceMock;
        private Mock<IJwtService> _jwtServiceMock;
        private Mock<IConfiguration> _configMock;
        private Mock<ILogger<AuthenticationController>> _loggerMock;

        [SetUp]
        public void Setup()
        {
            // Tạo các mock cho các dịch vụ và đối tượng cần thiết
            _userServiceMock = new Mock<IUserService>();
            _tokenBlacklistServiceMock = new Mock<ITokenBlacklistService>();
            _jwtServiceMock = new Mock<IJwtService>();
            _configMock = new Mock<IConfiguration>();
            _loggerMock = new Mock<ILogger<AuthenticationController>>();

            // Khởi tạo AuthenticationController sử dụng các mock đã tạo
            _controller = new AuthenticationController(
                _userServiceMock.Object,
                _tokenBlacklistServiceMock.Object,
                _jwtServiceMock.Object,
                _configMock.Object,
                _loggerMock.Object
            );
        }

        [Test]
        public async Task Authenticate_ReturnsOkResult_WhenAuthenticationSucceeds()
        {
            // Arrange
            var loginRequest = new LoginRequest { UserName = "user", Password = "password" };
            _userServiceMock
                .Setup(x => x.AuthenticateAsync(It.IsAny<LoginRequest>()))
                .ReturnsAsync(new CommonResponse { Status = 200 });
            // Act
            var result = await _controller.Authenticate(loginRequest) as ObjectResult;
            // Assert
            Assert.IsNotNull(result);
            Assert.IsNotNull(result.StatusCode);
            Assert.That(result.StatusCode, Is.EqualTo(200));
            // Kiểm tra dữ liệu trả về (nếu có)
            if (result.StatusCode == 200)
            {
                var responseData = result.Value as CommonResponse;
                Assert.IsNotNull(responseData);
                // Thực hiện kiểm tra dữ liệu cụ thể trong CommonResponse
                // Ví dụ: Assert.AreEqual(expectedDataProperty, responseData.SomeProperty);
            }
        }

        [Test]
        public async Task Authenticate_ReturnsBadRequestResult_WhenAuthenticationFails()
        {
            // Arrange
            var loginRequest = new LoginRequest { UserName = "user", Password = "password" };
            _userServiceMock
                .Setup(x => x.AuthenticateAsync(It.IsAny<LoginRequest>()))
                .ReturnsAsync(new CommonResponse { Status = 401 });
            // Act
            var result = await _controller.Authenticate(loginRequest) as ObjectResult;
            // Assert
            Assert.IsNotNull(result);
            Assert.IsNotNull(result.StatusCode);
            Assert.That(result.StatusCode, Is.EqualTo(401));
            // Kiểm tra dữ liệu trả về (nếu có)
            if (result.StatusCode == 401)
            {
                var responseData = result.Value as CommonResponse;
                Assert.IsNotNull(responseData);
                // Thực hiện kiểm tra dữ liệu cụ thể trong CommonResponse
                // Ví dụ: Assert.AreEqual(expectedDataProperty, responseData.SomeProperty);
            }
        }

        [Test]
        public async Task Authenticate_ReturnsInternalServerErrorResult_WhenAuthenticationFails()
        {
            // Arrange
            var loginRequest = new LoginRequest { UserName = "user", Password = "password" };
            _userServiceMock
                .Setup(x => x.AuthenticateAsync(It.IsAny<LoginRequest>()))
                .ReturnsAsync(new CommonResponse { Status = 500 });
            // Act
            var result = await _controller.Authenticate(loginRequest) as ObjectResult;
            // Assert
            Assert.IsNotNull(result);
            Assert.IsNotNull(result.StatusCode);
            Assert.That(result.StatusCode, Is.EqualTo(500));
            // Kiểm tra dữ liệu trả về (nếu có)
            if (result.StatusCode == 500)
            {
                var responseData = result.Value as CommonResponse;
                Assert.IsNotNull(responseData);
                // Thực hiện kiểm tra dữ liệu cụ thể trong CommonResponse
                // Ví dụ: Assert.AreEqual(expectedDataProperty, responseData.SomeProperty);
            }
        }

        [Test]
        public async Task Authenticate_ReturnsForbidentResult_WhenAuthenticationFails()
        {
            // Arrange
            var loginRequest = new LoginRequest { UserName = "user", Password = "password" };
            _userServiceMock
                .Setup(x => x.AuthenticateAsync(It.IsAny<LoginRequest>()))
                .ReturnsAsync(new CommonResponse { Status = 403 });
            // Act
            var result = await _controller.Authenticate(loginRequest) as ObjectResult;
            // Assert
            Assert.IsNotNull(result);
            Assert.IsNotNull(result.StatusCode);
            Assert.That(result.StatusCode, Is.EqualTo(403));
            // Kiểm tra dữ liệu trả về (nếu có)
            if (result.StatusCode == 403)
            {
                var responseData = result.Value as CommonResponse;
                Assert.IsNotNull(responseData);
                // Thực hiện kiểm tra dữ liệu cụ thể trong CommonResponse
                // Ví dụ: Assert.AreEqual(expectedDataProperty, responseData.SomeProperty);
            }
        }

        // Add more test cases for different scenarios

        [Test]
        public void Logout_WhenInternalErrorOccurs_ReturnsInternalServerError()
        {
            // Arrange
            _configMock
                .Setup(c => c["ResponseMessages:AuthenticationMsg:InternalServerErrorMsg"])
                .Returns("InternalServerErrorMsg");
            _jwtServiceMock
                .Setup(j => j.GetClaimsPrincipal(It.IsAny<string>()))
                .Throws(new Exception());

            // Act
            var result = _controller.Logout().Result;

            // Assert
            var statusCodeResult = result as ObjectResult;
            Assert.IsNotNull(statusCodeResult);

            var commonResponse = statusCodeResult.Value as CommonResponse;
            Assert.IsNotNull(commonResponse);
            Assert.That(commonResponse.Status, Is.EqualTo(500));
            Assert.That(commonResponse.Message, Is.EqualTo("InternalServerErrorMsg"));
        }
    }
}
