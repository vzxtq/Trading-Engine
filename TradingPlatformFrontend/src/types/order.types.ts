import { z } from 'zod'
import { OrderSide } from './enums/order-side.enum'
import { OrderStatus } from './enums/order-status.enum'
import { OrderType } from './enums/order-type.enum'

export const OrderBookLevelSchema = z.object({
  price: z.number(),
  quantity: z.number(),
})

export type OrderBookLevel = z.infer<typeof OrderBookLevelSchema>


export const OrderBookStateSchema = z.object({
  symbol: z.string().nullable(),
  bids: z.array(OrderBookLevelSchema),
  asks: z.array(OrderBookLevelSchema),
})

export type OrderBookState = z.infer<typeof OrderBookStateSchema>

export const PriceSchema = z.object({
  amount: z.number(),
  currency: z.string(),
})

export type Price = z.infer<typeof PriceSchema>

export const OrderDtoSchema = z.object({
  id: z.string().uuid(),
  userId: z.string().uuid(),
  symbolName: z.string(),
  price: z.number().nullable(),
  quantity: z.number(),
  remainingQuantity: z.number(),
  filledQuantity: z.number().optional(),
  side: z.nativeEnum(OrderSide),
  status: z.nativeEnum(OrderStatus),
  createdAt: z.number(),
  updatedAt: z.number().nullable(),
})

export type OrderDto = z.infer<typeof OrderDtoSchema>

export const OrderBookResponseSchema = z.object({
  symbol: z.string(),
  buyOrders: z.array(OrderDtoSchema),
  sellOrders: z.array(OrderDtoSchema),
})

export type OrderBookResponse = z.infer<typeof OrderBookResponseSchema>

export const MoneyDtoSchema = z.object({
  amount: z.number(),
  currency: z.string(),
})

export type MoneyDto = z.infer<typeof MoneyDtoSchema>

export const OrderListDtoSchema = z.object({
  id: z.string().uuid(),
  userId: z.string().uuid(),
  symbolName: z.string(),
  currency: z.string(),
  side: z.nativeEnum(OrderSide),
  type: z.nativeEnum(OrderType),
  price: MoneyDtoSchema,
  quantity: z.number(),
  filledQuantity: z.number(),
  remainingQuantity: z.number(),
  status: z.nativeEnum(OrderStatus),
  createdAt: z.number(),
  updatedAt: z.number().nullable(),
})

export type OrderListDto = z.infer<typeof OrderListDtoSchema>

export const PagedResultSchema = z.object({
  items: z.array(OrderListDtoSchema),
  totalCount: z.number(),
  page: z.number(),
  pageSize: z.number(),
  totalPages: z.number(),
  hasNextPage: z.boolean(),
  hasPreviousPage: z.boolean(),
})

export type PagedResult<T> = z.infer<typeof PagedResultSchema> & {
  items: T[];
};

export const OrderSummaryDtoSchema = z.object({
  totalOrders: z.number(),
  openOrders: z.number(),
  filledOrders: z.number(),
  cancelledOrders: z.number(),
  totalVolume: z.number(),
  fillRate: z.number(),
})

export type OrderSummaryDto = z.infer<typeof OrderSummaryDtoSchema>

export const SortingOptionsSchema = z.object({
  column: z.string(),
  direction: z.string(),
})

export type SortingOptions = z.infer<typeof SortingOptionsSchema>

export const OrderListResponseDtoSchema = z.object({
  orders: PagedResultSchema,
  summary: OrderSummaryDtoSchema,
  sorting: SortingOptionsSchema.nullable().optional(),
})

export type OrderListResponseDto = z.infer<typeof OrderListResponseDtoSchema>


