namespace WIMS.Domain.Constant;
public static class AuditActions
{
    // CRUD
    public const string Created = "Created";
    public const string Updated = "Updated";
    public const string Deleted = "Deleted";

    // Status
    public const string StatusChanged = "StatusChanged";
    public const string Activated = "Activated";
    public const string Deactivated = "Deactivated";

    // Auth
    public const string Login = "Login";
    public const string LoginFailed = "LoginFailed";
    public const string Logout = "Logout";
    public const string AccountLocked = "AccountLocked";
    public const string ForgotPasswordRequested = "ForgotPasswordRequested";
    public const string PasswordReset = "PasswordReset";
    public const string PasswordChanged = "PasswordChanged";

    // Workflow
    public const string Approved = "Approved";
    public const string Rejected = "Rejected";
    public const string Cancelled = "Cancelled";
    public const string Submitted = "Submitted";
}