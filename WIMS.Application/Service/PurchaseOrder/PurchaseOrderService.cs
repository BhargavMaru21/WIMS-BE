using AutoMapper;
using WIMS.Application.DTOs;
using WIMS.Application.DTOs.PurchaseOrder;
using WIMS.Application.Interfaces.Common;
using WIMS.Application.Interfaces.Repositories;
using WIMS.Application.Interfaces.Services.PurchaseOrder;
using WIMS.Domain.Entity;
using WIMS.Domain.Enums;


namespace WIMS.Application.Service;

public class PurchaseOrderService(
    IPurchaseOrderRepository purchaseOrderRepository,
    IUserRepository userRepository,
    IWarehouseRepository warehouseRepository,
    IInputNormalizer inputNormalizer,
    IMapper mapper,
    ICodeGeneratorService codeGeneratorService
) : IPurchaseOrderService
{
    public async Task<ApiResponse<PurchaseOrderResponse>> CreatePurchaseOrder(PurchaseOrderCreateRequest request, int createdByUserId, int warehouseId)
    {
        request = inputNormalizer.NormalizeObject(request);
        Console.WriteLine(warehouseId);

        bool isWarehouseExists = await warehouseRepository.ExistsAsync(x => x.Id == warehouseId && x.Status == EntityStatus.Active);

        if(!isWarehouseExists)
            return ApiResponse<PurchaseOrderResponse>.Failure("Warehouse not found or Inactive");
    

        await purchaseOrderRepository.BeginTransactionAsync();
        try
        {
            var poEntity = mapper.Map<PurchaseOrder>(request);
            poEntity.CreatedBy = createdByUserId;
            poEntity.WarehouseId = warehouseId;
            poEntity.OrderDate = DateOnly.FromDateTime(DateTime.UtcNow);

            var createdPurchaseOrder = await purchaseOrderRepository.CreateAsync(poEntity);

            string generatedCode = codeGeneratorService.GenerateCode("purchaseorder",createdPurchaseOrder.Id);
            createdPurchaseOrder.PoNumber = generatedCode;

            await purchaseOrderRepository.SaveChangesAsync();

            var response = mapper.Map<PurchaseOrderResponse>(createdPurchaseOrder);
            await purchaseOrderRepository.CommitTransactionAsync();
            return ApiResponse<PurchaseOrderResponse>.Success(response,"Purchase Order Created Successfully",statusCode:201);
        }
        catch (Exception)
        {
            await purchaseOrderRepository.RollbackTransactionAsync();
            return ApiResponse<PurchaseOrderResponse>.Failure("error occurred while creating the purchase order.",statusCode:500);
        }
    }

}
