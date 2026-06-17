export type BadgeVariant = 'default' | 'secondary' | 'destructive' | 'outline' | 'success' | 'warning'

export function mapDocumentStatusToVariant(status: string): BadgeVariant {
  switch (status) {
    case 'Pending': return 'warning'
    case 'Processing': return 'default'
    case 'Processed': return 'success'
    case 'Success': return 'success'
    case 'Failed': return 'destructive'
    default: return 'secondary'
  }
}
