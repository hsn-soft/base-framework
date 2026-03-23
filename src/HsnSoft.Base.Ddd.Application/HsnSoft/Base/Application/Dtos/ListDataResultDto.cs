using System;
using System.Collections.Generic;

namespace HsnSoft.Base.Application.Dtos;

[Serializable]
public class ListDataResultDto<T> : IListDataResult<T>
{
    public IReadOnlyList<T> Items
    {
        get { return _items ??= new List<T>(); }
        set { _items = value; }
    }

    private IReadOnlyList<T> _items;

    public ListDataResultDto()
    {
    }

    public ListDataResultDto(IReadOnlyList<T> items)
    {
        Items = items;
    }
}