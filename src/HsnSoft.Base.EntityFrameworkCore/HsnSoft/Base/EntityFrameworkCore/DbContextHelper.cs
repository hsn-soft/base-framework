using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using HsnSoft.Base.Domain.Entities;
using HsnSoft.Base.Reflection;
using Microsoft.EntityFrameworkCore;

namespace HsnSoft.Base.EntityFrameworkCore;

internal static class DbContextHelper
{
    public static IEnumerable<Type> GetEntityTypes(Type dbContextType)
    {
        return
            from property in dbContextType.GetTypeInfo().GetProperties(BindingFlags.Public | BindingFlags.Instance)
            where
                ReflectionHelper.IsAssignableToGenericType(property.PropertyType, typeof(DbSet<>)) &&
                typeof(IEntity).IsAssignableFrom(property.PropertyType.GenericTypeArguments[0])
            select property.PropertyType.GenericTypeArguments[0];
    }
}