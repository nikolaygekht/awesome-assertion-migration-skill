using FluentAssertions;
using TestKit;
using Xunit;
using Xunit.Sdk;

namespace ApiClient.Tests;

public class ApiResponseTests
{
    private static ApiResponse OkResponse() => new()
    {
        StatusCode = 200,
        Body = """{"status":"ok"}""",
        Headers = new Dictionary<string, string>
        {
            ["Content-Type"] = "application/json",
            ["X-Request-Id"] = "abc-123",
        },
    };

    [Fact]
    public void Ok_response_is_successful()
    {
        var response = OkResponse();

        response.Should().BeSuccessful();
    }

    [Fact]
    public void Server_error_fails_with_the_status_code_in_the_message()
    {
        var response = OkResponse();
        response.StatusCode = 503;

        Action act = () => response.Should().BeSuccessful("the service was healthy");

        act.Should().Throw<XunitException>()
            .WithMessage("Expected response to be successful because the service was healthy, but the status code is 503*");
    }

    [Fact]
    public void Header_assertion_chains_into_the_header_value()
    {
        var response = OkResponse();

        response.Should().HaveHeader("Content-Type")
            .Which.Should().StartWith("application/");
    }

    [Fact]
    public void Missing_header_lists_the_available_ones()
    {
        var response = OkResponse();

        Action act = () => response.Should().HaveHeader("Authorization");

        act.Should().Throw<XunitException>()
            .WithMessage("*to have header \"Authorization\"*");
    }

    [Fact]
    public void Body_round_trips_equivalently()
    {
        var response = OkResponse();
        var expected = new ApiResponse
        {
            StatusCode = 200,
            Body = """{"status":"ok"}""",
            Headers = new Dictionary<string, string>
            {
                ["Content-Type"] = "application/json",
                ["X-Request-Id"] = "abc-123",
            },
        };

        // cast: the custom Should(this ApiResponse) extension hides the object-level one
        ((object)response).Should().BeEquivalentTo(expected, options => options
            .PreferringDeclaredMemberTypes());
    }
}
