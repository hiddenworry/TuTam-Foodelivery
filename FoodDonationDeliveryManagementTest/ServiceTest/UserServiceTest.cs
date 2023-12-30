using BusinessLogic.Services.Implements;
using BusinessLogic.Utils.EmailService;
using BusinessLogic.Utils.FirebaseService;
using BusinessLogic.Utils.SecurityServices;
using BusinessLogic.Utils.SmsService;
using DataAccess.Entities;
using DataAccess.EntityEnums;
using DataAccess.Models.Requests;
using DataAccess.Models.Responses;
using DataAccess.ModelsEnum;
using DataAccess.Repositories;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Moq;

namespace FoodDonationDeliveryManagementTest.ServiceTest
{
    public class UserServiceTest
    {
        private Mock<IUserRepository> _userRepositoryMock;
        private Mock<IJwtService> _jwtServiceMock;
        private Mock<IPermissionRepository> _permissionRepositoryMock;
        private Mock<IPasswordHasher> _passwordHasherMock;
        private Mock<IRoleRepository> _roleRepositoryMock;
        private Mock<IRolePermissionRepository> _rolePermissionRepositoryMock;
        private Mock<IUserPermissionRepository> _userPermissionRepositoryMock;
        private Mock<IEmailService> _emailServiceMock;
        private Mock<ISMSService> _smsServiceMock;
        private Mock<ILogger<UserService>> _loggerMock;
        private Mock<IFirebaseStorageService> _firebaseStorageServiceMock;
        private Mock<ICharityUnitRepository> _charityUnitRepositoryMock;
        private Mock<ICollaboratorRepository> _collaboratorRepositoryMock;
        private Mock<IConfiguration> _configurationMock;
        private UserService _userService;

        [SetUp]
        public void Setup()
        {
            _userRepositoryMock = new Mock<IUserRepository>();
            _jwtServiceMock = new Mock<IJwtService>();
            _permissionRepositoryMock = new Mock<IPermissionRepository>();
            _passwordHasherMock = new Mock<IPasswordHasher>();
            _roleRepositoryMock = new Mock<IRoleRepository>();
            _rolePermissionRepositoryMock = new Mock<IRolePermissionRepository>();
            _userPermissionRepositoryMock = new Mock<IUserPermissionRepository>();
            _emailServiceMock = new Mock<IEmailService>();
            _smsServiceMock = new Mock<ISMSService>();
            _loggerMock = new Mock<ILogger<UserService>>();
            _firebaseStorageServiceMock = new Mock<IFirebaseStorageService>();
            _charityUnitRepositoryMock = new Mock<ICharityUnitRepository>();
            _collaboratorRepositoryMock = new Mock<ICollaboratorRepository>();
            _configurationMock = new Mock<IConfiguration>();

            _userService = new UserService(
                _userRepositoryMock.Object,
                _jwtServiceMock.Object,
                _permissionRepositoryMock.Object,
                _configurationMock.Object,
                _passwordHasherMock.Object,
                _roleRepositoryMock.Object,
                _rolePermissionRepositoryMock.Object,
                _userPermissionRepositoryMock.Object,
                _emailServiceMock.Object,
                _smsServiceMock.Object,
                _loggerMock.Object,
                _firebaseStorageServiceMock.Object,
                _charityUnitRepositoryMock.Object,
                _collaboratorRepositoryMock.Object
            );
        }

