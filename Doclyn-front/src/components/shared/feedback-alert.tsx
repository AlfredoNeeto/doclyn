import { Alert, AlertTitle, AlertDescription } from '@/components/ui/alert'

type FeedbackVariant = 'default' | 'destructive' | 'success' | 'warning'

interface FeedbackAlertProps {
  variant?: FeedbackVariant
  title: string
  description?: string
}

export function FeedbackAlert({ variant = 'default', title, description }: FeedbackAlertProps) {
  return (
    <Alert variant={variant}>
      <AlertTitle>{title}</AlertTitle>
      {description && <AlertDescription>{description}</AlertDescription>}
    </Alert>
  )
}
