using MailKit.Net.Smtp;
using Microsoft.AspNetCore.Http;
using Moq;
using WIMS.Application.DTOs.Auth;
using WIMS.Application.DTOs;
using WIMS.Application.Interfaces.Common;
using WIMS.Application.Interfaces.Repositories;
using WIMS.Application.Service.Auth;
using WIMS.Domain.Entity;
using WIMS.Domain.Enums;
using Xunit;

namespace WIMS.Tests.Services.Auth;


public class AuthServiceTests
{
    private readonly Mock<IJwtService> _mockJwtService;
    private readonly Mock<IUserRepository> _mockUserRepo;
    private readonly Mock<IPasswordHasher> _mockPasswordHasher;
    private readonly Mock<IInputNormalizer> _mockInputNormalizer;
    private readonly Mock<ICodeGeneratorService> _mockCodeGenerator;
    private readonly Mock<IEmailService> _mockEmailService;
    private readonly Mock<IHttpContextAccessor> _mockHttpContextAccessor;
    private readonly Mock<ICurrentUserService> _mockCurrentUser;
    private readonly DefaultHttpContext _httpContext;
    private readonly AuthService _service;

    public AuthServiceTests()
    {
        _mockJwtService = new Mock<IJwtService>();
        _mockUserRepo = new Mock<IUserRepository>();
        _mockPasswordHasher = new Mock<IPasswordHasher>();
        _mockInputNormalizer = new Mock<IInputNormalizer>();
        _mockCodeGenerator = new Mock<ICodeGeneratorService>();
        _mockEmailService = new Mock<IEmailService>();
        _mockHttpContextAccessor = new Mock<IHttpContextAccessor>();
        _mockCurrentUser = new Mock<ICurrentUserService>();

        _httpContext = new DefaultHttpContext();
        _mockHttpContextAccessor.Setup(x => x.HttpContext).Returns(_httpContext);

        // Default pass-through behavior for the normalizer — individual tests can override.
        _mockInputNormalizer.Setup(x => x.NormalizeObject(It.IsAny<LoginRequest>())).Returns((LoginRequest r) => r);
        _mockInputNormalizer.Setup(x => x.NormalizeObject(It.IsAny<ChangePasswordRequest>())).Returns((ChangePasswordRequest r) => r);
        _mockInputNormalizer.Setup(x => x.NormalizeObject(It.IsAny<ResetPasswordRequest>())).Returns((ResetPasswordRequest r) => r);
        _mockInputNormalizer.Setup(x => x.NormalizeObject(It.IsAny<ValidateLinkRequest>())).Returns((ValidateLinkRequest r) => r);
        _mockInputNormalizer.Setup(x => x.Normalize(It.IsAny<string>())).Returns((string s) => s);
        _mockInputNormalizer.Setup(x => x.NormalizeEmail(It.IsAny<string>())).Returns((string s) => s);

        _service = new AuthService(
            _mockJwtService.Object,
            _mockUserRepo.Object,
            _mockPasswordHasher.Object,
            _mockInputNormalizer.Object,
            _mockCodeGenerator.Object,
            _mockEmailService.Object,
            _mockHttpContextAccessor.Object,
            _mockCurrentUser.Object);
    }
    private void SetIncomingCookie(string value)
    {
        _httpContext.Request.Headers["Cookie"] = $"RefreshToken={value}";
    }

    private static User MakeUser(UserStatus status = UserStatus.Active, int failedAttempts = 0, DateTime? lockedUntil = null) =>
        new()
        {
            Id = 5,
            FullName = "Viv Viewer",
            Email = "viv@wims.com",
            PasswordHash = "hashed-password",
            Status = status,
            FailedLoginAttempts = failedAttempts,
            LockedUntil = lockedUntil
        };


    [Fact]
    public async Task Login_GivenUserNotFound_ReturnsNotFound()
    {
        _mockUserRepo.Setup(x => x.GetByEmailAsync("viv@wims.com")).ReturnsAsync((User?)null);

        var result = await _service.Login(new LoginRequest { Email = "viv@wims.com", Password = "pw" });

        Assert.False(result.IsSuccess);
        Assert.Equal(404, result.StatusCode);
    }

