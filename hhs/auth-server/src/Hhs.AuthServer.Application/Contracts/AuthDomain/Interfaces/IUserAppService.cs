namespace Hhs.AuthServer.Application.Contracts.AuthDomain.Interfaces;

public interface IUserAppService
{
    IEnumerable<string> GetAll();
    string GetById(int id);
    void Create(string value);
    void Update(string value);
    void DeleteById(int id);
}