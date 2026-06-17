import api from './api'
import type {
  DocumentsResponse,
  DocumentDetail,
  ExtractedData,
  ReviewFieldsResponse,
  UploadDocumentResponse,
  ProcessDocumentResponse,
  ReprocessDocumentResponse,
  ReclassifyDocumentResponse,
  DocumentQueryParams,
} from '@/types/documents'
import type { DocumentInsight, DocumentLog } from '@/types/insights'

export const documentsService = {
  async upload(file: File): Promise<UploadDocumentResponse> {
    const formData = new FormData()
    formData.append('file', file)
    const { data } = await api.post<UploadDocumentResponse>('/Documents/upload', formData, {
      headers: { 'Content-Type': 'multipart/form-data' },
    })
    return data
  },

  async getAll(params: DocumentQueryParams = {}): Promise<DocumentsResponse> {
    const { data } = await api.get<DocumentsResponse>('/Documents', { params })
    return data
  },

  async getById(id: string): Promise<DocumentDetail> {
    const { data } = await api.get<DocumentDetail>(`/Documents/${id}`)
    return data
  },

  async getExtractedData(id: string): Promise<ExtractedData> {
    const { data } = await api.get<ExtractedData>(`/Documents/${id}/extracted-data`)
    return data
  },

  async getReviewFields(id: string): Promise<ReviewFieldsResponse> {
    const { data } = await api.get<ReviewFieldsResponse>(`/Documents/${id}/review-fields`)
    return data
  },

  async getInsights(id: string): Promise<DocumentInsight[]> {
    const { data } = await api.get<DocumentInsight[]>(`/Documents/${id}/insights`)
    return data
  },

  async getLogs(id: string): Promise<DocumentLog[]> {
    const { data } = await api.get<DocumentLog[]>(`/Documents/${id}/logs`)
    return data
  },

  async process(id: string): Promise<ProcessDocumentResponse> {
    const { data } = await api.post<ProcessDocumentResponse>(`/Documents/${id}/process`)
    return data
  },

  async reprocess(id: string): Promise<ReprocessDocumentResponse> {
    const { data } = await api.post<ReprocessDocumentResponse>(`/Documents/${id}/reprocess`)
    return data
  },

  async reclassify(id: string): Promise<ReclassifyDocumentResponse> {
    const { data } = await api.post<ReclassifyDocumentResponse>(`/Documents/${id}/reclassify`)
    return data
  },

  async delete(id: string): Promise<void> {
    await api.delete(`/Documents/${id}`)
  },

  async download(id: string): Promise<Blob> {
    const response = await api.get(`/Documents/${id}/download`, {
      responseType: 'blob',
    })
    return response.data
  },
}
