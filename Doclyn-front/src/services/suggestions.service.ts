import type { Suggestion, SuggestionsResponse } from '@/types/suggestions'

const mockSuggestions: Suggestion[] = [
  {
    id: '1',
    suggestedName: 'numeroEmpenho',
    documentClass: 'Relatório Técnico Preliminar',
    documentClassId: '00000000-0000-0000-0000-000000000001',
    occurrences: 12,
    averageConfidence: 0.85,
    status: 'Pending',
  },
  {
    id: '2',
    suggestedName: 'valorTotal',
    documentClass: 'Nota Fiscal',
    documentClassId: '00000000-0000-0000-0000-000000000004',
    occurrences: 8,
    averageConfidence: 0.92,
    status: 'Pending',
  },
  {
    id: '3',
    suggestedName: 'dataAssinatura',
    documentClass: 'Contrato Administrativo',
    documentClassId: '00000000-0000-0000-0000-000000000002',
    occurrences: 15,
    averageConfidence: 0.78,
    status: 'Approved',
  },
]

export const suggestionsService = {
  async getAll(): Promise<SuggestionsResponse> {
    // Mock implementation - replace with API call when available
    await new Promise((resolve) => setTimeout(resolve, 300))
    return { items: mockSuggestions }
  },

  async approve(id: string): Promise<void> {
    await new Promise((resolve) => setTimeout(resolve, 300))
    const suggestion = mockSuggestions.find((s) => s.id === id)
    if (suggestion) {
      suggestion.status = 'Approved'
    }
  },

  async reject(id: string): Promise<void> {
    await new Promise((resolve) => setTimeout(resolve, 300))
    const suggestion = mockSuggestions.find((s) => s.id === id)
    if (suggestion) {
      suggestion.status = 'Rejected'
    }
  },
}
