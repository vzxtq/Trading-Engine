import { useNavigate } from 'react-router-dom'
import { 
  FileText, 
  BarChart2, 
  History, 
  LogOut,
  ArrowUpRight,
  UserCircle,
  UserCog
} from 'lucide-react'
import { cn, dangerTextClass } from '@/lib/utils'
import { useAuth } from '../hooks/useAuth'
import { useAccount } from '../api/auth.api'

interface ProfileSidebarProps {
  activeTab: 'overview' | 'update' | 'orders'
  onTabChange: (tab: 'overview' | 'update' | 'orders') => void
}

export const ProfileSidebar = ({ activeTab, onTabChange }: ProfileSidebarProps) => {
  const navigate = useNavigate()
  const { userId, logout } = useAuth()
  const { data: account } = useAccount(userId)

  const getInitials = (name: string) => {
    return name
      .split(' ')
      .filter(Boolean)
      .map((n) => n[0])
      .join('')
      .toUpperCase()
      .slice(0, 2)
  }

  return (
    <aside className="w-[280px] border-r border-border bg-background flex flex-col shrink-0">
      <div className="p-6">
        <div className="flex items-center justify-between mb-10">
          <span 
            className="text-sm font-bold tracking-wider text-foreground uppercase cursor-pointer" 
            onClick={() => navigate('/dashboard')}
          >
            Trading Engine
          </span>
          <ArrowUpRight size={16} className="text-muted-foreground" />
        </div>

        <div className="mb-10">
          <div className="w-16 h-16 rounded-full bg-muted flex items-center justify-center text-xl font-medium text-muted-foreground mb-4">
            {account ? getInitials(`${account.firstName} ${account.lastName}`) : '??'}
          </div>
          <h2 className="text-lg font-bold text-foreground leading-tight mb-1">
            {account?.firstName} {account?.lastName}
          </h2>
          <p className="text-sm text-muted-foreground mb-3">{account?.email}</p>
          <div className="inline-block px-2 py-0.5 border border-green-500/20 rounded text-xs font-semibold text-green-500 bg-green-500/10">
            Active
          </div>
        </div>

        <nav className="space-y-1">
          <button
            onClick={() => onTabChange('overview')}
            className={cn(
              "w-full flex items-center gap-3 px-4 py-3 text-sm font-medium transition-colors relative group",
              activeTab === 'overview' ? "text-foreground bg-muted/50" : "text-muted-foreground hover:text-foreground"
            )}
          >
            {activeTab === 'overview' && <div className="absolute left-0 top-0 bottom-0 w-1 bg-green-500" />}
            <UserCircle size={18} className={cn(activeTab === 'overview' ? "text-foreground" : "text-muted-foreground group-hover:text-foreground")} />
            Profile
          </button>
          <button 
            onClick={() => onTabChange('orders')}
            className={cn(
              "w-full flex items-center gap-3 px-4 py-3 text-sm font-medium transition-colors relative group",
              activeTab === 'orders' ? "text-foreground bg-muted/50" : "text-muted-foreground hover:text-foreground"
            )}
          >
            {activeTab === 'orders' && <div className="absolute left-0 top-0 bottom-0 w-1 bg-green-500" />}
            <FileText size={18} className={cn(activeTab === 'orders' ? "text-foreground" : "text-muted-foreground group-hover:text-foreground")} />
            My orders
          </button>
          <button className="w-full flex items-center gap-3 px-4 py-3 text-sm font-medium text-muted-foreground hover:text-foreground transition-colors">
            <BarChart2 size={18} />Positions
          </button>
          <button className="w-full flex items-center gap-3 px-4 py-3 text-sm font-medium text-muted-foreground hover:text-foreground transition-colors">
            <History size={18} />Trade history
          </button>
          <button
            onClick={() => onTabChange('update')}
            className={cn(
              "w-full flex items-center gap-3 px-4 py-3 text-sm font-medium transition-colors relative group",
              activeTab === 'update' ? "text-foreground bg-muted/50" : "text-muted-foreground hover:text-foreground"
            )}
          >
            {activeTab === 'update' && <div className="absolute left-0 top-0 bottom-0 w-1 bg-green-500" />}
            <UserCog size={18} className={cn(activeTab === 'update' ? "text-foreground" : "text-muted-foreground group-hover:text-foreground")} />
            Update account
          </button>
        </nav>
      </div>

      <div className="mt-auto p-6">
        <button 
          onClick={logout}
          className={cn(
            'flex items-center gap-3 text-muted-foreground transition-colors text-sm font-medium',
            'hover:text-red-500 dark:hover:text-red-400'
          )}
        >
          <LogOut size={18} />
          <span className={dangerTextClass}>Sign out</span>
        </button>
      </div>
    </aside>
  )
}