        [Test]
        public async Task AuthenticateAsync_SuccessfulLogin_ReturnsStatus200()
        {
            _configurationMock.Setup(c => c["RefreshToken:ExpiredTimeDays"]).Returns("4.0");
            _configurationMock
                .Setup(c => c["ResponseMessages:AuthenticationMsg:LoginSuccessMsg"])
                .Returns("Login successful");
            var loginRequest = new LoginRequest
            {
                UserName = "User@gmail.com",
                Password = "1234567890a",
                LoginRole = UserRole.CONTRIBUTOR
            };
            var validRole = new Role { Name = RoleEnum.CONTRIBUTOR.ToString() };
            var validUser = new User { Role = validRole, Password = "hashpassword" };

            _userRepositoryMock
                .Setup(repo => repo.FindUserByEmailOrPhoneAsync(It.IsAny<string>()))
                .ReturnsAsync(validUser);

            _userRepositoryMock
                .Setup(
                    repo =>
                        repo.UpdateRefreshTokenAsync(
                            It.IsAny<Guid>(),
                            It.IsAny<string>(),
                            It.IsAny<DateTime>()
                        )
                )
                .ReturnsAsync(validUser);
            _userRepositoryMock
                .Setup(repo => repo.UpdateAccessTokenAsync(It.IsAny<Guid>(), It.IsAny<string>()))
                .ReturnsAsync(validUser);

            _passwordHasherMock
                .Setup(p => p.Verify(It.IsAny<string>(), It.IsAny<string>()))
                .Returns(true);

            var dateTimeProviderMock = new Mock<ISettedUpDateTime>();
            dateTimeProviderMock
                .Setup(s => s.GetCurrentVietNamTime())
                .Returns(new DateTime(2023, 1, 1));

            var authResponse = new AuthenticationResponse { AccessToken = "AccessToken" };
            _jwtServiceMock
                .Setup(j => j.GenerateAuthenResponse(It.IsAny<User>()))
                .Returns(authResponse);

            var result = await _userService.AuthenticateAsync(loginRequest);
            Assert.That(result.Status, Is.EqualTo(200));
            Assert.That(result.Message, Is.EqualTo("Login successful"));
            Assert.NotNull(result.Data);
        }

        [Test]
        public async Task AuthenticateAsync_Failed_ReturnsStatus401_IncorectPassword()
        {
            var loginRequest = new LoginRequest
            {
                UserName = "User@gnail.com",
                Password = "WrongPassword",
                LoginRole = UserRole.CONTRIBUTOR
            };
            var validRole = new Role { Name = RoleEnum.CONTRIBUTOR.ToString() };
            var validUser = new User { Role = validRole, Password = "hashpassword" };
            _passwordHasherMock
                .Setup(p => p.Verify(It.IsAny<string>(), It.IsAny<string>()))
                .Returns(true);
            _userRepositoryMock
                .Setup(repo => repo.FindUserByEmailOrPhoneAsync(It.IsAny<string>()))
                .ReturnsAsync((User)null!); // Explicitly return null

            _configurationMock
                .Setup(c => c["ResponseMessages:AuthenticationMsg:LoginFailedMsg"])
                .Returns("Login Failed");
            var result = await _userService.AuthenticateAsync(loginRequest);
            Assert.That(result.Status, Is.EqualTo(401));
            Assert.That(result.Message, Is.EqualTo("Login Failed"));
            Assert.Null(result.Data);
        }

        [Test]
        public async Task AuthenticateAsync_SuccessfulLogin_ReturnsStatus400_UserIsUnverify()
        {
            var loginRequest = new LoginRequest
            {
                UserName = "UnverifyUser@gmail.com",
                Password = "1234567890a",
                LoginRole = UserRole.CONTRIBUTOR
            };
            var validRole = new Role { Name = RoleEnum.CONTRIBUTOR.ToString() };
            var validUser = new User
            {
                Role = validRole,
                Password = "hash-password",
                Status = UserStatus.UNVERIFIED
            };
            _passwordHasherMock
                .Setup(p => p.Verify(It.IsAny<string>(), It.IsAny<string>()))
                .Returns(true);
            _userRepositoryMock
                .Setup(repo => repo.FindUserByEmailOrPhoneAsync(It.IsAny<string>()))
                .ReturnsAsync((validUser));

            _configurationMock
                .Setup(c => c["ResponseMessages:AuthenticationMsg:UnVerifyUserMsg"])
                .Returns("UnVerifyUserMsg");
            var result = await _userService.AuthenticateAsync(loginRequest);
            Assert.That(result.Status, Is.EqualTo(400));
            Assert.That(result.Message, Is.EqualTo("UnVerifyUserMsg"));
            Assert.Null(result.Data);
        }

