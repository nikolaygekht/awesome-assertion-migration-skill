using FluentAssertions;
using FluentAssertions.Execution;
using FluentAssertions.Primitives;

namespace TestKit;

public class ApiResponse
{
    public int StatusCode { get; set; }
    public string Body { get; set; } = "";
    public Dictionary<string, string> Headers { get; set; } = new();
}

public static class ApiResponseExtensions
{
    public static ApiResponseAssertions Should(this ApiResponse instance)
    {
        return new ApiResponseAssertions(instance, AssertionChain.GetOrCreate());
    }
}

public class ApiResponseAssertions
    : ReferenceTypeAssertions<ApiResponse, ApiResponseAssertions>
{
    private readonly AssertionChain chain;

    public ApiResponseAssertions(ApiResponse instance, AssertionChain chain)
        : base(instance, chain)
    {
        this.chain = chain;
    }

    protected override string Identifier => "response";

    [CustomAssertion]
    public AndConstraint<ApiResponseAssertions> BeSuccessful(
        string because = "", params object[] becauseArgs)
    {
        chain
            .BecauseOf(because, becauseArgs)
            .WithExpectation("Expected {context:response} to be successful{reason}, ", c => c
                .ForCondition(Subject is not null)
                .FailWith("but found <null>.")
                .Then
                .ForCondition(Subject!.StatusCode is >= 200 and < 300)
                .FailWith("but the status code is {0}.", Subject.StatusCode));

        return new AndConstraint<ApiResponseAssertions>(this);
    }

    [CustomAssertion]
    public AndWhichConstraint<ApiResponseAssertions, string> HaveHeader(
        string name, string because = "", params object[] becauseArgs)
    {
        chain
            .BecauseOf(because, becauseArgs)
            .ForCondition(Subject.Headers.ContainsKey(name))
            .FailWith("Expected {context:response} to have header {0}{reason}, but found {1}.",
                name, Subject.Headers.Keys);

        Subject.Headers.TryGetValue(name, out string? value);

        return new AndWhichConstraint<ApiResponseAssertions, string>(this, value!, chain, "[" + name + "]");
    }
}
