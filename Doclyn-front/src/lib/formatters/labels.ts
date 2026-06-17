export const DOCUMENT_STATUS_LABELS: Record<string, string> = {
  Pending: 'Pendente',
  Processing: 'Processando',
  Processed: 'Processado',
  Failed: 'Falhou',
  Success: 'Sucesso',
}

export const INSIGHT_SEVERITY_LABELS: Record<string, string> = {
  Critical: 'Crítico',
  Warning: 'Atenção',
  Info: 'Informação',
  Success: 'Sucesso',
}

export const VALIDATION_STATUS_LABELS: Record<string, string> = {
  Validated: 'Validado',
  NeedsReview: 'Precisa revisão',
  Rejected: 'Rejeitado',
}

export const VALIDATION_STATUS_VARIANTS: Record<string, string> = {
  Validated: 'success',
  NeedsReview: 'warning',
  Rejected: 'destructive',
}

export const EXTRACTION_SOURCE_LABELS: Record<string, string> = {
  Regex: 'Regex',
  AI: 'IA',
  Manual: 'Manual',
  Merged: 'Mesclado',
}

export const INDEXER_DATA_TYPE_LABELS: Record<string, string> = {
  Text: 'Texto',
  Number: 'Número',
  Decimal: 'Decimal',
  Date: 'Data',
  Boolean: 'Sim/Não',
  Cpf: 'CPF',
  Cnpj: 'CNPJ',
  Email: 'E-mail',
  Phone: 'Telefone',
  Cep: 'CEP',
  Currency: 'Moeda',
  Object: 'Objeto',
  Array: 'Lista',
}

export function formatDocumentStatus(status: string): string {
  return DOCUMENT_STATUS_LABELS[status] ?? status
}

export function formatInsightSeverity(severity: string): string {
  return INSIGHT_SEVERITY_LABELS[severity] ?? severity
}

export function formatValidationStatus(status: string): string {
  return VALIDATION_STATUS_LABELS[status] ?? status
}

export function formatExtractionSource(source: string): string {
  return EXTRACTION_SOURCE_LABELS[source] ?? source
}

export function formatIndexerDataType(dataType: string): string {
  return INDEXER_DATA_TYPE_LABELS[dataType] ?? dataType
}

export const PROCESSING_STEP_LABELS: Record<string, string> = {
  Upload: 'Upload recebido',
  ProcessingStarted: 'Processamento iniciado',
  OcrRequired: 'OCR necessário',
  OcrStarted: 'OCR iniciado',
  OcrCompleted: 'OCR concluído',
  TextExtracted: 'Texto extraído',
  DocumentClassified: 'Documento classificado',
  AiClassificationCompleted: 'Classificação por IA concluída',
  AiClassificationFailed: 'Falha na classificação por IA',
  DocumentClassFound: 'Classe documental encontrada',
  DocumentClassIndexersLoaded: 'Indexadores carregados',
  AiExtractionStarted: 'Extração por IA iniciada',
  AiExtractionCompleted: 'Extração por IA concluída',
  AiExtractionEmpty: 'Extração por IA vazia',
  ClassGuidedExtractionCompleted: 'Extração orientada concluída',
  ExtractionInsufficient: 'Extração insuficiente',
  ExtractionLowQuality: 'Qualidade da extração baixa',
  InsightsGenerated: 'Insights gerados',
  ProcessingCompleted: 'Processamento concluído',
  ProcessingFailed: 'Falha no processamento',
  ReprocessRequested: 'Reprocessamento solicitado',
  ReclassifyRequested: 'Reclassificação solicitada',
  DocumentDeleted: 'Documento excluído',
  DocumentRestored: 'Documento restaurado',
}

export function formatProcessingStep(step: string): string {
  return PROCESSING_STEP_LABELS[step] ?? step
}
