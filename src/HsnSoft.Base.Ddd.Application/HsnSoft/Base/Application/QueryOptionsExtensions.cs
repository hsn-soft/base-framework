using System;
using System.Linq;
using System.Linq.Expressions;
using HsnSoft.Base.Application.Dtos;
using HsnSoft.Base.Domain.Repositories;
using HsnSoft.Base.Domain.Sorting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace HsnSoft.Base.Application;

/// <summary>Turns what a client asked for into what a repository takes, and what a repository answers into what the client is sent.</summary>
public static class QueryOptionsExtensions
{
    /// <summary>The options of a list: the filter built by the service, the sorting the client asked for (or <paramref name="defaultSorting"/>) checked against <paramref name="sortFields"/>, and the limit.</summary>
    public static ListQueryOptions<TEntity> ToListOptions<TEntity>(this ISortedDataRequest request, Expression<Func<TEntity, bool>> filter, SortFields<TEntity> sortFields, string defaultSorting)
    {
        ArgumentNullException.ThrowIfNull(request);
        ArgumentNullException.ThrowIfNull(sortFields);

        return new ListQueryOptions<TEntity> { Filter = filter, Sort = sortFields.Parse(request.SortingText, defaultSorting), MaxResultCount = request.MaxResultCount };
    }

    /// <summary>The options of a page.</summary>
    public static PagedQueryOptions<TEntity> ToPagedOptions<TEntity>(this IPagedDataRequest request, Expression<Func<TEntity, bool>> filter, SortFields<TEntity> sortFields, string defaultSorting)
    {
        ArgumentNullException.ThrowIfNull(request);
        ArgumentNullException.ThrowIfNull(sortFields);

        return new PagedQueryOptions<TEntity> { Filter = filter, Sort = sortFields.Parse(request.SortingText, defaultSorting), PageNumber = request.PageNumber, MaxResultCount = request.MaxResultCount };
    }

    /// <summary>The page a repository answered, as the client is sent it: each entity shown by <paramref name="map"/>.</summary>
    public static PagedDataResultDto<TDto> ToPagedDto<TEntity, TDto>(this PagedQueryResult<TEntity> result, IPagedDataRequest request, Func<TEntity, TDto> map)
    {
        ArgumentNullException.ThrowIfNull(result);
        ArgumentNullException.ThrowIfNull(request);
        ArgumentNullException.ThrowIfNull(map);

        return new PagedDataResultDto<TDto>(result.TotalCount, request.PageNumber, request.MaxResultCount, [.. result.Items.Select(map)]);
    }

    /// <summary>The page a repository answered with DTOs already (<c>GetPagedListAsync&lt;TDto&gt;</c>), as the client is sent it.</summary>
    public static PagedDataResultDto<TDto> ToPagedDto<TDto>(this PagedQueryResult<TDto> result, IPagedDataRequest request)
        => ToPagedDto(result, request, dto => dto);

    /// <summary>Reads the <c>Paging</c> section: the most a client may ask for in a list.</summary>
    public static IServiceCollection AddBasePaging(this IServiceCollection services, Microsoft.Extensions.Configuration.IConfiguration? configuration = null)
    {
        ArgumentNullException.ThrowIfNull(services);

        OptionsBuilder<PagingOptions> options = services.AddOptions<PagingOptions>();
        if (configuration is not null)
        {
            options.Bind(configuration.GetSection(PagingOptions.SectionName));
        }

        return services;
    }
}
