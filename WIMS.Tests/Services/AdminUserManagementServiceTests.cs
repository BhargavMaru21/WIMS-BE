using System.Linq.Expressions;
using AutoMapper;
using MailKit.Net.Smtp;
using Moq;
using WIMS.Application.DTOs;
using WIMS.Application.DTOs.Admin;
using WIMS.Application.Interfaces.Common;
using WIMS.Application.Interfaces.Repositories;
using WIMS.Application.Service.Admin;
using WIMS.Domain.Entity;
using WIMS.Domain.Enums;
using Xunit.Abstractions;

namespace WIMS.Tests.Services.Admin;

public class AdminUserManagementServiceTests
{
    private readonly Mock<IUserRepository> _mockUserRepo;
    private readonly Mock<IWarehouseRepository> _mockWarehouseRepo;
    private readonly Mock<IPasswordHasher> _mockPasswordHasher;
    private readonly Mock<IEmailService> _mockEmailService;
    private readonly Mock<IMapper> _mockMapper;
    private readonly Mock<IInputNormalizer> _mockInputNormalizer;
    private readonly Mock<ICurrentUserService> _mockCurrentUser;
    private readonly AdminUserManagementService _service;
    private readonly ITestOutputHelper _output;

    public AdminUserManagementServiceTests(ITestOutputHelper output)
    {
        _mockUserRepo = new Mock<IUserRepository>();
        _mockWarehouseRepo = new Mock<IWarehouseRepository>();
        _mockPasswordHasher = new Mock<IPasswordHasher>();
        _mockEmailService = new Mock<IEmailService>();
        _mockMapper = new Mock<IMapper>();
        _mockInputNormalizer = new Mock<IInputNormalizer>();
        _mockCurrentUser = new Mock<ICurrentUserService>();
        _output = output;

        _service = new AdminUserManagementService(
            _mockUserRepo.Object,
            _mockPasswordHasher.Object,
            _mockWarehouseRepo.Object,
            _mockEmailService.Object,
            _mockMapper.Object,
            _mockInputNormalizer.Object,
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


    //create
    [Fact]
    public async Task CreateUser_GivenDuplicateEmail_ReturnsFailure()
    {
        var request = new CreateUserRequest { FullName = "Bhargav", Email = "bhargav@gmail.com", Password = "Test@123", Role = UserRole.Viewer };

        _mockInputNormalizer.Setup(x => x.NormalizeObject(request)).Returns(request);
        _mockUserRepo.Setup(x => x.IsEmailTakenAsync(request.Email, null)).ReturnsAsync(true);

        var result = await _service.CreateUser(request);

        Assert.False(result.IsSuccess);
        Assert.Equal(400, result.StatusCode);
        _mockUserRepo.Verify(x => x.CreateAsync(It.IsAny<User>()), Times.Never);
    }

    [Fact]
    public async Task CreateUser_CreatingSecondAdmin_ReturnsFailure()
    {
        var request = new CreateUserRequest { FullName = "admin", Email = "admin2@gmail.com", Password = "Test@123", Role = UserRole.Administrator };

        _mockInputNormalizer.Setup(x => x.NormalizeObject(request)).Returns(request);
        _mockUserRepo.Setup(x => x.IsEmailTakenAsync(request.Email, null)).ReturnsAsync(false);
        _mockUserRepo.Setup(x => x.ExistsAsync(It.IsAny<Expression<Func<User, bool>>>())).ReturnsAsync(true);

        var result = await _service.CreateUser(request);

        Assert.False(result.IsSuccess);
        Assert.Contains("Only one administrator is allowed.", result.Message);
        _mockUserRepo.Verify(x => x.CreateAsync(It.IsAny<User>()), Times.Never);
    }

    [Fact]
    public async Task CreateUser_GivenRoleRequiresWarehouseButNotGiven_ReturnsFailure()
    {
        var request = new CreateUserRequest { FullName = "sk", Email = "sk@gmail.com", Password = "Test@123", Role = UserRole.StockKeeper, WarehouseId = null };

        _mockInputNormalizer.Setup(x => x.NormalizeObject(request)).Returns(request);
        _mockUserRepo.Setup(x => x.IsEmailTakenAsync(request.Email, null)).ReturnsAsync(false);

        var result = await _service.CreateUser(request);

        Assert.False(result.IsSuccess);
        Assert.Equal(400, result.StatusCode);
        Assert.Contains("must be assigned to a warehouse", result.Message);
        _mockUserRepo.Verify(x => x.CreateAsync(It.IsAny<User>()), Times.Never);
    }

    [Fact]
    public async Task CreateUser_GivenRoleDoNotRequiresWarehouseButGiven_ReturnsFailure()
    {
        var request = new CreateUserRequest { FullName = "Viewer", Email = "v@gmail.com", Password = "Test@123", Role = UserRole.Viewer, WarehouseId = 2 };

        _mockInputNormalizer.Setup(x => x.NormalizeObject(request)).Returns(request);
        _mockUserRepo.Setup(x => x.IsEmailTakenAsync(request.Email, null)).ReturnsAsync(false);

        var result = await _service.CreateUser(request);

        Assert.False(result.IsSuccess);
        Assert.Equal(400, result.StatusCode);
        Assert.Contains("should not be assigned to a warehouse.", result.Message);
        _mockUserRepo.Verify(x => x.CreateAsync(It.IsAny<User>()), Times.Never);
    }

    [Fact]
    public async Task CreateUser_GivenWarehouseNotExistOrInactive_ReturnsFailure()
    {
        var request = new CreateUserRequest { FullName = "manager", Email = "v@gmail.com", Password = "Test@123", Role = UserRole.WarehouseManager, WarehouseId = 2 };

        _mockInputNormalizer.Setup(x => x.NormalizeObject(request)).Returns(request);
        _mockUserRepo.Setup(x => x.IsEmailTakenAsync(request.Email, null)).ReturnsAsync(false);

        _mockWarehouseRepo.Setup(x => x.ExistsAsync(It.IsAny<Expression<Func<Warehouse, bool>>>())).ReturnsAsync(false);

        var result = await _service.CreateUser(request);

        Assert.False(result.IsSuccess);
        Assert.Equal(400, result.StatusCode);
        Assert.Contains("Warehouse not found or is inactive.", result.Message);
        _mockUserRepo.Verify(x => x.CreateAsync(It.IsAny<User>()), Times.Never);
    }

    [Fact]
    public async Task CreateUser_GivenEmailDomainIsInvalid_ReturnsFailure()
    {
        var request = new CreateUserRequest { FullName = "Viewer", Email = "v@gmail.com", Password = "Test@123", Role = UserRole.Viewer };

        _mockInputNormalizer.Setup(x => x.NormalizeObject(request)).Returns(request);
        _mockUserRepo.Setup(x => x.IsEmailTakenAsync(request.Email, null)).ReturnsAsync(false);
        _mockEmailService.Setup(x => x.IsEmailDomainValidAsync(request.Email)).ReturnsAsync(false);

        var result = await _service.CreateUser(request);

        Assert.False(result.IsSuccess);
        Assert.Contains("Invalid email domain", result.Message);
        _mockUserRepo.Verify(x => x.CreateAsync(It.IsAny<User>()), Times.Never);
    }

    [Fact]
    public async Task CreateUser_WithValidRequest_CreatesUserAndSendsEmailAndReturnsSuccess()
    {
        var request = new CreateUserRequest { FullName = "Test", Email = "Test@gmail.com", Password = "Test@123", Role = UserRole.Viewer };
        var createdUser = new User { Id = 50, FullName = "Test", Email = "Test@gmail.com", PasswordHash = "hashed" };

        _mockCurrentUser.Setup(x => x.GetUserId()).Returns(1);
        _mockInputNormalizer.Setup(x => x.NormalizeObject(request)).Returns(request);
        _mockUserRepo.Setup(x => x.IsEmailTakenAsync(request.Email, null)).ReturnsAsync(false);
        _mockEmailService.Setup(x => x.IsEmailDomainValidAsync(request.Email)).ReturnsAsync(true);
        _mockPasswordHasher.Setup(x => x.Hash(request.Password)).Returns("hashed");
        _mockUserRepo.Setup(x => x.CreateAsync(It.IsAny<User>())).ReturnsAsync(createdUser);

        SetupUserGetAsync(createdUser);

        _mockMapper
            .Setup(x => x.Map<UserResponseDto>(It.IsAny<User>()))
            .Returns(new UserResponseDto { Id = 50, FullName = "Test", Email = "Test@gmail.com", Role = UserRole.Viewer, Status = UserStatus.Active });

        var result = await _service.CreateUser(request);

        Assert.True(result.IsSuccess);
        Assert.Equal(201, result.StatusCode);
        _mockEmailService.Verify(x => x.SendEmailAsync(request.Email, It.IsAny<string>(), It.IsAny<string>()), Times.Once);
        _mockUserRepo.Verify(x => x.CommitTransactionAsync(), Times.Once);
        _mockUserRepo.Verify(x => x.RollbackTransactionAsync(), Times.Never);
    }

    [Fact]
    public async Task CreateUser_WhenEmailAddressDoesNotExist_RollsBackAndReturnsFailure()
    {
        var request = new CreateUserRequest { FullName = "test", Email = "test@dfgdfg.com", Password = "Test@123", Role = UserRole.Viewer };

        _mockCurrentUser.Setup(x => x.GetUserId()).Returns(1);
        _mockInputNormalizer.Setup(x => x.NormalizeObject(request)).Returns(request);
        _mockUserRepo.Setup(x => x.IsEmailTakenAsync(request.Email, null)).ReturnsAsync(false);
        _mockEmailService.Setup(x => x.IsEmailDomainValidAsync(request.Email)).ReturnsAsync(true);
        _mockPasswordHasher.Setup(x => x.Hash(request.Password)).Returns("hashed");
        _mockUserRepo.Setup(x => x.CreateAsync(It.IsAny<User>())).ReturnsAsync(new User { Id = 51, FullName = "test", Email = request.Email, PasswordHash = "hashed" });

        _mockEmailService
            .Setup(x => x.SendEmailAsync(request.Email, It.IsAny<string>(), It.IsAny<string>()))
            .ThrowsAsync(new SmtpCommandException(SmtpErrorCode.RecipientNotAccepted, SmtpStatusCode.MailboxUnavailable, "No such person"));

        var result = await _service.CreateUser(request);

        Assert.False(result.IsSuccess);
        Assert.Contains("does not exist", result.Message);
        _mockUserRepo.Verify(x => x.RollbackTransactionAsync(), Times.Once);
        _mockUserRepo.Verify(x => x.CommitTransactionAsync(), Times.Never);
    }

    [Fact]
    public async Task CreateUser_WhenUnexpectedErrorOccurs_RollsBackAndReturnsFailure()
    {
        var request = new CreateUserRequest { FullName = "test", Email = "test@dfgdfg.com", Password = "Test@123", Role = UserRole.Viewer };

        _mockCurrentUser.Setup(x => x.GetUserId()).Returns(1);
        _mockInputNormalizer.Setup(x => x.NormalizeObject(request)).Returns(request);
        _mockUserRepo.Setup(x => x.IsEmailTakenAsync(request.Email, null)).ReturnsAsync(false);
        _mockEmailService.Setup(x => x.IsEmailDomainValidAsync(request.Email)).ReturnsAsync(true);
        _mockPasswordHasher.Setup(x => x.Hash(request.Password)).Returns("hashed");

        _mockUserRepo.Setup(x => x.CreateAsync(It.IsAny<User>())).ThrowsAsync(new Exception("Unexpected error"));

        var result = await _service.CreateUser(request);

        Assert.False(result.IsSuccess);
        Assert.Contains("try again later", result.Message);
        _mockUserRepo.Verify(x => x.RollbackTransactionAsync(), Times.Once);
    }


    //get all users

    [Fact(Skip = "return Null")]
    public async Task GetUsers_ReturnsListOfUsers()
    {
        var qp = new QueryParameters { };

        var pagedResponse = new PagedResult<User>
        {
            TotalCount = 2,
            Items = new List<User>()
            {
                new User(){Id = 1 , FullName = "Test" , Email = "test@gmail.com" , PasswordHash = "hash", Role = UserRole.Viewer , Status = UserStatus.Active},
                new User(){Id = 2 , FullName = "Test2" , Email = "test2@gmail.com" ,  PasswordHash = "hash" , Role = UserRole.Viewer , Status = UserStatus.Active},
            },
            PageNumber = 1,
            PageSize = 10
        };

        var itemResponse = new List<UserSummaryResponse>()
            {
                new UserSummaryResponse(){Id = 1 , FullName = "Test" , Email = "test@gmail.com" ,Role = "Viewer" , Status = "Active"},
                new UserSummaryResponse(){Id = 2 , FullName = "Test2" , Email = "test2@gmail.com" ,Role = "Viewer" , Status = "Active"},
            };

        _mockMapper.Setup(x => x.Map<List<UserSummaryResponse>>(pagedResponse.Items)).Returns(itemResponse);


        _mockInputNormalizer.Setup(x => x.NormalizeObject(qp)).Returns(qp);

        _mockUserRepo.Setup(x => x.GetPaginatedAsync(
            It.IsAny<QueryParameters>(),
            It.IsAny<string[]>(),
            It.IsAny<Func<IQueryable<User>, IQueryable<User>>>()
        )).ReturnsAsync(pagedResponse);

        var result = await _service.GetUsers(qp);

        foreach (var item in result.Data!.Items)
        {
            _output.WriteLine(item.FullName);
        }

        Assert.True(result.IsSuccess);
        Assert.Equal(200, result.StatusCode);
    }


    [Fact(Skip = "Retrun null")]
    public async Task GetUsers_ReturnsEmptyList()
    {
        var qp = new QueryParameters { };

        var pagedResponse = new PagedResult<User> { };

        var itemResponse = new List<UserSummaryResponse> { };

        _mockMapper.Setup(x => x.Map<List<UserSummaryResponse>>(pagedResponse.Items)).Returns(itemResponse);


        _mockInputNormalizer.Setup(x => x.NormalizeObject(qp)).Returns(qp);

        _mockUserRepo.Setup(x => x.GetPaginatedAsync(
            It.IsAny<QueryParameters>(),
            It.IsAny<string[]>(),
            It.IsAny<Func<IQueryable<User>, IQueryable<User>>>()
        )).ReturnsAsync(pagedResponse);

        var result = await _service.GetUsers(qp);

        Assert.True(result.IsSuccess);
        Assert.Equal(200, result.StatusCode);
    }




    //get by id
    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public async Task GetUserById_GivenInvalidId_ReturnsFailure(int invalidId)
    {
        var result = await _service.GetUserById(invalidId);

        Assert.False(result.IsSuccess);
        Assert.Equal(400, result.StatusCode);
    }

    [Fact]
    public async Task GetUserById_UserNotFound_ReturnsFailure()
    {
        SetupUserGetAsync(null);

        var result = await _service.GetUserById(998999);

        Assert.False(result.IsSuccess);
        Assert.Equal(404, result.StatusCode);
    }

    [Fact]
    public async Task GetUserById_GivenValidId_ReturnsSuccess()
    {
        var user = new User { Id = 10, FullName = "Test", Email = "test@gmail.com", PasswordHash = "hashed", Role = UserRole.Viewer };

        SetupUserGetAsync(user);

        _mockMapper.Setup(x => x.Map<UserResponseDto>(user))
            .Returns(new UserResponseDto { Id = 10, FullName = "Test", Email = "test@gmail.com", Role = UserRole.Viewer, Status = UserStatus.Active });

        var result = await _service.GetUserById(10);

        Assert.True(result.IsSuccess);
        Assert.Equal("Test", result.Data!.FullName);
    }



    //update user status

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public async Task UpdateUserStatus_InvalidUserId_ReturnsFailure(int id)
    {
        var request = new UpdateUserStatusRequest { Status = UserStatus.Active };
        var result = await _service.UpdateUserStatus(id, request);

        Assert.False(result.IsSuccess);
        Assert.Equal(400, result.StatusCode);
    }


    [Fact]
    public async Task UpdateUserStatus_UserNotFound_ReturnsFailure()
    {
        _mockCurrentUser.Setup(x => x.GetUserId()).Returns(1);
        SetupUserGetAsync(null);

        var result = await _service.UpdateUserStatus(99899, new UpdateUserStatusRequest { Status = UserStatus.Inactive });

        Assert.False(result.IsSuccess);
        Assert.Equal(404, result.StatusCode);
    }

    [Fact]
    public async Task UpdateUserStatus_WhenChangingOwnStatus_ReturnsFailure()
    {
        var user = new User { Id = 5, FullName = "test", Email = "test@gmail.com", PasswordHash = "hashed", Role = UserRole.Viewer, Status = UserStatus.Active };

        _mockCurrentUser.Setup(x => x.GetUserId()).Returns(5);
        SetupUserGetAsync(user);

        var result = await _service.UpdateUserStatus(5, new UpdateUserStatusRequest { Status = UserStatus.Inactive });

        Assert.False(result.IsSuccess);
        Assert.Contains("cannot change your status", result.Message);
    }

    [Fact]
    public async Task UpdateUserStatus_WhenRequestingLockedStatus_ReturnsFailure()
    {
        var user = new User { Id = 5, FullName = "test", Email = "test@gmail.com", PasswordHash = "hashed", Role = UserRole.Viewer, Status = UserStatus.Active };

        _mockCurrentUser.Setup(x => x.GetUserId()).Returns(1);
        SetupUserGetAsync(user);

        var result = await _service.UpdateUserStatus(5, new UpdateUserStatusRequest { Status = UserStatus.Locked });

        Assert.False(result.IsSuccess);
        Assert.Contains("managed by the system", result.Message);
    }

    [Fact]
    public async Task UpdateUserStatus_RequestingStatusIsSameAsCurrentStatus_ReturnsFailure()
    {
        var user = new User { Id = 5, FullName = "test", Email = "test@gmail.com", PasswordHash = "hashed", Role = UserRole.Viewer, Status = UserStatus.Active };

        _mockCurrentUser.Setup(x => x.GetUserId()).Returns(1);
        SetupUserGetAsync(user);

        var result = await _service.UpdateUserStatus(5, new UpdateUserStatusRequest { Status = UserStatus.Active });

        Assert.False(result.IsSuccess);
        Assert.Contains("User is already", result.Message);
    }

    [Fact]
    public async Task UpdateUserStatus_WhenUpdateAdminStatus_ReturnsFailure()
    {
        var admin = new User { Id = 5, FullName = "Admin", Email = "admin@gmail.com", PasswordHash = "hashed", Role = UserRole.Administrator, Status = UserStatus.Active };

        _mockCurrentUser.Setup(x => x.GetUserId()).Returns(1);
        SetupUserGetAsync(admin);

        var result = await _service.UpdateUserStatus(5, new UpdateUserStatusRequest { Status = UserStatus.Inactive });

        Assert.False(result.IsSuccess);
        Assert.Contains("Admin Status can not change", result.Message);
    }

    [Theory]
    [InlineData(UserRole.WarehouseManager)]
    [InlineData(UserRole.StockKeeper)]
    public async Task UpdateUserStatus_WhenInactivatingLastActiveStockKeeperOrManager_ReturnsFailure(UserRole role)
    {
        var user = new User { Id = 5, FullName = "test", Email = "test@gmail.com", PasswordHash = "hashed", Role = role, Status = UserStatus.Active, WarehouseId = 1 };

        _mockCurrentUser.Setup(x => x.GetUserId()).Returns(1);
        SetupUserGetAsync(user);
        _mockUserRepo.Setup(x => x.CountAsync(It.IsAny<Expression<Func<User, bool>>>())).ReturnsAsync(0);

        var result = await _service.UpdateUserStatus(5, new UpdateUserStatusRequest { Status = UserStatus.Inactive });

        Assert.False(result.IsSuccess);
        Assert.Contains("must have at least one active", result.Message);
    }

    [Fact]
    public async Task UpdateUserStatus_WithValidRequest_ReturnSuccess()
    {
        var user = new User { Id = 5, FullName = "test", Email = "test@gmail.com", PasswordHash = "hashed", Role = UserRole.Viewer, Status = UserStatus.Active };

        _mockCurrentUser.Setup(x => x.GetUserId()).Returns(1);
        SetupUserGetAsync(user);
        _mockUserRepo.Setup(x => x.SaveChangesAsync()).ReturnsAsync(true);
        _mockMapper.Setup(x => x.Map<UserResponseDto>(user))
            .Returns(new UserResponseDto { Id = 5, FullName = "test", Email = "test@gmail.com", Role = UserRole.Viewer, Status = UserStatus.Inactive });

        var result = await _service.UpdateUserStatus(5, new UpdateUserStatusRequest { Status = UserStatus.Inactive });

        Assert.True(result.IsSuccess);
        Assert.Equal(UserStatus.Inactive, user.Status);
        _mockUserRepo.Verify(x => x.SaveChangesAsync(), Times.Once);
    }


    //Update user role
    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public async Task UpdateUserRole_GivenInvalidid_ReturnsFailure(int id)
    {
        var request = new UpdateUserRoleRequest { Role = UserRole.Viewer };
        var result = await _service.UpdateUserRole(id, request);

        Assert.False(result.IsSuccess);
        Assert.Equal(400, result.StatusCode);

    }

    [Fact]
    public async Task UpdateUserRole_UserNotFound_ReturnsFailure()
    {
        var request = new UpdateUserRoleRequest { Role = UserRole.Viewer };

        SetupUserGetAsync(null);

        var result = await _service.UpdateUserRole(989898, request);

        Assert.False(result.IsSuccess);
        Assert.Equal(404, result.StatusCode);
    }

    [Fact]
    public async Task UpdateUserRole_WhenChangingOwnRole_ReturnsFailure()
    {
        var user = new User { Id = 5, FullName = "test", Email = "test@gmail.com", PasswordHash = "hashed", Role = UserRole.Viewer, Status = UserStatus.Active };
        var request = new UpdateUserRoleRequest { Role = UserRole.WarehouseManager };

        _mockCurrentUser.Setup(x => x.GetUserId()).Returns(5);
        SetupUserGetAsync(user);

        var result = await _service.UpdateUserRole(5, request);

        Assert.False(result.IsSuccess);
        Assert.Equal(400, result.StatusCode);
        Assert.Contains("You cannot change your own role.", result.Message);
    }

    [Fact]
    public async Task UpdateUserRole_WhenUserInactive_RetrunFailure()
    {
        var user = new User { Id = 5, FullName = "test", Email = "test@gmail.com", PasswordHash = "hashed", Role = UserRole.Viewer, Status = UserStatus.Inactive };
        var request = new UpdateUserRoleRequest { Role = UserRole.WarehouseManager };

        _mockCurrentUser.Setup(x => x.GetUserId()).Returns(1);
        SetupUserGetAsync(user);

        var result = await _service.UpdateUserRole(5, request);

        Assert.False(result.IsSuccess);
        Assert.Equal(400, result.StatusCode);
        Assert.Contains("Cannot change role of an inactive user", result.Message);
    }

    [Fact]
    public async Task UpdateUserRole_GivenRoleIsSameAsCurrent_RetrunFailure()
    {
        var user = new User { Id = 5, FullName = "test", Email = "test@gmail.com", PasswordHash = "hashed", Role = UserRole.Viewer, Status = UserStatus.Inactive };
        var request = new UpdateUserRoleRequest { Role = UserRole.Viewer };

        _mockCurrentUser.Setup(x => x.GetUserId()).Returns(1);
        SetupUserGetAsync(user);

        var result = await _service.UpdateUserRole(5, request);

        Assert.False(result.IsSuccess);
        Assert.Equal(400, result.StatusCode);
    }

    [Theory]
    [InlineData(UserRole.WarehouseManager)]
    [InlineData(UserRole.StockKeeper)]
    public async Task UpdateUserRole_WhenLastActiveManagerOrKeeper_RetrunFailure(UserRole role)
    {
        var user = new User { Id = 5, FullName = "test", Email = "test@gmail.com", PasswordHash = "hashed", Role = role, Status = UserStatus.Active };
        var request = new UpdateUserRoleRequest { Role = UserRole.Viewer };

        _mockCurrentUser.Setup(x => x.GetUserId()).Returns(1);
        SetupUserGetAsync(user);

        _mockUserRepo.Setup(x => x.CountAsync(It.IsAny<Expression<Func<User, bool>>>())).ReturnsAsync(0);

        var result = await _service.UpdateUserRole(5, request);

        Assert.False(result.IsSuccess);
        Assert.Equal(400, result.StatusCode);
    }

    [Theory]
    [InlineData(UserRole.WarehouseManager)]
    [InlineData(UserRole.StockKeeper)]
    public async Task UpdateUserRole_RequestedRoleNeedWarehouseButNotAssigned_RetrunFailure(UserRole role)
    {
        var user = new User { Id = 5, FullName = "test", Email = "test@gmail.com", PasswordHash = "hashed", Role = UserRole.Viewer, Status = UserStatus.Active };
        var request = new UpdateUserRoleRequest { Role = role };

        _mockCurrentUser.Setup(x => x.GetUserId()).Returns(1);
        SetupUserGetAsync(user);

        var result = await _service.UpdateUserRole(5, request);

        Assert.False(result.IsSuccess);
        Assert.Equal(400, result.StatusCode);
        Assert.Contains("must be assigned to a warehouse.", result.Message);
    }

    [Theory]
    [InlineData(UserRole.WarehouseManager)]
    [InlineData(UserRole.StockKeeper)]
    public async Task UpdateUserRole_AssignedWarehouseNotExists_RetrunFailure(UserRole role)
    {
        var user = new User { Id = 5, FullName = "test", Email = "test@gmail.com", PasswordHash = "hashed", Role = UserRole.Viewer, Status = UserStatus.Active };
        var request = new UpdateUserRoleRequest { Role = role, WarehouseId = 9889898 };

        _mockCurrentUser.Setup(x => x.GetUserId()).Returns(1);
        SetupUserGetAsync(user);
        _mockWarehouseRepo.Setup(x => x.ExistsAsync(It.IsAny<Expression<Func<Warehouse, bool>>>())).ReturnsAsync(false);

        var result = await _service.UpdateUserRole(5, request);

        Assert.False(result.IsSuccess);
        Assert.Equal(400, result.StatusCode);
        Assert.Contains("Warehouse not found or is inactive.", result.Message);
    }

    [Fact]
    public async Task UpdateUserRole_WhenValidRequest_ReturnsSuccess()
    {
        var user = new User { Id = 5, FullName = "test", Email = "test@gmail.com", PasswordHash = "hashed", Role = UserRole.Viewer, Status = UserStatus.Active };
        var updatedUser = new User { Id = 5, FullName = "test", Email = "test@gmail.com", PasswordHash = "hashed", Role = UserRole.WarehouseManager, Status = UserStatus.Active };
        var mappedUser = new UserResponseDto { Id = 5, FullName = "test", Email = "test@gmail.com", Role = UserRole.WarehouseManager, Status = UserStatus.Active, WarehouseId = 10 };
        var request = new UpdateUserRoleRequest { Role = UserRole.WarehouseManager, WarehouseId = 10 };

        _mockCurrentUser.Setup(x => x.GetUserId()).Returns(1);
        SetupUserGetAsync(user);
        _mockWarehouseRepo.Setup(x => x.ExistsAsync(It.IsAny<Expression<Func<Warehouse, bool>>>())).ReturnsAsync(true);

        _mockUserRepo.Setup(x => x.SaveChangesAsync()).ReturnsAsync(true);
        _mockMapper.Setup(x => x.Map<UserResponseDto>(updatedUser)).Returns(mappedUser);

        var result = await _service.UpdateUserRole(5, request);

        _output.WriteLine("log is === ");
        _output.WriteLine($"the result is : {result.Message} and code is {result.StatusCode} and the status {result.IsSuccess}");

        Assert.True(result.IsSuccess);
        Assert.Equal("User role updated successfully.", result.Message);
        _mockUserRepo.Verify(x => x.SaveChangesAsync(), Times.Once);
    }



    // updateUserWarehouse
    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public async Task UpdateUserWarehouse_GivenInvalidid_ReturnsFailure(int id)
    {
        var request = new UpdateUserWarehouseRequest { WarehouseId = 10 };
        var result = await _service.UpdateUserWarehouse(id, request);

        Assert.False(result.IsSuccess);
        Assert.Equal(400, result.StatusCode);
    }

    [Fact]
    public async Task UpdateUserWarehouse_UserNotFound_ReturnsFailure()
    {
        var request = new UpdateUserWarehouseRequest { WarehouseId = 10 };

        SetupUserGetAsync(null);

        var result = await _service.UpdateUserWarehouse(989898, request);

        Assert.False(result.IsSuccess);
        Assert.Equal(404, result.StatusCode);
    }

    [Fact]
    public async Task UpdateUserWarehouse_WhenRoleNotNeedWarehouse_ReturnsFailure()
    {
        var user = new User { Id = 5, FullName = "test", Email = "test@gmail.com", PasswordHash = "hashed", Role = UserRole.Viewer, Status = UserStatus.Active };
        var request = new UpdateUserWarehouseRequest { WarehouseId = 10 };

        SetupUserGetAsync(user);

        var result = await _service.UpdateUserWarehouse(5, request);

        Assert.False(result.IsSuccess);
        Assert.Equal(400, result.StatusCode);
        Assert.Contains("does not require a warehouse assignment.", result.Message);
    }

    [Fact]
    public async Task UpdateUserWarehouse_WarehouseNotFound_ReturnsFailure()
    {
        var user = new User { Id = 5, FullName = "test", Email = "test@gmail.com", PasswordHash = "hashed", Role = UserRole.WarehouseManager, Status = UserStatus.Active };
        var request = new UpdateUserWarehouseRequest { WarehouseId = 10 };

        SetupUserGetAsync(user);

        _mockWarehouseRepo.Setup(x => x.ExistsAsync(It.IsAny<Expression<Func<Warehouse, bool>>>())).ReturnsAsync(false);

        var result = await _service.UpdateUserWarehouse(5, request);

        Assert.False(result.IsSuccess);
        Assert.Equal(400, result.StatusCode);
        Assert.Contains("Warehouse not found or is inactive.", result.Message);
    }

    [Fact]
    public async Task UpdateUserWarehouse_UserIsAssignedToSameWarehouse_ReturnsFailure()
    {
        var user = new User { Id = 5, FullName = "test", Email = "test@gmail.com", PasswordHash = "hashed", Role = UserRole.WarehouseManager, Status = UserStatus.Active, WarehouseId = 10 };
        var request = new UpdateUserWarehouseRequest { WarehouseId = 10 };

        SetupUserGetAsync(user);

        _mockWarehouseRepo.Setup(x => x.ExistsAsync(It.IsAny<Expression<Func<Warehouse, bool>>>())).ReturnsAsync(true);

        var result = await _service.UpdateUserWarehouse(5, request);

        Assert.False(result.IsSuccess);
        Assert.Equal(400, result.StatusCode);
        Assert.Contains("User is already assigned to this warehouse.", result.Message);
    }

    [Theory]
    [InlineData(UserRole.WarehouseManager)]
    [InlineData(UserRole.StockKeeper)]
    public async Task UpdateUserWarehouse_WhenLastActiveManagerOrKeeper_RetrunFailure(UserRole role)
    {
        var user = new User { Id = 5, FullName = "test", Email = "test@gmail.com", PasswordHash = "hashed", Role = role, Status = UserStatus.Active, WarehouseId = 10 };
        var request = new UpdateUserWarehouseRequest { WarehouseId = 11 };

        SetupUserGetAsync(user);
        _mockWarehouseRepo.Setup(x => x.ExistsAsync(It.IsAny<Expression<Func<Warehouse, bool>>>())).ReturnsAsync(true);

        _mockUserRepo.Setup(x => x.CountAsync(It.IsAny<Expression<Func<User, bool>>>())).ReturnsAsync(0);

        var result = await _service.UpdateUserWarehouse(5, request);

        Assert.False(result.IsSuccess);
        Assert.Equal(400, result.StatusCode);
        Assert.Contains($"Each warehouse must have at least one active {role}", result.Message);
    }

    [Fact]
    public async Task UpdateUserWarehouse_WhenValidRequest_ReturnsSuccess()
    {
        var user = new User { Id = 5, FullName = "test", Email = "test@gmail.com", PasswordHash = "hashed", Role = UserRole.WarehouseManager, Status = UserStatus.Active, WarehouseId = 10 };
        var request = new UpdateUserWarehouseRequest { WarehouseId = 11 };
        var updatedUser = new User { Id = 5, FullName = "test", Email = "test@gmail.com", PasswordHash = "hashed", Role = UserRole.WarehouseManager, Status = UserStatus.Active, WarehouseId = 10 };
        var mappedUser = new UserResponseDto { Id = 5, FullName = "test", Email = "test@gmail.com", Role = UserRole.WarehouseManager, Status = UserStatus.Active, WarehouseId = 11 };


        _mockCurrentUser.Setup(x => x.GetUserId()).Returns(1);
        SetupUserGetAsync(user);
        _mockWarehouseRepo.Setup(x => x.ExistsAsync(It.IsAny<Expression<Func<Warehouse, bool>>>())).ReturnsAsync(true);
        _mockUserRepo.Setup(x => x.CountAsync(It.IsAny<Expression<Func<User, bool>>>())).ReturnsAsync(10);

        _mockUserRepo.Setup(x => x.SaveChangesAsync()).ReturnsAsync(true);
        _mockMapper.Setup(x => x.Map<UserResponseDto>(updatedUser)).Returns(mappedUser);

        var result = await _service.UpdateUserWarehouse(5, request);

        Assert.True(result.IsSuccess);
        Assert.Equal("Warehouse assignment updated successfully.", result.Message);
        _mockUserRepo.Verify(x => x.SaveChangesAsync(), Times.Once);
    }


    //delete
    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public async Task DeleteUser_GivenInvalidId_ReturnsFailure(int id)
    {
        var result = await _service.Deleteuser(id);

        Assert.False(result.IsSuccess);
        Assert.Equal(400, result.StatusCode);
    }


    [Fact]
    public async Task DeleteUser_UserNotFound_ReturnsFailure()
    {
        SetupUserGetAsync(null);

        var result = await _service.Deleteuser(9999);

        Assert.False(result.IsSuccess);
        Assert.Equal(404, result.StatusCode);
    }

    [Fact]
    public async Task DeleteUser_WhenDeleteAdmin_ReturnsFailure()
    {
        var admin = new User { Id = 5, FullName = "Admin", Email = "admin@gmail.com", PasswordHash = "hashed", Role = UserRole.Administrator };

        _mockCurrentUser.Setup(x => x.GetUserId()).Returns(1);
        SetupUserGetAsync(admin);

        var result = await _service.Deleteuser(5);

        Assert.False(result.IsSuccess);
        Assert.Contains("cannot be deleted", result.Message);
    }

    [Fact]
    public async Task DeleteUser_WhenDeletingOwnAccount_ReturnsFailure()
    {
        var user = new User { Id = 5, FullName = "test", Email = "test@gmail.com", PasswordHash = "hashed", Role = UserRole.Viewer };

        _mockCurrentUser.Setup(x => x.GetUserId()).Returns(5);

        SetupUserGetAsync(user);

        var result = await _service.Deleteuser(5);

        Assert.False(result.IsSuccess);
        Assert.Contains("cannot delete your own account", result.Message);
    }

    [Theory]
    [InlineData(UserRole.WarehouseManager)]
    [InlineData(UserRole.StockKeeper)]
    public async Task DeleteUser_WhenLastActiveManagerOrKeeper_ReturnsFailure(UserRole role)
    {
        var user = new User { Id = 5, FullName = "Test", Email = "test@gmail.com", PasswordHash = "hased", Role = role, WarehouseId = 1 };

        _mockCurrentUser.Setup(x => x.GetUserId()).Returns(1);

        SetupUserGetAsync(user);

        _mockUserRepo.Setup(x => x.CountAsync(It.IsAny<Expression<Func<User, bool>>>())).ReturnsAsync(0);

        var result = await _service.Deleteuser(5);

        Assert.False(result.IsSuccess);
        Assert.Contains("must have at least one active", result.Message);
        _mockUserRepo.Verify(x => x.SoftDeleteAsync(It.IsAny<User>(), It.IsAny<int>()), Times.Never);
    }

    [Fact]
    public async Task DeleteUser_WithValidRequest_SoftDeletesAndReturnsSuccess()
    {
        var user = new User { Id = 5, FullName = "Test", Email = "test@gmail.com", PasswordHash = "hashed", Role = UserRole.Viewer };

        _mockCurrentUser.Setup(x => x.GetUserId()).Returns(1);

        SetupUserGetAsync(user);

        _mockUserRepo.Setup(x => x.SoftDeleteAsync(user, 1)).ReturnsAsync(true);

        var result = await _service.Deleteuser(5);

        Assert.True(result.IsSuccess);
        _mockUserRepo.Verify(x => x.SoftDeleteAsync(user, 1), Times.Once);
    }
}
