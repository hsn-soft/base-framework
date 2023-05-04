using HsnSoft.Base.DependencyInjection;

namespace HsnSoft.Base.UI.Branding;

public class DefaultBrandingProvider : IBrandingProvider, ITransientDependency
{
    public virtual string AppName => "HsnSoftApplication";

    public virtual string LogoUrl => null;

    public virtual string LogoReverseUrl => null;
}