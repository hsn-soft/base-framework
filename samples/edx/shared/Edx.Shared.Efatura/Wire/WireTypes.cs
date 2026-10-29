using System.Xml.Schema;
using System.Xml.Serialization;

namespace Edx.Shared.Efatura.Wire;

/// <summary>The namespaces of the WSDL of the center (<c>EFatura.wsdl</c>).</summary>
public static class WireNames
{
    public const string Namespace = "http://gib.gov.tr/vedop3/eFatura";
    public const string XmlMime = "http://www.w3.org/2005/05/xmlmime";

    /// <summary>The name the contract is known by (<c>EFaturaPortType</c>).</summary>
    public const string Contract = "EFaturaPortType";

    /// <summary>Where the center listens (and an integrator's post box, by habit): <c>/EFaturaMerkez/services/EFatura/</c>.</summary>
    public const string CenterPath = "/EFaturaMerkez/services/EFatura/";
}

/// <summary>A binary element of the message (<c>xmime:base64Binary</c>): the bytes, sent as an MTOM attachment, and their content type.</summary>
[XmlType(TypeName = "base64Binary", Namespace = WireNames.XmlMime)]
public class Base64Binary
{
    [XmlAttribute("contentType", Form = XmlSchemaForm.Qualified)]
    public string? ContentType { get; set; }

    [XmlText(DataType = "base64Binary")]
    public byte[]? Value { get; set; }
}

/// <summary>What <c>sendDocument</c> takes: the file name (<c>{envelope uuid}.zip</c>), the zip and the MD5 of the zip (hex).</summary>
[XmlType(TypeName = "documentType", Namespace = WireNames.Namespace)]
public class DocumentType
{
    [XmlElement("fileName", Form = XmlSchemaForm.Unqualified, Order = 0)]
    public string? FileName { get; set; }

    [XmlElement("binaryData", Form = XmlSchemaForm.Unqualified, Order = 1)]
    public Base64Binary? BinaryData { get; set; }

    [XmlElement("hash", Form = XmlSchemaForm.Unqualified, Order = 2)]
    public string? Hash { get; set; }
}

/// <summary>What <c>sendDocument</c> answers: a message (<c>ZARF KUYRUGA EKLENDI</c>) and the hash it was given.</summary>
[XmlType(TypeName = "documentReturnType", Namespace = WireNames.Namespace)]
public class DocumentReturnType
{
    [XmlElement("msg", Form = XmlSchemaForm.Unqualified, Order = 0)]
    public string? Msg { get; set; }

    [XmlElement("hash", Form = XmlSchemaForm.Unqualified, Order = 1)]
    public string? Hash { get; set; }
}

[XmlType(TypeName = "getAppRespRequestType", Namespace = WireNames.Namespace)]
public class GetAppRespRequestType
{
    [XmlElement("instanceIdentifier", Form = XmlSchemaForm.Unqualified, Order = 0)]
    public string? InstanceIdentifier { get; set; }
}

[XmlType(TypeName = "getAppRespResponseType", Namespace = WireNames.Namespace)]
public class GetAppRespResponseType
{
    [XmlElement("applicationResponse", Form = XmlSchemaForm.Unqualified, Order = 0)]
    public string? ApplicationResponse { get; set; }
}

/// <summary>The fault of the center: a code (<c>1110</c>, <c>2004</c>, ...) and a message.</summary>
[XmlType(TypeName = "EFaturaFaultType", Namespace = WireNames.Namespace)]
public class EFaturaFaultType
{
    [XmlElement("code", Form = XmlSchemaForm.Unqualified, Order = 0)]
    public int Code { get; set; }

    [XmlIgnore]
    public bool CodeSpecified { get; set; } = true;

    [XmlElement("msg", Form = XmlSchemaForm.Unqualified, Order = 1)]
    public string? Msg { get; set; }
}
