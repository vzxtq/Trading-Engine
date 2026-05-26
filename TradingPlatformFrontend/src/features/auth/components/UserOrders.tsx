import { useState, useMemo } from 'react'
import { useUserOrders, useCancelOrder } from '@/features/trading/api/trading.api'
import { OrderSide } from '@/types/enums/order-side.enum'
import { OrderStatus } from '@/types/enums/order-status.enum'
import { Badge } from '@/components/ui/badge'
import { Button } from '@/components/ui/button'
import { Input } from '@/components/ui/input'
import { 
  Search, 
  SlidersHorizontal, 
  FileDown, 
  ChevronLeft, 
  ChevronRight
} from 'lucide-react'
import { cn, formatAmount } from '@/lib/utils'

export const UserOrders = () => {
  // Local state for pagination, filtering, and sorting
  const [currentPage, setCurrentPage] = useState(1)
  const pageSize = 10 // Fixed page size for now
  const [searchQuery, setSearchQuery] = useState('')
  const [statusFilter, setStatusFilter] = useState<'All' | 'Open' | 'Filled' | 'Cancelled' | 'Rejected' | 'PartiallyFilled'>('All')
  const [sideFilter, setSideFilter] = useState<'All' | 'Buy' | 'Sell'>('All')
  const [sortBy, setSortBy] = useState('createdAt') // Default sort by creation date
  const [sortOrder, setSortOrder] = useState<'asc' | 'desc'>('desc') // Default sort order

  const queryParams = useMemo(() => {
    const params: Parameters<typeof useUserOrders>[0] = {
      page: currentPage,
      pageSize: pageSize,
      'SortBy': sortBy,
      'SortOrder': sortOrder,
    }

    if (searchQuery) {
      // Assuming backend can search by symbol or ID via a single filter param
      params['Filter.Symbol'] = searchQuery 
      // If backend had a generic search, we'd use that, e.g., params['Filter.Search'] = searchQuery
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
  }, [currentPage, pageSize, searchQuery, statusFilter, sideFilter, sortBy, sortOrder])

  const { data: responseData, isLoading } = useUserOrders(queryParams)
  const pagedOrders = responseData?.orders
  const orders = pagedOrders?.items || []
  const cancelOrder = useCancelOrder()
  
  // Stats calculation
  const stats = useMemo(() => {
    // We now use the exact summary provided by the backend!
    const summary = responseData?.summary
    
    const total = summary?.totalOrders || 0
    const open = summary?.openOrders || 0
    const filled = summary?.filledOrders || 0
    const cancelled = summary?.cancelledOrders || 0
    const volume = summary?.totalVolume || 0
    
    const fillRate = summary?.fillRate ? summary.fillRate * 100 : (total > 0 ? (filled / total) * 100 : 0)
    const cancelledRate = total > 0 ? (cancelled / total) * 100 : 0

    return { total, open, filled, cancelled, volume, fillRate, cancelledRate }
  }, [responseData])

  // Filtering is now handled by the backend, so filteredOrders becomes just orders
  const filteredOrders = orders

  const getStatusColor = (status: OrderStatus) => {
    switch (status) {
      case OrderStatus.Open: return 'bg-amber-500/10 text-amber-500 border-amber-500/20'
      case OrderStatus.Filled: return 'bg-green-500/10 text-green-500 border-green-500/20'
      case OrderStatus.PartiallyFilled: return 'bg-blue-500/10 text-blue-500 border-blue-500/20'
      case OrderStatus.Cancelled: return 'bg-muted text-muted-foreground border-border'
      case OrderStatus.Rejected: return 'bg-destructive/10 text-destructive border-destructive/20'
      default: return ''
    }
  }

  const formatOrderDate = (ts: number) => {
    return new Date(ts).toLocaleString('en-US', { 
      month: 'short', 
      day: 'numeric', 
      hour: '2-digit', 
      minute: '2-digit',
      hour12: false 
    })
  }

  return (
    <div className="space-y-8 animate-in fade-in duration-500">
      <div className="flex items-center justify-between">
        <h1 className="text-3xl font-bold text-foreground">My orders</h1>
        <div className="flex items-center gap-3">
          <Button variant="outline" size="sm" className="h-9 gap-2 text-xs font-bold uppercase tracking-wider border-border bg-background">
            <SlidersHorizontal size={14} />
            Filters
          </Button>
          <Button variant="outline" size="sm" className="h-9 gap-2 text-xs font-bold uppercase tracking-wider border-border bg-background">
            <FileDown size={14} />
            Export CSV
          </Button>
        </div>
      </div>

      {/* Stats Cards */}
      <div className="grid grid-cols-5 gap-4">
        <div className="bg-card p-5 rounded-xl border border-border">
          <span className="text-[10px] font-bold uppercase tracking-widest text-muted-foreground block mb-3">Total Orders</span>
          <div className="flex flex-col">
            <span className="text-2xl font-bold text-foreground">{stats.total}</span>
            <span className="text-[10px] text-muted-foreground font-medium mt-1">All time</span>
          </div>
        </div>
        <div className="bg-card p-5 rounded-xl border border-border">
          <span className="text-[10px] font-bold uppercase tracking-widest text-muted-foreground block mb-3">Open Orders</span>
          <div className="flex flex-col">
            <span className="text-2xl font-bold text-amber-500">{stats.open}</span>
            <span className="text-[10px] text-muted-foreground font-medium mt-1">Pending execution</span>
          </div>
        </div>
        <div className="bg-card p-5 rounded-xl border border-border">
          <span className="text-[10px] font-bold uppercase tracking-widest text-muted-foreground block mb-3">Filled</span>
          <div className="flex flex-col">
            <span className="text-2xl font-bold text-green-500">{stats.filled}</span>
            <span className="text-[10px] text-muted-foreground font-medium mt-1">{stats.fillRate.toFixed(1)}% fill rate</span>
          </div>
        </div>
        <div className="bg-card p-5 rounded-xl border border-border">
          <span className="text-[10px] font-bold uppercase tracking-widest text-muted-foreground block mb-3">Cancelled</span>
          <div className="flex flex-col">
            <span className="text-2xl font-bold text-foreground">{stats.cancelled}</span>
            <span className="text-[10px] text-muted-foreground font-medium mt-1">{stats.cancelledRate.toFixed(1)}% of total</span>
          </div>
        </div>
        <div className="bg-card p-5 rounded-xl border border-border">
          <span className="text-[10px] font-bold uppercase tracking-widest text-muted-foreground block mb-3">Total Volume</span>
          <div className="flex flex-col">
            <span className="text-2xl font-bold text-foreground">${formatAmount(stats.volume)}</span>
            <span className="text-[10px] text-muted-foreground font-medium mt-1">USD traded</span>
          </div>
        </div>
      </div>

      {/* Filter Bar */}
      <div className="flex items-center gap-4">
        <div className="relative flex-1 max-w-[300px]">
          <Search className="absolute left-3 top-1/2 -translate-y-1/2 text-muted-foreground" size={16} />
          <Input 
            placeholder="Search by symbol or order ID" 
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
              {tab}
            </button>
          ))}
        </div>

        <div className="flex gap-2 ml-auto">
          <select 
            className="bg-card border border-border rounded-lg px-3 py-2 text-xs font-bold text-foreground outline-none cursor-pointer h-10"
            value={sideFilter}
            onChange={(e) => setSideFilter(e.target.value as 'All' | 'Buy' | 'Sell')}
          >
            <option value="All">All sides</option>
            <option value="Buy">Buy</option>
            <option value="Sell">Sell</option>
          </select>
          {/* <select className="bg-card border border-border rounded-lg px-3 py-2 text-xs font-bold text-foreground outline-none cursor-pointer h-10">
            <option>All symbols</option>
          </select> */}
          <select 
            className="bg-card border border-border rounded-lg px-3 py-2 text-xs font-bold text-foreground outline-none cursor-pointer h-10"
            value={sortBy}
            onChange={(e) => setSortBy(e.target.value)}
          >
            <option value="createdAt">Sort by Date</option>
            <option value="symbol">Sort by Symbol</option>
            <option value="price">Sort by Price</option>
            {/* Add more sorting options as needed */}
          </select>
          <select 
            className="bg-card border border-border rounded-lg px-3 py-2 text-xs font-bold text-foreground outline-none cursor-pointer h-10"
            value={sortOrder}
            onChange={(e) => setSortOrder(e.target.value as 'asc' | 'desc')}
          >
            <option value="desc">Newest First</option>
            <option value="asc">Oldest First</option>
          </select>
        </div>
      </div>

      {/* Table */}
      <div className="bg-card rounded-xl border border-border overflow-hidden">
        <table className="w-full text-left border-collapse">
          <thead>
            <tr className="border-b border-border bg-muted/30">
              <th className="px-6 py-4 text-[10px] font-bold uppercase tracking-wider text-muted-foreground">Order ID</th>
              <th className="px-6 py-4 text-[10px] font-bold uppercase tracking-wider text-muted-foreground">Symbol</th>
              <th className="px-6 py-4 text-[10px] font-bold uppercase tracking-wider text-muted-foreground">Side</th>
              <th className="px-6 py-4 text-[10px] font-bold uppercase tracking-wider text-muted-foreground">Type</th>
              <th className="px-6 py-4 text-[10px] font-bold uppercase tracking-wider text-muted-foreground">Price</th>
              <th className="px-6 py-4 text-[10px] font-bold uppercase tracking-wider text-muted-foreground text-right">Quantity</th>
              <th className="px-6 py-4 text-[10px] font-bold uppercase tracking-wider text-muted-foreground">Filled</th>
              <th className="px-6 py-4 text-[10px] font-bold uppercase tracking-wider text-muted-foreground text-right">Total</th>
              <th className="px-6 py-4 text-[10px] font-bold uppercase tracking-wider text-muted-foreground">Status</th>
              <th className="px-6 py-4 text-[10px] font-bold uppercase tracking-wider text-muted-foreground">Date</th>
              <th className="px-6 py-4 text-[10px] font-bold uppercase tracking-wider text-muted-foreground text-right">Actions</th>
            </tr>
          </thead>
          <tbody className="divide-y divide-border/50">
            {isLoading ? (
              <tr>
                <td colSpan={11} className="px-6 py-12 text-center text-muted-foreground animate-pulse">Loading orders...</td>
              </tr>
            ) : filteredOrders.length === 0 ? (
              <tr>
                <td colSpan={11} className="px-6 py-12 text-center text-muted-foreground">No orders found</td>
              </tr>
            ) : (
              filteredOrders.map((order) => {
                const totalAmount = order.price * order.quantity

                return (
                  <tr key={order.id} className="hover:bg-muted/20 transition-colors group">
                    <td className="px-6 py-4 whitespace-nowrap">
                      <div className="flex flex-col">
                        <span className="text-xs font-bold text-foreground">#ORD-{order.id.slice(0, 5).toUpperCase()}</span>
                        <span className="text-[10px] text-muted-foreground font-medium">Limit</span>
                      </div>
                    </td>
                    <td className="px-6 py-4 whitespace-nowrap">
                      <div className="flex items-center gap-2">
                        <div className="w-6 h-6 rounded-full bg-muted flex items-center justify-center text-[10px] font-bold text-muted-foreground">
                          {order.symbolName[0]}
                        </div>
                        <div className="flex flex-col">
                          <span className="text-xs font-bold text-foreground">{order.symbolName}</span>
                          <span className="text-[10px] text-muted-foreground font-medium">Limit</span>
                        </div>
                      </div>
                    </td>
                    <td className="px-6 py-4 whitespace-nowrap">
                      <span className={cn(
                        "text-[10px] font-bold uppercase tracking-wider",
                        order.side === OrderSide.Buy ? "text-green-500" : "text-red-500"
                      )}>
                        {order.side === OrderSide.Buy ? 'Buy' : 'Sell'}
                      </span>
                    </td>
                    <td className="px-6 py-4 whitespace-nowrap">
                      <span className="text-[10px] font-bold text-muted-foreground uppercase">Limit</span>
                    </td>
                    <td className="px-6 py-4 whitespace-nowrap">
                      <span className="text-xs font-bold text-foreground font-mono">${formatAmount(order.price)}</span>
                    </td>
                    <td className="px-6 py-4 whitespace-nowrap text-right">
                      <span className="text-xs font-bold text-foreground font-mono">{order.quantity.toFixed(4)}</span>
                    </td>
                    <td className="px-6 py-4 whitespace-nowrap min-w-[120px]">
                      <span className="text-xs font-bold text-foreground font-mono">
                        {(order.filledQuantity ?? 0).toFixed(2)} // TODO
                      </span>
                    </td>
                    <td className="px-6 py-4 whitespace-nowrap text-right font-mono">
                      <span className="text-xs font-bold text-foreground">${formatAmount(totalAmount)}</span>
                    </td>
                    <td className="px-6 py-4 whitespace-nowrap">
                      <Badge variant="outline" className={cn("text-[9px] font-bold px-2 py-0 h-5 border", getStatusColor(order.status))}>
                        {order.status.toUpperCase()}
                      </Badge>
                    </td>
                    <td className="px-6 py-4 whitespace-nowrap">
                      <span className="text-xs font-medium text-muted-foreground whitespace-nowrap">{formatOrderDate(order.createdAt)}</span>
                    </td>
                    <td className="px-6 py-4 whitespace-nowrap text-right">
                      <div className="flex items-center justify-end gap-2">
                        <Button variant="ghost" size="sm" className="h-8 px-3 text-[10px] font-bold uppercase tracking-wider text-muted-foreground hover:text-foreground hover:bg-muted">
                          View
                        </Button>
                        {(order.status === OrderStatus.Open || order.status === OrderStatus.PartiallyFilled) && (
                          <Button
                            variant="ghost"
                            size="sm"
                            className="h-8 px-3 text-[10px] font-bold uppercase tracking-wider text-destructive hover:text-destructive hover:bg-destructive/10"
                            onClick={() => cancelOrder.mutate(order.id)}
                            disabled={cancelOrder.isPending}
                          >                            Cancel
                          </Button>
                        )}
                      </div>
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
          <span className="text-[11px] font-medium text-muted-foreground">
            Showing {filteredOrders.length} of {pagedOrders.totalCount} orders
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
