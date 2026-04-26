namespace LegalCaseAPI.DTOs;

public record RegisterDto(string FullName, string Email, string Password, string Role);
public record LoginDto(string Email, string Password);
public record AuthResponse(string Token, string UserId, int ProfileId, string Role, string FullName, string Email, bool TwoFactorEnabled = false);
public record ForgotPasswordDto(string Email);
public record ResetPasswordDto(string Token, string NewPassword);
public record TwoFactorLoginDto(string Email, string Code);
public record TwoFactorVerifyDto(string Code);
public record ChangePasswordDto(string OldPassword, string NewPassword);
