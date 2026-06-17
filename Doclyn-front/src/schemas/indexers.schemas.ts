import { z } from 'zod'

export const createIndexerSchema = z.object({
  name: z.string().min(1, 'Nome é obrigatório'),
  displayName: z.string().min(1, 'Nome de exibição é obrigatório'),
  description: z.string().optional(),
  dataType: z.number().min(0, 'Tipo é obrigatório'),
  isRequired: z.boolean(),
  isMultiple: z.boolean(),
  extractionHint: z.string().nullable().optional(),
  regexPattern: z.string().nullable().optional(),
})

export const updateIndexerSchema = createIndexerSchema

export type CreateIndexerFormData = z.infer<typeof createIndexerSchema>
export type UpdateIndexerFormData = z.infer<typeof updateIndexerSchema>
