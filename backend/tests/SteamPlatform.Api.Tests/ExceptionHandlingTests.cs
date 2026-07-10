using SteamPlatform.Api.Infrastructure;
using SteamPlatform.Shared;

namespace SteamPlatform.Api.Tests;

public sealed class ExceptionHandlingTests
{
    [Fact]
    public void Missing_resources_map_to_404()
    {
        var (statusCode, response) = ApiExceptionHandlingExtensions.CreateApiResponse(new ResourceNotFoundException("Notice does not exist."));

        Assert.Equal(404, statusCode);
        Assert.Equal("Notice does not exist.", response.Message);
    }

    [Fact]
    public void Business_rules_map_to_409_with_error_code_title()
    {
        var (statusCode, response) = ApiExceptionHandlingExtensions.CreateApiResponse(
            new BusinessRuleException("REVIEW_ALREADY_EXISTS", "The player already reviewed this game."));

        Assert.Equal(409, statusCode);
        Assert.Equal(40900, response.Code);
        Assert.Equal("The player already reviewed this game.", response.Message);
    }

    [Fact]
    public void Invalid_operation_maps_to_generic_500()
    {
        var (statusCode, response) = ApiExceptionHandlingExtensions.CreateApiResponse(new InvalidOperationException("ConnectionStrings:Oracle is not configured."));

        Assert.Equal(500, statusCode);
        Assert.DoesNotContain("ConnectionStrings", response.Message, StringComparison.OrdinalIgnoreCase);
    }
}
