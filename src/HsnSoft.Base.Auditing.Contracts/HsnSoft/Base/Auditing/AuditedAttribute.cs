using System;

namespace HsnSoft.Base.Auditing;

[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method | AttributeTargets.Property)]
public class AuditedAttribute : Attribute
{

}
