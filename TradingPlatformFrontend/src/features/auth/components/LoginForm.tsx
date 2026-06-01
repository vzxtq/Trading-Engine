import React, { useState } from 'react'
import { Link, useNavigate } from 'react-router-dom'
import { useLogin } from '../api/auth.api'
import { Button } from '@/components/ui/button'
import { Input } from '@/components/ui/input'
import { Label } from '@/components/ui/label'
import { Card, CardContent, CardDescription, CardFooter, CardHeader, CardTitle } from '@/components/ui/card'
import { AuthBrandLogo } from './AuthBrandLogo'

export const LoginForm: React.FC = () => {
  const [email, setEmail] = useState('')
  const [password, setPassword] = useState('')
  const loginMutation = useLogin()
  const navigate = useNavigate()

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault()
    loginMutation.mutate({ email, password }, {
      onSuccess: (data) => {
        if (data.success) {
          navigate('/dashboard')
        }
      }
    })
  }

  return (
    <div className="flex min-h-screen flex-col items-center justify-center gap-8 bg-background p-4">
      <AuthBrandLogo />
      <Card className="w-full max-w-md border-border bg-card text-foreground">
        <CardHeader className="space-y-1">
          <CardTitle className="text-2xl font-bold">Welcome back</CardTitle>
          <CardDescription className="text-muted-foreground">
            Sign in to your trading account
          </CardDescription>
        </CardHeader>
        <form onSubmit={handleSubmit}>
          <CardContent className="space-y-4">
            <div className="space-y-2">
              <Label htmlFor="email">Email</Label>
              <Input
                id="email"
                type="email"
                placeholder="you@example.com"
                value={email}
                onChange={(e) => setEmail(e.target.value)}
                required
                className="border-border bg-muted text-foreground placeholder:text-muted-foreground"
              />
            </div>
            <div className="space-y-2">
              <Label htmlFor="password">Password</Label>
              <Input
                id="password"
                type="password"
                placeholder="Your password"
                value={password}
                onChange={(e) => setPassword(e.target.value)}
                required
                className="border-border bg-muted text-foreground placeholder:text-muted-foreground"
              />
            </div>
          </CardContent>
          <CardFooter className="flex flex-col space-y-4 border-t-0 bg-transparent">
            <Button 
              type="submit" 
              className="h-10 w-full"
              disabled={loginMutation.isPending}
            >
              {loginMutation.isPending ? 'Signing in...' : 'Sign in'}
            </Button>

            <p className="text-center text-sm text-muted-foreground">
              Forgot your password? <Link to="/reset" className="text-foreground hover:underline font-semibold">Reset it</Link>
            </p>
            <p className="text-center text-sm text-muted-foreground">
              Don't have an account? <Link to="/register" className="text-foreground hover:underline font-semibold">Create account</Link>
            </p>
          </CardFooter>
        </form>
      </Card>
    </div>
  )
}
