using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;

namespace HsnSoft.Base.Domain.Repositories;

/// <summary>
/// The <c>Select</c> of an entity into a DTO (<see cref="For{TEntity, TDto}"/>), made from the DTO itself so nobody writes it field by field and no mapper is needed. It is an expression, so the store reads only the columns (or the fields) the DTO has, and a relational store
/// joins only what the DTO reaches. Built once for each pair of types.
/// </summary>
/// <remarks>
/// The rules, for each public property of the DTO that has a setter and no <see cref="NotMappedAttribute"/>: the entity's property of the same name (case does not matter); else a name that goes through objects (<c>CustomerName</c> is <c>Customer.Name</c>);
/// an object or a list of objects is projected the same way, member by member. A DTO property nothing answers is an error, not a silent <see langword="null"/>. A value that needs more than this (a calculation, a format) is written as the selector
/// of the repository method instead.
/// </remarks>
public static class EntityProjection
{
    /// <summary>The expression that turns a <typeparamref name="TEntity"/> into a <typeparamref name="TDto"/>, built the first time and kept.</summary>
    public static Expression<Func<TEntity, TDto>> For<TEntity, TDto>()
        where TEntity : class
        where TDto : class
        => Cache<TEntity, TDto>.Selector;

    /// <summary>
    /// Whether every field of <typeparamref name="TDto"/> is a property of <typeparamref name="TEntity"/> itself: no path through another object (<c>CustomerName</c> from <c>Customer.Name</c>) and no list of objects. Only then can an entity in memory be
    /// turned into the DTO (<see cref="ToDto{TEntity, TDto}"/>) with no read: what a path reaches is not loaded in an entity that was just made.
    /// </summary>
    public static bool IsFlat<TEntity, TDto>()
        where TEntity : class
        where TDto : class
        => Cache<TEntity, TDto>.Flat;

    /// <summary>
    /// The DTO of an entity that is in memory (the one a repository gave back from <c>AddAsync</c>), made by the rules of <see cref="For{TEntity, TDto}"/> and without a read of the store: this is how a service answers a create or an update in one round trip.
    /// </summary>
    /// <exception cref="InvalidOperationException">The DTO reaches another object or a list (<see cref="IsFlat{TEntity, TDto}"/> is false): what it reaches is not in the entity, so the service reads the DTO from the store instead.</exception>
    public static TDto ToDto<TEntity, TDto>(TEntity entity)
        where TEntity : class
        where TDto : class
    {
        ArgumentNullException.ThrowIfNull(entity);

        return Cache<TEntity, TDto>.Flat
            ? Cache<TEntity, TDto>.Mapper(entity)
            : throw new InvalidOperationException($"{typeof(TDto).Name} reaches another object or a list of {typeof(TEntity).Name}, which an entity in memory does not have: read the DTO from the repository (FindAsync<{typeof(TDto).Name}>) instead.");
    }

    private static class Cache<TEntity, TDto>
        where TEntity : class
        where TDto : class
    {
        private static readonly Lazy<Expression<Func<TEntity, TDto>>> s_selector = new(Build);
        private static readonly Lazy<Func<TEntity, TDto>> s_mapper = new(() => s_selector.Value.Compile());
        private static readonly Lazy<bool> s_flat = new(() => new FlatCheck().IsFlat(s_selector.Value));

        public static Expression<Func<TEntity, TDto>> Selector => s_selector.Value;

        public static Func<TEntity, TDto> Mapper => s_mapper.Value;

        public static bool Flat => s_flat.Value;

        private static Expression<Func<TEntity, TDto>> Build()
        {
            ParameterExpression entity = Expression.Parameter(typeof(TEntity), "entity");
            List<string> missing = [];
            MemberInitExpression body = Initialize(entity, typeof(TDto), typeof(TDto).Name, missing);
            if (missing.Count > 0)
            {
                throw new InvalidOperationException($"{typeof(TDto).Name} cannot be filled from {typeof(TEntity).Name}: nothing answers {string.Join(", ", missing)}. Name them as the entity's properties (or as a path such as CustomerName for Customer.Name), mark them [NotMapped], or give the selector yourself.");
            }

            return Expression.Lambda<Func<TEntity, TDto>>(body, entity);
        }
    }

    private static MemberInitExpression Initialize(Expression source, Type dtoType, string path, List<string> missing)
    {
        if (dtoType.GetConstructor(Type.EmptyTypes) is null)
        {
            throw new InvalidOperationException($"{dtoType.Name} needs a public constructor without parameters to be filled from an entity.");
        }

        List<MemberBinding> bindings = [];
        foreach (PropertyInfo property in dtoType.GetProperties(BindingFlags.Public | BindingFlags.Instance))
        {
            if (property.SetMethod is not { IsPublic: true } || property.GetCustomAttribute<NotMappedAttribute>() is not null)
            {
                continue;
            }

            Expression? value = Find(source, property.Name);
            Expression? converted = value is null ? null : Convert(value, property.PropertyType, $"{path}.{property.Name}", missing);
            if (converted is null)
            {
                if (value is null)
                {
                    missing.Add($"{path}.{property.Name}");
                }

                continue;
            }

            bindings.Add(Expression.Bind(property, converted));
        }

        return Expression.MemberInit(Expression.New(dtoType), bindings);
    }

