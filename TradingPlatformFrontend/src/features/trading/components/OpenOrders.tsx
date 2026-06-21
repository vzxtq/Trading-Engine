import React from 'react'
import { cn, dangerActionClass, formatNumber, numericClass } from '@/lib/utils'
import { useUserOrders, useCancelOrder } from '../api/trading.api'
import { OrderSide, OrderSideLabels } from '@/types/enums/order-side.enum'
import { OrderStatus } from '@/types/enums' // Corrected import path for OrderStatus

interface OpenOrdersProps {
}

export const OpenOrders: React.FC<OpenOrdersProps> = () => {
  const { data: responseData } = useUserOrders({
    page: 1,
    pageSize: 10,
    'Filter.Status': OrderStatus.Open,
  })
  const orders = responseData?.orders?.items || []
  const cancelOrder = useCancelOrder()
  const [cancellingIds, setCancellingIds] = React.useState<Set<string>>(new Set())

  const openOrders = orders

  return (
    <div className="flex-1 flex flex-col text-sm font-sans h-full">
      <div className="p-3 border-b border-border bg-muted flex text-muted-foreground font-semibold text-xs">
        <span className="w-[10%]">Side</span>
        <span className="w-[15%]">Symbol</span>
        <span className="w-[20%] text-right">Price</span>
        <span className="w-[20%] text-right">Filled</span>
        <span className="w-[20%] text-right">Total</span>
        <span className="w-[15%] text-right">Action</span>
      </div>

      <div className="flex-1 overflow-y-auto no-scrollbar">
        {openOrders.length === 0 ? (
          <div className="flex items-center justify-center h-full text-muted-foreground/50 text-xs font-medium">
            No open orders
          </div>
        ) : (
          openOrders.map((order) => (
            <div key={order.id} className="flex justify-between items-center px-3 py-2 border-b border-border/50 hover:bg-accent/50 transition-colors">
              <span className={`w-[10%] font-bold ${order.side === OrderSide.Buy ? 'text-green-600 dark:text-green-500' : 'text-red-600 dark:text-red-500'}`}>
                {OrderSideLabels[order.side]}
              </span>
              <span className="w-[15%] font-medium text-foreground">{order.symbolName}</span>
              <span className={cn('w-[20%] text-right', numericClass)}>{formatNumber(order.price.amount)}</span>
              <span className={cn('w-[20%] text-right', numericClass)}>
                {formatNumber(order.filledQuantity ?? 0)}
              </span>
              <span className={cn('w-[20%] text-right', numericClass)}>
                {formatNumber(order.price.amount * order.quantity)}
              </span>
              <div className="w-[15%] text-right">
                <button
                  onClick={() => {
                    setCancellingIds(prev => new Set(prev).add(order.id))
                    cancelOrder.mutate(order.id, {
                      onSettled: () => {
                        setCancellingIds(prev => {
                          const next = new Set(prev)
                          next.delete(order.id)
                          return next
                        })
                      }
                    })
                  }}
                  disabled={cancellingIds.has(order.id)}
                  className={cn(dangerActionClass, 'font-semibold text-xs transition-colors disabled:opacity-50')}
                >
                  {cancellingIds.has(order.id) ? '...' : 'Cancel'}
                </button>
              </div>
            </div>
          ))
        )}
      </div>
    </div>
  )
}
