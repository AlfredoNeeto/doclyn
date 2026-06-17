import { useForm } from 'react-hook-form'
import { zodResolver } from '@hookform/resolvers/zod'
import { forgotPasswordEmailSchema, type ForgotPasswordEmailData } from '@/schemas/auth.schemas'
import { FeedbackAlert } from '@/components/shared/feedback-alert'
import { Button } from '@/components/ui/button'
import { Input } from '@/components/ui/input'
import { Label } from '@/components/ui/label'
import { Loader2, ArrowLeft } from 'lucide-react'
import { Link } from 'react-router-dom'
import { ROUTES } from '@/lib/constants/routes'

interface Props {
  onSubmit: (data: ForgotPasswordEmailData) => Promise<void>
  error: string
  isLoading: boolean
}

export function ForgotPasswordEmailStep({ onSubmit, error, isLoading }: Props) {
  const { register, handleSubmit, formState: { errors } } = useForm<ForgotPasswordEmailData>({
    resolver: zodResolver(forgotPasswordEmailSchema),
  })

  return (
    <div>
      {error && <FeedbackAlert variant="destructive" title="Erro" description={error} />}
      <form onSubmit={handleSubmit(onSubmit)} className={error ? 'mt-4 space-y-4' : 'space-y-4'}>
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
  )
}