        [Test]
        public async Task AuthenticateAsync_SuccessfulLogin_ReturnsStatus403_UserIsBaned()
        {
            var loginRequest = new LoginRequest
            {
                UserName = "BannedUser@gmail.com",
                Password = "1234567890a",
                LoginRole = UserRole.CONTRIBUTOR
            };
            var validRole = new Role { Name = RoleEnum.CONTRIBUTOR.ToString() };
            var validUser = new User
            {
                Role = validRole,
                Password = "password",
                Status = UserStatus.INACTIVE
            };
            _passwordHasherMock
                .Setup(p => p.Verify(It.IsAny<string>(), It.IsAny<string>()))
                .Returns(true);
            _userRepositoryMock
                .Setup(repo => repo.FindUserByEmailOrPhoneAsync(It.IsAny<string>()))
                .ReturnsAsync((validUser));

            _configurationMock
                .Setup(c => c["ResponseMessages:AuthenticationMsg:InactiveUserMsg"])
                .Returns("InactiveUserMsg");
            var result = await _userService.AuthenticateAsync(loginRequest);
            Assert.That(result.Status, Is.EqualTo(403));
            Assert.That(result.Message, Is.EqualTo("InactiveUserMsg"));
            Assert.Null(result.Data);
        }

        [Test]
        public async Task AuthenticateAsync_SuccessfulLogin_ReturnsStatus401_IncorectRole()
        {
            var loginRequest = new LoginRequest
            {
                UserName = "SystemAdmin@gmail.com",
                Password = "1234567890a",
                LoginRole = UserRole.CONTRIBUTOR
            };
            var validRole = new Role { Name = RoleEnum.BRANCH_ADMIN.ToString() };
            var validUser = new User { Role = validRole, Password = "password" };

            _userRepositoryMock
                .Setup(repo => repo.FindUserByEmailOrPhoneAsync(It.IsAny<string>()))
                .ReturnsAsync(validUser); // Explicitly return null

            _configurationMock
                .Setup(c => c["ResponseMessages:AuthenticationMsg:LoginFailedMsg"])
                .Returns("Login Failed");
            var result = await _userService.AuthenticateAsync(loginRequest);
            Assert.That(result.Status, Is.EqualTo(401));
            Assert.That(result.Message, Is.EqualTo("Login Failed"));
            Assert.Null(result.Data);
        }

        [Test]
        public async Task RefreshAccessTokenAsync_Successful_ReturnsStatus200()
        {
            _configurationMock
                .Setup(c => c["ResponseMessages:AuthenticationMsg:LoginSuccessMsg"])
                .Returns("Login successful");
            User user = new User { RefreshTokenExpiration = DateTime.Now.AddDays(1) };
            _userRepositoryMock
                .Setup(repo => repo.FindUserByRefreshTokenAsync(It.IsAny<string>()))
                .ReturnsAsync((user));
            // Arrange: Setup the refresh token request
            var refreshTokenRequest = new RefreshAccessTokenRequest
            {
                refreshToken = "sfsfrre4t54grer453534ert"
            };
            var dateTimeProviderMock = new Mock<ISettedUpDateTime>();
            dateTimeProviderMock.Setup(s => s.GetCurrentVietNamTime()).Returns(DateTime.Now);
            _jwtServiceMock
                .Setup(service => service.GenerateAuthenResponse(It.IsAny<User>()))
                .Returns(
                    new AuthenticationResponse { AccessToken = "string", RefreshToken = "string" }
                );
            _userRepositoryMock
                .Setup(repo => repo.UpdateAccessTokenAsync(It.IsAny<Guid>(), It.IsAny<string>()))
                .ReturnsAsync(user);
            var result = await _userService.RefreshAccessTokenAsync(refreshTokenRequest);

            // Assert: Verify the successful response
            Assert.That(result.Status, Is.EqualTo(200));
            Assert.That(result.Message, Is.EqualTo("Login successful"));
            Assert.NotNull(result.Data);
        }

