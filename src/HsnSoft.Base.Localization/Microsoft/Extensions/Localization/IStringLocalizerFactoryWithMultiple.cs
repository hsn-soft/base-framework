using System;
using System.Collections.Generic;
using JetBrains.Annotations;

namespace Microsoft.Extensions.Localization;

public interface IStringLocalizerFactoryWithMultiple
{
    [CanBeNull]
    IStringLocalizer CreateMultiple(List<Type> resourceTypes);
}