import React, { useEffect, useRef, useState } from 'react'
import { createChart, ColorType, CandlestickSeries, type ISeriesApi } from 'lightweight-charts'
import { useThemeStore } from '@/store/theme'
import { useTradesStore } from '@/store/trades'
import { useCandles } from '../api/trading.api'

interface PriceChartProps {
  symbol: string
}

export const SUPPORTED_TIMEFRAMES = ["1m", "5m", "15m", "1h", "4h", "1d"] as const;
export type Timeframe = typeof SUPPORTED_TIMEFRAMES[number];

export const PriceChart: React.FC<PriceChartProps> = ({ symbol }) => {
  const chartContainerRef = useRef<HTMLDivElement>(null)
  const chartRef = useRef<any>(null)
  const seriesRef = useRef<ISeriesApi<'Candlestick'> | null>(null)
  const { theme } = useThemeStore()
  const recent = useTradesStore((state) => state.recent)
  const [interval, setInterval] = useState<Timeframe>("1m");

  const { data: candles = [] } = useCandles(symbol, interval)

  // ── Effect 1: chart initialisation + historical candle data ────────────────
  useEffect(() => {
    if (!chartContainerRef.current) return

    const handleResize = () => {
      chartRef.current?.applyOptions({ width: chartContainerRef.current?.clientWidth })
    }

    const isDark = theme === 'dark'
    const bgColor = isDark ? '#0a0a0a' : '#ffffff'
    const textColor = isDark ? '#737373' : '#737373'
    const gridColor = isDark ? '#1e1e1e' : '#f0f0f0'

    const chart = createChart(chartContainerRef.current, {
      layout: {
        background: { type: ColorType.Solid, color: bgColor },
        textColor,
      },
      grid: {
        vertLines: { color: gridColor },
        horzLines: { color: gridColor },
      },
      width: chartContainerRef.current.clientWidth,
      height: 300,
      timeScale: {
        borderColor: gridColor,
        timeVisible: true,
        secondsVisible: false,
      },
    })

    const candleSeries = chart.addSeries(CandlestickSeries, {
      upColor: '#22c55e',
      downColor: '#ef4444',
      borderUpColor: '#22c55e',
      borderDownColor: '#ef4444',
      wickUpColor: '#22c55e',
      wickDownColor: '#ef4444',
    })

    chartRef.current = chart
    seriesRef.current = candleSeries

    window.addEventListener('resize', handleResize)

    return () => {
      window.removeEventListener('resize', handleResize)
      chart.remove()
      seriesRef.current = null
    }
  }, [symbol, theme])

  // ── Effect 1b: historical candle data ──────────────────────────────────────
  useEffect(() => {
    if (!seriesRef.current || candles.length === 0) return
    
    seriesRef.current.setData(
      candles.map((c) => ({
        time: c.time as any,
        open: Number(c.open),
        high: Number(c.high),
        low: Number(c.low),
        close: Number(c.close),
      }))
    )
  }, [candles, theme])

  // ── Effect 2: live trade tick updates ──────────────────────────────────────
  useEffect(() => {
    if (!seriesRef.current || candles.length === 0) return

    const lastTrade = recent.find((t) => t.symbol === symbol)
    if (!lastTrade) return

    const lastCandle = candles[candles.length - 1]
    seriesRef.current.update({
      time: lastCandle.time as any,
      open: Number(lastCandle.open),
      high: Math.max(Number(lastCandle.high), lastTrade.price),
      low: Math.min(Number(lastCandle.low), lastTrade.price),
      close: lastTrade.price,
    })
  }, [recent, symbol, candles, theme])

  // ── Derived display values ─────────────────────────────────────────────────
  const lastCandle = candles[candles.length - 1]
  const lastTrade = recent.find((t) => t.symbol === symbol)
  const displayPrice = lastTrade?.price ?? lastCandle?.close

  const hasData = candles.length > 0 || recent.some((t) => t.symbol === symbol)

  return (
    <div className="w-full h-full flex flex-col relative">
      <div className="p-2 border-b border-border flex justify-between items-center bg-card">
        <div className="flex items-center gap-4">
          <span className="text-xs font-bold text-foreground uppercase tracking-wider">{symbol} / USD</span>
          <div className="flex bg-muted p-0.5 rounded-md">
            {SUPPORTED_TIMEFRAMES.map((tf) => (
              <button
                key={tf}
                onClick={() => setInterval(tf)}
                className={`px-2 py-1 text-[10px] font-bold rounded-sm transition-all ${
                  interval === tf
                    ? 'bg-background text-foreground shadow-sm'
                    : 'text-muted-foreground hover:text-foreground'
                }`}
              >
                {tf}
              </button>
            ))}
          </div>
        </div>
        <div className="flex gap-2">
          {displayPrice !== undefined ? (
            <span className="text-[10px] text-green-500 font-mono">${displayPrice.toFixed(2)}</span>
          ) : (
            <span className="text-[10px] text-muted-foreground/50 font-mono">--</span>
          )}
        </div>
      </div>
      {!hasData && (
        <div className="absolute inset-0 z-10 flex items-center justify-center text-muted-foreground/40 text-xs font-bold uppercase tracking-widest pointer-events-none">
          Loading chart data...
        </div>
      )}
      <div ref={chartContainerRef} className="flex-1" />
    </div>
  )
}
