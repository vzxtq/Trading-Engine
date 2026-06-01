import React from 'react'
import { useNavigate } from 'react-router-dom'
import { Menu, User, Settings, LogOut, Sun, Moon } from 'lucide-react'
import { useAuthStore } from '@/store/auth'
import { useThemeStore } from '@/store/theme'
import { useAccount } from '@/features/auth/api/auth.api'
import { useSymbols } from '../api/trading.api'
import { Select, SelectContent, SelectItem, SelectTrigger, SelectValue } from '@/components/ui/select'
import { 
  DropdownMenu, 
  DropdownMenuContent, 
  DropdownMenuItem, 
  DropdownMenuSeparator, 
  DropdownMenuTrigger 
} from '@/components/ui/dropdown-menu'
import { cn, formatCurrency, numericClass } from '@/lib/utils'

interface TradingHeaderProps {
  userId: string | null
  symbol: string
  setSymbol: (symbol: string) => void
}

export const TradingHeader: React.FC<TradingHeaderProps> = ({ userId, symbol, setSymbol }) => {
  const { clearAuth } = useAuthStore()
  const { theme, toggleTheme } = useThemeStore()
  const { data: account } = useAccount(userId)
  const { data: symbols = [] } = useSymbols()
  const navigate = useNavigate()

  const handleLogout = () => {
    clearAuth()
    navigate('/login')
  }

  return (
    <header className="h-12 border-b border-border grid grid-cols-[1fr_auto_1fr] items-center px-4 bg-background shrink-0 z-50">
      {/* Left: Logo */}
      <div className="flex items-center">
        <span className="text-xl font-bold text-foreground tracking-tighter uppercase">Trading Engine</span>
      </div>

      {/* Middle: Symbol Selector */}
      <div className="flex justify-center">
        <Select value={symbol} onValueChange={(val) => val && setSymbol(val)}>
          <SelectTrigger className="w-[120px] h-8 bg-transparent border-border text-foreground focus:ring-0 focus:ring-offset-0">
            <SelectValue placeholder="Symbol" />
          </SelectTrigger>
          <SelectContent className="bg-popover border-border text-popover-foreground">
            {symbols.map((s) => (
              <SelectItem key={s} value={s}>{s}</SelectItem>
            ))}
          </SelectContent>
        </Select>
      </div>

      {/* Right: Balance & Menu */}
      <div className="flex items-center justify-end gap-0">
        <div className="flex items-center">
          {/* AVAILABLE Section */}
          <div className="flex flex-col items-end px-3">
            <span className="text-xs text-muted-foreground font-semibold leading-none mb-1">Available</span>
            <span className={cn(numericClass, 'text-sm text-green-500 leading-none')}>
              {account ? formatCurrency(account.availableBalance.amount, account.availableBalance.currency) : '0.00'}
            </span>
          </div>

          {/* Vertical Divider */}
          {account && account.reservedBalance.amount > 0 && (
            <div className="w-[0.5px] h-6 bg-border" />
          )}

          {/* RESERVED Section */}
          {account && account.reservedBalance.amount > 0 && (
            <div className="flex flex-col items-end px-3">
              <span className="text-xs text-muted-foreground font-semibold leading-none mb-1">Reserved</span>
              <span className={cn(numericClass, 'text-sm text-amber-500 leading-none')}>
                {formatCurrency(account.reservedBalance.amount, account.reservedBalance.currency)}
              </span>
            </div>
          )}
        </div>

        {/* Hamburger Menu */}
        <DropdownMenu>
          <DropdownMenuTrigger className="p-2 text-muted-foreground hover:text-foreground transition-colors outline-none">
            <Menu size={20} />
          </DropdownMenuTrigger>
          <DropdownMenuContent align="end" className="w-56 bg-popover border-border text-popover-foreground">
            <DropdownMenuItem 
              onClick={() => navigate('/profile')}
              className="cursor-pointer focus:bg-accent focus:text-accent-foreground"
            >
              <User className="mr-2 size-4" />
              <span>Profile</span>
            </DropdownMenuItem>
            <DropdownMenuItem className="cursor-pointer focus:bg-accent focus:text-accent-foreground">
              <Settings className="mr-2 size-4" />
              <span>Settings</span>
            </DropdownMenuItem>
            
            <DropdownMenuSeparator className="bg-border" />
            
            <DropdownMenuItem 
              onClick={toggleTheme}
              className="cursor-pointer focus:bg-accent focus:text-accent-foreground"
            >
              {theme === 'dark' ? (
                <>
                  <Sun className="mr-2 size-4" />
                  <span>Light Mode</span>
                </>
              ) : (
                <>
                  <Moon className="mr-2 size-4" />
                  <span>Dark Mode</span>
                </>
              )}
            </DropdownMenuItem>

            <DropdownMenuSeparator className="bg-border" />
            
            <DropdownMenuItem
              variant="destructive"
              onClick={handleLogout}
              className="cursor-pointer"
            >
              <LogOut className="mr-2 size-4" />
              <span>Logout</span>
            </DropdownMenuItem>
          </DropdownMenuContent>
        </DropdownMenu>
      </div>
    </header>
  )
}
