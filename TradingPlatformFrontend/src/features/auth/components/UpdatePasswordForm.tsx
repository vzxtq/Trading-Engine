import { Shield } from 'lucide-react'
import { Button } from '@/components/ui/button'
import { Input } from '@/components/ui/input'
import { Label } from '@/components/ui/label'
import { cn, dangerTextClass } from '@/lib/utils'
import { usePasswordForm } from '../hooks/usePasswordForm'

export const UpdatePasswordForm = () => {
  const { form, onSubmit } = usePasswordForm()

  return (
    <form onSubmit={form.handleSubmit(onSubmit)} className="space-y-8">
      <div className="flex items-center gap-3 mb-8">
        <Shield size={18} className="text-muted-foreground" />
        <span className="text-sm font-semibold text-muted-foreground whitespace-nowrap">
          Security
        </span>
        <div className="h-[1px] w-full bg-border" />
      </div>
      <div className="space-y-6">
        <div className="space-y-2">
          <Label className="text-sm text-muted-foreground font-medium">Current password</Label>
          <Input 
            {...form.register('currentPassword')} 
            type="password" 
            className="bg-card border-border text-foreground h-[52px] px-4 text-base focus-visible:ring-1 focus-visible:ring-muted-foreground/30 placeholder:text-muted-foreground/30" 
          />
        </div>
        <div className="grid grid-cols-2 gap-x-6 gap-y-6">
          <div className="space-y-2">
            <Label className="text-sm text-muted-foreground font-medium">New password</Label>
            <Input 
              {...form.register('newPassword')} 
              type="password" 
              className="bg-card border-border text-foreground h-[52px] px-4 text-base focus-visible:ring-1 focus-visible:ring-muted-foreground/30 placeholder:text-muted-foreground/30" 
            />
          </div>
          <div className="space-y-2">
            <Label className="text-sm text-muted-foreground font-medium">Confirm new password</Label>
            <Input 
              {...form.register('confirmPassword')} 
              type="password" 
              className="bg-card border-border text-foreground h-[52px] px-4 text-base focus-visible:ring-1 focus-visible:ring-muted-foreground/30 placeholder:text-muted-foreground/30" 
            />
            {form.formState.errors.confirmPassword && (
              <p className={cn(dangerTextClass, 'text-sm font-medium mt-2')}>
                {form.formState.errors.confirmPassword.message}
              </p>
            )}
          </div>
        </div>
      </div>
      <div className="flex justify-end">
        <Button 
          type="submit" 
          variant="outline" 
          className="border border-border bg-transparent text-foreground font-bold px-10 h-[48px] text-base rounded-lg hover:bg-muted transition-all active:scale-[0.98]"
        >
          Update password
        </Button>
      </div>
    </form>
  )
}
