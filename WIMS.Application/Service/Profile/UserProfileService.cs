using AutoMapper;
using Microsoft.EntityFrameworkCore;
using WIMS.Application.DTOs;
using WIMS.Application.DTOs.Profile;
using WIMS.Application.Interfaces.Common;
using WIMS.Application.Interfaces.Repositories;
using WIMS.Application.Interfaces.Services.Profile;

namespace WIMS.Application.Service.Profile;


public class UserProfileService : IUserProfileService
{
    private readonly IUserRepository _userRepository;
    private readonly IInputNormalizer _inputNormalizer;
    private readonly IMapper _mapper;
    private readonly ICurrentUserService _currentUser;

    public UserProfileService(
        IUserRepository userRepository,
        IInputNormalizer inputNormalizer,
        IMapper mapper,
        ICurrentUserService currentUser
        )
    {
        _userRepository = userRepository;
        _inputNormalizer = inputNormalizer;
        _mapper = mapper;
        _currentUser = currentUser;
    }

    public async Task<ApiResponse<UserProfileResponse>> GetProfile()
    {
        int userId = _currentUser.GetUserId();

        var user = await _userRepository.GetAsync(
            u => u.Id == userId,
            includes: q => q.Include(u => u.Warehouse));

        if (user is null)
            return ApiResponse<UserProfileResponse>.Failure("User not found.", statusCode: 404);

        var response = _mapper.Map<UserProfileResponse>(user);
        return ApiResponse<UserProfileResponse>.Success(response);
    }

    public async Task<ApiResponse<UserProfileResponse>> UpdateProfile(UpdateProfileRequest request)
    {
        int userId = _currentUser.GetUserId();
        request = _inputNormalizer.NormalizeObject(request);

        var user = await _userRepository.GetAsync(
            u => u.Id == userId,
            useNoTracking: false,
            includes: q => q.Include(u => u.Warehouse));

        if (user is null)
            return ApiResponse<UserProfileResponse>.Failure("User not found.", statusCode: 404);

        user.FullName = string.IsNullOrWhiteSpace(request.FullName) ? user.FullName : request.FullName;
        user.PhoneNumber = string.IsNullOrWhiteSpace(request.PhoneNumber) ? null : request.PhoneNumber;
        user.ModifiedAt = DateTime.UtcNow;
        user.ModifiedBy = userId;

        await _userRepository.SaveChangesAsync();

        var response = _mapper.Map<UserProfileResponse>(user);
        return ApiResponse<UserProfileResponse>.Success(response, "Profile updated successfully.");
    }
}