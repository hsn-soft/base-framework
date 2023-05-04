using System.Threading.Tasks;

namespace HsnSoft.Base.UI.Navigation
{
    public interface IMenuContributor
    {
        Task ConfigureMenuAsync(MenuConfigurationContext context);
    }
}