    [Fact]
    public async Task Login_GivenInactiveAccount_ReturnsFailure()
    {
        var user = MakeUser(UserStatus.Inactive);
        _mockUserRepo.Setup(x => x.GetByEmailAsync(user.Email)).ReturnsAsync(user);

        var result = await _service.Login(new LoginRequest { Email = user.Email, Password = "pw" });

        Assert.False(result.IsSuccess);
        Assert.Equal(403, result.StatusCode);
        Assert.Contains("inactive", result.Message);
    }

    [Fact]
    public async Task Login_GivenAccountStillLocked_ReturnsFailureWithUnlockTime()
    {
        var user = MakeUser(UserStatus.Locked, failedAttempts: 5, lockedUntil: DateTime.UtcNow.AddMinutes(20));
        _mockUserRepo.Setup(x => x.GetByEmailAsync(user.Email)).ReturnsAsync(user);

        var result = await _service.Login(new LoginRequest { Email = user.Email, Password = "pw" });

        Assert.False(result.IsSuccess);
        Assert.Equal(403, result.StatusCode);
        Assert.Contains("locked", result.Message);
    }

    [Fact]
    public async Task Login_GivenLockWindowExpired_UnlocksAccountThenEvaluatesPassword()
    {
        var user = MakeUser(UserStatus.Locked, failedAttempts: 5, lockedUntil: DateTime.UtcNow.AddMinutes(-1));
        _mockUserRepo.Setup(x => x.GetByEmailAsync(user.Email)).ReturnsAsync(user);
        _mockUserRepo.Setup(x => x.SaveChangesAsync()).ReturnsAsync(true);
        _mockPasswordHasher.Setup(x => x.Verify("wrong-pw", user.PasswordHash)).Returns(false);

        var result = await _service.Login(new LoginRequest { Email = user.Email, Password = "wrong-pw" });

        Assert.False(result.IsSuccess);
        Assert.Equal(400, result.StatusCode);
        Assert.Equal("Invalid Credentials", result.Message);
        Assert.Equal(UserStatus.Active, user.Status); 
        Assert.Null(user.LockedUntil);
    }

    [Fact]
    public async Task Login_GivenWrongPasswordReachingFifthAttempt_LocksAccountAndReturnsFailure()
    {
        var user = MakeUser(failedAttempts: 4); 
        _mockUserRepo.Setup(x => x.GetByEmailAsync(user.Email)).ReturnsAsync(user);
        _mockUserRepo.Setup(x => x.SaveChangesAsync()).ReturnsAsync(true);
        _mockPasswordHasher.Setup(x => x.Verify("wrong-pw", user.PasswordHash)).Returns(false);

        var result = await _service.Login(new LoginRequest { Email = user.Email, Password = "wrong-pw" });

        Assert.False(result.IsSuccess);
        Assert.Equal(403, result.StatusCode);
        Assert.Equal(UserStatus.Locked, user.Status);
        Assert.NotNull(user.LockedUntil);
    }

    [Fact]
    public async Task Login_GivenWrongPasswordBelowThreshold_ReturnsInvalidCredentials()
    {
        var user = MakeUser(failedAttempts: 0);
        _mockUserRepo.Setup(x => x.GetByEmailAsync(user.Email)).ReturnsAsync(user);
        _mockUserRepo.Setup(x => x.SaveChangesAsync()).ReturnsAsync(true);
        _mockPasswordHasher.Setup(x => x.Verify("wrong-pw", user.PasswordHash)).Returns(false);

        var result = await _service.Login(new LoginRequest { Email = user.Email, Password = "wrong-pw" });

        Assert.False(result.IsSuccess);
        Assert.Equal(400, result.StatusCode);
        Assert.Equal(1, user.FailedLoginAttempts);
        Assert.Equal(UserStatus.Active, user.Status); 
    }

