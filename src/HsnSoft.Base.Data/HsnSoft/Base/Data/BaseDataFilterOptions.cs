using System;
using System.Collections.Generic;

namespace HsnSoft.Base.Data;

public class BaseDataFilterOptions
{
    public Dictionary<Type, DataFilterState> DefaultStates { get; }

    public BaseDataFilterOptions()
    {
        DefaultStates = new Dictionary<Type, DataFilterState>();
    }
}