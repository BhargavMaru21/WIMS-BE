using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using WIMS.Application.DTOs;
using WIMS.Application.DTOs.Profile;
using WIMS.Application.Interfaces.Services.Profile;

namespace WIMS.Api.Controllers.Profile;

[ApiController]
[Route("api/profile")]
[Authorize]
public class ProfileController : ControllerBase
{
    private readonly IUserProfileService _userProfileService;

    public ProfileController(IUserProfileService userProfileService)
    {
        _userProfileService = userProfileService;
    }

    [HttpGet]
    public async Task<ApiResponse<UserProfileResponse>> GetProfile()
    {
        var result = await _userProfileService.GetProfile();
        return result;
    }

    [HttpPatch]
    public async Task<ApiResponse<UserProfileResponse>> UpdateProfile(UpdateProfileRequest request)
    {
        var result = await _userProfileService.UpdateProfile(request);
        return result;
    }
}