        [Test]
        public async Task RefreshAccessTokenAsync_Fail_WhenUserIsBanned_ReturnsStatus400()
        {
            _configurationMock
                .Setup(c => c["ResponseMessages:AuthenticationMsg:InactiveUserMsg"])
                .Returns("InactiveUserMsg");
            User user = new User
            {
                RefreshTokenExpiration = DateTime.Now.AddDays(1),
                Status = UserStatus.INACTIVE
            };
            _userRepositoryMock
                .Setup(repo => repo.FindUserByRefreshTokenAsync(It.IsAny<string>()))
                .ReturnsAsync((user));

            var refreshTokenRequest = new RefreshAccessTokenRequest
            {
                refreshToken = "validRefreshToken"
            };

            var result = await _userService.RefreshAccessTokenAsync(refreshTokenRequest);

            // Assert: Verify the successful response
            Assert.That(result.Status, Is.EqualTo(403));
            Assert.That(result.Message, Is.EqualTo("InactiveUserMsg"));
            Assert.Null(result.Data);
        }

        [Test]
        public async Task RegisterUserByPhone_When_Success()
        {
            _configurationMock.Setup(c => c["OTP:ExpiredMinutes"]).Returns("4.0");
            _configurationMock
                .Setup(c => c["ResponseMessages:UserMsg:RegisterByPhoneSuccessMsg"])
                .Returns("RegisterByPhoneSuccessMsg");
            _configurationMock
                .Setup(c => c["ResponseMessages:UserMsg:DuplicatedUserMsg"])
                .Returns("DuplicatedUserMsg");
            var dateTimeProviderMock = new Mock<ISettedUpDateTime>();
            dateTimeProviderMock
                .Setup(s => s.GetCurrentVietNamTime())
                .Returns(new DateTime(2023, 1, 1));

            var existingUser = new User { Status = UserStatus.UNVERIFIED };
            _userRepositoryMock
                .Setup(repo => repo.FindUserByEmailOrPhoneAsync(It.IsAny<string>()))
                .ReturnsAsync(existingUser);
            _userRepositoryMock
                .Setup(repo => repo.CreateUserAsync(It.IsAny<User>()))
                .ReturnsAsync(existingUser);
            _userRepositoryMock
                .Setup(repo => repo.UpdateUserAsync(It.IsAny<User>()))
                .ReturnsAsync(existingUser);
            _smsServiceMock
                .Setup(service => service.sendSMS(It.IsAny<string>(), It.IsAny<string>()))
                .Returns(true);
            var request = new UserRegisterByPhoneRequest { Phone = "123456789", FullName = "abc" };

            // Act
            var response = await _userService.RegisterUserByPhone(request);

            Assert.That(response.Status, Is.EqualTo(200));
        }

        [Test]
        public async Task RegisterUserByPhone_When_DuplicatedPhone()
        {
            _configurationMock.Setup(c => c["OTP:ExpiredMinutes"]).Returns("4.0");
            _configurationMock
                .Setup(c => c["ResponseMessages:UserMsg:RegisterByPhoneSuccessMsg"])
                .Returns("RegisterByPhoneSuccessMsg");
            _configurationMock
                .Setup(c => c["ResponseMessages:UserMsg:DuplicatedUserMsg"])
                .Returns("DuplicatedUserMsg");
            var dateTimeProviderMock = new Mock<ISettedUpDateTime>();
            dateTimeProviderMock
                .Setup(s => s.GetCurrentVietNamTime())
                .Returns(new DateTime(2023, 1, 1));

            var existingUser = new User { Status = UserStatus.ACTIVE };
            _userRepositoryMock
                .Setup(repo => repo.FindUserByEmailOrPhoneAsync(It.IsAny<string>()))
                .ReturnsAsync(existingUser);
            _userRepositoryMock
                .Setup(repo => repo.CreateUserAsync(It.IsAny<User>()))
                .ReturnsAsync(existingUser);
            _userRepositoryMock
                .Setup(repo => repo.UpdateUserAsync(It.IsAny<User>()))
                .ReturnsAsync(existingUser);
            _smsServiceMock
                .Setup(service => service.sendSMS(It.IsAny<string>(), It.IsAny<string>()))
                .Returns(true);
            var request = new UserRegisterByPhoneRequest { Phone = "123456789", FullName = "abc" };

            // Act
            var response = await _userService.RegisterUserByPhone(request);

            Assert.That(response.Status, Is.EqualTo(400));
        }

