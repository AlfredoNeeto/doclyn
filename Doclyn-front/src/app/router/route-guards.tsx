import { Navigate, Outlet } from 'react-router-dom'
import { useAuth } from '@/app/providers/auth-provider'
import { Loader2, ShieldCheck } from 'lucide-react'
import { ROUTES } from '@/lib/constants/routes'

function AuthLoadingScreen() {
  return (
    <div className="flex h-screen flex-col items-center justify-center gap-4 bg-background">
      <ShieldCheck className="h-10 w-10 text-muted-foreground/40" />
      <Loader2 className="h-6 w-6 animate-spin text-primary" />
      <p className="text-sm text-muted-foreground">Verificando autenticação...</p>
    </div>
  )
}

export function AuthGuard() {
  const { isAuthenticated, isLoading } = useAuth()

  if (isLoading) return <AuthLoadingScreen />

  if (!isAuthenticated) {
    return <Navigate to={ROUTES.LOGIN} replace />
  }

  return <Outlet />
}

export function GuestGuard() {
  const { isAuthenticated, isLoading } = useAuth()

  if (isLoading) return <AuthLoadingScreen />

  if (isAuthenticated) {
    return <Navigate to={ROUTES.DASHBOARD} replace />
  }

  return <Outlet />
}
