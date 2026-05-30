import React, { useState } from 'react'
import { useAuthStore } from '@/store/auth'
import { useAccount, usePositions } from '@/features/auth/api/auth.api'
import { usePlaceOrder } from '../api/trading.api'
import { OrderSide } from '@/types/enums/order-side.enum'
import { Button } from '@/components/ui/button'
import { Input } from '@/components/ui/input'
import { Label } from '@/components/ui/label'

interface OrderPanelProps {
  symbol: string
}

export const OrderPanel: React.FC<OrderPanelProps> = ({ symbol }) => {
  const { userId } = useAuthStore()
  const { data: account } = useAccount(userId)
  const { data: positions } = usePositions()
  const [side, setSide] = useState<OrderSide>(OrderSide.Buy)
  const [price, setPrice] = useState('')
  const [quantity, setQuantity] = useState('')
  const placeOrder = usePlaceOrder()

  const currentPosition = positions?.find(p => p.symbol === symbol)

  const total = parseFloat(price || '0') * parseFloat(quantity || '0')
  const fee = total * 0.001

  const handleQuickFill = (percent: number) => {
    if (side === OrderSide.Buy) {
        if (!account) return
        const available = account.availableBalance.amount
        const p = parseFloat(price)
        if (p > 0) {
            setQuantity(((available * percent) / p).toFixed(4))
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
      price: parseFloat(price),
      quantity: parseFloat(quantity),
      side,
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
          className={`flex-1 py-1.5 text-xs font-bold rounded ${side === OrderSide.Buy ? 'bg-green-600 text-white' : 'text-muted-foreground hover:text-foreground'}`}
          onClick={() => setSide(OrderSide.Buy)}
        >
          BUY
        </button>
        <button
          className={`flex-1 py-1.5 text-xs font-bold rounded ${side === OrderSide.Sell ? 'bg-red-600 text-white' : 'text-muted-foreground hover:text-foreground'}`}
          onClick={() => setSide(OrderSide.Sell)}
        >
          SELL
        </button>
      </div>

      <div className="space-y-4">
        <div className="flex justify-between text-[11px]">
          <span className="text-muted-foreground uppercase font-bold">
            {side === OrderSide.Buy ? 'Available Balance' : 'Available Position'}
          </span>
          <span className="text-foreground font-mono">
            {side === OrderSide.Buy 
              ? `${account?.availableBalance?.amount.toLocaleString()} ${account?.availableBalance?.currency === 0 ? 'USD' : 'EUR'}`
              : `${currentPosition?.quantity || 0} ${symbol.split('/')[0]}`
            }
          </span>
        </div>

        <div className="space-y-2">
          <Label htmlFor="price" className="text-[10px] uppercase text-muted-foreground font-bold">Price</Label>
          <div className="relative">
            <Input
              id="price"
              type="number"
              step="0.01"
              value={price}
              onChange={(e) => setPrice(e.target.value)}
              className="bg-background border-border text-foreground font-mono h-9"
            />
            <span className="absolute right-3 top-2 text-[10px] text-muted-foreground font-bold uppercase">USD</span>
          </div>
        </div>

        <div className="space-y-2">
          <Label htmlFor="quantity" className="text-[10px] uppercase text-muted-foreground font-bold">Quantity</Label>
          <div className="relative">
            <Input
              id="quantity"
              type="number"
              step="0.0001"
              value={quantity}
              onChange={(e) => setQuantity(e.target.value)}
              className="bg-background border-border text-foreground font-mono h-9"
            />
            <span className="absolute right-3 top-2 text-[10px] text-muted-foreground font-bold uppercase">{symbol}</span>
          </div>
        </div>

        <div className="grid grid-cols-4 gap-1">
          {[0.25, 0.5, 0.75, 1].map((p) => (
            <button
              key={p}
              onClick={() => handleQuickFill(p)}
              className="bg-muted hover:bg-accent text-[10px] py-1 rounded border border-border text-muted-foreground transition-colors"
            >
              {p * 100}%
            </button>
          ))}
        </div>

        <div className="pt-4 border-t border-border space-y-1.5 text-[11px]">
          <div className="flex justify-between">
            <span className="text-muted-foreground uppercase font-bold">Total</span>
            <span className="text-foreground font-mono">{total.toFixed(2)} USD</span>
          </div>
          <div className="flex justify-between">
            <span className="text-muted-foreground uppercase font-bold">Fee (0.1%)</span>
            <span className="text-foreground font-mono">{fee.toFixed(2)} USD</span>
          </div>
        </div>

        <Button
          onClick={handleSubmit}
          className={`w-full font-bold uppercase tracking-widest h-10 ${side === OrderSide.Buy ? 'bg-green-600 hover:bg-green-700' : 'bg-red-600 hover:bg-red-700'} text-white border-0`}
          disabled={placeOrder.isPending || !price || !quantity}
        >
          {placeOrder.isPending ? 'Placing...' : `${side === OrderSide.Buy ? 'BUY' : 'SELL'} ${symbol}`}
        </Button>
      </div>
    </div>
  )
}
