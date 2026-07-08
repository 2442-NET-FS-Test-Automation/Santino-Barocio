namespace Library.Api.Fulfillment;

public sealed class UnkownSkuException : Exception
{
    public string Sku {get;}

    public UnkownSkuException(string sku) : base($"Unknown SKU: {sku}")
    {
        Sku = sku;
    }
}