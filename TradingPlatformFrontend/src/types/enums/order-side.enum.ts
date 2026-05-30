export enum OrderSide {
    Buy = "Buy",
    Sell = "Sell"
}

export const OrderSideLabels: Record<OrderSide, string> = {
    [OrderSide.Buy]: "Buy",
    [OrderSide.Sell]: "Sell"
};
