using System.Runtime.CompilerServices;
using FluentAssertions;

namespace InventoryApp.Tests;

internal static class TestSetup
{
    [ModuleInitializer]
    public static void Initialize()
    {
        AssertionOptions.AssertEquivalencyUsing(options => options
            .ExcludingMissingMembers());
    }
}