    // the entity's property of this name, or a path through objects that spells it (CustomerName: Customer, then Name)
    private static Expression? Find(Expression source, string name)
    {
        PropertyInfo[] properties = [.. source.Type.GetProperties(BindingFlags.Public | BindingFlags.Instance).Where(property => property.GetIndexParameters().Length == 0 && property.GetMethod is { IsPublic: true })];
        PropertyInfo? exact = properties.FirstOrDefault(property => string.Equals(property.Name, name, StringComparison.OrdinalIgnoreCase));
        if (exact is not null)
        {
            return Expression.Property(source, exact);
        }

        foreach (PropertyInfo property in properties.Where(property => IsObject(property.PropertyType) && name.Length > property.Name.Length && name.StartsWith(property.Name, StringComparison.OrdinalIgnoreCase)))
        {
            Expression? inner = Find(Expression.Property(source, property), name[property.Name.Length..]);
            if (inner is not null)
            {
                return inner;
            }
        }

        return null;
    }

    private static Expression? Convert(Expression value, Type target, string path, List<string> missing)
    {
        Type from = value.Type;
        // (a value type is "assignable" to its nullable type for reflection, but the expression needs the conversion)
        if (Nullable.GetUnderlyingType(target) == from)
        {
            return Expression.Convert(value, target);
        }

        if (target.IsAssignableFrom(from))
        {
            return value;
        }

        // a list of objects becomes a list of DTOs, each filled the same way
        if (ElementOf(from) is { } fromItem && ElementOf(target) is { } toItem && IsObject(fromItem) && IsObject(toItem) && ListOf(target, toItem) is { } makeList)
        {
            ParameterExpression item = Expression.Parameter(fromItem, "item");
            LambdaExpression select = Expression.Lambda(Initialize(item, toItem, path, missing), item);
            return makeList(Expression.Call(typeof(Enumerable), nameof(Enumerable.Select), [fromItem, toItem], value, select));
        }

        // an object becomes a DTO, filled the same way
        if (IsObject(from) && IsObject(target) && ElementOf(from) is null)
        {
            return Initialize(value, target, path, missing);
        }

        missing.Add($"{path} (a {from.Name} cannot be a {target.Name})");
        return null;
    }

    private static bool IsObject(Type type) => type.IsClass && type != typeof(string) && !type.IsArray && ElementOf(type) is null;

    private static Type? ElementOf(Type type)
    {
        if (type == typeof(string))
        {
            return null;
        }

        if (type.IsArray)
        {
            return type.GetElementType();
        }

        Type? enumerable = type.IsGenericType && type.GetGenericTypeDefinition() == typeof(IEnumerable<>)
            ? type
            : type.GetInterfaces().FirstOrDefault(contract => contract.IsGenericType && contract.GetGenericTypeDefinition() == typeof(IEnumerable<>));
        return enumerable?.GetGenericArguments()[0];
    }

    // how a sequence of DTOs is made into the type the DTO declares: an array, a List, or anything a List can be assigned to (IEnumerable, IReadOnlyList, IList, ICollection)
    private static Func<Expression, Expression>? ListOf(Type target, Type item)
    {
        if (target.IsArray)
        {
            return sequence => Expression.Call(typeof(Enumerable), nameof(Enumerable.ToArray), [item], sequence);
        }

        Type list = typeof(List<>).MakeGenericType(item);
        return target.IsAssignableFrom(list) && typeof(IEnumerable).IsAssignableFrom(target)
            ? sequence => Expression.Call(typeof(Enumerable), nameof(Enumerable.ToList), [item], sequence)
            : null;
    }
}

// a selector is flat when it reads only properties of its parameter: no member of a member, no method (the Select of a list), no object made inside an object
sealed file class FlatCheck : ExpressionVisitor
{
    private bool _flat = true;
    private bool _insideRoot;

    public bool IsFlat(LambdaExpression selector)
    {
        Visit(selector.Body);
        return _flat;
    }

    protected override Expression VisitMemberInit(MemberInitExpression node)
    {
        if (_insideRoot)
        {
            _flat = false;   // an object inside the DTO
            return node;
        }

        _insideRoot = true;
        return base.VisitMemberInit(node);
    }

    protected override Expression VisitMember(MemberExpression node)
    {
        if (node.Expression is not (ParameterExpression or null))
        {
            _flat = false;   // a path: a member of a member
            return node;
        }

        return base.VisitMember(node);
    }

    protected override Expression VisitMethodCall(MethodCallExpression node)
    {
        _flat = false;
        return node;
    }
}
