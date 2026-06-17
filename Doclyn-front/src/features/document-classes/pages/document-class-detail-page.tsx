import { useParams, useNavigate } from 'react-router-dom'
import { useQuery } from '@tanstack/react-query'
import { documentClassesService } from '@/services/document-classes.service'
import { PageHeader } from '@/components/shared/page-header'
import { EmptyState } from '@/components/shared/empty-state'
import { LoadingSkeleton } from '@/components/shared/loading-skeleton'
import { Card, CardContent, CardHeader, CardTitle } from '@/components/ui/card'
import { Badge } from '@/components/ui/badge'
import { Button } from '@/components/ui/button'
import { ArrowLeft, List } from 'lucide-react'
import { ROUTES } from '@/lib/constants/routes'

export function DocumentClassDetailPage() {
  const { id } = useParams<{ id: string }>()
  const navigate = useNavigate()

  const { data: cls, isLoading, error } = useQuery({
    queryKey: ['document-class', id],
    queryFn: () => documentClassesService.getById(id!),
    enabled: !!id,
  })

  const { data: examples } = useQuery({
    queryKey: ['document-class-examples', id],
    queryFn: () => documentClassesService.getExamples(id!),
    enabled: !!id,
  })

  if (isLoading) return <LoadingSkeleton />
  if (error || !cls) return <EmptyState title="Classe documental não encontrada" />

  return (
    <div className="space-y-6">
      <PageHeader title={cls.displayName}>
        <Button variant="outline" onClick={() => navigate(ROUTES.DOCUMENT_CLASSES)}>
          <ArrowLeft className="mr-2 h-4 w-4" /> Voltar
        </Button>
        <Button onClick={() => navigate(`${ROUTES.DOCUMENT_CLASSES}/${id}/indexers`)}>
          <List className="mr-2 h-4 w-4" /> Indexadores
        </Button>
      </PageHeader>

      <div className="grid gap-6 md:grid-cols-2">
        <Card>
          <CardHeader><CardTitle className="text-base">Informações da classe</CardTitle></CardHeader>
          <CardContent className="space-y-3">
            <div><p className="text-xs text-muted-foreground">Nome técnico</p><p className="text-sm font-mono">{cls.name}</p></div>
            <div><p className="text-xs text-muted-foreground">Nome amigável</p><p className="text-sm">{cls.displayName}</p></div>
            <div><p className="text-xs text-muted-foreground">Grupo</p><p className="text-sm">{cls.group || '-'}</p></div>
            <div><p className="text-xs text-muted-foreground">Subgrupo</p><p className="text-sm">{cls.subGroup || '-'}</p></div>
            <div><p className="text-xs text-muted-foreground">Descrição</p><p className="text-sm">{cls.description || '-'}</p></div>
            <div className="flex gap-2">
              <Badge variant={cls.isActive ? 'success' : 'secondary'}>{cls.isActive ? 'Ativa' : 'Inativa'}</Badge>
              {cls.isSystemDefined && <Badge variant="outline">Sistema</Badge>}
            </div>
          </CardContent>
        </Card>

        <Card>
          <CardHeader><CardTitle className="text-base">Exemplos ({examples?.length ?? 0})</CardTitle></CardHeader>
          <CardContent>
            {!examples?.length ? (
              <p className="text-sm text-muted-foreground">Nenhum exemplo associado a esta classe.</p>
            ) : (
              <div className="space-y-2">
                {examples.map((ex) => (
                  <div key={ex.id} className="flex items-center justify-between border-b pb-2">
                    <p className="text-sm">{ex.fileName}</p>
                    <Badge variant="secondary">{Math.round(ex.confidence * 100)}%</Badge>
                  </div>
                ))}
              </div>
            )}
          </CardContent>
        </Card>
      </div>
    </div>
  )
}
