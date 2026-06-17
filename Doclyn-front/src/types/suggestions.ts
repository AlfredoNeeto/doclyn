export interface Suggestion {
  id: string
  suggestedName: string
  documentClass: string
  documentClassId: string
  occurrences: number
  averageConfidence: number
  status: string
}

export interface SuggestionsResponse {
  items: Suggestion[]
}
