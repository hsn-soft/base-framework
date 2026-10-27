using System;

namespace HsnSoft.Base.Data;

/// <summary>
/// Switches the filters the persistence puts on every query. <see cref="ISoftDelete"/> (hide the deleted) and <c>IMultiTenant</c> of <c>HsnSoft.Base.MultiTenancy</c> (hide other tenants) are on
/// unless code turns them off for a while: <c>using (filter.Disable&lt;ISoftDelete&gt;()) { ... }</c>. The switch is per flow of the code (it works across <c>await</c>), not per application.
/// </summary>
public interface IDataFilter
{
    bool IsEnabled<TFilter>()
        where TFilter : class;

    /// <summary>Turns the filter on until the returned scope is disposed.</summary>
    IDisposable Enable<TFilter>()
        where TFilter : class;

    /// <summary>Turns the filter off until the returned scope is disposed.</summary>
    IDisposable Disable<TFilter>()
        where TFilter : class;
}
