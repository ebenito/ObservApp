using ObservApp.Shared.Services;
using Xunit;

namespace ObservApp.Shared.Tests.Services.Autenticacion;

public sealed class AuthServiceTests
{
	[Theory]
	[InlineData("Invalid login credentials", "Auth_Error_InvalidCredentials")]
	[InlineData("AuthException: Email not confirmed", "Auth_Error_EmailNotConfirmed")]
	[InlineData("User already registered", "Auth_Error_AlreadyRegistered")]
	[InlineData("Password should be at least 6 characters.", "Auth_Error_WeakPassword")]
	[InlineData("{\"code\":\"weak_password\"}", "Auth_Error_WeakPassword")]
	[InlineData("New password should be different from the old password.", "Auth_Error_SamePassword")]
	[InlineData("For security purposes, you can only request this after 60 seconds.", "Auth_Error_RateLimited")]
	[InlineData("over_email_send_rate_limit", "Auth_Error_RateLimited")]
	[InlineData("Token has expired or is invalid", "Auth_Error_CodeExpired")]
	[InlineData("Invalid token", "Auth_Error_CodeInvalid")]
	[InlineData("otp_disabled", "Auth_Error_CodeInvalid")]
	[InlineData("", "Auth_Error_Generic")]
	[InlineData(null, "Auth_Error_Generic")]
	public void MapAuthErrorKey_KnownSupabaseMessages_ReturnResourceKey(string? message, string expectedKey)
	{
		Assert.Equal(expectedKey, AuthService.MapAuthErrorKey(message));
	}

	[Fact]
	public void MapAuthErrorKey_UnknownMessage_ReturnsNullSoRawMessageIsShown()
	{
		Assert.Null(AuthService.MapAuthErrorKey("Something unexpected happened"));
	}
}
