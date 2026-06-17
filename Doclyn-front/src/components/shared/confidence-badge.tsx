import { Badge } from '@/components/ui/badge'
import { formatConfidence, confidenceVariant } from '@/lib/formatters/confidence'

export function ConfidenceBadge({ value }: { value: number }) {
  return <Badge variant={confidenceVariant(value)}>{formatConfidence(value)}</Badge>
}
