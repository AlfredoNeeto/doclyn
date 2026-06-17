import { z } from 'zod'

export const approveSuggestionSchema = z.object({
  suggestionId: z.string().min(1),
})

export const rejectSuggestionSchema = z.object({
  suggestionId: z.string().min(1),
})

export type ApproveSuggestionData = z.infer<typeof approveSuggestionSchema>
export type RejectSuggestionData = z.infer<typeof rejectSuggestionSchema>
