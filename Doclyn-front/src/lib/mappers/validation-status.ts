import type { BadgeVariant } from './document-status'

export function mapValidationStatusToVariant(status: string): BadgeVariant {
  switch (status) {
    case 'Validated': return 'success'
    case 'NeedsReview': return 'warning'
    case 'Rejected': return 'destructive'
    default: return 'secondary'
  }
}
