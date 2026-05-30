export enum OrderType { Market = 'Market', Limit = 'Limit' }

export const OrderTypeLabels: Record<OrderType, string> = {
    [OrderType.Market]: "Market",
    [OrderType.Limit]: "Limit"
};