    [Fact]
    public async Task Login_GivenValidCredentials_ReturnsAccessTokenAndSetsRefreshTokenCookie()
    {
        var user = MakeUser();
        _mockUserRepo.Setup(x => x.GetByEmailAsync(user.Email)).ReturnsAsync(user);
        _mockPasswordHasher.Setup(x => x.Verify("correct-pw", user.PasswordHash)).Returns(true);
        _mockJwtService.Setup(x => x.generateToken(user)).Returns(new GenerateTokenResponse
        {
            AccessToken = "access-token-abc",
            RefreshToken = "refresh-token-xyz",
            RefreshTokenExpiryTime = DateTime.UtcNow.AddDays(10)
        });
        _mockPasswordHasher.Setup(x => x.NormalHash("refresh-token-xyz")).Returns("hashed-refresh-token");
        _mockUserRepo.Setup(x => x.SaveChangesAsync()).ReturnsAsync(true);

        var result = await _service.Login(new LoginRequest { Email = user.Email, Password = "correct-pw" });

        Assert.True(result.IsSuccess);
        Assert.Equal("access-token-abc", result.Data!.AccessToken);
        Assert.Equal("hashed-refresh-token", user.RefreshTokenHash);
        Assert.Equal(0, user.FailedLoginAttempts);
        Assert.Contains("RefreshToken", _httpContext.Response.Headers.SetCookie.ToString());
    }

    [Fact]
    public async Task RefreshToken_GivenNoCookiePresent_ReturnsUnauthorized()
    {
        var result = await _service.RefreshToken();

        Assert.False(result.IsSuccess);
        Assert.Equal(401, result.StatusCode);
    }

    [Fact]
    public async Task RefreshToken_GivenInvalidOrExpiredToken_ReturnsBadRequest()
    {
        SetIncomingCookie("some-token");
        _mockPasswordHasher.Setup(x => x.NormalHash("some-token")).Returns("hashed-token");
        _mockUserRepo.Setup(x => x.GetUserByRefreshTokenAsync("hashed-token")).ReturnsAsync((User?)null);

        var result = await _service.RefreshToken();

        Assert.False(result.IsSuccess);
        Assert.Equal(400, result.StatusCode);
    }

    [Fact]
    public async Task RefreshToken_GivenValidToken_ReturnsNewAccessTokenAndRotatesCookie()
    {
        var user = MakeUser();
        user.RefreshTokenExpiryTime = DateTime.UtcNow.AddDays(5);

        SetIncomingCookie("old-token");
        _mockPasswordHasher.Setup(x => x.NormalHash("old-token")).Returns("hashed-old-token");
        _mockUserRepo.Setup(x => x.GetUserByRefreshTokenAsync("hashed-old-token")).ReturnsAsync(user);
        _mockJwtService.Setup(x => x.generateToken(user)).Returns(new GenerateTokenResponse
        {
            AccessToken = "new-access-token",
            RefreshToken = "new-refresh-token",
            RefreshTokenExpiryTime = DateTime.UtcNow.AddDays(10)
        });
        _mockPasswordHasher.Setup(x => x.NormalHash("new-refresh-token")).Returns("hashed-new-token");
        _mockUserRepo.Setup(x => x.SaveChangesAsync()).ReturnsAsync(true);

        var result = await _service.RefreshToken();

        Assert.True(result.IsSuccess);
        Assert.Equal("new-access-token", result.Data!.AccessToken);
        Assert.Equal("hashed-new-token", user.RefreshTokenHash);
    }

    [Fact]
    public async Task ChangePassword_GivenUserNotFound_ReturnsNotFound()
    {
        _mockCurrentUser.Setup(x => x.GetUserId()).Returns(5);
        _mockUserRepo
            .Setup(x => x.GetAsync(It.IsAny<System.Linq.Expressions.Expression<Func<User, bool>>>(), It.IsAny<bool>(), It.IsAny<Func<IQueryable<User>, IQueryable<User>>?>()))
            .ReturnsAsync((User?)null);

        var result = await _service.ChangePassword(new ChangePasswordRequest { CurrentPassword = "old", NewPassword = "new" });

        Assert.False(result.IsSuccess);
        Assert.Equal(404, result.StatusCode);
    }

    [Fact]
    public async Task ChangePassword_GivenWrongCurrentPassword_ReturnsFailure()
    {
        var user = MakeUser();
        _mockCurrentUser.Setup(x => x.GetUserId()).Returns(5);
        _mockUserRepo
            .Setup(x => x.GetAsync(It.IsAny<System.Linq.Expressions.Expression<Func<User, bool>>>(), It.IsAny<bool>(), It.IsAny<Func<IQueryable<User>, IQueryable<User>>?>()))
            .ReturnsAsync(user);
        _mockPasswordHasher.Setup(x => x.Verify("wrong-old", user.PasswordHash)).Returns(false);

        var result = await _service.ChangePassword(new ChangePasswordRequest { CurrentPassword = "wrong-old", NewPassword = "new" });

        Assert.False(result.IsSuccess);
        Assert.Equal(400, result.StatusCode);
    }

