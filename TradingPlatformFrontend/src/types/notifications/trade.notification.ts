import { z } from 'zod'
import { OrderSide } from '@/types/enums/order-side.enum'

export const TradeNotificationSchema = z.object({
  tradeId: z.uuid(),
  symbol: z.string().min(1),
  price: z.number(),
  quantity: z.number(),
  aggressorSide: z.enum(OrderSide),
  executedAt: z.number(),
})

export type TradeNotification = z.infer<typeof TradeNotificationSchema>