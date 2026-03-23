using System;

namespace HsnSoft.Base.MongoDBOld.Base;

public interface IFullAuditDocument : IBaseDocument
{
    bool IsDeleted { get; set; }
    DateTime CreationTime { get; set; }
    string CreatorId { get; set; }
    DateTime? LastModificationTime { get; set; }
    string LastModifierId { get; set; }
}

public interface IBaseDocument
{
    string Id { get; set; }
}