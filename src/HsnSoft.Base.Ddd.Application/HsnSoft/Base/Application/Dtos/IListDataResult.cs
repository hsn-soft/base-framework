using System.Collections.Generic;

namespace HsnSoft.Base.Application.Dtos;

public interface IListDataResult<T>
{
    IReadOnlyList<T> Items { get; set; }
}