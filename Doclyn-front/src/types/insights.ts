export interface DocumentInsight {
  id: string
  type: string
  severity: string
  title: string
  message: string
  confidence: number
  source: string
  relatedFieldName: string | null
  createdAt: string
}

export interface DocumentLog {
  id: string
  step: string
  status: string
  message: string
  createdAt: string
}
