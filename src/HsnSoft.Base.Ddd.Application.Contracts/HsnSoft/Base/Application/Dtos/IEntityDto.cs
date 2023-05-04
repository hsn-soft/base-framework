namespace HsnSoft.Base.Application.Dtos;

public interface IEntityDto
{

}

public interface IEntityDto<TKey> : IEntityDto
{
    TKey Id { get; set; }
}
