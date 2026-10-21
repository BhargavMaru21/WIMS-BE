using AutoMapper;
using MailKit.Net.Smtp;
using Microsoft.EntityFrameworkCore;
using WIMS.Application.DTOs;
using WIMS.Application.DTOs.Admin;
using WIMS.Application.Interfaces.Common;
using WIMS.Application.Interfaces.Repositories;
using WIMS.Application.Interfaces.Services.Admin;
using WIMS.Domain.Entity;
using WIMS.Domain.Enums;
namespace WIMS.Application.Service.Admin;

public class AdminUserManagementService : IAdminUserManagementService
{
    private readonly IUserRepository _userRepository;
    private readonly IWarehouseRepository _warehouseRepo;
    private readonly IPasswordHasher _passwordHasher;
    private readonly IEmailService _emailService;
    private readonly IMapper _mapper;
    private readonly IInputNormalizer _inputNormalizer;
    private readonly ICurrentUserService _currentUser;
    private static readonly UserRole[] _rolesRequiringWarehouse =
        [UserRole.WarehouseManager, UserRole.StockKeeper];

    private static readonly UserRole[] _rolesWithoutWarehouse =
        [UserRole.Administrator, UserRole.Viewer];


    public AdminUserManagementService(
        IUserRepository userRepository,
        IPasswordHasher passwordHasher,
        IWarehouseRepository warehouseRepo,
        IEmailService emailService,
        IMapper mapper,
        IInputNormalizer inputNormalizer,
        ICurrentUserService currentUser
        )
    {
        _userRepository = userRepository;
        _passwordHasher = passwordHasher;
        _emailService = emailService;
        _warehouseRepo = warehouseRepo;
        _mapper = mapper;
        _inputNormalizer = inputNormalizer;
        _currentUser = currentUser;
    }

    public async Task<ApiResponse<UserResponseDto>> CreateUser(CreateUserRequest request)
    {
        int createdByUserId = _currentUser.GetUserId();
        request = _inputNormalizer.NormalizeObject(request);

        var emailTaken = await _userRepository.IsEmailTakenAsync(request.Email);

        if (emailTaken)
        {
            return ApiResponse<UserResponseDto>.Failure("This Email is used by another account", null, 400);
        }

        if (request.Role == UserRole.Administrator && await _userRepository.ExistsAsync(u => u.Role == UserRole.Administrator))
        {
            return ApiResponse<UserResponseDto>.Failure("An administrator account already exists. Only one administrator is allowed.", null, 400);
        }

        var warehouseValidation = await ValidateWarehouseForRole(request.Role, request.WarehouseId);
        if (warehouseValidation is not null)
            return warehouseValidation;

        if (!await _emailService.IsEmailDomainValidAsync(request.Email))
        {
            return ApiResponse<UserResponseDto>.Failure("Invalid email domain. Please check and try again.", null, 400);
        }

        var passwordHash = _passwordHasher.Hash(request.Password);

        User newUser = new User
        {
            FullName = request.FullName,
            Email = request.Email,
            PasswordHash = passwordHash,
            Role = request.Role,
            WarehouseId = request.WarehouseId,
            Status = UserStatus.Active,
            CreatedAt = DateTime.UtcNow,
            CreatedBy = createdByUserId
        };

        await _userRepository.BeginTransactionAsync();
        try
        {
            var result = await _userRepository.CreateAsync(newUser);

            string subject = "WIMS Credentials";
            string body = $"Dear {request.FullName},\n\nYour account has been created successfully. Here are your credentials:\n\nEmail: {request.Email}\nPassword: {request.Password}\n\nPlease log in and change your password immediately for security reasons.\n\nBest regards,\nWIMS Team";

            await _emailService.SendEmailAsync(request.Email, subject, body);
            await _userRepository.CommitTransactionAsync();

            var userWithWarehouse = await _userRepository.GetAsync(
                u => u.Email == newUser.Email,
                includes: q => q.Include(u => u.Warehouse));

            UserResponseDto response = _mapper.Map<UserResponseDto>(userWithWarehouse!);

            return ApiResponse<UserResponseDto>.Success(response, "User created Successfully", 201);
        }
        catch (SmtpCommandException ex) when (ex.StatusCode == SmtpStatusCode.MailboxUnavailable || ex.Message.Contains("No such person"))
        {
            await _userRepository.RollbackTransactionAsync();
            return ApiResponse<UserResponseDto>.Failure("The email address does not exist. Please check and try again.");
        }
        catch (Exception)
        {
            await _userRepository.RollbackTransactionAsync();
            return ApiResponse<UserResponseDto>.Failure("Failed to Create User. Please try again later.");
        }
    }

