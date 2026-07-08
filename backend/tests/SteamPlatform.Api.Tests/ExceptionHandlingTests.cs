using SteamPlatform.Api.Infrastructure;
using SteamPlatform.Shared;

namespace SteamPlatform.Api.Tests;

public sealed class ExceptionHandlingTests
{
    [Fact]
    public void Missing_resources_map_to_404()
    {
        var problem = ApiExceptionHandlingExtensions.CreateProblem(new ResourceNotFoundException("Notice does not exist."));

        Assert.Equal(404, problem.Status);
        Assert.Equal("Notice does not exist.", problem.Detail);
    }

    [Fact]
    public void Business_rules_map_to_409_with_error_code_title()
    {
        var problem = ApiExceptionHandlingExtensions.CreateProblem(
            new BusinessRuleException("REVIEW_ALREADY_EXISTS", "The player already reviewed this game."));

        Assert.Equal(409, problem.Status);
        Assert.Equal("REVIEW_ALREADY_EXISTS", problem.Title);
        Assert.Equal("The player already reviewed this game.", problem.Detail);
    }

    [Fact]
    public void Invalid_operation_maps_to_generic_500()
    {
        var problem = ApiExceptionHandlingExtensions.CreateProblem(new InvalidOperationException("ConnectionStrings:Oracle is not configured."));

        Assert.Equal(500, problem.Status);
        Assert.DoesNotContain("ConnectionStrings", problem.Detail, StringComparison.OrdinalIgnoreCase);
    }
}
