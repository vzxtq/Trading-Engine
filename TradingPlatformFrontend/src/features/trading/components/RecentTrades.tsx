import React from 'react'
import { cn, formatNumber, numericClass } from '@/lib/utils'
import { useTradesStore } from '@/store/trades'
import { OrderSide } from '@/types/enums/order-side.enum'

interface RecentTradesProps {
  symbol: string
}

export const RecentTrades: React.FC<RecentTradesProps> = ({ symbol }) => {
  const recent = useTradesStore((state) => state.recent)
  
  const filteredTrades = recent.filter(t => t.symbol === symbol)

  const formatTime = (ts: number) => {
    const date = new Date(ts)
    return date.toLocaleTimeString('en-GB', { hour12: false })
  }

  return (
    <div className="flex-1 flex flex-col text-xs overflow-hidden">
      <div className="p-2 border-b border-border bg-muted flex justify-between text-muted-foreground font-semibold text-xs">
        <span className="w-1/3 text-left">Price</span>
        <span className="w-1/3 text-right">Size</span>
        <span className="w-1/3 text-right">Time</span>
      </div>

      <div className="flex-1 overflow-y-auto no-scrollbar">
        {filteredTrades.map((trade, i) => (
          <div key={`${trade.tradeId}-${i}`} className="flex justify-between items-center px-2 h-6 hover:bg-accent transition-colors">
            <span
              className={cn(
                'w-1/3 text-left font-normal tabular-nums',
                trade.side === OrderSide.Buy ? 'text-green-500' : 'text-red-500'
              )}
            >
              {formatNumber(trade.price)}
            </span>
            <span className={cn('w-1/3 text-right', numericClass)}>{formatNumber(trade.quantity)}</span>
            <span className="w-1/3 text-right text-muted-foreground">
              {formatTime(trade.executedAt)}
            </span>
          </div>
        ))}
      </div>
    </div>
  )
}