    [Fact]
    public async Task ChangePassword_GivenNewPasswordSameAsCurrent_ReturnsFailure()
    {
        var user = MakeUser();
        _mockCurrentUser.Setup(x => x.GetUserId()).Returns(5);
        _mockUserRepo
            .Setup(x => x.GetAsync(It.IsAny<System.Linq.Expressions.Expression<Func<User, bool>>>(), It.IsAny<bool>(), It.IsAny<Func<IQueryable<User>, IQueryable<User>>?>()))
            .ReturnsAsync(user);
        _mockPasswordHasher.Setup(x => x.Verify("samepw", user.PasswordHash)).Returns(true);

        var result = await _service.ChangePassword(new ChangePasswordRequest { CurrentPassword = "samepw", NewPassword = "samepw" });

        Assert.False(result.IsSuccess);
        Assert.Contains("cannot be the same", result.Message);
    }

    [Fact]
    public async Task ChangePassword_GivenValidRequest_UpdatesHashAndReturnsSuccess()
    {
        var user = MakeUser();
        _mockCurrentUser.Setup(x => x.GetUserId()).Returns(5);
        _mockUserRepo
            .Setup(x => x.GetAsync(It.IsAny<System.Linq.Expressions.Expression<Func<User, bool>>>(), It.IsAny<bool>(), It.IsAny<Func<IQueryable<User>, IQueryable<User>>?>()))
            .ReturnsAsync(user);
        _mockPasswordHasher.Setup(x => x.Verify("oldpw", user.PasswordHash)).Returns(true);
        _mockPasswordHasher.Setup(x => x.Hash("newpw")).Returns("new-hashed-pw");
        _mockUserRepo.Setup(x => x.SaveChangesAsync()).ReturnsAsync(true);

        var result = await _service.ChangePassword(new ChangePasswordRequest { CurrentPassword = "oldpw", NewPassword = "newpw" });

        Assert.True(result.IsSuccess);
        Assert.Equal("new-hashed-pw", user.PasswordHash);
    }

    [Fact]
    public async Task Logout_GivenNoCookiePresent_ReturnsUnauthorized()
    {
        var result = await _service.Logout();

        Assert.False(result.IsSuccess);
        Assert.Equal(401, result.StatusCode);
    }

    [Fact]
    public async Task Logout_GivenInvalidToken_ReturnsFailure()
    {
        SetIncomingCookie("some-token");
        _mockPasswordHasher.Setup(x => x.NormalHash("some-token")).Returns("hashed-token");
        _mockUserRepo.Setup(x => x.GetUserByRefreshTokenAsync("hashed-token")).ReturnsAsync((User?)null);

        var result = await _service.Logout();

        Assert.False(result.IsSuccess);
        Assert.Equal(400, result.StatusCode);
    }

    [Fact]
    public async Task Logout_GivenValidToken_ClearsRefreshTokenAndReturnsSuccess()
    {
        var user = MakeUser();
        user.RefreshTokenHash = "hashed-token";
        user.RefreshTokenExpiryTime = DateTime.UtcNow.AddDays(5);

        SetIncomingCookie("some-token");
        _mockPasswordHasher.Setup(x => x.NormalHash("some-token")).Returns("hashed-token");
        _mockUserRepo.Setup(x => x.GetUserByRefreshTokenAsync("hashed-token")).ReturnsAsync(user);
        _mockUserRepo.Setup(x => x.SaveChangesAsync()).ReturnsAsync(true);

        var result = await _service.Logout();

        Assert.True(result.IsSuccess);
        Assert.Null(user.RefreshTokenHash);
        Assert.Null(user.RefreshTokenExpiryTime);
    }

    [Fact]
    public async Task ForgotPassword_GivenUserNotFound_ReturnsNotFound()
    {
        _mockUserRepo.Setup(x => x.GetByEmailAsync("ghost@wims.com")).ReturnsAsync((User?)null);

        var result = await _service.ForgotPassword(new ForgotPasswordRequest { Email = "ghost@wims.com" });

        Assert.False(result.IsSuccess);
        Assert.Equal(404, result.StatusCode);
    }

