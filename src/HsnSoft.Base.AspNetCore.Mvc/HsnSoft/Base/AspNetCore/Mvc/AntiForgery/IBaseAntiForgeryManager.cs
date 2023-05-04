namespace HsnSoft.Base.AspNetCore.Mvc.AntiForgery;

public interface IBaseAntiForgeryManager
{
    void SetCookie();

    string GenerateToken();
}