import type { BadgeVariant } from './document-status'

export function mapSeverityToVariant(severity: string): BadgeVariant {
  switch (severity) {
    case 'Critical': return 'destructive'
    case 'Warning': return 'warning'
    case 'Info': return 'default'
    case 'Success': return 'success'
    default: return 'secondary'
  }
}