    [Fact]
    public async Task ForgotPassword_GivenInvalidEmailDomain_ReturnsFailure()
    {
        var user = MakeUser();
        _mockUserRepo.Setup(x => x.GetByEmailAsync(user.Email)).ReturnsAsync(user);
        _mockEmailService.Setup(x => x.IsEmailDomainValidAsync(user.Email)).ReturnsAsync(false);

        var result = await _service.ForgotPassword(new ForgotPasswordRequest { Email = user.Email });

        Assert.False(result.IsSuccess);
        Assert.Equal(400, result.StatusCode);
    }

    [Fact]
    public async Task ForgotPassword_GivenValidRequest_SendsEmailAndReturnsSuccess()
    {
        var user = MakeUser();
        _mockUserRepo.Setup(x => x.GetByEmailAsync(user.Email)).ReturnsAsync(user);
        _mockEmailService.Setup(x => x.IsEmailDomainValidAsync(user.Email)).ReturnsAsync(true);
        _mockPasswordHasher.Setup(x => x.NormalHash(It.IsAny<string>())).Returns("hashed-reset-token");
        _mockUserRepo.Setup(x => x.SaveChangesAsync()).ReturnsAsync(true);

        var result = await _service.ForgotPassword(new ForgotPasswordRequest { Email = user.Email });

        Assert.True(result.IsSuccess);
        Assert.Equal("hashed-reset-token", user.PasswordResetTokenHash);
        _mockEmailService.Verify(x => x.SendEmailAsync(user.Email, It.IsAny<string>(), It.IsAny<string>()), Times.Once);
        _mockUserRepo.Verify(x => x.CommitTransactionAsync(), Times.Once);
    }

    [Fact]
    public async Task ForgotPassword_GivenMailboxDoesNotExist_RollsBackAndReturnsFriendlyMessage()
    {
        var user = MakeUser();
        _mockUserRepo.Setup(x => x.GetByEmailAsync(user.Email)).ReturnsAsync(user);
        _mockEmailService.Setup(x => x.IsEmailDomainValidAsync(user.Email)).ReturnsAsync(true);
        _mockPasswordHasher.Setup(x => x.NormalHash(It.IsAny<string>())).Returns("hashed-reset-token");
        _mockUserRepo.Setup(x => x.SaveChangesAsync()).ReturnsAsync(true);
        _mockEmailService
            .Setup(x => x.SendEmailAsync(user.Email, It.IsAny<string>(), It.IsAny<string>()))
            .ThrowsAsync(new SmtpCommandException(SmtpErrorCode.RecipientNotAccepted, SmtpStatusCode.MailboxUnavailable, "No such person"));

        var result = await _service.ForgotPassword(new ForgotPasswordRequest { Email = user.Email });

        Assert.False(result.IsSuccess);
        Assert.Contains("does not exist", result.Message);
        _mockUserRepo.Verify(x => x.RollbackTransactionAsync(), Times.Once);
    }

    [Fact]
    public async Task ResetPassword_GivenUserNotFound_ReturnsNotFound()
    {
        _mockUserRepo.Setup(x => x.GetByEmailAsync("ghost@wims.com")).ReturnsAsync((User?)null);

        var result = await _service.ResetPassword(new ResetPasswordRequest { Email = "ghost@wims.com", Token = "tok", NewPassword = "new" });

        Assert.False(result.IsSuccess);
        Assert.Equal(404, result.StatusCode);
    }

    [Fact]
    public async Task ResetPassword_GivenNoResetTokenWasEverRequested_ReturnsFailure()
    {
        var user = MakeUser();
        _mockUserRepo.Setup(x => x.GetByEmailAsync(user.Email)).ReturnsAsync(user);

        var result = await _service.ResetPassword(new ResetPasswordRequest { Email = user.Email, Token = "tok", NewPassword = "new" });

        Assert.False(result.IsSuccess);
        Assert.Contains("Invalid or Expired", result.Message);
    }

    [Fact]
    public async Task ResetPassword_GivenExpiredToken_ReturnsFailure()
    {
        var user = MakeUser();
        user.PasswordResetTokenHash = "hashed-tok";
        user.PasswordResetTokenExpiry = DateTime.UtcNow.AddMinutes(-5); 
        _mockUserRepo.Setup(x => x.GetByEmailAsync(user.Email)).ReturnsAsync(user);
        _mockPasswordHasher.Setup(x => x.NormalHash("tok")).Returns("hashed-tok");

        var result = await _service.ResetPassword(new ResetPasswordRequest { Email = user.Email, Token = "tok", NewPassword = "new" });

        Assert.False(result.IsSuccess);
        Assert.Contains("Invalid or Expired", result.Message);
    }

