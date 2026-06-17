import { z } from 'zod'

export const uploadSchema = z.object({
  file: z
    .instanceof(File, { message: 'Arquivo é obrigatório' })
    .refine((f) => f.type === 'application/pdf' || f.name.toLowerCase().endsWith('.pdf'), {
      message: 'Apenas arquivos PDF são aceitos',
    })
    .refine((f) => f.size <= 10 * 1024 * 1024, {
      message: 'Arquivo deve ter no máximo 10 MB',
    }),
})

export type UploadFormData = z.infer<typeof uploadSchema>
