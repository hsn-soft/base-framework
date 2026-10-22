using System.Reflection;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using FluentAssertions;
using HsnSoft.Base.Json;
using HsnSoft.Base.Json.Newtonsoft;
using HsnSoft.Base.Json.Newtonsoft.Converters;
using HsnSoft.Base.Json.SystemTextJson;
using HsnSoft.Base.Json.SystemTextJson.JsonConverters;
using HsnSoft.Base.Timing;
using HsnSoft.Base.VirtualFileSystem;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;
using MsOptions = Microsoft.Extensions.Options.Options;
using JsonException = System.Text.Json.JsonException;
using JsonSerializer = System.Text.Json.JsonSerializer;
using NewtonsoftJson = Newtonsoft.Json.JsonConvert;

namespace HsnSoft.Base.Test.Unit.Timing;

public class TimingTests
{
    // ---------- Timing ----------

    [Theory]
    [InlineData(DateTimeKind.Utc, true)]
    [InlineData(DateTimeKind.Local, false)]
    [InlineData(DateTimeKind.Unspecified, false)]
    public void Clock_ReportsKindAndNow(DateTimeKind kind, bool multiTz)
    {
        var clock = new Clock(MsOptions.Create(new BaseClockOptions { Kind = kind }));
        clock.Kind.Should().Be(kind);
        clock.SupportsMultipleTimezone.Should().Be(multiTz);
        if (kind == DateTimeKind.Utc) clock.Now.Kind.Should().Be(DateTimeKind.Utc);
        if (kind == DateTimeKind.Local) clock.Now.Kind.Should().Be(DateTimeKind.Local);
    }

    [Fact]
    public void Clock_Normalize_Works()
    {
        var utcClock = new Clock(MsOptions.Create(new BaseClockOptions { Kind = DateTimeKind.Utc }));
        var localClock = new Clock(MsOptions.Create(new BaseClockOptions { Kind = DateTimeKind.Local }));
        var unspecClock = new Clock(MsOptions.Create(new BaseClockOptions()));

        var utc = new DateTime(2020, 1, 1, 0, 0, 0, DateTimeKind.Utc);
        var local = new DateTime(2020, 1, 1, 0, 0, 0, DateTimeKind.Local);
        var unspec = new DateTime(2020, 1, 1, 0, 0, 0, DateTimeKind.Unspecified);

        unspecClock.Normalize(local).Should().Be(local);
        utcClock.Normalize(utc).Should().Be(utc);
        localClock.Normalize(utc).Kind.Should().Be(DateTimeKind.Local);
        utcClock.Normalize(local).Kind.Should().Be(DateTimeKind.Utc);
        utcClock.Normalize(unspec).Kind.Should().Be(DateTimeKind.Utc);
        localClock.Normalize(unspec).Kind.Should().Be(DateTimeKind.Local);
        new BaseClockOptions().Kind.Should().Be(DateTimeKind.Unspecified);
    }

    [Fact]
    public void TimingExtensions_And_TimezoneProvider_Work()
    {
        var provider = new ServiceCollection().AddBaseTimingServiceCollection().BuildServiceProvider();
        provider.GetRequiredService<IClock>().Kind.Should().Be(DateTimeKind.Utc);

        var tz = new TZConvertTimezoneProvider();
        tz.GetWindowsTimezones().Should().Contain(x => x.Name == "Turkey Standard Time");
        tz.GetIanaTimezones().Should().Contain(x => x.Name == "Europe/Istanbul");
        tz.WindowsToIana("Turkey Standard Time").Should().Be("Europe/Istanbul");
        tz.IanaToWindows("Europe/Istanbul").Should().Be("Turkey Standard Time");
        tz.GetTimeZoneInfo("Europe/Istanbul").Should().NotBeNull();
        TimingSettingNames.TimeZone.Should().Be("Base.Timing.TimeZone");
        new DisableDateTimeNormalizationAttribute().Should().NotBeNull();
    }

}
