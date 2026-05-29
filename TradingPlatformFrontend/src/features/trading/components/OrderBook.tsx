import React, { useMemo } from 'react'
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
    <div className="flex-1 flex flex-col text-[11px] font-mono select-none overflow-hidden">
      {/* Header */}
      <div className="p-2 border-b border-border flex justify-between items-center bg-card shrink-0">
        <span className="text-xs font-bold text-foreground uppercase tracking-wider">Order Book</span>
        <span className="text-[10px] px-1.5 py-0.5 rounded bg-muted text-muted-foreground font-bold tracking-wider">
          {symbol}
        </span>
      </div>

      {/* Column labels */}
      <div className="px-2 py-1 flex justify-between text-[10px] text-muted-foreground font-bold uppercase tracking-wider border-b border-border shrink-0">
        <span>Price</span>
        <span>Size</span>
      </div>

      <div className="flex-1 overflow-hidden flex flex-col">
        {/* Asks — ascending (lowest ask at bottom, nearest mid-price) */}
        <div className="flex-1 flex flex-col justify-end overflow-hidden">
          {isLoading
            ? Array.from({ length: ROWS }).map((_, i) => <SkeletonRow key={i} side="ask" />)
            : asks.length === 0
            ? (
              <div className="flex-1 flex items-center justify-center text-[10px] font-bold uppercase tracking-widest text-red-500/30">
                No data
              </div>
            )
            : [...asks].reverse().map((entry, i) => {
                const isTop = i === asks.length - 1
                return (
                  <div
                    key={`ask-${i}`}
                    className={`relative h-5 flex justify-between items-center px-2 hover:bg-accent transition-colors cursor-pointer ${isTop ? 'font-bold' : ''}`}
                  >
                    <div
                      className="absolute right-0 top-0 bottom-0 bg-red-500/10 transition-all duration-300"
                      style={{ width: `${(entry.quantity / maxQty) * 100}%` }}
                    />
                    <span className={`relative z-10 ${isTop ? 'text-red-400' : 'text-red-500/80'}`}>
                      {entry.price.toFixed(2)}
                    </span>
                    <span className="text-foreground/70 relative z-10">{entry.quantity.toFixed(4)}</span>
                  </div>
                )
              })}
        </div>

        {/* Spread */}
        <div className="h-7 border-y border-border flex items-center justify-center gap-2 bg-muted/50 shrink-0">
          {spread !== null ? (
            <>
              <span className="text-muted-foreground font-bold uppercase tracking-wider text-[9px]">Spread</span>
              <span className="text-foreground">{spread.toFixed(2)}</span>
              <span className="text-muted-foreground text-[9px]">({spreadPct!.toFixed(3)}%)</span>
            </>
          ) : (
            <span className="text-muted-foreground/40 text-[9px] uppercase tracking-widest">—</span>
          )}
        </div>

        {/* Bids — descending (highest bid at top) */}
        <div className="flex-1 flex flex-col overflow-hidden">
          {isLoading
            ? Array.from({ length: ROWS }).map((_, i) => <SkeletonRow key={i} side="bid" />)
            : bids.length === 0
            ? (
              <div className="flex-1 flex items-center justify-center text-[10px] font-bold uppercase tracking-widest text-green-500/30">
                No data
              </div>
            )
            : bids.map((entry, i) => {
                const isTop = i === 0
                return (
                  <div
                    key={`bid-${i}`}
                    className={`relative h-5 flex justify-between items-center px-2 hover:bg-accent transition-colors cursor-pointer ${isTop ? 'font-bold' : ''}`}
                  >
                    <div
                      className="absolute right-0 top-0 bottom-0 bg-green-500/10 transition-all duration-300"
                      style={{ width: `${(entry.quantity / maxQty) * 100}%` }}
                    />
                    <span className={`relative z-10 ${isTop ? 'text-green-400' : 'text-green-500/80'}`}>
                      {entry.price.toFixed(2)}
                    </span>
                    <span className="text-foreground/70 relative z-10">{entry.quantity.toFixed(4)}</span>
                  </div>
                )
              })}
        </div>
      </div>
    </div>
  )
}
