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
        Assert.Contains("REVIEW_ALREADY_EXISTS", response.Message);
        Assert.Contains("The player already reviewed this game.", response.Message);
    }

    [Fact]
    public void Invalid_operation_maps_to_generic_500()
    {
        var (statusCode, response) = ApiExceptionHandlingExtensions.CreateApiResponse(new InvalidOperationException("ConnectionStrings:Oracle is not configured."));

        Assert.Equal(500, statusCode);
        Assert.DoesNotContain("ConnectionStrings", response.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Theory]
    [InlineData(1, 409, 40901)]
    [InlineData(54, 409, 40903)]
    [InlineData(60, 409, 40903)]
    [InlineData(1400, 400, 40002)]
    [InlineData(12899, 400, 40002)]
    [InlineData(2290, 409, 40902)]
    [InlineData(2291, 409, 40902)]
    [InlineData(2292, 409, 40902)]
    [InlineData(12541, 503, 50301)]
    [InlineData(99999, 500, 50002)]
    public void Oracle_errors_map_to_actionable_http_responses(int oracleErrorNumber, int expectedStatus, int expectedCode)
    {
        var (statusCode, response) = ApiExceptionHandlingExtensions.CreateOracleApiResponse(oracleErrorNumber);

        Assert.Equal(expectedStatus, statusCode);
        Assert.Equal(expectedCode, response.Code);
        Assert.False(string.IsNullOrWhiteSpace(response.Message));
    }
}
