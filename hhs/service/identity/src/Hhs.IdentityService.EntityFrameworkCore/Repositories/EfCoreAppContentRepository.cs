using AutoMapper;
using AutoMapper.QueryableExtensions;
using Hhs.IdentityService.Domain.TenantDomain.Entities;
using Hhs.IdentityService.Domain.TenantDomain.Repositories;
using Hhs.IdentityService.EntityFrameworkCore.Context;
using HsnSoft.Base.Domain.Models;
using HsnSoft.Base.Domain.Repositories;
using HsnSoft.Base.Users;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Localization;

namespace Hhs.IdentityService.EntityFrameworkCore.Repositories;

public sealed class EfCoreAppContentRepository :
    EfCoreGenericRepository<AppContent, Guid>,
    IAppContentRepository
{
    private readonly IdentityServiceDbContext _dbContext;
    private readonly ICurrentUser _currentUser;
    private readonly IConfigurationProvider _mapperConfiguration;

    public EfCoreAppContentRepository(
        IServiceProvider provider,
        IStringLocalizerFactory stringLocalizerFactory,
        IdentityServiceDbContext dbContext,
        ICurrentUser currentUser,
        IConfigurationProvider mapperConfiguration)
        : base(provider, dbContext)
    {
        _dbContext = dbContext;
        _currentUser = currentUser;
        _mapperConfiguration = mapperConfiguration;
    }

    public async Task<PagedQueryResult<AppContentListDto>> GetAccessiblePagedListAsync(
        AppContentPagedInput input,
        CancellationToken cancellationToken = default)
    {
        IQueryable<AppContent> query = _dbContext.Set<AppContent>()
            .AsNoTracking();
            // .ApplyContentScope(_currentUser);

        if (input.ClientId.HasValue)
            query = query.Where(x => x.ClientId == input.ClientId.Value);

        if (input.ProductTypeId.HasValue)
            query = query.Where(x => x.ProductTypeId == input.ProductTypeId.Value);

        if (!string.IsNullOrWhiteSpace(input.SlugKey))
            query = query.Where(x => x.SlugKey.Contains(input.SlugKey));

        if (input.OperationStatus.HasValue)
            query = query.Where(x => x.OperationStatus == input.OperationStatus.Value);

        var totalCount = await query.LongCountAsync(cancellationToken);

        var items = await query
            .OrderByDescending(x => x.CreationTime)
            .Skip((input.PageNumber - 1) * input.MaxResultCount)
            .Take(input.MaxResultCount)
            .ProjectTo<AppContentListDto>(_mapperConfiguration)
            .ToListAsync(cancellationToken);

        return new PagedQueryResult<AppContentListDto>
        {
            TotalCount = totalCount,
            Items = items
        };
    }
}

// public static class ContentScopeQueryableExtensions
// {
//     public static IQueryable<AppContent> ApplyContentScope(this IQueryable<AppContent> query, ICurrentUser currentUser)
//     {
//         if (currentUser.IsSystemTenant)
//             return query;
//
//         var allowedContents = currentUser.AllowedContents;
//
//         if (allowedContents.Count == 0)
//             return query.Where(x => false);
//
//         Expression<Func<AppContent, bool>> expression = x => false;
//
//         foreach (var scope in allowedContents)
//         {
//             var clientId = scope.ClientId;
//             var productTypeId = scope.ProductTypeId;
//
//             Expression<Func<AppContent, bool>> itemExpression =
//                 x => x.ClientId == clientId && x.ProductTypeId == productTypeId;
//
//             expression = expression.OrElse(itemExpression);
//         }
//
//         return query.Where(expression);
//     }
//
//     private static Expression<Func<T, bool>> OrElse<T>(
//         this Expression<Func<T, bool>> left,
//         Expression<Func<T, bool>> right)
//     {
//         var parameter = Expression.Parameter(typeof(T));
//
//         var leftVisitor = new ReplaceExpressionVisitor(left.Parameters[0], parameter);
//         var leftBody = leftVisitor.Visit(left.Body);
//
//         var rightVisitor = new ReplaceExpressionVisitor(right.Parameters[0], parameter);
//         var rightBody = rightVisitor.Visit(right.Body);
//
//         return Expression.Lambda<Func<T, bool>>(
//             Expression.OrElse(leftBody!, rightBody!),
//             parameter);
//     }
//
//     private sealed class ReplaceExpressionVisitor : ExpressionVisitor
//     {
//         private readonly Expression _oldValue;
//         private readonly Expression _newValue;
//
//         public ReplaceExpressionVisitor(Expression oldValue, Expression newValue)
//         {
//             _oldValue = oldValue;
//             _newValue = newValue;
//         }
//
//         public override Expression Visit(Expression node)
//         {
//             return node == _oldValue ? _newValue : base.Visit(node);
//         }
//     }
// }