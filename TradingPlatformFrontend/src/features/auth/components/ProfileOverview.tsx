import { useAuthStore } from '@/store/auth'
import { useAccount } from '../api/auth.api'
import { cn, formatAmount, numericClass } from '@/lib/utils'

export const ProfileOverview = () => {
  const userId = useAuthStore((state) => state.userId)
  const { data: account } = useAccount(userId)

  return (
    <>
      <h1 className="text-3xl font-bold text-foreground mb-8">Overview</h1>
      <div className="grid grid-cols-2 gap-6 mb-12">
        <div className="bg-card p-6 rounded-xl border border-border">
          <span className="text-xs font-semibold text-muted-foreground block mb-4">Balance</span>
          <div className="flex items-baseline gap-2">
            <span className={cn(numericClass, 'text-3xl')}>
              {account ? formatAmount(account.balance.amount) : '0.00'}
            </span>
            <span className="text-sm font-bold text-muted-foreground">{account?.balance.currency}</span>
          </div>
        </div>
        <div className="bg-card p-6 rounded-xl border border-border">
          <span className="text-xs font-semibold text-muted-foreground block mb-4">Reserved</span>
          <div className="flex items-baseline gap-2">
            <span className={cn(numericClass, 'text-3xl text-amber-500')}>
              {account ? formatAmount(account.reservedBalance.amount) : '0.00'}
            </span>
            <span className="text-sm font-bold text-muted-foreground">{account?.reservedBalance.currency}</span>
          </div>
        </div>
        <div className="bg-card p-6 rounded-xl border border-border">
          <span className="text-xs font-semibold text-muted-foreground block mb-4">Available</span>
          <div className="flex items-baseline gap-2">
            <span className={cn(numericClass, 'text-3xl text-green-500')}>
              {account ? formatAmount(account.availableBalance.amount) : '0.00'}
            </span>
            <span className="text-sm font-bold text-muted-foreground">{account?.availableBalance.currency}</span>
          </div>
        </div>
        <div className="bg-card p-6 rounded-xl border border-border">
          <span className="text-xs font-semibold text-muted-foreground block mb-4">Total P&L</span>
          <div className="flex items-baseline gap-2">
            <span className={cn(numericClass, 'text-3xl text-green-500')}>+0.00</span>
            <span className="text-sm font-bold text-muted-foreground">{account?.balance.currency}</span>
          </div>
        </div>
      </div>
      <h2 className="text-2xl font-bold text-foreground mb-6">Account details</h2>
      <div className="bg-card rounded-xl border border-border overflow-hidden">
        <div className="flex items-center justify-between p-6 border-b border-border">
          <span className="text-sm font-medium text-muted-foreground">Full name</span>
          <span className="text-sm font-bold text-foreground">{account?.firstName} {account?.lastName}</span>
        </div>
        <div className="flex items-center justify-between p-6 border-b border-border">
          <span className="text-sm font-medium text-muted-foreground">Email</span>
          <span className="text-sm font-bold text-foreground">{account?.email}</span>
        </div>
        <div className="flex items-center justify-between p-6 border-b border-border">
          <span className="text-sm font-medium text-muted-foreground">Member since</span>
          <span className="text-sm font-bold text-foreground">
            {account ? new Date(account.createdAt).toLocaleDateString('en-US', { month: 'short', day: 'numeric', year: 'numeric' }) : 'N/A'}
          </span>
        </div>
        <div className="flex items-center justify-between p-6">
          <span className="text-sm font-medium text-muted-foreground">Last login</span>
          <span className="text-sm font-bold text-foreground">
            {account?.lastLoginAt ? new Date(account.lastLoginAt).toLocaleDateString('en-US', { month: 'short', day: 'numeric', year: 'numeric' }) : 'N/A'}
          </span>
        </div>
      </div>
    </>
  )
}