    public async Task<ApiResponse<PagedResult<UserSummaryResponse>>> GetUsers(QueryParameters qp)
    {
        qp = _inputNormalizer.NormalizeObject(qp);

        var paged = await _userRepository.GetPaginatedAsync(
            qp,
            searchableColumns: ["FullName", "Email"],
            includes: q => q.Include(u => u.Warehouse)
        );

        var result = new PagedResult<UserSummaryResponse>
        {
            Items = _mapper.Map<List<UserSummaryResponse>>(paged.Items),
            TotalCount = paged.TotalCount,
            PageNumber = paged.PageNumber,
            PageSize = paged.PageSize
        };

        return ApiResponse<PagedResult<UserSummaryResponse>>.Success(result);
    }

    public async Task<ApiResponse<UserResponseDto>> GetUserById(int userId)
    {
        if (userId <= 0)
            return ApiResponse<UserResponseDto>.Failure("Invalid ID", statusCode: 400);

        var user = await _userRepository.GetAsync(
            u => u.Id == userId,
            includes: q => q.Include(u => u.Warehouse));

        if (user is null)
            return ApiResponse<UserResponseDto>.Failure("User not found.", statusCode: 404);

        UserResponseDto response = _mapper.Map<UserResponseDto>(user);

        return ApiResponse<UserResponseDto>.Success(response);
    }

    public async Task<ApiResponse<UserResponseDto>> UpdateUserStatus(int userId, UpdateUserStatusRequest request)
    {
        if (userId <= 0)
            return ApiResponse<UserResponseDto>.Failure("Invalid ID", statusCode: 400);

        int modifiedByUserId = _currentUser.GetUserId();

        var user = await _userRepository.GetAsync(
            u => u.Id == userId,
            useNoTracking: false,
            includes: q => q.Include(u => u.Warehouse));

        if (user is null)
            return ApiResponse<UserResponseDto>.Failure("User not found.", statusCode: 404);

        if (userId == modifiedByUserId)
            return ApiResponse<UserResponseDto>.Failure(
                "You cannot change your status.", statusCode: 400);

        if (request.Status == UserStatus.Locked)
            return ApiResponse<UserResponseDto>.Failure(
                "Locked status is managed by the system only.", statusCode: 400);

        if (user.Status == request.Status)
            return ApiResponse<UserResponseDto>.Failure(
                $"User is already {request.Status}.", statusCode: 400);

        if (user.Role == UserRole.Administrator)
            return ApiResponse<UserResponseDto>.Failure("Admin Status can not change", statusCode: 400);

        //if user is manager/keeper then check at least one active manager/keeper exists before inactivating per warehouse
        if ((user.Role == UserRole.WarehouseManager || user.Role == UserRole.StockKeeper) && request.Status == UserStatus.Inactive)
        {
            var activeManagersCount = await ActiveSameRoleCount(user);

            if (activeManagersCount == 0)
            {
                return ApiResponse<UserResponseDto>.Failure($"Cannot inactivate this user. Each warehouse must have at least one active {user.Role}.", statusCode: 400);
            }
        }

        user.Status = request.Status;
        user.ModifiedAt = DateTime.UtcNow;
        user.ModifiedBy = modifiedByUserId;

        // If reactivating a locked user, clear lock fields
        if (request.Status == UserStatus.Active)
        {
            user.FailedLoginAttempts = 0;
            user.LockedUntil = null;
        }

        await _userRepository.SaveChangesAsync();

        UserResponseDto response = _mapper.Map<UserResponseDto>(user);

        return ApiResponse<UserResponseDto>.Success(
           response,
            $"User {request.Status.ToString().ToLower()} successfully.");
    }

