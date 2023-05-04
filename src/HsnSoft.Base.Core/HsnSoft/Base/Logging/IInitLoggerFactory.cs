namespace HsnSoft.Base.Logging;

public interface IInitLoggerFactory
{
    IInitLogger<T> Create<T>();
}