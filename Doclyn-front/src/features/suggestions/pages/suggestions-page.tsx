import { useState } from 'react'
import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query'
import { suggestionsService } from '@/services/suggestions.service'
import { PageHeader } from '@/components/shared/page-header'
import { EmptyState } from '@/components/shared/empty-state'
import { TableSkeleton } from '@/components/shared/loading-skeleton'
import { ConfirmDialog } from '@/components/shared/confirm-dialog'
import { Table, TableBody, TableCell, TableHead, TableHeader, TableRow } from '@/components/ui/table'
import { Card, CardContent } from '@/components/ui/card'
import { Badge } from '@/components/ui/badge'
import { Button } from '@/components/ui/button'
import { ConfidenceBadge } from '@/components/shared/confidence-badge'
import { useToast } from '@/components/ui/toaster'
import { Lightbulb, Check, X } from 'lucide-react'

export function SuggestionsPage() {
  const queryClient = useQueryClient()
  const { toast } = useToast()
  const [approveId, setApproveId] = useState<string | null>(null)
  const [rejectId, setRejectId] = useState<string | null>(null)

  const { data, isLoading } = useQuery({
    queryKey: ['suggestions'],
    queryFn: () => suggestionsService.getAll(),
  })

  const approveMutation = useMutation({
    mutationFn: (id: string) => suggestionsService.approve(id),
    onSuccess: () => { toast('success', 'Sugestão aprovada.'); setApproveId(null); queryClient.invalidateQueries({ queryKey: ['suggestions'] }) },
    onError: () => { toast('error', 'Erro ao aprovar.'); setApproveId(null) },
  })

  const rejectMutation = useMutation({
    mutationFn: (id: string) => suggestionsService.reject(id),
    onSuccess: () => { toast('success', 'Sugestão rejeitada.'); setRejectId(null); queryClient.invalidateQueries({ queryKey: ['suggestions'] }) },
    onError: () => { toast('error', 'Erro ao rejeitar.'); setRejectId(null) },
  })

  return (
    <div className="space-y-6">
      <PageHeader
        title="Sugestões de aprendizado"
        description="O Doclyn encontrou possíveis novos campos recorrentes em documentos semelhantes. Aprove sugestões úteis para melhorar futuras extrações."
      />

      <Card>
        <CardContent className="pt-6">
          {isLoading ? (
            <TableSkeleton rows={3} />
          ) : !data?.items?.length ? (
            <EmptyState title="Nenhuma sugestão pendente no momento." icon={Lightbulb} description="O sistema analisará documentos processados e sugerirá novos indexadores automaticamente." />
          ) : (
            <Table>
              <TableHeader>
                <TableRow>
                  <TableHead>Nome sugerido</TableHead>
                  <TableHead>Classe documental</TableHead>
                  <TableHead>Ocorrências</TableHead>
                  <TableHead>Confiança média</TableHead>
                  <TableHead>Status</TableHead>
                  <TableHead>Ações</TableHead>
                </TableRow>
              </TableHeader>
              <TableBody>
                {data.items.map((s) => (
                  <TableRow key={s.id}>
                    <TableCell className="font-medium">{s.suggestedName}</TableCell>
                    <TableCell>{s.documentClass}</TableCell>
                    <TableCell>{s.occurrences}</TableCell>
                    <TableCell><ConfidenceBadge value={s.averageConfidence} /></TableCell>
                    <TableCell>
                      <Badge variant={s.status === 'Approved' ? 'success' : s.status === 'Rejected' ? 'destructive' : 'warning'}>
                        {s.status === 'Pending' ? 'Pendente' : s.status === 'Approved' ? 'Aprovada' : 'Rejeitada'}
                      </Badge>
                    </TableCell>
                    <TableCell>
                      {s.status === 'Pending' && (
                        <div className="flex gap-1">
                          <Button variant="ghost" size="sm" onClick={() => setApproveId(s.id)}>
                            <Check className="mr-1 h-4 w-4 text-emerald-600" /> Aprovar
                          </Button>
                          <Button variant="ghost" size="sm" onClick={() => setRejectId(s.id)}>
                            <X className="mr-1 h-4 w-4 text-red-600" /> Rejeitar
                          </Button>
                        </div>
                      )}
                    </TableCell>
                  </TableRow>
                ))}
              </TableBody>
            </Table>
          )}
        </CardContent>
      </Card>

      <ConfirmDialog open={!!approveId} onOpenChange={(o) => { if (!o) setApproveId(null) }} title="Aprovar sugestão" description="O indexador será adicionado ao catálogo da classe documental." confirmLabel="Aprovar" variant="default" isLoading={approveMutation.isPending} onConfirm={() => approveId && approveMutation.mutate(approveId)} />
      <ConfirmDialog open={!!rejectId} onOpenChange={(o) => { if (!o) setRejectId(null) }} title="Rejeitar sugestão" description="A sugestão será descartada." confirmLabel="Rejeitar" variant="destructive" isLoading={rejectMutation.isPending} onConfirm={() => rejectId && rejectMutation.mutate(rejectId)} />
    </div>
  )
}
