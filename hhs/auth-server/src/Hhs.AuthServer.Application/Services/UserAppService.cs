using Hhs.AuthServer.Application.Contracts.AuthDomain.Interfaces;

namespace Hhs.AuthServer.Application.Services;

public class UserAppService : ApplicationServiceBase, IUserAppService
{
    public UserAppService(IServiceProvider provider) : base(provider)
    {
    }

    public IEnumerable<string> GetAll()
    {
        return new string[] { "user-01", "user-02" };
    }

    public string GetById(int id)
    {
        return $"value{id}";
    }

    public void Create(string value)
    {
    }

    public void Update(string value)
    {
    }

    public void DeleteById(int id)
    {
    }
}