import { useState, useEffect, useRef } from 'react'
import { useNavigate, useLocation, Link } from 'react-router-dom'
import { authService } from '@/services/auth.service'
import { FeedbackAlert } from '@/components/shared/feedback-alert'
import { useForm } from 'react-hook-form'
import { zodResolver } from '@hookform/resolvers/zod'
import { z } from 'zod'
import { Button } from '@/components/ui/button'
import { Input } from '@/components/ui/input'
import { Label } from '@/components/ui/label'
import { Card, CardContent, CardDescription, CardHeader, CardTitle } from '@/components/ui/card'
import { Loader2, ArrowLeft, Clock } from 'lucide-react'
import { ROUTES } from '@/lib/constants/routes'

const verifyCodeSchema = z.object({
  code: z.string().length(6, 'Código deve ter 6 dígitos').regex(/^\d+$/, 'Código deve conter apenas números'),
})

type VerifyCodeData = z.infer<typeof verifyCodeSchema>

function formatTimer(seconds: number): string {
  const m = Math.floor(seconds / 60)
  const s = seconds % 60
  return `${String(m).padStart(2, '0')}:${String(s).padStart(2, '0')}`
}

export function VerifyResetCodePage() {
  const navigate = useNavigate()
  const location = useLocation()
  const state = location.state as { email?: string } | null
  const email = state?.email ?? ''

  useEffect(() => {
    if (!email) {
      navigate(ROUTES.FORGOT_PASSWORD, { replace: true })
    }
  }, [email, navigate])

  const [error, setError] = useState('')
  const [isLoading, setIsLoading] = useState(false)
  const [resendCooldown, setResendCooldown] = useState(40)
  const [isResending, setIsResending] = useState(false)
  const [resendSuccess, setResendSuccess] = useState(false)
  const timerRef = useRef<ReturnType<typeof setInterval> | null>(null)

  useEffect(() => {
    timerRef.current = setInterval(() => {
      setResendCooldown((prev) => {
        if (prev <= 1) {
          if (timerRef.current) clearInterval(timerRef.current)
          return 0
        }
        return prev - 1
      })
    }, 1000)

    return () => {
      if (timerRef.current) clearInterval(timerRef.current)
    }
  }, [resendCooldown])

  const { register, handleSubmit, formState: { errors } } = useForm<VerifyCodeData>({
    resolver: zodResolver(verifyCodeSchema),
  })

  const onSubmit = async (data: VerifyCodeData) => {
    setError('')
    setIsLoading(true)
    try {
      const response = await authService.verifyResetCode(email, data.code)
      navigate(ROUTES.RESET_PASSWORD, { state: { email, resetToken: response.resetToken } })
    } catch {
      setError('Código inválido ou expirado.')
    } finally {
      setIsLoading(false)
    }
  }

  const handleResend = async () => {
    setError('')
    setResendSuccess(false)
    setIsResending(true)
    try {
      await authService.forgotPassword(email)
      setResendSuccess(true)
      setResendCooldown(40)
    } catch {
      setError('Não foi possível conectar ao servidor. Tente novamente.')
    } finally {
      setIsResending(false)
    }
  }

  if (!email) return null

  return (
    <div className="flex min-h-screen items-center justify-center bg-muted/30 p-4">
      <Card className="w-full max-w-sm">
        <CardHeader className="text-center">
          <div className="mx-auto mb-3 flex items-center justify-center">
            <img src="/logo.png" alt="Doclyn" className="w-auto max-w-[220px] mx-auto" />
          </div>
          <CardTitle className="text-xl">Verificar código</CardTitle>
          <CardDescription>Digite o código de 6 dígitos enviado para seu e-mail.</CardDescription>
        </CardHeader>
        <CardContent>
          {resendSuccess && (
            <FeedbackAlert variant="success" title="Código reenviado" description="Se o e-mail existir, um código de recuperação foi enviado." />
          )}
          {error && <FeedbackAlert variant="destructive" title="Código inválido" description={error} />}
          <form onSubmit={handleSubmit(onSubmit)} className={error || resendSuccess ? 'mt-4 space-y-4' : 'space-y-4'}>
            <div className="space-y-2">
              <Label htmlFor="code">Código de verificação</Label>
              <Input id="code" type="text" placeholder="000000" maxLength={6} className="text-center text-lg tracking-widest" {...register('code')} />
              {errors.code && <p className="text-xs text-destructive">{errors.code.message}</p>}
            </div>
            <Button type="submit" className="w-full" disabled={isLoading}>
              {isLoading && <Loader2 className="mr-2 h-4 w-4 animate-spin" />}
              Validar código
            </Button>
            <div className="text-center">
              {resendCooldown > 0 ? (
                <span className="inline-flex items-center gap-1.5 text-sm text-muted-foreground">
                  <Clock className="h-3.5 w-3.5" />
                  Reenviar código em {formatTimer(resendCooldown)}
                </span>
              ) : (
                <button
                  type="button"
                  onClick={handleResend}
                  disabled={isResending}
                  className="text-sm text-primary hover:underline disabled:opacity-50 disabled:cursor-not-allowed"
                >
                  {isResending ? 'Reenviando...' : 'Reenviar código'}
                </button>
              )}
            </div>
            <p className="text-xs text-center text-muted-foreground">Código enviado para {email}</p>
            <div className="text-center">
              <Link to={ROUTES.LOGIN} className="flex items-center justify-center gap-1 text-sm text-muted-foreground hover:text-foreground">
                <ArrowLeft className="h-3 w-3" />
                Voltar para login
              </Link>
            </div>
          </form>
        </CardContent>
      </Card>
    </div>
  )
}
