import { useQuery } from '@tanstack/react-query'
import { documentClassesService } from '@/services/document-classes.service'
import { resolveDocumentTypeLabel } from '@/lib/mappers/document-type'

interface DocumentTypeLabelProps {
  documentType: string | null | undefined
}

export function DocumentTypeLabel({ documentType }: DocumentTypeLabelProps) {
  const { data } = useQuery({
    queryKey: ['document-classes-catalog'],
    queryFn: async () => {
      const result = await documentClassesService.getAll()
      return result.items
    },
    staleTime: 10 * 60 * 1000,
  })

  const catalog = data ?? []
  const label = resolveDocumentTypeLabel(documentType, catalog)

  return <span>{label}</span>
}
