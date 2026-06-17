import { Badge } from '@/components/ui/badge'
import { formatDocumentStatus } from '@/lib/formatters/labels'
import { mapDocumentStatusToVariant } from '@/lib/mappers/document-status'

interface StatusBadgeProps {
  status: string
}

export function StatusBadge({ status }: StatusBadgeProps) {
  return <Badge variant={mapDocumentStatusToVariant(status)}>{formatDocumentStatus(status)}</Badge>
}
