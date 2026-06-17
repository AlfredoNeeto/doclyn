import { useForm } from 'react-hook-form'
import { zodResolver } from '@hookform/resolvers/zod'
import { verifyResetCodeSchema, type VerifyResetCodeData } from '@/schemas/auth.schemas'
import { FeedbackAlert } from '@/components/shared/feedback-alert'
import { Button } from '@/components/ui/button'
import { Input } from '@/components/ui/input'
import { Label } from '@/components/ui/label'
import { Loader2 } from 'lucide-react'

interface Props {
  onSubmit: (data: VerifyResetCodeData) => Promise<void>
  error: string
  isLoading: boolean
  email: string
}

export function VerifyResetCodeStep({ onSubmit, error, isLoading, email }: Props) {
  const { register, handleSubmit, formState: { errors } } = useForm<VerifyResetCodeData>({
    resolver: zodResolver(verifyResetCodeSchema),
  })

  return (
    <div>
      {error && <FeedbackAlert variant="destructive" title="Código inválido" description={error} />}
      <form onSubmit={handleSubmit(onSubmit)} className={error ? 'mt-4 space-y-4' : 'space-y-4'}>
        <div className="space-y-2">
          <Label htmlFor="code">Código de verificação</Label>
          <Input id="code" type="text" placeholder="000000" maxLength={6} className="text-center text-lg tracking-widest" {...register('code')} />
          {errors.code && <p className="text-xs text-destructive">{errors.code.message}</p>}
        </div>
        <Button type="submit" className="w-full" disabled={isLoading}>
          {isLoading && <Loader2 className="mr-2 h-4 w-4 animate-spin" />}
          Validar código
        </Button>
        <p className="text-xs text-center text-muted-foreground">Código enviado para {email}</p>
      </form>
    </div>
  )
}
