export interface ClassEntry {
  name: string
  displayName: string
}

export function resolveDocumentTypeLabel(documentType: string | null | undefined, catalog: ClassEntry[]): string {
  if (!documentType) return 'Desconhecido'

  const match = catalog.find(c => c.name === documentType)
  if (match) return match.displayName

  if (documentType === 'Unknown' || documentType === 'DOCUMENTO_DESCONHECIDO') return 'Desconhecido'
  if (documentType === 'UNKNOWN') return 'Desconhecido'

  return formatDocumentTypeFallback(documentType)
}

function formatDocumentTypeFallback(value: string): string {
  return value
    .replace(/_/g, ' ')
    .replace(/([a-z])([A-Z])/g, '$1 $2')
    .replace(/\b\w/g, c => c.toUpperCase())
}
