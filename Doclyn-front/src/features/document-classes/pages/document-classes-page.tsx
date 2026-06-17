import { useQuery } from '@tanstack/react-query'
import { useNavigate } from 'react-router-dom'
import { documentClassesService } from '@/services/document-classes.service'
import { PageHeader } from '@/components/shared/page-header'
import { EmptyState } from '@/components/shared/empty-state'
import { FeedbackAlert } from '@/components/shared/feedback-alert'
import { TableSkeleton } from '@/components/shared/loading-skeleton'
import { Table, TableBody, TableCell, TableHead, TableHeader, TableRow } from '@/components/ui/table'
import { Card, CardContent } from '@/components/ui/card'
import { Badge } from '@/components/ui/badge'
import { Button } from '@/components/ui/button'
import { Eye, Tags } from 'lucide-react'
import { ROUTES } from '@/lib/constants/routes'

export function DocumentClassesPage() {
  const navigate = useNavigate()
  const { data, isLoading, error } = useQuery({
    queryKey: ['document-classes'],
    queryFn: () => documentClassesService.getAll(),
  })

  if (error) {
    return (
      <div className="space-y-6">
        <PageHeader title="Classes documentais" />
        <FeedbackAlert
          variant="destructive"
          title="Erro ao carregar classes"
          description="Verifique sua conexão com o servidor."
        />
        <Button variant="outline" onClick={() => window.location.reload()}>Tentar novamente</Button>
      </div>
    )
  }

  return (
    <div className="space-y-6">
      <PageHeader title="Classes documentais" description="Catálogo de tipos documentais conhecidos pelo sistema" />

      <Card>
        <CardContent className="pt-6">
          {isLoading ? (
            <TableSkeleton rows={4} />
          ) : !data?.items?.length ? (
            <EmptyState title="Nenhuma classe documental cadastrada." icon={Tags} />
          ) : (
            <Table>
              <TableHeader>
                <TableRow>
                  <TableHead>Nome</TableHead>
                  <TableHead>Nome técnico</TableHead>
                  <TableHead>Grupo</TableHead>
                  <TableHead>Subgrupo</TableHead>
                  <TableHead>Status</TableHead>
                  <TableHead>Ações</TableHead>
                </TableRow>
              </TableHeader>
              <TableBody>
                {data.items.map((cls) => (
                  <TableRow key={cls.id}>
                    <TableCell className="font-medium">{cls.displayName}</TableCell>
                    <TableCell className="text-xs text-muted-foreground font-mono">{cls.name}</TableCell>
                    <TableCell>{cls.group || '-'}</TableCell>
                    <TableCell>{cls.subGroup || '-'}</TableCell>
                    <TableCell>
                      <Badge variant={cls.isActive ? 'success' : 'secondary'}>
                        {cls.isActive ? 'Ativa' : 'Inativa'}
                      </Badge>
                    </TableCell>
                    <TableCell>
                      <Button variant="ghost" size="sm" onClick={() => navigate(`${ROUTES.DOCUMENT_CLASSES}/${cls.id}`)}>
                        <Eye className="mr-1 h-4 w-4" /> Detalhes
                      </Button>
                    </TableCell>
                  </TableRow>
                ))}
              </TableBody>
            </Table>
          )}
        </CardContent>
      </Card>
    </div>
  )
}
