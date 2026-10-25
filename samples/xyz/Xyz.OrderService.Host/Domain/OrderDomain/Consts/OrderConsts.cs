namespace Xyz.OrderService.Domain.OrderDomain.Consts;

public static class OrderConsts
{
    public const string TableName = "Order";
    public const string LineTableName = "OrderLine";

    public const int CustomerUserNameMaxLength = 64;
    public const int NoteMaxLength = 512;
    public const int ShippingAddressMaxLength = 256;
    public const int ProductNameMaxLength = 128;
}
