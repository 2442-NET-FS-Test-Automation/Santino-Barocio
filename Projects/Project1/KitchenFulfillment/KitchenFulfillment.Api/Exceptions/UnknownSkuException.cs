namespace KitchenFulfillment.Api;

public class UnknownSkuException : Exception
{
    public string Sku { get; }

    public UnknownSkuException(string sku) : base($"El platillo con SKU '{sku}' no existe.")
    {
        Sku = sku;
    }
}