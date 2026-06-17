export interface NormalizedField {
  name: string
  value: string
}

const FIELD_NAME_LABELS: Record<string, string> = {
  numeroProcesso: 'Número do processo',
  numeroContrato: 'Número do contrato',
  orgao: 'Órgão',
  empresa: 'Empresa',
  cnpj: 'CNPJ',
  cpf: 'CPF',
  cpfs: 'CPFs',
  datas: 'Datas',
  valores: 'Valores',
  resumo: 'Resumo',
  assunto: 'Assunto',
  documentosRelacionados: 'Documentos relacionados',
  dataInicioVigencia: 'Data de início da vigência',
  dataFimVigencia: 'Data de fim da vigência',
  valorContrato: 'Valor do contrato',
  contratada: 'Contratada',
  contratante: 'Contratante',
  objeto: 'Objeto',
  numeroEmpenho: 'Número do empenho',
  valorTotal: 'Valor total',
  dataAssinatura: 'Data de assinatura',
  numeroNotaFiscal: 'Número da nota fiscal',
  dataEmissao: 'Data de emissão',
  razaoSocial: 'Razão social',
  nomeFantasia: 'Nome fantasia',
  inscricaoEstadual: 'Inscrição estadual',
  endereco: 'Endereço',
  telefone: 'Telefone',
  email: 'E-mail',
}

export function formatFieldName(name: string): string {
  if (FIELD_NAME_LABELS[name]) return FIELD_NAME_LABELS[name]
  return name
    .replace(/([a-z])([A-Z])/g, '$1 $2')
    .replace(/([A-Z]+)([A-Z][a-z])/g, '$1 $2')
    .replace(/_/g, ' ')
    .replace(/\b\w/g, (c) => c.toUpperCase())
}

export function normalizeExtractedData(data: unknown): NormalizedField[] {
  if (!data || typeof data !== 'object') return []

  const record = data as Record<string, unknown>

  if ('fields' in record && typeof record.fields === 'object' && record.fields !== null) {
    const inner = record.fields as Record<string, unknown>
    return Object.entries(inner).map(([name, raw]) => ({
      name: formatFieldName(name),
      value: extractFieldValue(raw, name),
    }))
  }

  const skipKeys = new Set(['classification', 'summary'])
  return Object.entries(record)
    .filter(([key]) => !skipKeys.has(key))
    .map(([name, value]) => ({
      name: formatFieldName(name),
      value: formatValue(value, name),
    }))
}

function extractFieldValue(raw: unknown, fieldName: string): string {
  if (!raw || typeof raw !== 'object') return formatValue(raw, fieldName)

  if (Array.isArray(raw)) {
    const flattened = flattenArray(raw)
    return flattened.map((v) => formatValue(v, fieldName)).join(', ')
  }

  const obj = raw as Record<string, unknown>
  if ('value' in obj) return extractFieldValue(obj.value, fieldName)
  return formatValue(raw, fieldName)
}

function flattenArray(arr: unknown[]): unknown[] {
  const result: unknown[] = []
  for (const item of arr) {
    if (Array.isArray(item)) {
      result.push(...flattenArray(item))
    } else {
      result.push(item)
    }
  }
  return result
}

export function formatValue(value: unknown, fieldName?: string): string {
  if (value === null || value === undefined) return '-'

  if (typeof value === 'string') {
    if (isIsoDate(value)) return formatPtBrDate(value)
    if (isCpf(fieldName)) return formatCpf(value)
    if (isCnpj(fieldName)) return formatCnpj(value)
    if (isCurrency(fieldName)) return formatCurrency(value)
    return value
  }

  if (typeof value === 'number') {
    if (isCurrency(fieldName)) return formatCurrency(String(value))
    if (isCpf(fieldName) || isCnpj(fieldName)) return String(value)
    if (isDecimal(value)) return value.toLocaleString('pt-BR', { minimumFractionDigits: 2, maximumFractionDigits: 2 })
    return value.toLocaleString('pt-BR')
  }

  if (Array.isArray(value)) {
    const flattened = flattenArray(value)
    return flattened.map((v) => formatValue(v, fieldName)).join(', ')
  }

  if (typeof value === 'boolean') return value ? 'Sim' : 'Não'

  if (typeof value === 'object') {
    const obj = value as Record<string, unknown>
    if ('value' in obj) {
      if (obj.value === null || obj.value === undefined) return '-'
      return formatValue(obj.value, fieldName)
    }
    return '-'
      .replace(/[{}"\\]/g, '')
      .replace(/,/g, ', ')
      .replace(/:/g, ': ')
      .trim() || '-'
  }

  return String(value)
}

function isIsoDate(value: string): boolean {
  return /^\d{4}-\d{2}-\d{2}T/.test(value) && !isNaN(Date.parse(value))
}

function isCpf(fieldName?: string): boolean {
  return !!fieldName && /cpf/i.test(fieldName) && !/cnpj/i.test(fieldName)
}

function isCnpj(fieldName?: string): boolean {
  return !!fieldName && /cnpj/i.test(fieldName)
}

function isCurrency(fieldName?: string): boolean {
  return !!fieldName && /(valor|preco|custo|total|montante|saldo|pagamento)/i.test(fieldName)
}

function isDecimal(value: number): boolean {
  return value % 1 !== 0
}

function formatPtBrDate(value: string): string {
  try {
    const [datePart] = value.split('T')
    const [year, month, day] = datePart.split('-')
    if (year && month && day) {
      const date = new Date(Number(year), Number(month) - 1, Number(day))
      if (!isNaN(date.getTime())) {
        return date.toLocaleDateString('pt-BR', { day: '2-digit', month: '2-digit', year: 'numeric' })
      }
    }
    return value
  } catch {
    return value
  }
}

function formatCpf(value: string): string {
  const digits = value.replace(/\D/g, '')
  if (digits.length === 11) return digits.replace(/(\d{3})(\d{3})(\d{3})(\d{2})/, '$1.$2.$3-$4')
  return value
}

function formatCnpj(value: string): string {
  const digits = value.replace(/\D/g, '')
  if (digits.length === 14) return digits.replace(/(\d{2})(\d{3})(\d{3})(\d{4})(\d{2})/, '$1.$2.$3/$4-$5')
  return value
}

function formatCurrency(value: string): string {
  const num = parseFloat(value.replace(/[^\d,.-]/g, '').replace(',', '.'))
  if (isNaN(num)) return value
  return num.toLocaleString('pt-BR', { style: 'currency', currency: 'BRL' })
}
