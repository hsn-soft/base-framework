using System.Reflection;

namespace HsnSoft.Base.Json.Mask.MaskingInfo;

public class PropertyMaskingInfo
{
    public PropertyMaskingInfo(PropertyInfo propertyInfo, bool isMasked)
    {
        PropertyInfo = propertyInfo;
        IsMasked = isMasked;
    }

    public PropertyInfo PropertyInfo { get; }

    public bool IsMasked { get; }
}