    public async Task<ApiResponse<UserResponseDto>> UpdateUserRole(int userId, UpdateUserRoleRequest request)
    {
        if (userId <= 0)
            return ApiResponse<UserResponseDto>.Failure("Invalid ID", statusCode: 400);

        int modifiedByUserId = _currentUser.GetUserId();

        var user = await _userRepository.GetAsync(
            u => u.Id == userId,
            useNoTracking: false,
            includes: q => q.Include(u => u.Warehouse));

        if (user is null)
            return ApiResponse<UserResponseDto>.Failure("User not found.", statusCode: 404);

        if (userId == modifiedByUserId)
            return ApiResponse<UserResponseDto>.Failure("You cannot change your own role.", statusCode: 400);

        if (user.Status == UserStatus.Inactive)
            return ApiResponse<UserResponseDto>.Failure("Cannot change role of an inactive user. Activate the user first.", statusCode: 400);

        if (user.Role == request.Role)
            return ApiResponse<UserResponseDto>.Failure($"User is already {request.Role}.", statusCode: 400);

        if (user.Role == UserRole.WarehouseManager || user.Role == UserRole.StockKeeper)
        {
            var activeManagersCount = await ActiveSameRoleCount(user);

            if (activeManagersCount == 0)
            {
                return ApiResponse<UserResponseDto>.Failure($"Cannot change Role. Each warehouse must have at least one active {user.Role}", statusCode: 400);
            }
        }

        if (_rolesWithoutWarehouse.Contains(user.Role) && _rolesRequiringWarehouse.Contains(request.Role))
        {
            if (!request.WarehouseId.HasValue)
                return ApiResponse<UserResponseDto>.Failure($"{request.Role} must be assigned to a warehouse.", statusCode: 400);

            var warehouseExists = await _warehouseRepo.ExistsAsync(
                w => w.Id == request.WarehouseId && w.Status == EntityStatus.Active);

            if (!warehouseExists)
                return ApiResponse<UserResponseDto>.Failure("Warehouse not found or is inactive.", statusCode: 400);

            user.WarehouseId = request.WarehouseId;
        }
        else if (_rolesRequiringWarehouse.Contains(user.Role) && _rolesWithoutWarehouse.Contains(request.Role))
        {

            user.WarehouseId = null;
        }

        user.Role = request.Role;
        user.ModifiedAt = DateTime.UtcNow;
        user.ModifiedBy = modifiedByUserId;

        await _userRepository.SaveChangesAsync();

        var updatedUser = await _userRepository.GetAsync(u => u.Id == user.Id, includes: q => q.Include(x => x.Warehouse));

        UserResponseDto response = _mapper.Map<UserResponseDto>(updatedUser);

        return ApiResponse<UserResponseDto>.Success(
            response,
            "User role updated successfully.");
    }

