import { useState } from 'react'
import { useForm } from 'react-hook-form'
import { zodResolver } from '@hookform/resolvers/zod'
import { resetPasswordSchema, type ResetPasswordData } from '@/schemas/auth.schemas'
import { FeedbackAlert } from '@/components/shared/feedback-alert'
import { Button } from '@/components/ui/button'
import { Input } from '@/components/ui/input'
import { Label } from '@/components/ui/label'
import { Loader2, Eye, EyeOff } from 'lucide-react'

interface Props {
  onSubmit: (data: ResetPasswordData) => Promise<void>
  error: string
  isLoading: boolean
}

export function ResetPasswordStep({ onSubmit, error, isLoading }: Props) {
  const [showPassword, setShowPassword] = useState(false)
  const { register, handleSubmit, formState: { errors } } = useForm<ResetPasswordData>({
    resolver: zodResolver(resetPasswordSchema),
  })

  return (
    <div>
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
      </form>
    </div>
  )
}
