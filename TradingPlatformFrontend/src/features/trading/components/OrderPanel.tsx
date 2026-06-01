import React, { useState } from 'react'
import { useAuthStore } from '@/store/auth'
import { useAccount, usePositions } from '@/features/auth/api/auth.api'
import { usePlaceOrder } from '../api/trading.api'
import { OrderSide, OrderSideLabels } from '@/types/enums/order-side.enum'
import { OrderType } from '@/types/enums/order-type.enum'
import { Button } from '@/components/ui/button'
import { Input } from '@/components/ui/input'
import { Label } from '@/components/ui/label'
import { OrderTypeLabels } from '@/types/enums/order-type.enum'
import { cn, formatCurrency, formatNumber, numericClass } from '@/lib/utils'
import { Currency } from '@/types/enums'

const ORDER_TYPES = [OrderType.Limit, OrderType.Market] as const

interface OrderPanelProps {
  symbol: string
}

export const OrderPanel: React.FC<OrderPanelProps> = ({ symbol }) => {
  const { userId } = useAuthStore()
  const { data: account } = useAccount(userId)
  const { data: positions } = usePositions()
  const [side, setSide] = useState<OrderSide>(OrderSide.Buy)
  const [type, setType] = useState<OrderType>(OrderType.Limit)
  const [price, setPrice] = useState('')
  const [quantity, setQuantity] = useState('')
  const placeOrder = usePlaceOrder()

  const currentPosition = positions?.find(p => p.symbol === symbol)

  const isMarket = type === OrderType.Market
  const total = isMarket ? 0 : parseFloat(price || '0') * parseFloat(quantity || '0')
  const fee = total * 0.001

  const handleQuickFill = (percent: number) => {
    if (side === OrderSide.Buy) {
        if (!account) return
        const available = account.availableBalance.amount
        if (!isMarket) {
            const p = parseFloat(price)
            if (p > 0) {
                setQuantity(((available * percent) / p).toFixed(4))
            }
        } else {
            // Simplification: Market buy with balance might need price estimation
            setQuantity('0')
        }
    } else {
        const availableQuantity = currentPosition?.quantity || 0
        setQuantity((availableQuantity * percent).toFixed(4))
    }
  }

  const handleSubmit = (e: React.FormEvent) => {
    e.preventDefault()
    placeOrder.mutate({
      symbol,
      price: isMarket ? null : parseFloat(price),
      quantity: parseFloat(quantity),
      side,
      type
    }, {
        onSuccess: () => {
            setQuantity('')
        }
    })
  }

  return (
    <div className="flex-1 flex flex-col p-4 space-y-4">
      <div className="flex bg-muted rounded-md p-1">
        <button
          className={`flex-1 py-1.5 text-sm font-bold rounded ${side === OrderSide.Buy ? 'bg-green-600 text-white' : 'text-muted-foreground hover:text-foreground'}`}
          onClick={() => setSide(OrderSide.Buy)}
        >
          {OrderSideLabels[OrderSide.Buy]}
        </button>
        <button
          className={`flex-1 py-1.5 text-sm font-bold rounded ${side === OrderSide.Sell ? 'bg-red-600 text-white' : 'text-muted-foreground hover:text-foreground'}`}
          onClick={() => setSide(OrderSide.Sell)}
        >
          {OrderSideLabels[OrderSide.Sell]}
        </button>
      </div>

      <div className="space-y-4">
        <div className="flex justify-between text-xs">
          <span className="text-muted-foreground font-semibold">
            {side === OrderSide.Buy ? 'Available balance' : 'Available position'}
          </span>
          <span className={numericClass}>
            {side === OrderSide.Buy && account
              ? formatCurrency(
                  account.availableBalance.amount,
                  account.availableBalance.currency as Currency
                )
              : `${formatNumber(currentPosition?.quantity || 0)} ${symbol.split('/')[0]}`}
          </span>
        </div>

        <div className="flex bg-muted rounded-md p-1">
          {ORDER_TYPES.map((orderType) => (
            <button
              key={orderType}
              type="button"
              onClick={() => setType(orderType)}
              className={`flex-1 py-1.5 text-xs font-semibold rounded transition-all outline-none ${
                type === orderType
                  ? 'bg-background text-foreground shadow-sm'
                  : 'text-muted-foreground hover:text-foreground'
              }`}
            >
              {OrderTypeLabels[orderType]}
            </button>
          ))}
        </div>

        {!isMarket && (
        <div className="space-y-2">
          <Label htmlFor="price" className="text-xs text-muted-foreground font-semibold">Price</Label>
          <div className="relative">
            <Input
              id="price"
              type="number"
              step="0.01"
              value={price}
              onChange={(e) => setPrice(e.target.value)}
              className={cn('bg-background border-border h-9', numericClass)}
            />
            <span className="absolute right-3 top-2 text-xs text-muted-foreground font-semibold">USD</span>
          </div>
        </div>
        )}

        <div className="space-y-2">
          <Label htmlFor="quantity" className="text-xs text-muted-foreground font-semibold">Quantity</Label>
          <div className="relative">
            <Input
              id="quantity"
              type="number"
              step="0.0001"
              value={quantity}
              onChange={(e) => setQuantity(e.target.value)}
              className={cn('bg-background border-border h-9', numericClass)}
            />
            <span className="absolute right-3 top-2 text-xs text-muted-foreground font-semibold">{symbol}</span>
          </div>
        </div>

        <div className="grid grid-cols-4 gap-1">
          {[0.25, 0.5, 0.75, 1].map((p) => (
            <button
              key={p}
              onClick={() => handleQuickFill(p)}
              className="bg-muted hover:bg-accent text-xs py-1 rounded border border-border text-muted-foreground transition-colors"
            >
              {p * 100}%
            </button>
          ))}
        </div>

        {!isMarket && (
        <div className="pt-4 border-t border-border space-y-1.5 text-xs">
          <div className="flex justify-between">
            <span className="text-muted-foreground font-semibold">Total</span>
            <span className={numericClass}>{formatNumber(total)} USD</span>
          </div>
          <div className="flex justify-between">
            <span className="text-muted-foreground font-semibold">Fee (0.1%)</span>
            <span className={numericClass}>{formatNumber(fee)} USD</span>
          </div>
        </div>
        )}

        <Button
          onClick={handleSubmit}
          className={`w-full font-bold text-sm h-10 ${side === OrderSide.Buy ? 'bg-green-600 hover:bg-green-700' : 'bg-red-600 hover:bg-red-700'} text-white border-0`}
          disabled={placeOrder.isPending || !quantity || (!isMarket && !price)}
        >
          {placeOrder.isPending ? 'Placing...' : `${OrderSideLabels[side]} ${symbol}`}
        </Button>
      </div>
    </div>
  )
}