        [Test]
        public async Task VerifyUserEmail_When_ValidVerificationPhone_UserSuccessfullyVerified()
        {
            _configurationMock
                .Setup(c => c["ResponseMessages:UserMsg:VerifyPhoneSuccessMsg"])
                .Returns("VerifyPhoneSuccessMsg");
            // Arrange
            var mockUser = new User { Status = UserStatus.UNVERIFIED };
            _userRepositoryMock
                .Setup(repo => repo.FindUserByVerifyCodeAsync(It.IsAny<string>()))
                .ReturnsAsync(mockUser);
            _userRepositoryMock
                .Setup(
                    repo =>
                        repo.FindUserByOtpcodeAndPhoneAsync(It.IsAny<string>(), It.IsAny<string>())
                )
                .ReturnsAsync(mockUser);
            _userRepositoryMock
                .Setup(repo => repo.UpdateUserAsync(It.IsAny<User>()))
                .ReturnsAsync(mockUser);
            List<RolePermission> rolePermissions = new List<RolePermission>();
            Role userRole = new Role { Id = Guid.NewGuid(), Name = "CONTRIBUTOR" };
            _roleRepositoryMock
                .Setup(r => r.GetRoleByName(It.Is<string>(name => name == "CONTRIBUTOR")))
                .ReturnsAsync(userRole);

            VerifyOtpRequest verifyOtpRequest = new VerifyOtpRequest
            {
                Otp = "1232",
                Phone = "0948457079"
            };

            // Act
            var response = await _userService.VerifyUserPhone(verifyOtpRequest);

            // Assert
            Assert.That(response.Status, Is.EqualTo(200));
            Assert.That(response.Message, Is.EqualTo("VerifyPhoneSuccessMsg"));
        }

        [Test]
        public async Task VerifyUserEmail_When_ValidVerificationPhone_UserUnSuccessfullyVerified()
        {
            _configurationMock
                .Setup(c => c["ResponseMessages:UserMsg:VerifyPhoneErrorMsg"])
                .Returns("VerifyPhoneErrorMsg");
            // Arrange
            var mockUser = new User { Status = UserStatus.INACTIVE };
            _userRepositoryMock
                .Setup(repo => repo.FindUserByVerifyCodeAsync(It.IsAny<string>()))
                .ReturnsAsync(mockUser);
            _userRepositoryMock
                .Setup(
                    repo =>
                        repo.FindUserByOtpcodeAndPhoneAsync(It.IsAny<string>(), It.IsAny<string>())
                )
                .ReturnsAsync(mockUser);
            _userRepositoryMock
                .Setup(repo => repo.UpdateUserAsync(It.IsAny<User>()))
                .ReturnsAsync((User)null!);
            List<RolePermission> rolePermissions = new List<RolePermission>();
            Role userRole = new Role { Id = Guid.NewGuid(), Name = "CONTRIBUTOR" };
            _roleRepositoryMock
                .Setup(r => r.GetRoleByName(It.Is<string>(name => name == "CONTRIBUTOR")))
                .ReturnsAsync(userRole);

            VerifyOtpRequest verifyOtpRequest = new VerifyOtpRequest
            {
                Otp = "1232",
                Phone = "0948457079"
            };

            // Act
            var response = await _userService.VerifyUserPhone(verifyOtpRequest);

            // Assert
            Assert.That(response.Status, Is.EqualTo(400));
            Assert.That(response.Message, Is.EqualTo("VerifyPhoneErrorMsg"));
        }

