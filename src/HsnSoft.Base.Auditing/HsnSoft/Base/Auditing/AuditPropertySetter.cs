using HsnSoft.Base.Auditing.Contracts;
using HsnSoft.Base.DependencyInjection;
using HsnSoft.Base.Timing;
using HsnSoft.Base.Users;

namespace HsnSoft.Base.Auditing;

public class AuditPropertySetter : IAuditPropertySetter, ITransientDependency
{
    public AuditPropertySetter(ICurrentUser currentUser, IClock clock)
    {
        CurrentUser = currentUser;
        Clock = clock;
    }

    protected ICurrentUser CurrentUser { get; }
    protected IClock Clock { get; }

    public void SetCreationProperties(object targetObject)
    {
        SetCreationTime(targetObject);
        SetCreatorId(targetObject);
    }

    public void SetModificationProperties(object targetObject)
    {
        SetLastModificationTime(targetObject);
        SetLastModifierId(targetObject);
    }

    public void SetDeletionProperties(object targetObject)
    {
        SetDeletionTime(targetObject);
        SetDeleterId(targetObject);
    }

    private void SetCreationTime(object targetObject)
    {
        if (targetObject is not IHasCreationTime objectWithCreationTime)
        {
            return;
        }

        var now = Clock.Now;
        ObjectHelper.TrySetProperty(objectWithCreationTime, x => x.CreationTime, () => now);
        if (targetObject is IHasModificationTime objectWithModificationTime)
        {
            ObjectHelper.TrySetProperty(objectWithModificationTime, x => x.LastModificationTime, () => now);
        }
    }

    private void SetCreatorId(object targetObject)
    {
        if (targetObject is not IMayHaveCreator mayHaveCreatorObject)
        {
            return;
        }

        if (!CurrentUser.Id.HasValue)
        {
            ObjectHelper.TrySetProperty(mayHaveCreatorObject, x => x.CreatorId, () => null);
            return;
        }

        // if (targetObject is IMultiTenant multiTenantEntity)
        // {
        //     if (multiTenantEntity.TenantId != CurrentUser.TenantId)
        //     {
        //         ObjectHelper.TrySetProperty(mayHaveCreatorObject, x => x.CreatorId, () => null);
        //         return;
        //     }
        // }

        // if (mayHaveCreatorObject.CreatorId.HasValue && mayHaveCreatorObject.CreatorId.Value != Guid.Empty)
        // {
        //     return;
        // }

        ObjectHelper.TrySetProperty(mayHaveCreatorObject, x => x.CreatorId, () => CurrentUser.Id);
        SetLastModifierId(targetObject);
    }

    private void SetLastModificationTime(object targetObject)
    {
        if (targetObject is IHasModificationTime objectWithModificationTime)
        {
            ObjectHelper.TrySetProperty(objectWithModificationTime, x => x.LastModificationTime, () => Clock.Now);
        }
    }

    private void SetLastModifierId(object targetObject)
    {
        if (targetObject is not IModificationAuditedObject modificationAuditedObject)
        {
            return;
        }

        if (!CurrentUser.Id.HasValue)
        {
            ObjectHelper.TrySetProperty(modificationAuditedObject, x => x.LastModifierId, () => null);
            return;
        }

        // if (targetObject is IMultiTenant multiTenantEntity)
        // {
        //     if (multiTenantEntity.TenantId != CurrentUser.TenantId)
        //     {
        //         ObjectHelper.TrySetProperty(modificationAuditedObject, x => x.LastModifierId, () => null);
        //         return;
        //     }
        // }

        // if (modificationAuditedObject.LastModifierId.HasValue && modificationAuditedObject.LastModifierId.Value != Guid.Empty)
        // {
        //     return;
        // }

        ObjectHelper.TrySetProperty(modificationAuditedObject, x => x.LastModifierId, () => CurrentUser.Id);
    }

    private void SetDeletionTime(object targetObject)
    {
        if (targetObject is IHasDeletionTime objectWithDeletionTime)
        {
            if (objectWithDeletionTime.DeletionTime == null)
            {
                ObjectHelper.TrySetProperty(objectWithDeletionTime, x => x.DeletionTime, () => Clock.Now);
            }
        }
    }

    private void SetDeleterId(object targetObject)
    {
        if (targetObject is not IDeletionAuditedObject deletionAuditedObject)
        {
            return;
        }

        if (!CurrentUser.Id.HasValue)
        {
            ObjectHelper.TrySetProperty(deletionAuditedObject, x => x.DeleterId, () => null);
            return;
        }

        // if (targetObject is IMultiTenant multiTenantEntity)
        // {
        //     if (multiTenantEntity.TenantId != CurrentUser.TenantId)
        //     {
        //         ObjectHelper.TrySetProperty(deletionAuditedObject, x => x.DeleterId, () => null);
        //         return;
        //     }
        // }

        // if (deletionAuditedObject.DeleterId.HasValue && deletionAuditedObject.DeleterId.Value != Guid.Empty)
        // {
        //     return;
        // }

        ObjectHelper.TrySetProperty(deletionAuditedObject, x => x.DeleterId, () => CurrentUser.Id);
    }
}