import React, { useMemo } from 'react'
import { cn, formatNumber, numericClass } from '@/lib/utils'
import { useBinanceOrderBook } from '../api/trading.api'

interface OrderBookProps {
  symbol: string
}

const ROWS = 10

// Skeleton row for loading state
const SkeletonRow: React.FC<{ side: 'bid' | 'ask' }> = ({ side }) => (
  <div className="h-5 flex justify-between items-center px-2 gap-2">
    <div className={`h-2 rounded w-20 animate-pulse ${side === 'ask' ? 'bg-red-500/20' : 'bg-green-500/20'}`} />
    <div className="h-2 rounded w-14 bg-muted/40 animate-pulse" />
  </div>
)

export const OrderBook: React.FC<OrderBookProps> = ({ symbol }) => {
  const { data, isLoading } = useBinanceOrderBook(symbol)

  const bids = useMemo(
    () => [...(data?.bids ?? [])].sort((a, b) => b.price - a.price).slice(0, ROWS),
    [data]
  )
  const asks = useMemo(
    () => [...(data?.asks ?? [])].sort((a, b) => a.price - b.price).slice(0, ROWS),
    [data]
  )

  const bestBid = bids[0]
  const bestAsk = asks[0]

  const spread = bestBid && bestAsk ? bestAsk.price - bestBid.price : null
  const spreadPct = spread && bestAsk ? (spread / bestAsk.price) * 100 : null

  const maxQty = useMemo(
    () => Math.max(...[...bids, ...asks].map((e) => e.quantity), 1),
    [bids, asks]
  )

  return (
    <div className="flex flex-col text-xs select-none w-full shrink-0">
      {/* Header */}
      <div className="p-2 border-b border-border flex justify-between items-center bg-card shrink-0">
        <span className="text-sm font-semibold text-foreground">Order Book</span>
        <span className="text-xs px-1.5 py-0.5 rounded bg-muted text-muted-foreground font-bold tracking-wider">
          {symbol}
        </span>
      </div>

      {/* Column labels */}
      <div className="px-2 py-1 flex justify-between text-xs text-muted-foreground font-semibold border-b border-border shrink-0">
        <span>Price</span>
        <span>Size</span>
      </div>

      <div className="flex flex-col">
        {/* Asks — lowest ask nearest spread */}
        <div className="flex flex-col">
          {isLoading
            ? Array.from({ length: ROWS }).map((_, i) => <SkeletonRow key={i} side="ask" />)
            : asks.length === 0
            ? (
              <div className="py-4 flex items-center justify-center text-xs font-semibold text-red-500/30">
                No data
              </div>
            )
            : [...asks].reverse().map((entry, i) => {
                const isTop = i === asks.length - 1
                return (
                  <div
                    key={`ask-${i}`}
                    className="relative h-5 flex justify-between items-center px-2 hover:bg-accent transition-colors cursor-pointer"
                  >
                    <div
                      className="absolute right-0 top-0 bottom-0 bg-red-500/10 transition-all duration-300"
                      style={{ width: `${(entry.quantity / maxQty) * 100}%` }}
                    />
                    <span
                      className={cn(
                        'relative z-10 font-normal tabular-nums',
                        isTop ? 'text-red-400' : 'text-red-500/80'
                      )}
                    >
                      {formatNumber(entry.price)}
                    </span>
                    <span className={cn('relative z-10', numericClass)}>{formatNumber(entry.quantity)}</span>
                  </div>
                )
              })}
        </div>

        {/* Spread */}
        <div className="h-7 border-y border-border flex items-center justify-center gap-2 bg-muted/50 shrink-0">
          {spread !== null ? (
            <>
              <span className="text-muted-foreground font-semibold text-xs">Spread</span>
              <span className={numericClass}>{formatNumber(spread)}</span>
              <span className="text-muted-foreground text-xs font-normal tabular-nums">
                ({formatNumber(spreadPct!, 3)}%)
              </span>
            </>
          ) : (
            <span className="text-muted-foreground/40 text-xs">—</span>
          )}
        </div>

        {/* Bids — highest bid at top */}
        <div className="flex flex-col">
          {isLoading
            ? Array.from({ length: ROWS }).map((_, i) => <SkeletonRow key={i} side="bid" />)
            : bids.length === 0
            ? (
              <div className="py-4 flex items-center justify-center text-xs font-semibold text-green-500/30">
                No data
              </div>
            )
            : bids.map((entry, i) => {
                const isTop = i === 0
                return (
                  <div
                    key={`bid-${i}`}
                    className="relative h-5 flex justify-between items-center px-2 hover:bg-accent transition-colors cursor-pointer"
                  >
                    <div
                      className="absolute right-0 top-0 bottom-0 bg-green-500/10 transition-all duration-300"
                      style={{ width: `${(entry.quantity / maxQty) * 100}%` }}
                    />
                    <span
                      className={cn(
                        'relative z-10 font-normal tabular-nums',
                        isTop ? 'text-green-400' : 'text-green-500/80'
                      )}
                    >
                      {formatNumber(entry.price)}
                    </span>
                    <span className={cn('relative z-10', numericClass)}>{formatNumber(entry.quantity)}</span>
                  </div>
                )
              })}
        </div>
      </div>
    </div>
  )
}
