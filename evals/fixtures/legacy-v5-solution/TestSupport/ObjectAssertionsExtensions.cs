using System;
using System.Reflection;
using FluentAssertions;
using FluentAssertions.Execution;
using FluentAssertions.Primitives;

namespace TestSupport
{
    /// <summary>
    /// Shared assertion helpers used by all test projects of the solution.
    /// </summary>
    public static class ObjectAssertionsExtensions
    {
        public static AndConstraint<ObjectAssertions> HavePropertyValue(
            this ObjectAssertions assertions, string name, object expected,
            string because = null, params object[] becauseParameters)
        {
            Execute.Assertion
                .BecauseOf(because, becauseParameters)
                .Given(() => assertions.Subject)
                .ForCondition(subject => HasPropertyValue(subject, name, expected))
                .FailWith("Expected the object to have a property {0} equal to {1}{reason}, but it does not.",
                    _ => name, _ => expected);

            return new AndConstraint<ObjectAssertions>(assertions);
        }

        private static bool HasPropertyValue(object subject, string name, object expected)
        {
            if (subject == null)
            {
                return false;
            }

            PropertyInfo property = subject.GetType().GetProperty(name);
            if (property == null)
            {
                return false;
            }

            object actual = property.GetValue(subject);
            return Equals(actual, expected);
        }
    }
}
