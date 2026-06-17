import api from './api'
import type {
  DocumentClassListItem,
  DocumentClassDetail,
  DocumentClassExample,
  TopDocumentClass,
  DocumentClassIndexer,
  CreateIndexerPayload,
  UpdateIndexerPayload,
} from '@/types/document-classes'

export const documentClassesService = {
  async getAll(): Promise<{ items: DocumentClassListItem[] }> {
    const { data } = await api.get<{ items: DocumentClassListItem[] }>('/document-classes')
    return data
  },

  async getById(id: string): Promise<DocumentClassDetail> {
    const { data } = await api.get<DocumentClassDetail>(`/document-classes/${id}`)
    return data
  },

  async getExamples(id: string): Promise<DocumentClassExample[]> {
    const { data } = await api.get<DocumentClassExample[]>(`/document-classes/${id}/examples`)
    return data
  },

  async getTop(take = 10): Promise<TopDocumentClass[]> {
    const { data } = await api.get<TopDocumentClass[]>('/document-classes/top', { params: { take } })
    return data
  },
}

export const indexersService = {
  async getByDocumentClass(documentClassId: string): Promise<DocumentClassIndexer[]> {
    const { data } = await api.get<DocumentClassIndexer[]>(`/document-classes/${documentClassId}/indexers`)
    return data
  },

  async create(documentClassId: string, payload: CreateIndexerPayload): Promise<{ id: string }> {
    const { data } = await api.post<{ id: string }>(`/document-classes/${documentClassId}/indexers`, payload)
    return data
  },

  async update(documentClassId: string, id: string, payload: UpdateIndexerPayload): Promise<void> {
    await api.put(`/document-classes/${documentClassId}/indexers/${id}`, payload)
  },

  async disable(documentClassId: string, id: string): Promise<void> {
    await api.delete(`/document-classes/${documentClassId}/indexers/${id}`)
  },
}
