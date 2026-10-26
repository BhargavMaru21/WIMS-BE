using System.Linq.Expressions;
using AutoMapper;
using Moq;
using WIMS.Application.DTOs.Profile;
using WIMS.Application.Interfaces.Common;
using WIMS.Application.Interfaces.Repositories;
using WIMS.Application.Service.Profile;
using WIMS.Domain.Entity;
using WIMS.Domain.Enums;
using Xunit;
using Xunit.Abstractions;

namespace WIMS.Tests.Services.Profile;

public class UserProfileServiceTests
{
    private readonly Mock<IUserRepository> _mockUserRepo;
    private readonly Mock<IInputNormalizer> _mockInputNormalizer;
    private readonly Mock<IMapper> _mockMapper;
    private readonly Mock<ICurrentUserService> _mockCurrentUser;
    private readonly UserProfileService _service;
    private readonly ITestOutputHelper _output;

    public UserProfileServiceTests(ITestOutputHelper output)
    {
        _mockUserRepo = new Mock<IUserRepository>();
        _mockInputNormalizer = new Mock<IInputNormalizer>();
        _mockMapper = new Mock<IMapper>();
        _mockCurrentUser = new Mock<ICurrentUserService>();
        _output = output;

        _service = new UserProfileService(
            _mockUserRepo.Object,
            _mockInputNormalizer.Object,
            _mockMapper.Object,
            _mockCurrentUser.Object);
    }

    private void SetupUserGetAsync(User? user)
    {
        _mockUserRepo
            .Setup(x => x.GetAsync(
                It.IsAny<Expression<Func<User, bool>>>(),
                It.IsAny<bool>(),
                It.IsAny<Func<IQueryable<User>, IQueryable<User>>?>()))
            .ReturnsAsync(user);
    }


    // [Fact]
    // public async Task GetProfile_GivenUserNotFound_ReturnsNotFound()
    // {
    //     _mockCurrentUser.Setup(x => x.GetUserId()).Returns(5);
    //     SetupUserGetAsync(null);

    //     var result = await _service.GetProfile();

    //     Assert.False(result.IsSuccess);
    //     Assert.Equal(404, result.StatusCode);
    // }

    // [Fact]
    // public async Task GetProfile_GivenExistingUser_ReturnsSuccessWithMappedData()
    // {
    //     var user = new User { Id = 5, FullName = "Test", Email = "test@gmail.com", PasswordHash = "hashed" };

    //     _mockCurrentUser.Setup(x => x.GetUserId()).Returns(5);
    //     SetupUserGetAsync(user);
    //     _mockMapper.Setup(x => x.Map<UserProfileResponse>(user))
    //         .Returns(new UserProfileResponse { FullName = "Test", Email = "test@gmail.com" , Role = "Viewer" , Status = "Active"});

    //     var result = await _service.GetProfile();

    //     Assert.True(result.IsSuccess);
    //     Assert.Equal("Test", result.Data!.FullName);
    // }

    // [Fact]
    // public async Task UpdateProfile_GivenUserNotFound_ReturnsNotFound()
    // {
    //     _mockCurrentUser.Setup(x => x.GetUserId()).Returns(5);
    //     _mockInputNormalizer.Setup(x => x.NormalizeObject(It.IsAny<UpdateProfileRequest>())).Returns((UpdateProfileRequest r) => r);
    //     SetupUserGetAsync(null);

    //     var result = await _service.UpdateProfile(new UpdateProfileRequest { FullName = "New Name" });

    //     Assert.False(result.IsSuccess);
    //     Assert.Equal(404, result.StatusCode);
    // }

    // [Fact]
    // public async Task UpdateProfile_GivenBlankFullName_KeepsExistingFullName()
    // {
    //     var user = new User { Id = 5, FullName = "Test", Email = "test@gmail.com", PasswordHash = "hashed", PhoneNumber = "1234567890" };

    //     _mockCurrentUser.Setup(x => x.GetUserId()).Returns(5);
    //     _mockInputNormalizer.Setup(x => x.NormalizeObject(It.IsAny<UpdateProfileRequest>())).Returns((UpdateProfileRequest r) => r);
    //     SetupUserGetAsync(user);
    //     _mockUserRepo.Setup(x => x.SaveChangesAsync()).ReturnsAsync(true);
    //     _mockMapper.Setup(x => x.Map<UserProfileResponse>(user)).Returns(new UserProfileResponse { FullName = "Test",Email = "test@gmail.com", Role = "Viewer" , Status = "Active" });

    //     var result = await _service.UpdateProfile(new UpdateProfileRequest { FullName = "   ", PhoneNumber = "9999999999" });

    //     Assert.True(result.IsSuccess);
    //     Assert.Equal("Test", user.FullName);
    //     Assert.Equal("9999999999", user.PhoneNumber);
    // }

    // [Fact]
    // public async Task UpdateProfile_GivenBlankPhoneNumber_ClearsPhoneNumberToNull()
    // {
    //     var user = new User { Id = 5, FullName = "Test", Email = "test@gmail.com", PasswordHash = "hashed", PhoneNumber = "1234567890" };

    //     _mockCurrentUser.Setup(x => x.GetUserId()).Returns(5);
    //     _mockInputNormalizer.Setup(x => x.NormalizeObject(It.IsAny<UpdateProfileRequest>())).Returns((UpdateProfileRequest r) => r);
    //     SetupUserGetAsync(user);
    //     _mockUserRepo.Setup(x => x.SaveChangesAsync()).ReturnsAsync(true);
    //     _mockMapper.Setup(x => x.Map<UserProfileResponse>(user)).Returns(new UserProfileResponse { FullName = "Test", Email = "test@gmail.com", Role = "Viewer" , Status = "Active" });

    //     var result = await _service.UpdateProfile(new UpdateProfileRequest { PhoneNumber = "  " });

    //     Assert.True(result.IsSuccess);
    //     Assert.Null(user.PhoneNumber);
    // }

    // [Fact]
    // public async Task UpdateProfile_GivenValidChanges_UpdatesFieldsAndReturnsSuccess()
    // {
    //     var user = new User { Id = 5, FullName = "Test", Email = "test@gmail.com", PasswordHash = "hashed" };

    //     _mockCurrentUser.Setup(x => x.GetUserId()).Returns(5);
    //     _mockInputNormalizer.Setup(x => x.NormalizeObject(It.IsAny<UpdateProfileRequest>())).Returns((UpdateProfileRequest r) => r);
    //     SetupUserGetAsync(user);
    //     _mockUserRepo.Setup(x => x.SaveChangesAsync()).ReturnsAsync(true);
    //     _mockMapper.Setup(x => x.Map<UserProfileResponse>(user)).Returns(new UserProfileResponse { FullName = "Vivian Viewer", PhoneNumber = "9999999999" , Email = "test@gmail.com", Role = "Viewer" , Status = "Active" });

    //     var result = await _service.UpdateProfile(new UpdateProfileRequest { FullName = "Vivian Viewer", PhoneNumber = "9999999999" });

    //     Assert.True(result.IsSuccess);
    //     Assert.Equal("Vivian Viewer", user.FullName);
    //     Assert.Equal("9999999999", user.PhoneNumber);
    //     Assert.Equal(5, user.ModifiedBy);
    //     _mockUserRepo.Verify(x => x.SaveChangesAsync(), Times.Once);
    // }
}