    public async Task<ApiResponse<UserResponseDto>> UpdateUserWarehouse(int userId, UpdateUserWarehouseRequest request)
    {
        if (userId <= 0)
            return ApiResponse<UserResponseDto>.Failure("Invalid ID", statusCode: 400);

        int modifiedByUserId = _currentUser.GetUserId();

        var user = await _userRepository.GetAsync(
            u => u.Id == userId,
            useNoTracking: false,
            includes: q => q.Include(u => u.Warehouse));

        if (user is null)
            return ApiResponse<UserResponseDto>.Failure("User not found.", statusCode: 404);

        if (_rolesWithoutWarehouse.Contains(user.Role))
            return ApiResponse<UserResponseDto>.Failure($"{user.Role} does not require a warehouse assignment.", statusCode: 400);

        var warehouseExists = await _warehouseRepo.ExistsAsync(w => w.Id == request.WarehouseId && w.Status == EntityStatus.Active);

        if (!warehouseExists)
            return ApiResponse<UserResponseDto>.Failure("Warehouse not found or is inactive.", statusCode: 400);

        if (user.WarehouseId == request.WarehouseId)
            return ApiResponse<UserResponseDto>.Failure("User is already assigned to this warehouse.", statusCode: 400);

        if (user.Role == UserRole.WarehouseManager || user.Role == UserRole.StockKeeper)
        {
            var activeManagersCount = await ActiveSameRoleCount(user);

            if (activeManagersCount == 0)
            {
                return ApiResponse<UserResponseDto>.Failure($"Cannot change warehouse. Each warehouse must have at least one active {user.Role}", statusCode: 400);
            }
        }

        user.WarehouseId = request.WarehouseId;
        user.ModifiedAt = DateTime.UtcNow;
        user.ModifiedBy = modifiedByUserId;

        await _userRepository.SaveChangesAsync();

        var updatedUser = await _userRepository.GetAsync(u => u.Id == user.Id, includes: q => q.Include(x => x.Warehouse));

        UserResponseDto response = _mapper.Map<UserResponseDto>(updatedUser);

        return ApiResponse<UserResponseDto>.Success(response, "Warehouse assignment updated successfully.");
    }

    public async Task<ApiResponse<string>> Deleteuser(int userId)
    {
        if (userId <= 0)
            return ApiResponse<string>.Failure("Invalid ID", statusCode: 400);

        int deletedByUserId = _currentUser.GetUserId();

        var user = await _userRepository.GetAsync(u => u.Id == userId, useNoTracking: false);

        if (user is null)
            return ApiResponse<string>.Failure("User not found.", statusCode: 404);

        if (user.Role == UserRole.Administrator)
            return ApiResponse<string>.Failure("Administrator account cannot be deleted.", statusCode: 400);

        if (userId == deletedByUserId)
            return ApiResponse<string>.Failure("You cannot delete your own account.", statusCode: 400);

        if (user.Role == UserRole.WarehouseManager || user.Role == UserRole.StockKeeper)
        {
            var activeSameRoleCount = await ActiveSameRoleCount(user);

            if (activeSameRoleCount == 0)
            {
                return ApiResponse<string>.Failure($"Cannot delete this user. Each warehouse must have at least one active {user.Role}.", statusCode: 400);
            }
        }

        await _userRepository.SoftDeleteAsync(user, deletedByUserId);

        return ApiResponse<string>.Success("User deleted successfully.", "User deleted successfully.");
    }


    private async Task<ApiResponse<UserResponseDto>?> ValidateWarehouseForRole(
        UserRole role, int? warehouseId)
    {
        if (_rolesWithoutWarehouse.Contains(role) && warehouseId is not null)
            return ApiResponse<UserResponseDto>.Failure($"{role} should not be assigned to a warehouse.", statusCode: 400);

        if (_rolesRequiringWarehouse.Contains(role) && warehouseId is null)
            return ApiResponse<UserResponseDto>.Failure($"{role} must be assigned to a warehouse.", statusCode: 400);

        if (warehouseId is not null)
        {
            var warehouseExists = await _warehouseRepo.ExistsAsync(
                w => w.Id == warehouseId && w.Status == EntityStatus.Active);

            if (!warehouseExists)
                return ApiResponse<UserResponseDto>.Failure("Warehouse not found or is inactive.", statusCode: 400);
        }

        return null;
    }

    private async Task<int> ActiveSameRoleCount(User user)
    {

        var count = await _userRepository.CountAsync(
               u => u.Id != user.Id &&
               u.Role == user.Role &&
               u.WarehouseId == user.WarehouseId &&
               u.Status == UserStatus.Active);

        return count;
    }
}
