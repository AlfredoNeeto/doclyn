export function formatConfidence(value: number | null | undefined): string {
  if (value == null) return '-'
  return `${Math.round(value * 100)}%`
}

export function confidenceVariant(value: number): 'success' | 'warning' | 'destructive' {
  if (value >= 0.9) return 'success'
  if (value >= 0.7) return 'warning'
  return 'destructive'
}
