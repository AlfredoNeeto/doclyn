import { useState, useEffect } from 'react'
import { useNavigate, useLocation } from 'react-router-dom'
import { authService } from '@/services/auth.service'
import { FeedbackAlert } from '@/components/shared/feedback-alert'
import { useToast } from '@/components/ui/toaster'
import { useForm } from 'react-hook-form'
import { zodResolver } from '@hookform/resolvers/zod'
import { z } from 'zod'
import { Button } from '@/components/ui/button'
import { Input } from '@/components/ui/input'
import { Label } from '@/components/ui/label'
import { Card, CardContent, CardDescription, CardHeader, CardTitle } from '@/components/ui/card'
import { Loader2, Eye, EyeOff, ArrowLeft } from 'lucide-react'
import { Link } from 'react-router-dom'
import { ROUTES } from '@/lib/constants/routes'

const newPasswordSchema = z.object({
  newPassword: z.string().min(8, 'Senha deve ter pelo menos 8 caracteres'),
  confirmPassword: z.string(),
}).refine((data) => data.newPassword === data.confirmPassword, {
  message: 'Senhas não conferem',
  path: ['confirmPassword'],
})

type NewPasswordData = z.infer<typeof newPasswordSchema>

export function ResetPasswordPage() {
  const navigate = useNavigate()
  const location = useLocation()
  const state = location.state as { email?: string; resetToken?: string } | null
  const resetToken = state?.resetToken ?? ''

  useEffect(() => {
    if (!resetToken) {
      navigate(ROUTES.FORGOT_PASSWORD, { replace: true })
    }
  }, [resetToken, navigate])

  const { toast } = useToast()
  const [error, setError] = useState('')
  const [isLoading, setIsLoading] = useState(false)
  const [showPassword, setShowPassword] = useState(false)

  const { register, handleSubmit, formState: { errors } } = useForm<NewPasswordData>({
    resolver: zodResolver(newPasswordSchema),
  })

  const onSubmit = async (data: NewPasswordData) => {
    setError('')
    setIsLoading(true)
    try {
      await authService.resetPassword(resetToken, data.newPassword)
      toast('success', 'Senha redefinida com sucesso. Faça login novamente.')
      navigate(ROUTES.LOGIN, { state: { passwordReset: true }, replace: true })
    } catch {
      setError('Não foi possível conectar ao servidor. Tente novamente.')
    } finally {
      setIsLoading(false)
    }
  }

  if (!resetToken) return null

  return (
    <div className="flex min-h-screen items-center justify-center bg-muted/30 p-4">
      <Card className="w-full max-w-sm">
        <CardHeader className="text-center">
          <div className="mx-auto mb-3 flex items-center justify-center">
            <img src="/logo.png" alt="Doclyn" className="w-auto max-w-[220px] mx-auto" />
          </div>
          <CardTitle className="text-xl">Criar nova senha</CardTitle>
          <CardDescription>Crie uma nova senha segura para acessar sua conta.</CardDescription>
        </CardHeader>
        <CardContent>
          {error && <FeedbackAlert variant="destructive" title="Erro ao redefinir" description={error} />}
          <form onSubmit={handleSubmit(onSubmit)} className={error ? 'mt-4 space-y-4' : 'space-y-4'}>
            <div className="space-y-2">
              <Label htmlFor="newPassword">Nova senha</Label>
              <div className="relative">
                <Input id="newPassword" type={showPassword ? 'text' : 'password'} placeholder="Mínimo 8 caracteres" {...register('newPassword')} className="pr-10" />
                <button type="button" onClick={() => setShowPassword(!showPassword)} className="absolute right-3 top-1/2 -translate-y-1/2 text-muted-foreground hover:text-foreground">
                  {showPassword ? <EyeOff className="h-4 w-4" /> : <Eye className="h-4 w-4" />}
                </button>
              </div>
              {errors.newPassword && <p className="text-xs text-destructive">{errors.newPassword.message}</p>}
            </div>
            <div className="space-y-2">
              <Label htmlFor="confirmPassword">Confirmar senha</Label>
              <Input id="confirmPassword" type="password" placeholder="Repita a senha" {...register('confirmPassword')} />
              {errors.confirmPassword && <p className="text-xs text-destructive">{errors.confirmPassword.message}</p>}
            </div>
            <Button type="submit" className="w-full" disabled={isLoading}>
              {isLoading && <Loader2 className="mr-2 h-4 w-4 animate-spin" />}
              Redefinir senha
            </Button>
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