        public async Task Update_Profile_When_Successfull()
        {
            User mockUser = new User(); // Set properties as needed
            _userRepositoryMock
                .Setup(repo => repo.FindUserByIdAsync(It.IsAny<Guid>()))
                .ReturnsAsync(mockUser);
            _userRepositoryMock
                .Setup(repo => repo.UpdateUserAsync(It.IsAny<User>()))
                .ReturnsAsync(mockUser);

            _firebaseStorageServiceMock
                .Setup(
                    service => service.UploadImageToFirebase(It.IsAny<Stream>(), It.IsAny<string>())
                )
                .ReturnsAsync("http://mockimageurl.com");
            Guid testUserId = Guid.NewGuid();

            // Create a UserProfileRequest with mock data
            UserProfileRequest request = new UserProfileRequest
            {
                Name = "Test Name",
                Location = new double[] { 10.2, 106.3 },
                Address = "Test Address",
                // For Avatar, you need to mock an IFormFile, which can be complex. You might skip testing this part or use a simple string for testing purposes.
                // Avatar = MockIFormFile // This requires a more complex setup
            };
            var response = await _userService.UpdateProfile(testUserId, request);
            Assert.That(response.Status, Is.EqualTo(200));
            Assert.That(response.Message, Is.EqualTo("VerifyPhoneErrorMsg"));
        }

        public async Task Update_Profile_When_UserNotFound()
        {
            _configurationMock
                .Setup(c => c["ResponseMessages:UserMsg:UserNotFoundMsg"])
                .Returns("UserNotFoundMsg");
            _userRepositoryMock
                .Setup(repo => repo.FindUserByIdAsync(It.IsAny<Guid>()))
                .ReturnsAsync((User)null!);
            Guid testUserId = Guid.NewGuid();

            // Create a UserProfileRequest with mock data
            UserProfileRequest request = new UserProfileRequest
            {
                Name = "Test Name",
                Location = new double[] { 10.2, 106.3 },
                Address = "Test Address",
                // For Avatar, you need to mock an IFormFile, which can be complex. You might skip testing this part or use a simple string for testing purposes.
                // Avatar = MockIFormFile // This requires a more complex setup
            };
            var response = await _userService.UpdateProfile(testUserId, request);
            Assert.That(response.Status, Is.EqualTo(400));
            Assert.That(response.Message, Is.EqualTo("UserNotFoundMsg"));
        }

        [Test]
        public async Task GetProfile_ReturnSuccess()
        {
            // Arrange
            var userId = Guid.NewGuid();
            var mockUser = new User
            {
                // Populate necessary user properties, including Role
                Role = new Role { Name = RoleEnum.CHARITY.ToString() },
                // ... other properties
            };

            var mockCharityUnit = new CharityUnit
            {
                // Populate necessary charity unit properties
                IsHeadquarter = true,
                LegalDocuments = "Some Legal Documents",
                Description = "Charity Unit Description",
                Id = Guid.NewGuid(),
                Charity = new Charity
                {
                    // Populate necessary charity properties
                    Name = "Charity Name",
                    Id = Guid.NewGuid(),
                    Email = "charity@email.com",
                    Description = "Charity Description",
                    Logo = "Charity Logo URL"
                },
                // ... other properties
            };

            _userRepositoryMock
                .Setup(repo => repo.FindUserProfileByIdAsync(userId))
                .ReturnsAsync(mockUser);
            _charityUnitRepositoryMock
                .Setup(repo => repo.FindCharityUnitByUserIdAsync(userId))
                .ReturnsAsync(mockCharityUnit);

            // Act
            var result = await _userService.GetProfile(userId);
            Assert.NotNull(result);
            Assert.That(result.Status, Is.EqualTo(200));
            Assert.NotNull(result.Data);
        }

        [Test]
        public async Task GetProfile_Return_400()
        {
            // Arrange
            var userId = Guid.NewGuid();
            var mockUser = new User
            {
                // Populate necessary user properties, including Role
                Role = new Role { Name = RoleEnum.CHARITY.ToString() },
                // ... other properties
            };

            _userRepositoryMock
                .Setup(repo => repo.FindUserProfileByIdAsync(userId))
                .ReturnsAsync((User)null!);
            // Act
            var result = await _userService.GetProfile(userId);
            Assert.That(result.Status, Is.EqualTo(400));
            Assert.That(result.Data, Is.Null);
        }
    }
}

public interface ISettedUpDateTime
{
    DateTime GetCurrentVietNamTime();
}

public class SettedUpDateTime : ISettedUpDateTime
{
    public DateTime GetCurrentVietNamTime()
    {
        return DateTime.Now;
    }
}
