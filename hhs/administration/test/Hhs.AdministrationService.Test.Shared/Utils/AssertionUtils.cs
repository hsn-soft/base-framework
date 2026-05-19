using FluentAssertions;

namespace Hhs.AdministrationService.Test.Shared.Utils;

public static class AssertionUtils
{
    // Skip database vs linq millisecond different
    public static void ShouldBeEquivalentToWithMilliseconds<T>(this T actual, T expected)
    {
        actual.Should().BeEquivalentTo(expected, opt => opt
            .Using<DateTime>(ctx =>
                ctx.Subject.TruncateToMilliseconds().Should()
                    .Be(ctx.Expectation.TruncateToMilliseconds()))
            .WhenTypeIs<DateTime>());
    }

    private static DateTime TruncateToMilliseconds(this DateTime dt) => new(dt.Ticks - dt.Ticks % TimeSpan.TicksPerMillisecond, dt.Kind);
}