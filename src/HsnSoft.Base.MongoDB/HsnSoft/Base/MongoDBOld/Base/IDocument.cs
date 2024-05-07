using System;

namespace HsnSoft.Base.MongoDBOld.Base;

public interface IFullAuditDocument : IBaseDocument
{
    public bool IsDeleted { get; set; }
    public DateTime CreationTime { get; set; }
    public string CreatorId { get; set; }
    public DateTime? LastModificationTime { get; set; }
    public string LastModifierId { get; set; }
}

public interface IBaseDocument
{
    string Id { get; set; }
}