using SteamPlatform.Api.Infrastructure;

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
    public void Invalid_operation_maps_to_generic_500()
    {
        var problem = ApiExceptionHandlingExtensions.CreateProblem(new InvalidOperationException("ConnectionStrings:Oracle is not configured."));

        Assert.Equal(500, problem.Status);
        Assert.DoesNotContain("ConnectionStrings", problem.Detail, StringComparison.OrdinalIgnoreCase);
    }
}
