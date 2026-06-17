import type { ReviewField } from '@/types/documents'
import { Table, TableBody, TableCell, TableHead, TableHeader, TableRow } from '@/components/ui/table'
import { ConfidenceBadge } from './confidence-badge'
import { Badge } from '@/components/ui/badge'
import { formatValidationStatus, formatExtractionSource } from '@/lib/formatters/labels'
import { formatFieldName } from '@/features/documents/lib/normalize-extracted-data'

function parseReviewValue(raw: unknown, fieldName: string): string {
  if (typeof raw === 'string') {
    try {
      const parsed = JSON.parse(raw)
      return formatNestedValue(parsed, fieldName)
    } catch {
      return raw
    }
  }
  return formatNestedValue(raw, fieldName)
}

function formatNestedValue(value: unknown, fieldName: string): string {
  if (value === null || value === undefined) return '-'

  if (typeof value === 'object' && !Array.isArray(value)) {
    const obj = value as Record<string, unknown>
    if ('value' in obj) {
      if (obj.value === null || obj.value === undefined) return '-'
      return formatNestedValue(obj.value, fieldName)
    }
    return '-'
  }

  if (Array.isArray(value)) {
    const flattened = flattenDeep(value)
    return flattened.map((v) => formatNestedValue(v, fieldName)).filter((v) => v !== '' && v !== '-').join(', ') || '-'
  }

  return formatScalar(value)
}

function formatScalar(value: unknown): string {
  if (typeof value === 'string') {
    if (isPtBrDate(value)) return formatPtBrDate(value)
    return value.trim() || '-'
  }
  if (typeof value === 'boolean') return value ? 'Sim' : 'Não'
  if (typeof value === 'number') return value.toLocaleString('pt-BR')
  return String(value)
}

function isPtBrDate(value: string): boolean {
  return /^\d{4}-\d{2}-\d{2}T/.test(value) && !isNaN(Date.parse(value.split('T')[0] + 'T00:00:00'))
}

function formatPtBrDate(value: string): string {
  try {
    const [datePart] = value.split('T')
    const [year, month, day] = datePart.split('-')
    if (year && month && day) {
      return `${day}/${month}/${year}`
    }
  } catch { /* fallback */ }
  return value
}

function flattenDeep(arr: unknown[]): unknown[] {
  return arr.flatMap((item) => (Array.isArray(item) ? flattenDeep(item) : [item]))
}
import { mapValidationStatusToVariant } from '@/lib/mappers/validation-status'

interface ExtractedFieldTableProps {
  fields: ReviewField[]
}

export function ExtractedFieldTable({ fields }: ExtractedFieldTableProps) {
  if (!fields.length) {
    return <p className="text-sm text-muted-foreground py-4">Nenhum dado extraído disponível.</p>
  }

  return (
    <Table>
      <TableHeader>
        <TableRow>
          <TableHead>Campo</TableHead>
          <TableHead>Valor</TableHead>
          <TableHead>Origem</TableHead>
          <TableHead>Confiança</TableHead>
          <TableHead>Status</TableHead>
        </TableRow>
      </TableHeader>
      <TableBody>
        {fields.map((field) => (
          <TableRow key={field.fieldName}>
            <TableCell className="font-medium">{formatFieldName(field.fieldName)}</TableCell>
            <TableCell className="max-w-xs truncate">{parseReviewValue(field.value, field.fieldName)}</TableCell>
            <TableCell>{formatExtractionSource(field.source)}</TableCell>
            <TableCell><ConfidenceBadge value={field.confidence} /></TableCell>
            <TableCell>
              <Badge variant={mapValidationStatusToVariant(field.validationStatus)}>
                {formatValidationStatus(field.validationStatus)}
              </Badge>
            </TableCell>
          </TableRow>
        ))}
      </TableBody>
    </Table>
  )
}
