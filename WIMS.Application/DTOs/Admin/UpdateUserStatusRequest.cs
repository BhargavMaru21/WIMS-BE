using WIMS.Domain.Enums;

namespace WIMS.Application.DTOs.Admin;
public class UpdateUserStatusRequest
{
    public required UserStatus Status { get; set; }
}