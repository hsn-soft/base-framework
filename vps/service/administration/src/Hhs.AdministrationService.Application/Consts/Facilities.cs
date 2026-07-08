namespace Hhs.AdministrationService.Application.Consts;

/// <summary>
/// Log "facility" values describing the operation a FrameworkInfoLog/FrameworkErrorLog call
/// represents. Deliberately separate from EventNames (which names the actual integration events
/// published on the bus) — a facility describes what's happening right now, not which event fired.
/// </summary>
public static class Facilities
{
    public const string JobTriggered = "JOB_TRIGGERED_SUCCESS";
}
