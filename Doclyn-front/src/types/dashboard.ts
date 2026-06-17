export interface DocumentsSummary {
  total: number
  pending: number
  processing: number
  processed: number
  failed: number
}

export interface QualitySummary {
  averageConfidence: number
  fieldsValidated: number
  fieldsNeedsReview: number
  fieldsRejected: number
}

export interface InsightsSummary {
  total: number
  critical: number
  warning: number
  info: number
  success: number
}

export interface ClassUsage {
  id: string
  name: string
  displayName: string
  documentsCount: number
}

export interface ClassesSummary {
  total: number
  mostUsed: ClassUsage[]
}

export interface RecentDocument {
  id: string
  fileName: string
  documentStatus: string
  documentClass: string | null
  averageConfidence: number | null
  insightsCount: number
  needsReviewCount: number
  createdAt: string
}

export interface AttentionRequired {
  documentId: string
  fileName: string
  reason: string
  severity: string
  createdAt: string
}

export interface DashboardSummary {
  documents: DocumentsSummary
  quality: QualitySummary
  insights: InsightsSummary
  classes: ClassesSummary
  recentDocuments: RecentDocument[]
  attentionRequired: AttentionRequired[]
}
