export enum OrderStatus { 
    Open = "Open",
    PartiallyFilled = "PartiallyFilled",
    Filled = "Filled", 
    Cancelled = "Cancelled", 
    Rejected = "Rejected"
}

export const OrderStatusLabels: Record<OrderStatus, string> = {
    [OrderStatus.Open]: "Open",
    [OrderStatus.PartiallyFilled]: "Partially Filled",
    [OrderStatus.Filled]: "Filled",
    [OrderStatus.Cancelled]: "Cancelled",
    [OrderStatus.Rejected]: "Rejected"
};
