export interface DocumentClassListItem {
  id: string
  name: string
  displayName: string
  group: string
  subGroup: string
  isActive: boolean
}

export interface DocumentClassDetail {
  id: string
  name: string
  displayName: string
  group: string
  subGroup: string
  description: string
  isSystemDefined: boolean
  isActive: boolean
  createdAt: string
  updatedAt: string | null
}

export interface DocumentClassExample {
  id: string
  documentId: string
  fileName: string
  confidence: number
  createdAt: string
}

export interface TopDocumentClass {
  id: string
  name: string
  displayName: string
  group: string
  subGroup: string
  exampleCount: number
}

export interface DocumentClassIndexer {
  id: string
  name: string
  displayName: string
  description: string
  dataType: number
  isRequired: boolean
  isMultiple: boolean
  extractionHint: string | null
  hasRegexPattern: boolean
  isActive: boolean
  createdAt: string
  updatedAt: string | null
}

export interface CreateIndexerPayload {
  name: string
  displayName: string
  description: string
  dataType: number
  isRequired: boolean
  isMultiple: boolean
  extractionHint: string | null
  regexPattern: string | null
}

export interface UpdateIndexerPayload {
  name: string
  displayName: string
  description: string
  dataType: number
  isRequired: boolean
  isMultiple: boolean
  extractionHint: string | null
  regexPattern: string | null
}
