import { Currency } from '@/types/enums/currency.enum'

export const HUB_PATHS = {
  market: '/hubs/market',
  orders: '/hubs/orders',
} as const

export const CURRENCY_LABELS: Record<Currency, string> = {
  [Currency.USD]: 'USD',
  [Currency.EUR]: 'EUR',
  [Currency.GBP]: 'GBP',
  [Currency.JPY]: 'JPY',
};

export const ORDER_COMMISSION_RATE = 0.001;
export const ORDER_COMMISSION_PERCENT_LABEL = `${ORDER_COMMISSION_RATE * 100}%`;
