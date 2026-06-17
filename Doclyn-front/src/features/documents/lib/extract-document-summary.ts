export function extractDocumentSummary(extractedData: { data: unknown } | undefined, insights: { type: string; message: string }[] | undefined): string | null {
  if (!extractedData?.data) return null

  const data = extractedData.data as Record<string, unknown>

  const fields = data.fields as Record<string, unknown> | undefined
  if (fields?.resumo) {
    const resumoField = fields.resumo as Record<string, unknown>
    if (typeof resumoField.value === 'object' && resumoField.value !== null) {
      const nested = resumoField.value as Record<string, unknown>
      if (typeof nested.value === 'string' && nested.value.trim()) {
        return nested.value
      }
    }
    if (typeof resumoField.value === 'string' && resumoField.value.trim()) {
      return resumoField.value
    }
  }

  const finalResult = data.finalResult as Record<string, unknown> | undefined
  if (finalResult?.resumo) {
    const resumoVal = finalResult.resumo
    if (typeof resumoVal === 'object' && resumoVal !== null && 'value' in resumoVal) {
      const val = (resumoVal as Record<string, unknown>).value
      if (typeof val === 'string' && val.trim()) return val
    }
    if (typeof resumoVal === 'string' && resumoVal.trim()) {
      return resumoVal
    }
  }

  if (typeof data.summary === 'string' && data.summary.trim()) {
    return data.summary
  }

  if (insights?.length) {
    const summaryInsight = insights.find((i) => i.type === 'Summary')
    if (summaryInsight?.message) return summaryInsight.message
  }

  return null
}
