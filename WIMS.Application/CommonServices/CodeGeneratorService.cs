using WIMS.Application.Interfaces.Common;

namespace WIMS.Application.CommonServices;

public class CodeGeneratorService:ICodeGeneratorService
{
     public string GenerateCode(string entityName, int number)
    {
        var prefix = entityName.ToLower() switch
        {
            "warehouse" => "WH",
            "zone" => "Z",
            "bin" => "BIN",
            "product" => "SKU",
            "purchaseorder" => "PO",
            "goodsreceipt" => "GRN",
            "goodsdispatch" => "GDN",
            "stocktransfer" => "TRF",
            "stockadjustment" => "ADJ",

            _ => throw new ArgumentException("Invalid entity name")
        };

        return $"{prefix}-{number:D3}";
    }
}
