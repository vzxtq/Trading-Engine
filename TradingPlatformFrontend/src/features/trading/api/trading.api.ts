import { useMutation, useQuery } from '@tanstack/react-query'
import { api } from '@/api/axios'
import type { ApiResponse, OrderBookResponse, OrderDto, OrderListResponseDto } from '@/types'
import { OrderListResponseDtoSchema, OrderBookResponseSchema } from '@/types/order.types' // Import OrderBookResponseSchema
import type { PlaceOrderRequest } from '../types/trading.types'
import { useOrderBookStore } from '@/store/orderBook'
import { queryClient } from '@/lib/queryClient'
import { OrderSide, OrderStatus } from '@/types/enums'

interface UserOrderQueryParams {
  page?: number
  pageSize?: number
  'Filter.Side'?: OrderSide
  'Filter.Status'?: OrderStatus
  'Filter.Search'?: string

  SortingColumn?: string
  SortingDirection?: 'Ascending' | 'Descending'
}

export const useOrderBook = (symbol: string) => {
  const setOrderBook = useOrderBookStore((state) => state.setOrderBook)

  return useQuery({
    queryKey: ['orderBook', symbol],
    queryFn: async () => {
      const response = await api.get<ApiResponse<OrderBookResponse>>(`/orders/book/${symbol}`)
      // Validate the response data with Zod schema
      const parseResult = OrderBookResponseSchema.safeParse(response.data.data)
      if (!parseResult.success) {
        console.error('OrderBook schema mismatch:', parseResult.error)
        return null
      }
      const data = parseResult.data
      if (data) {
        // Aggregate OrderDto[] into OrderBookLevel[]
        const aggregate = (orders: OrderDto[]) => {
            const levels: Record<number, number> = {}
            orders.forEach(o => {
                if (o.price == null) return
                levels[o.price] = (levels[o.price] || 0) + o.remainingQuantity
            })
            return Object.entries(levels).map(([price, quantity]) => ({
                price: parseFloat(price),
                quantity
            }))
        }
        
        setOrderBook(data.symbol, aggregate(data.buyOrders), aggregate(data.sellOrders))
      }
      return data
    },
    enabled: !!symbol,
  })
}

export const usePlaceOrder = () => {
  return useMutation({
    mutationFn: async (order: PlaceOrderRequest) => {
      const idempotencyKey = crypto.randomUUID()
      const response = await api.post<ApiResponse<{ orderId: string; status: string; message: string }>>('/orders', {
        ...order,
        side: order.side,
        type: order.type
      }, {
        headers: { 
          'Content-Type': 'application/json',
          'Idempotency-Key': idempotencyKey
        }
      })
      return response.data
    },
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['userOrders'] })
    },
  })
}

export const useCancelOrder = () => {
  return useMutation({
    mutationFn: async (orderId: string) => {
      const response = await api.post<ApiResponse<{ orderId: string; success: boolean; message: string }>>(`/orders/${orderId}/cancel`, {}, {
        headers: { 'Content-Type': 'application/json' }
      })
      return response.data
    },
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['userOrders'] })
    },
  })
}

export const useUserOrders = (queryParams: UserOrderQueryParams) => {
  return useQuery({
    queryKey: ['userOrders', queryParams],
    queryFn: async () => {
      const params = new URLSearchParams()
      for (const key in queryParams) {
        const value = queryParams[key as keyof UserOrderQueryParams]
        if (value !== undefined) {
          params.append(key, String(value))
        }
      }
      const queryString = params.toString()
      const response = await api.get<ApiResponse<OrderListResponseDto>>(`/orders/user-orders?${queryString}`)
      
      const parseResult = OrderListResponseDtoSchema.safeParse(response.data.data)
      if (!parseResult.success) {
        console.error('OrderList schema mismatch:', parseResult.error)
        return {
          orders: { items: [], totalCount: 0, page: 1, pageSize: 10, totalPages: 0, hasNextPage: false, hasPreviousPage: false },
          summary: { totalOrders: 0, openOrders: 0, filledOrders: 0, cancelledOrders: 0, totalVolume: 0, fillRate: 0 }
        }
      }
      const parsedData = parseResult.data
      return parsedData || { 
        orders: { items: [], totalCount: 0, page: 1, pageSize: 10, totalPages: 0, hasNextPage: false, hasPreviousPage: false },
        summary: { totalOrders: 0, openOrders: 0, filledOrders: 0, cancelledOrders: 0, totalVolume: 0, fillRate: 0 }
      }
    },
  })
}

export const useSymbols = () => {
  return useQuery({
    queryKey: ['symbols'],
    queryFn: async () => {
      const response = await api.get<ApiResponse<string[]>>('/symbols')
      return response.data.data || []
    },
  })
}

export interface CandleDto {
  time: number
  open: number
  high: number
  low: number
  close: number
  volume: number
}

export const useCandles = (symbol: string, interval = '1m', limit = 100) => {
  return useQuery({
    queryKey: ['candles', symbol, interval, limit],
    queryFn: async () => {
      const response = await api.get<ApiResponse<CandleDto[]>>(
        `/marketdata/candles?symbol=${symbol}&interval=${interval}&limit=${limit}`
      )
      return response.data.data || []
    },
    enabled: !!symbol,
    staleTime: 60_000,
    refetchInterval: 30_000,
    refetchOnWindowFocus: false,
  })
}

export interface OrderBookEntry {
  price: number
  quantity: number
}

export interface BinanceOrderBookDto {
  symbol: string
  bids: OrderBookEntry[]
  asks: OrderBookEntry[]
}

export const useBinanceOrderBook = (symbol: string, limit = 20) => {
  return useQuery({
    queryKey: ['binance-orderbook', symbol, limit],
    queryFn: async () => {
      const response = await api.get<ApiResponse<BinanceOrderBookDto>>(
        `/marketdata/orderbook?symbol=${symbol}&limit=${limit}`
      )
      return response.data.data || { symbol, bids: [], asks: [] }
    },
    enabled: !!symbol,
    refetchInterval: 2000,
    staleTime: 1000,
    refetchOnWindowFocus: false,
  })
}
