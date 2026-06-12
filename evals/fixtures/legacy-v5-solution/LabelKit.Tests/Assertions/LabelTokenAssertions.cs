using System;
using FluentAssertions;
using FluentAssertions.Execution;
using FluentAssertions.Primitives;
using LabelKit;

namespace LabelKit.Tests.Assertions
{
    public static class LabelTokenExtensions
    {
        public static LabelTokenAssertions Should(this LabelToken token)
        {
            return new LabelTokenAssertions(token);
        }
    }

    public class LabelTokenAssertions
        : ReferenceTypeAssertions<LabelToken, LabelTokenAssertions>
    {
        public LabelTokenAssertions(LabelToken token)
        {
            Subject = token;
        }

        protected override string Identifier => "token";

        public AndConstraint<LabelTokenAssertions> Exist(
            string because = null, params object[] parameters)
        {
            Execute.Assertion
                .BecauseOf(because, parameters)
                .Given(() => Subject)
                .ForCondition(token => token != null)
                .FailWith("Expected {context:token} to exist{reason}, but it does not.");

            return new AndConstraint<LabelTokenAssertions>(this);
        }

        public AndConstraint<LabelTokenAssertions> BeBold(
            string because = null, params object[] parameters)
        {
            Execute.Assertion
                .BecauseOf(because, parameters)
                .Given(() => Subject)
                .ForCondition(token => token != null && token.Bold)
                .FailWith("Expected {context:token} to be bold{reason}, but it is not.");

            return new AndConstraint<LabelTokenAssertions>(this);
        }

        public AndConstraint<LabelTokenAssertions> BeAlignedLeft(
            string because = null, params object[] parameters)
        {
            Execute.Assertion
                .BecauseOf(because, parameters)
                .Given(() => Subject)
                .ForCondition(token => token != null && token.Alignment == TokenAlignment.Left)
                .FailWith("Expected {context:token} to be left-aligned{reason}, but it is {0}.",
                    token => token == null ? (object)"<null>" : token.Alignment);

            return new AndConstraint<LabelTokenAssertions>(this);
        }

        public AndConstraint<LabelTokenAssertions> HaveOffset(
            int expected, string because = null, params object[] parameters)
        {
            Execute.Assertion
                .BecauseOf(because, parameters)
                .Given(() => Subject)
                .ForCondition(token => token != null && token.Offset == expected)
                .FailWith("Expected {context:token} to have offset {0}{reason}, but found {1}.",
                    _ => expected, token => token == null ? (object)"<null>" : token.Offset);

            return new AndConstraint<LabelTokenAssertions>(this);
        }

        // Deliberately hides ReferenceTypeAssertions.Match with a predicate-based variant.
        public new AndConstraint<LabelTokenAssertions> Match(
            Predicate<LabelToken> predicate, string because = null, params object[] parameters)
        {
            Execute.Assertion
                .BecauseOf(because, parameters)
                .Given(() => Subject)
                .ForCondition(token => token != null && predicate(token))
                .FailWith("Expected {context:token} to match the given predicate{reason}, but it does not.");

            return new AndConstraint<LabelTokenAssertions>(this);
        }
    }
}
