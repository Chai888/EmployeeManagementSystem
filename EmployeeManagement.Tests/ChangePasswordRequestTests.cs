using System.ComponentModel.DataAnnotations;
using EmployeeManagement.Api.DTOs;
using Xunit;

namespace EmployeeManagement.Tests;

public class ChangePasswordRequestTests
{
    private static List<ValidationResult> Validate(
    ChangePasswordRequest request)
    {
        var results = new List<ValidationResult>();

        var context = new ValidationContext(request);

        Validator.TryValidateObject(
            request,
            context,
            results,
            validateAllProperties: true);

        return results;
    }

    [Fact]
    public void Validate_EmptyCurrentPassword_ShouldFail()
    {
        var request = new ChangePasswordRequest
        {
            CurrentPassword = "",
            NewPassword = "NewPassword123",
            ConfirmNewPassword = "NewPassword123"
        };

        var results = Validate(request);

        Assert.Contains(results, x =>
            x.MemberNames.Contains(nameof(request.CurrentPassword)));
    }

    [Fact]
    public void Validate_NewPasswordShorterThanEightCharacters_ShouldFail()
    {
        var request = new ChangePasswordRequest
        {
            CurrentPassword = "OldPassword123",
            NewPassword = "1234567",
            ConfirmNewPassword = "1234567"
        };

        var results = Validate(request);

        Assert.Contains(results, x =>
            x.MemberNames.Contains(nameof(request.NewPassword)));
    }

    [Fact]
    public void Validate_NewPasswordLongerThanOneHundredCharacters_ShouldFail()
    {
        var longPassword = new string('a', 101);

        var request = new ChangePasswordRequest
        {
            CurrentPassword = "OldPassword123",
            NewPassword = longPassword,
            ConfirmNewPassword = longPassword
        };

        var results = Validate(request);

        Assert.Contains(results, x =>
            x.MemberNames.Contains(nameof(request.NewPassword)));
    }

    [Fact]
    public void Validate_ConfirmPasswordDoesNotMatch_ShouldFail()
    {
        var request = new ChangePasswordRequest
        {
            CurrentPassword = "OldPassword123",
            NewPassword = "NewPassword123",
            ConfirmNewPassword = "DifferentPassword123"
        };

        var results = Validate(request);

        Assert.Contains(results, x =>
            x.MemberNames.Contains(nameof(request.ConfirmNewPassword)));
    }

    [Fact]
    public void Validate_ValidRequest_ShouldPass()
    {
        var request = new ChangePasswordRequest
        {
            CurrentPassword = "OldPassword123",
            NewPassword = "NewPassword123",
            ConfirmNewPassword = "NewPassword123"
        };

        var results = Validate(request);

        Assert.Empty(results);
    }
}