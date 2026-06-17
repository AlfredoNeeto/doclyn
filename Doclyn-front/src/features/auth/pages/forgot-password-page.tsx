import { useState } from 'react'
import { useNavigate } from 'react-router-dom'
import { authService } from '@/services/auth.service'
import { FeedbackAlert } from '@/components/shared/feedback-alert'
import { useForm } from 'react-hook-form'
import { zodResolver } from '@hookform/resolvers/zod'
import { forgotPasswordEmailSchema, type ForgotPasswordEmailData } from '@/schemas/auth.schemas'
import { Button } from '@/components/ui/button'
import { Input } from '@/components/ui/input'
import { Label } from '@/components/ui/label'
import { Card, CardContent, CardDescription, CardHeader } from '@/components/ui/card'
import { Loader2, ArrowLeft } from 'lucide-react'
import { Link } from 'react-router-dom'
import { ROUTES } from '@/lib/constants/routes'

export function ForgotPasswordPage() {
  const navigate = useNavigate()
  const [error, setError] = useState('')
  const [isLoading, setIsLoading] = useState(false)
  const [sent, setSent] = useState(false)

  const { register, handleSubmit, formState: { errors } } = useForm<ForgotPasswordEmailData>({
    resolver: zodResolver(forgotPasswordEmailSchema),
  })

  const sendCode = async (data: ForgotPasswordEmailData) => {
    setError('')
    setIsLoading(true)
    try {
      await authService.forgotPassword(data.email)
      setSent(true)
      navigate(`/verify-reset-code`, { state: { email: data.email } })
    } catch {
      setError('Não foi possível conectar ao servidor. Tente novamente.')
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
          <CardDescription>
            {sent ? 'Código enviado' : 'Informe seu e-mail para receber um código de recuperação.'}
          </CardDescription>
        </CardHeader>
        <CardContent>
          {sent ? (
            <div className="space-y-4">
              <FeedbackAlert
                variant="success"
                title="Código enviado"
                description="Se o e-mail existir, um código de recuperação foi enviado."
              />
              <p className="text-sm text-muted-foreground text-center">
                Você será redirecionado automaticamente...
              </p>
            </div>
          ) : (
            <div>
              {error && <FeedbackAlert variant="destructive" title="Erro" description={error} />}
              <form onSubmit={handleSubmit(sendCode)} className={error ? 'mt-4 space-y-4' : 'space-y-4'}>
                <div className="space-y-2">
                  <Label htmlFor="email">E-mail</Label>
                  <Input id="email" type="email" placeholder="seu@email.com" {...register('email')} />
                  {errors.email && <p className="text-xs text-destructive">{errors.email.message}</p>}
                </div>
                <Button type="submit" className="w-full" disabled={isLoading}>
                  {isLoading && <Loader2 className="mr-2 h-4 w-4 animate-spin" />}
                  Enviar código
                </Button>
                <div className="text-center">
                  <Link to={ROUTES.LOGIN} className="flex items-center justify-center gap-1 text-sm text-muted-foreground hover:text-foreground">
                    <ArrowLeft className="h-3 w-3" />
                    Voltar para login
                  </Link>
                </div>
              </form>
            </div>
          )}
        </CardContent>
      </Card>
    </div>
  )
}
