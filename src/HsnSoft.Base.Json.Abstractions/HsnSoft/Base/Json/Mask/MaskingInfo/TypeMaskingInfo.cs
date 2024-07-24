using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace HsnSoft.Base.Json.Mask.MaskingInfo;

public class TypeMaskingInfo
{
    private readonly IReadOnlyDictionary<PropertyInfo, PropertyMaskingInfo> _propertyMaskingInfos;

    private TypeMaskingInfo(Type type, PropertyMaskingInfo[] properties, bool hasMaskedProperties)
    {
        Type = type;
        HasMaskedProperties = hasMaskedProperties;
        _propertyMaskingInfos = properties.ToDictionary(i => i.PropertyInfo);
    }

    public Type Type { get; }

    public bool HasMaskedProperties { get; }

    public static TypeMaskingInfo ForTypesWithMaskedProperties(Type type, PropertyMaskingInfo[] properties)
    {
        return new TypeMaskingInfo(type, properties, true);
    }

    public static TypeMaskingInfo ForTypesWithoutMaskedProperties(Type type)
    {
        return new TypeMaskingInfo(type, Array.Empty<PropertyMaskingInfo>(), false);
    }

    public IEnumerable<PropertyMaskingInfo> GetAllProperties()
    {
        if (HasMaskedProperties == false)
            throw new InvalidOperationException("Type does not have masked properties. Properties are not accessible");

        return _propertyMaskingInfos.Values;
    }

    public bool IsMemberMasked(MemberInfo memberInfo)
    {
        if (HasMaskedProperties && memberInfo is PropertyInfo propertyInfo)
        {
            if (_propertyMaskingInfos.TryGetValue(propertyInfo, out var propertyMaskingInfo))
                return propertyMaskingInfo.IsMasked;
        }

        return false;
    }
}