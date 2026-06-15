namespace WIMS.Domain.Enums;

public enum UserRole
{
    Administrator,
    WarehouseManager,
    StockKeeper,
    Viewer
}

public enum UserStatus
{
    Active,
    Inactive,
    Locked
}
public enum EntityStatus
{
    Active,
    Inactive,
}

public enum PoStatus
{
    Draft,
    Submitted,
    Approved,
    PartiallyReceived,
    FullyReceived,
    Cancelled,
    Rejected
}

public enum DispatchStatus
{
    Draft,
    Confirmed,
    Dispatched
}

public enum TransferStatus
{
    Initiated,
    InTransit,
    Received,
    Cancelled
}

public enum AdjustmentType
{
    Increase,
    Decrease
}

public enum AdjustmentReason
{
    PhysicalCountCorrection,
    DamagedGoods,
    ExpiredItems,
    TheftLoss,
    Other
}

public enum AdjustmentApprovalStatus
{
    Pending,
    Approved,
    Rejected
}

public enum ReceiptCondition
{
    Good,
    Damaged,
    Rejected
}

public enum AuditActionType
{
    Created,
    Updated,
    Deleted,
    StatusChanged,
    Login,
    Logout,
    Approved,
    Rejected
}

public enum StockMovementType
{
    GoodsReceipt,
    GoodsDispatch,
    TransferOut,
    TransferIn,
    AdjustmentIncrease,
    AdjustmentDecrease,
    OpeningStock
}
