import { Badge } from '@/components/ui/badge'
import { formatInsightSeverity } from '@/lib/formatters/labels'
import { mapSeverityToVariant } from '@/lib/mappers/insight-severity'

export function SeverityBadge({ severity }: { severity: string }) {
  return <Badge variant={mapSeverityToVariant(severity)}>{formatInsightSeverity(severity)}</Badge>
}