    [Fact]
    public async Task ResetPassword_GivenWrongToken_ReturnsFailure()
    {
        var user = MakeUser();
        user.PasswordResetTokenHash = "hashed-correct-tok";
        user.PasswordResetTokenExpiry = DateTime.UtcNow.AddMinutes(20);
        _mockUserRepo.Setup(x => x.GetByEmailAsync(user.Email)).ReturnsAsync(user);
        _mockPasswordHasher.Setup(x => x.NormalHash("wrong-tok")).Returns("hashed-wrong-tok");

        var result = await _service.ResetPassword(new ResetPasswordRequest { Email = user.Email, Token = "wrong-tok", NewPassword = "new" });

        Assert.False(result.IsSuccess);
        Assert.Contains("Invalid or Expired", result.Message);
    }

    [Fact]
    public async Task ResetPassword_GivenValidToken_UpdatesPasswordClearsTokensAndReturnsSuccess()
    {
        var user = MakeUser();
        user.PasswordResetTokenHash = "hashed-correct-tok";
        user.PasswordResetTokenExpiry = DateTime.UtcNow.AddMinutes(20);
        user.RefreshTokenHash = "some-refresh-hash";

        _mockUserRepo.Setup(x => x.GetByEmailAsync(user.Email)).ReturnsAsync(user);
        _mockPasswordHasher.Setup(x => x.NormalHash("correct-tok")).Returns("hashed-correct-tok");
        _mockPasswordHasher.Setup(x => x.Hash("brand-new-pw")).Returns("new-hashed-pw");
        _mockUserRepo.Setup(x => x.SaveChangesAsync()).ReturnsAsync(true);

        var result = await _service.ResetPassword(new ResetPasswordRequest { Email = user.Email, Token = "correct-tok", NewPassword = "brand-new-pw" });

        Assert.True(result.IsSuccess);
        Assert.Equal("new-hashed-pw", user.PasswordHash);
        Assert.Null(user.PasswordResetTokenHash);
        Assert.Null(user.RefreshTokenHash); 
    }

    [Fact]
    public async Task ValidateLink_GivenUserNotFound_ReturnsNotFound()
    {
        _mockUserRepo.Setup(x => x.GetByEmailAsync("ghost@wims.com")).ReturnsAsync((User?)null);

        var result = await _service.ValidateLink(new ValidateLinkRequest { Email = "ghost@wims.com", Token = "tok" });

        Assert.False(result.IsSuccess);
        Assert.Equal(404, result.StatusCode);
    }

    [Fact]
    public async Task ValidateLink_GivenExpiredOrWrongToken_ReturnsFailure()
    {
        var user = MakeUser();
        user.PasswordResetTokenHash = "hashed-correct-tok";
        user.PasswordResetTokenExpiry = DateTime.UtcNow.AddMinutes(-1); 
        _mockUserRepo.Setup(x => x.GetByEmailAsync(user.Email)).ReturnsAsync(user);
        _mockPasswordHasher.Setup(x => x.NormalHash("tok")).Returns("hashed-correct-tok");

        var result = await _service.ValidateLink(new ValidateLinkRequest { Email = user.Email, Token = "tok" });

        Assert.False(result.IsSuccess);
        Assert.False(result.Data);
    }

    [Fact]
    public async Task ValidateLink_GivenValidToken_ReturnsSuccessTrue()
    {
        var user = MakeUser();
        user.PasswordResetTokenHash = "hashed-correct-tok";
        user.PasswordResetTokenExpiry = DateTime.UtcNow.AddMinutes(20);
        _mockUserRepo.Setup(x => x.GetByEmailAsync(user.Email)).ReturnsAsync(user);
        _mockPasswordHasher.Setup(x => x.NormalHash("correct-tok")).Returns("hashed-correct-tok");

        var result = await _service.ValidateLink(new ValidateLinkRequest { Email = user.Email, Token = "correct-tok" });

        Assert.True(result.IsSuccess);
        Assert.True(result.Data);
    }
}