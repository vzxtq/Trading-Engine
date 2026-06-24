import { create } from 'zustand'
import type { TradeDto } from '@/types'

type TradesState = {
  recent: TradeDto[]
  addTrade: (trade: TradeDto) => void
  clear: () => void
}

export const useTradesStore = create<TradesState>(set => ({
  recent: [],
  addTrade: (trade) => set(state => {
    if (state.recent.some(existing => existing.tradeId === trade.tradeId)) {
      return state
    }

    return { recent: [trade, ...state.recent].slice(0, 50) }
  }),
  clear: () => set({ recent: [] }),
}))