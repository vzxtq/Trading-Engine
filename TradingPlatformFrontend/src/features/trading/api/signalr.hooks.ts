import { useEffect } from 'react'
import { MarketDataConnection, OrderConnection } from '@/services/signalr'
import { useTradesStore } from '@/store/trades'
import { useOrderBookStore } from '@/store/orderBook'
import type { OrderBookNotification, OrderStatusNotification } from '@/types/notifications'
import { TradeNotificationSchema } from '@/types/notifications'
import { queryClient } from '@/lib/queryClient'
import { HubConnectionState } from '@microsoft/signalr'
import { useAuthStore } from '@/store/auth'

export function useMarketDataSignalR(symbol: string) {
  const addTrade = useTradesStore((state) => state.addTrade)
  const updateOrderBook = useOrderBookStore((state) => state.updateOrderBook)

  useEffect(() => {
    const market = new MarketDataConnection()

    const handleTrade = (notification: unknown) => {
      const parseResult = TradeNotificationSchema.safeParse(notification)
      if (!parseResult.success) {
        console.error('Trade notification schema mismatch:', parseResult.error)
        return
      }

      const trade = parseResult.data
      addTrade({
        tradeId: trade.tradeId,
        symbol: trade.symbol,
        price: trade.price,
        quantity: trade.quantity,
        side: trade.aggressorSide,
        executedAt: trade.executedAt,
      })
    }

    const handleOrderBook = (notification: OrderBookNotification) => {
      if (notification.symbol === symbol) {
        updateOrderBook(notification.stateChanges)
      }
    }

    market.on('TradeExecuted', handleTrade)
    market.on('OrderBookUpdated', handleOrderBook)

    const startConnection = async () => {
      try {
        await market.connect()
        await market.joinSymbol(symbol)
      } catch (err) {
        console.error('MarketData SignalR Error:', err)
      }
    }

    startConnection()

    return () => {
      market.off('TradeExecuted', handleTrade)
      market.off('OrderBookUpdated', handleOrderBook)
      if (market.connectionState === HubConnectionState.Connected) { // Use connectionState getter
        market.leaveSymbol(symbol).catch(console.error)
      }
      market.disconnect().catch(console.error)
    }
  }, [symbol, addTrade, updateOrderBook])
}

export function useOrdersSignalR() {
  const userId = useAuthStore((state) => state.userId)
  const hasHydrated = useAuthStore.persist.hasHydrated() // Corrected way to access hasHydrated

  useEffect(() => {
    const orders = new OrderConnection()

    const handleOrderStatus = (_notification: OrderStatusNotification) => {
      queryClient.invalidateQueries({ queryKey: ['userOrders'] })
      queryClient.invalidateQueries({ queryKey: ['account'] })
    }

    orders.on('OrderStatusChanged', handleOrderStatus)

    const startConnection = async () => {
      try {
        await orders.connect()
      } catch (err) {
        console.error('Orders SignalR Error:', err)
      }
    }

    if (userId && hasHydrated) { // Only connect if userId is available AND store has hydrated
      startConnection()
    }

    return () => {
      orders.off('OrderStatusChanged', handleOrderStatus)
      orders.disconnect().catch(console.error)
    }
  }, [userId, hasHydrated]) // Add hasHydrated to dependency array
}