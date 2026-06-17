import type { PagedResponse } from './common'

export interface DocumentListItem {
  id: string
  fileName: string
  documentType: string
  documentStatus: string
  createdAt: string
  processedAt: string | null
}

export type DocumentsResponse = PagedResponse<DocumentListItem>

export interface DocumentDetail {
  id: string
  userId: string
  fileName: string
  fileHash: string
  documentType: string
  documentStatus: string
  createdAt: string
  updatedAt: string | null
  processedAt: string | null
}

export interface ExtractedData {
  documentId: string
  data: Record<string, unknown> | null
  createdAt: string | null
}

export interface ReviewField {
  fieldName: string
  value: unknown
  confidence: number
  source: string
  validationStatus: string
}

export interface ReviewFieldsResponse {
  documentId: string
  fields: ReviewField[]
}

export interface UploadDocumentResponse {
  id: string
  fileName: string
  fileHash: string
  documentType: string
  documentStatus: string
  createdAt: string
}

export interface ProcessDocumentResponse {
  documentId: string
  status: string
  documentType: string
  processedAt: string | null
}

export interface ReprocessDocumentResponse {
  documentId: string
  status: string
}

export interface ReclassifyDocumentResponse {
  documentId: string
  status: string
}

export interface DocumentQueryParams {
  page?: number
  pageSize?: number
  status?: string
  documentType?: string
  from?: string
  to?: string
  search?: string
}
