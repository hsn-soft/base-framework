using System.Reflection;
using HsnSoft.Base.Json.Mask.MaskingInfo;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;

namespace HsnSoft.Base.Json.Newtonsoft.Converters;

public class MaskedContractResolver : DefaultContractResolver
{
    private MaskConverter MaskConverter => new MaskConverter();

    protected override JsonProperty CreateProperty(MemberInfo member, MemberSerialization memberSerialization)
    {
        var property = base.CreateProperty(member, memberSerialization);

        var reflectedType = member.ReflectedType;
        if (reflectedType == null)
            return property;

        var typeMaskingInfo = TypeMaskingInfoHelper.Get(reflectedType);
        if (typeMaskingInfo.HasMaskedProperties == false)
            return property;

        if (typeMaskingInfo.IsMemberMasked(member))
            property.Converter = MaskConverter;

        return property;
    }
}