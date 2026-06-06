import { useState, useMemo } from 'react'
import { useDebounce } from '@/hooks/useDebounce'
import { useUserOrders, useCancelOrder } from '@/features/trading/api/trading.api'
import { OrderSide } from '@/types/enums/order-side.enum'
import { OrderStatus, OrderStatusLabels } from '@/types/enums/order-status.enum'
import { Badge } from '@/components/ui/badge'
import { Button } from '@/components/ui/button'
import { Input } from '@/components/ui/input'
import {
  Select,
  SelectContent,
  SelectItem,
  SelectTrigger,
  SelectValue,
} from '@/components/ui/select'
import { 
  Search, 
  FileDown, 
  ChevronLeft, 
  ChevronRight
} from 'lucide-react'
import { cn, dangerActionClass, formatNumber, formatUsd, numericClass } from '@/lib/utils'

const SIDE_FILTER_ITEMS = {
  All: 'All sides',
  Buy: 'Buy',
  Sell: 'Sell',
} as const

const SORT_BY_ITEMS = {
  createdAt: 'Sort by Date',
  symbol: 'Sort by Symbol',
  price: 'Sort by Price',
} as const

const SORT_ORDER_ITEMS = {
  desc: 'Newest First',
  asc: 'Oldest First',
} as const

export const UserOrders = () => {
  // Local state for pagination, filtering, and sorting
  const [currentPage, setCurrentPage] = useState(1)
  const pageSize = 10 // Fixed page size for now
  const [searchQuery, setSearchQuery] = useState('')
  const [statusFilter, setStatusFilter] = useState<'All' | 'Open' | 'Filled' | 'Cancelled' | 'Rejected' | 'PartiallyFilled'>('All')
  const [sideFilter, setSideFilter] = useState<'All' | 'Buy' | 'Sell'>('All')
  const [sortBy, setSortBy] = useState<'createdAt' | 'symbol' | 'price'>('createdAt')
  const [sortOrder, setSortOrder] = useState<'asc' | 'desc'>('desc')

  const debouncedSearch = useDebounce(searchQuery, 400)

  const queryParams = useMemo(() => {
    const params: Parameters<typeof useUserOrders>[0] = {
      page: currentPage,
      pageSize: pageSize,
      SortingColumn: sortBy,
      SortingDirection: sortOrder === 'desc' ? 'Descending' : 'Ascending',
    }

    if (debouncedSearch) {
      params['Filter.Search'] = debouncedSearch
    }

    if (statusFilter !== 'All') {
      // Map frontend status filter to backend enum value
      let backendStatus: OrderStatus | undefined;
      switch (statusFilter) {
        case 'Open': backendStatus = OrderStatus.Open; break;
        case 'Filled': backendStatus = OrderStatus.Filled; break;
        case 'Cancelled': backendStatus = OrderStatus.Cancelled; break;
        case 'Rejected': backendStatus = OrderStatus.Rejected; break;
        case 'PartiallyFilled': backendStatus = OrderStatus.PartiallyFilled; break;
      }
      if (backendStatus !== undefined) {
        params['Filter.Status'] = backendStatus
      }
    }

    if (sideFilter !== 'All') {
      let backendSide: OrderSide | undefined;
      switch (sideFilter) {
        case 'Buy': backendSide = OrderSide.Buy; break;
        case 'Sell': backendSide = OrderSide.Sell; break;
      }
      if (backendSide !== undefined) {
        params['Filter.Side'] = backendSide
      }
    }

    return params
  }, [currentPage, pageSize, statusFilter, sideFilter, sortBy, sortOrder, debouncedSearch])

  const { data: responseData, isLoading } = useUserOrders(queryParams)
  const pagedOrders = responseData?.orders
  const orders = pagedOrders?.items || []
  const cancelOrder = useCancelOrder()
  const [cancellingIds, setCancellingIds] = useState<Set<string>>(new Set())
  
  // Stats calculation
  const stats = useMemo(() => {
    // We now use the exact summary provided by the backend!
    const summary = responseData?.summary
    
    const total = summary?.totalOrders || 0
    const open = summary?.openOrders || 0
    const filled = summary?.filledOrders || 0
    const cancelled = summary?.cancelledOrders || 0
    const volume = summary?.totalVolume || 0
    
    const fillRate = summary?.fillRate ?? (total > 0 ? (filled / total) * 100 : 0)
    const cancelledRate = total > 0 ? (cancelled / total) * 100 : 0

    return { total, open, filled, cancelled, volume, fillRate, cancelledRate }
  }, [responseData])

  const getStatusColor = (status: OrderStatus) => {
    switch (status) {
      case OrderStatus.Open: return 'bg-amber-500/10 text-amber-500 border-amber-500/20'
      case OrderStatus.Filled: return 'bg-green-500/10 text-green-500 border-green-500/20'
      case OrderStatus.PartiallyFilled: return 'bg-blue-500/10 text-blue-500 border-blue-500/20'
      case OrderStatus.Cancelled: return 'bg-muted text-muted-foreground border-border'
      case OrderStatus.Rejected: return 'bg-red-500/10 text-red-500 border-red-500/20 dark:text-red-400 dark:border-red-500/30'
      default: return ''
    }
  }

  return (
    <div className="space-y-8 animate-in fade-in duration-500">
      <div className="flex items-center justify-between">
        <h1 className="text-3xl font-bold text-foreground">My orders</h1>
        <div className="flex items-center gap-3">
          <Button variant="outline" size="sm" className="h-9 gap-2 text-sm font-semibold border-border bg-background">
            <FileDown size={14} />
            Export CSV
          </Button>
        </div>
      </div>

      {/* Stats Cards */}
      <div className="grid grid-cols-5 gap-4">
        <div className="bg-card p-5 rounded-xl border border-border">
          <span className="text-xs font-semibold text-muted-foreground block mb-3">Total Orders</span>
          <div className="flex flex-col">
            <span className="text-2xl font-bold text-foreground">{stats.total}</span>
            <span className="text-xs text-muted-foreground font-medium mt-1">All time</span>
          </div>
        </div>
        <div className="bg-card p-5 rounded-xl border border-border">
          <span className="text-xs font-semibold text-muted-foreground block mb-3">Open Orders</span>
          <div className="flex flex-col">
            <span className="text-2xl font-bold text-amber-500">{stats.open}</span>
            <span className="text-xs text-muted-foreground font-medium mt-1">Pending execution</span>
          </div>
        </div>
        <div className="bg-card p-5 rounded-xl border border-border">
          <span className="text-xs font-semibold text-muted-foreground block mb-3">Filled</span>
          <div className="flex flex-col">
            <span className="text-2xl font-bold text-green-500">{stats.filled}</span>
            <span className="text-xs text-muted-foreground font-medium mt-1">{stats.fillRate.toFixed(1)}% fill rate</span>
          </div>
        </div>
        <div className="bg-card p-5 rounded-xl border border-border">
          <span className="text-xs font-semibold text-muted-foreground block mb-3">Cancelled</span>
          <div className="flex flex-col">
            <span className="text-2xl font-bold text-foreground">{stats.cancelled}</span>
            <span className="text-xs text-muted-foreground font-medium mt-1">{stats.cancelledRate.toFixed(1)}% of total</span>
          </div>
        </div>
        <div className="bg-card p-5 rounded-xl border border-border">
          <span className="text-xs font-semibold text-muted-foreground block mb-3">Total Volume</span>
          <div className="flex flex-col">
            <span className={cn(numericClass, 'text-2xl')}>{formatUsd(stats.volume)}</span>
            <span className="text-xs text-muted-foreground font-medium mt-1">USD traded</span>
          </div>
        </div>
      </div>

      {/* Filter Bar */}
      <div className="flex flex-wrap items-center gap-4">
        <div className="relative flex-1 max-w-[300px]">
          <Search className="absolute left-3 top-1/2 -translate-y-1/2 text-muted-foreground" size={16} />
          <Input 
            placeholder="Search by symbol" 
            className="pl-10 h-10 bg-card border-border text-sm placeholder:text-muted-foreground/50"
            value={searchQuery}
            onChange={(e) => setSearchQuery(e.target.value)}
          />
        </div>

        <div className="flex bg-muted p-1 rounded-lg">
          {(['All', 'Open', 'Filled', 'Cancelled', 'Rejected', 'PartiallyFilled'] as const).map((tab) => (
            <button
              key={tab}
              onClick={() => setStatusFilter(tab)}
              className={cn(
                "px-4 py-1.5 text-xs font-bold rounded-md transition-all",
                statusFilter === tab 
                  ? "bg-background text-foreground shadow-sm" 
                  : "text-muted-foreground hover:text-foreground"
              )}
            >
              {tab === 'All' ? 'All' : OrderStatusLabels[tab as OrderStatus]}
            </button>
          ))}
        </div>

        <div className="flex shrink-0 gap-2 ml-auto">
          <Select
            items={SIDE_FILTER_ITEMS}
            value={sideFilter}
            onValueChange={(value) => value && setSideFilter(value as typeof sideFilter)}
          >
            <SelectTrigger className="h-10 w-[120px] bg-card border-border text-xs font-bold text-foreground focus:ring-0 focus:ring-offset-0">
              <SelectValue />
            </SelectTrigger>
            <SelectContent align="start" alignItemWithTrigger={false} className="bg-popover border-border text-popover-foreground">
              <SelectItem value="All" label="All sides">All sides</SelectItem>
              <SelectItem value="Buy" label="Buy">Buy</SelectItem>
              <SelectItem value="Sell" label="Sell">Sell</SelectItem>
            </SelectContent>
          </Select>

          <Select
            items={SORT_BY_ITEMS}
            value={sortBy}
            onValueChange={(value) => value && setSortBy(value as typeof sortBy)}
          >
            <SelectTrigger className="h-10 w-[148px] bg-card border-border text-xs font-bold text-foreground focus:ring-0 focus:ring-offset-0">
              <SelectValue />
            </SelectTrigger>
            <SelectContent align="start" alignItemWithTrigger={false} className="bg-popover border-border text-popover-foreground">
              <SelectItem value="createdAt" label="Sort by Date">Sort by Date</SelectItem>
              <SelectItem value="symbol" label="Sort by Symbol">Sort by Symbol</SelectItem>
              <SelectItem value="price" label="Sort by Price">Sort by Price</SelectItem>
            </SelectContent>
          </Select>

          <Select
            items={SORT_ORDER_ITEMS}
            value={sortOrder}
            onValueChange={(value) => value && setSortOrder(value as typeof sortOrder)}
          >
            <SelectTrigger className="h-10 w-[132px] bg-card border-border text-xs font-bold text-foreground focus:ring-0 focus:ring-offset-0">
              <SelectValue />
            </SelectTrigger>
            <SelectContent align="start" alignItemWithTrigger={false} className="bg-popover border-border text-popover-foreground">
              <SelectItem value="desc" label="Newest First">Newest First</SelectItem>
              <SelectItem value="asc" label="Oldest First">Oldest First</SelectItem>
            </SelectContent>
          </Select>
        </div>
      </div>

      {/* Table */}
      <div className="bg-card rounded-xl border border-border overflow-hidden">
        <table className="w-full table-fixed border-collapse">
          <colgroup>
            <col span={9} style={{ width: `${100 / 9}%` }} />
          </colgroup>
          <thead>
            <tr className="border-b border-border bg-muted/30">
              <th className="px-4 py-4 text-center text-sm font-semibold text-muted-foreground">Symbol</th>
              <th className="px-4 py-4 text-center text-sm font-semibold text-muted-foreground">Side</th>
              <th className="px-4 py-4 text-center text-sm font-semibold text-muted-foreground">Type</th>
              <th className="px-4 py-4 text-center text-sm font-semibold text-muted-foreground">Price</th>
              <th className="px-4 py-4 text-center text-sm font-semibold text-muted-foreground">Quantity</th>
              <th className="px-4 py-4 text-center text-sm font-semibold text-muted-foreground">Filled</th>
              <th className="px-4 py-4 text-center text-sm font-semibold text-muted-foreground">Total</th>
              <th className="px-4 py-4 text-center text-sm font-semibold text-muted-foreground">Status</th>
              <th className="px-4 py-4" />
            </tr>
          </thead>
          <tbody className="divide-y divide-border/50">
            {isLoading ? (
              <tr>
                <td colSpan={9} className="px-4 py-12 text-center text-muted-foreground animate-pulse">Loading orders...</td>
              </tr>
            ) : orders.length === 0 ? (
              <tr>
                <td colSpan={9} className="px-4 py-12 text-center text-muted-foreground">No orders found</td>
              </tr>
            ) : (
              orders.map((order) => {
                const totalAmount = order.price.amount * order.quantity

                return (
                  <tr key={order.id} className="hover:bg-muted/20 transition-colors group">
                    <td className="px-4 py-4 whitespace-nowrap text-center">
                      <div className="flex items-center justify-center gap-2">
                        <div className="w-6 h-6 shrink-0 rounded-full bg-muted flex items-center justify-center text-xs font-bold text-muted-foreground">
                          {order.symbolName[0]}
                        </div>
                        <span className="text-sm font-bold text-foreground">{order.symbolName}</span>
                      </div>
                    </td>
                    <td className="px-4 py-4 whitespace-nowrap text-center">
                      <span className={cn(
                        "text-xs font-semibold",
                        order.side === OrderSide.Buy ? "text-green-500" : "text-red-500"
                      )}>
                        {order.side === OrderSide.Buy ? 'Buy' : 'Sell'}
                      </span>
                    </td>
                    <td className="px-4 py-4 whitespace-nowrap text-center">
                      <span className="text-xs font-semibold text-muted-foreground">Limit</span>
                    </td>
                    <td className="px-4 py-4 whitespace-nowrap text-center">
                      <span className={cn(numericClass, 'text-sm')}>{formatUsd(order.price.amount)}</span>
                    </td>
                    <td className="px-4 py-4 whitespace-nowrap text-center">
                      <span className={cn(numericClass, 'text-sm')}>{formatNumber(order.quantity)}</span>
                    </td>
                    <td className="px-4 py-4 whitespace-nowrap text-center">
                      <span className={cn(numericClass, 'text-sm')}>
                        {formatNumber(order.filledQuantity ?? 0)}
                      </span>
                    </td>
                    <td className="px-4 py-4 whitespace-nowrap text-center">
                      <span className={cn(numericClass, 'text-sm')}>{formatUsd(totalAmount)}</span>
                    </td>
                    <td className="px-4 py-4 whitespace-nowrap text-center">
                      <Badge variant="outline" className={cn("text-[10px] font-bold px-2 py-0 h-5 border", getStatusColor(order.status))}>
                        {OrderStatusLabels[order.status]}
                      </Badge>
                    </td>
                    <td className="px-4 py-4 whitespace-nowrap text-center">
                      {(order.status === OrderStatus.Open || order.status === OrderStatus.PartiallyFilled) && (
                        <Button
                          variant="ghost"
                          size="sm"
                          className={cn('h-8 px-3 text-xs font-semibold', dangerActionClass)}
                          onClick={() => {
                            setCancellingIds(prev => new Set(prev).add(order.id))
                            cancelOrder.mutate(order.id, {
                              onSettled: () => {
                                setCancellingIds(prev => {
                                  const next = new Set(prev)
                                  next.delete(order.id)
                                  return next
                                })
                              }
                            })
                          }}
                          disabled={cancellingIds.has(order.id)}
                        >
                          {cancellingIds.has(order.id) ? '...' : 'Cancel'}
                        </Button>
                      )}
                    </td>
                  </tr>
                )
              })
            )}
          </tbody>
        </table>
      </div>

      {/* Pagination */}
      {pagedOrders && pagedOrders.totalPages > 1 && (
        <div className="flex items-center justify-between mt-6">
          <span className="text-xs font-medium text-muted-foreground">
            Showing {orders.length} of {pagedOrders.totalCount} orders
          </span>
          <div className="flex items-center gap-1">
            <Button 
              variant="outline" 
              size="icon" 
              className="h-8 w-8 border-border bg-background text-muted-foreground hover:text-foreground"
              onClick={() => setCurrentPage(prev => Math.max(1, prev - 1))}
              disabled={!pagedOrders.hasPreviousPage}
            >
              <ChevronLeft size={16} />
            </Button>
            <div className="flex items-center gap-1 mx-2">
              {Array.from({ length: pagedOrders.totalPages }, (_, i) => i + 1).map(page => (
                <Button 
                  key={page}
                  variant={page === currentPage ? "default" : "ghost"} 
                  size="sm" 
                  className={cn(
                    "h-8 w-8 text-xs p-0",
                    page === currentPage ? "bg-green-500/10 text-green-500 border-green-500/50" : "text-muted-foreground font-bold"
                  )}
                  onClick={() => setCurrentPage(page)}
                >
                  {page}
                </Button>
              ))}
            </div>
            <Button 
              variant="outline" 
              size="icon" 
              className="h-8 w-8 border-border bg-background text-muted-foreground hover:text-foreground"
              onClick={() => setCurrentPage(prev => Math.min(pagedOrders.totalPages, prev + 1))}
              disabled={!pagedOrders.hasNextPage}
            >
              <ChevronRight size={16} />
            </Button>
          </div>
        </div>
      )}
    </div>
  )
}