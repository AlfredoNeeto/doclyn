import { useState, useEffect } from 'react'
import { useNavigate, Link, useLocation, useSearchParams } from 'react-router-dom'
import { useForm } from 'react-hook-form'
import { zodResolver } from '@hookform/resolvers/zod'
import { loginSchema, type LoginFormData } from '@/schemas/auth.schemas'
import { useAuth } from '@/app/providers/auth-provider'
import { FeedbackAlert } from '@/components/shared/feedback-alert'
import { useToast } from '@/components/ui/toaster'
import { Button } from '@/components/ui/button'
import { Input } from '@/components/ui/input'
import { Label } from '@/components/ui/label'
import { Card, CardContent, CardDescription, CardHeader } from '@/components/ui/card'
import { Loader2, Eye, EyeOff } from 'lucide-react'
import { ROUTES } from '@/lib/constants/routes'

export function LoginPage() {
  const navigate = useNavigate()
  const location = useLocation()
  const [searchParams] = useSearchParams()
  const { login } = useAuth()
  const { toast } = useToast()
  const [error, setError] = useState('')
  const [isLoading, setIsLoading] = useState(false)
  const [showPassword, setShowPassword] = useState(false)
  const [registered, setRegistered] = useState(false)
  const [sessionExpired, setSessionExpired] = useState(false)
  const [passwordReset, setPasswordReset] = useState(false)

  useEffect(() => {
    const state = location.state as Record<string, unknown> | null
    const queryExpired = searchParams.get('sessionExpired')
    if (state?.registered) {
      setRegistered(true)
    }
    if (state?.sessionExpired || queryExpired === '1') {
      setSessionExpired(true)
      toast('warning', 'Sua sessão expirou. Faça login novamente.')
    }
    if (state?.passwordReset) {
      setPasswordReset(true)
    }
    if (state?.registered || state?.sessionExpired || state?.passwordReset || queryExpired === '1') {
      window.history.replaceState({}, document.title)
    }
  }, [location.state, searchParams, toast])

  const { register, handleSubmit, formState: { errors } } = useForm<LoginFormData>({
    resolver: zodResolver(loginSchema),
  })

  const onSubmit = async (data: LoginFormData) => {
    setError('')
    setRegistered(false)
    setSessionExpired(false)
    setPasswordReset(false)
    setIsLoading(true)
    try {
      await login(data.email, data.password)
      toast('success', 'Login realizado com sucesso.')
      navigate(ROUTES.DASHBOARD)
    } catch (err: unknown) {
      const axiosErr = err as { response?: { status?: number }; code?: string }
      if (axiosErr.response?.status === 401) {
        setError('E-mail ou senha inválidos.')
      } else if (axiosErr.code === 'ERR_NETWORK' || axiosErr.code === 'ERR_CONNECTION_REFUSED') {
        setError('Não foi possível conectar ao servidor. Tente novamente.')
      } else {
        setError('Não foi possível conectar ao servidor. Tente novamente.')
      }
    } finally {
      setIsLoading(false)
    }
  }

  return (
    <div className="flex min-h-screen items-center justify-center bg-muted/30 p-4">
      <Card className="w-full max-w-sm">
        <CardHeader className="text-center">
          <div className="mx-auto mb-3 flex items-center justify-center">
            <img src="/logo.png" alt="Doclyn" className="w-auto max-w-[220px] mx-auto" />
          </div>
          <CardDescription>Faça login para acessar sua conta</CardDescription>
        </CardHeader>
        <CardContent>
          {registered && (
            <FeedbackAlert variant="success" title="Conta criada com sucesso" description="Faça login para continuar." />
          )}
          {passwordReset && (
            <FeedbackAlert variant="success" title="Senha redefinida" description="Senha redefinida com sucesso. Faça login novamente." />
          )}
          {sessionExpired && (
            <FeedbackAlert variant="warning" title="Sessão expirada" description="Sua sessão expirou. Faça login novamente." />
          )}
          {error && (
            <FeedbackAlert variant="destructive" title="Falha no login" description={error} />
          )}
          <form onSubmit={handleSubmit(onSubmit)} className={error || registered || sessionExpired || passwordReset ? 'mt-4 space-y-4' : 'space-y-4'}>
            <div className="space-y-2">
              <Label htmlFor="email">E-mail</Label>
              <Input id="email" type="email" placeholder="seu@email.com" {...register('email')} />
              {errors.email && <p className="text-xs text-destructive">{errors.email.message}</p>}
            </div>
            <div className="space-y-2">
              <Label htmlFor="password">Senha</Label>
              <div className="relative">
                <Input id="password" type={showPassword ? 'text' : 'password'} placeholder="Sua senha" {...register('password')} className="pr-10" />
                <button type="button" onClick={() => setShowPassword(!showPassword)} className="absolute right-3 top-1/2 -translate-y-1/2 text-muted-foreground hover:text-foreground">
                  {showPassword ? <EyeOff className="h-4 w-4" /> : <Eye className="h-4 w-4" />}
                </button>
              </div>
              {errors.password && <p className="text-xs text-destructive">{errors.password.message}</p>}
            </div>
            <Button type="submit" className="w-full" disabled={isLoading}>
              {isLoading && <Loader2 className="mr-2 h-4 w-4 animate-spin" />}
              {isLoading ? 'Entrando...' : 'Entrar'}
            </Button>
          </form>
          <div className="mt-4 space-y-2 text-center">
            <div>
              <Link to={ROUTES.REGISTER} className="text-sm text-primary hover:underline">
                Não tem conta? Criar conta
              </Link>
            </div>
            <div>
              <Link to={ROUTES.FORGOT_PASSWORD} className="text-sm text-primary hover:underline">
                Esqueci minha senha
              </Link>
            </div>
          </div>
        </CardContent>
      </Card>
    </div>
  )
}
