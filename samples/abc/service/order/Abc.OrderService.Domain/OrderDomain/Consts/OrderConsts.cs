namespace Abc.OrderService.Domain.OrderDomain.Consts;

public static class OrderConsts
{
    public const string TableName = "orders";
    public const string LineTableName = "order_lines";

    public const int CustomerUserNameMaxLength = 64;
    public const int NoteMaxLength = 512;
    public const int ProductNameMaxLength = 128;
    public const int AuthorMaxLength = 64;
}
