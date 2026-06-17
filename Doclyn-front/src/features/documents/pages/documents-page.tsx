import React, { useState } from 'react'
import { useNavigate } from 'react-router-dom'
import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query'
import { documentsService } from '@/services/documents.service'
import type { DocumentQueryParams } from '@/types/documents'
import { PageHeader } from '@/components/shared/page-header'
import { StatusBadge } from '@/components/shared/status-badge'
import { EmptyState } from '@/components/shared/empty-state'
import { FeedbackAlert } from '@/components/shared/feedback-alert'
import { TableSkeleton } from '@/components/shared/loading-skeleton'
import { Table, TableBody, TableCell, TableHead, TableHeader, TableRow } from '@/components/ui/table'
import { Button } from '@/components/ui/button'
import { Input } from '@/components/ui/input'
import { Select } from '@/components/ui/select'
import { Card, CardContent } from '@/components/ui/card'
import { Pagination } from '@/components/ui/pagination'
import { ConfirmDialog } from '@/components/shared/confirm-dialog'
import { DocumentTypeLabel } from '@/components/shared/document-type-label'
import { Upload, Eye, Search, FileText, Trash2, Download } from 'lucide-react'
import { formatDate } from '@/lib/formatters/date'
import { ROUTES } from '@/lib/constants/routes'
import { useToast } from '@/components/ui/toaster'
import { triggerDownload } from '@/lib/download-file'

export function DocumentsPage() {
  const navigate = useNavigate()
  const [params, setParams] = useState<DocumentQueryParams>({ page: 1, pageSize: 10 })
  const [search, setSearch] = useState('')
  const { toast } = useToast()
  const queryClient = useQueryClient()
  const [deleteId, setDeleteId] = useState<string | null>(null)

  const { data, isLoading, error } = useQuery({
    queryKey: ['documents', params],
    queryFn: () => documentsService.getAll(params),
    refetchInterval(query) {
      const items = (query.state.data as { items?: Array<{ documentStatus: string }> } | undefined)?.items
      if (!items?.length) return false
      const hasActive = items.some(
        (d) => d.documentStatus === 'Pending' || d.documentStatus === 'Processing'
      )
      return hasActive ? 5000 : false
    },
    refetchIntervalInBackground: false,
  })

  const deleteMutation = useMutation({
    mutationFn: (id: string) => documentsService.delete(id),
    onSuccess: () => {
      toast('success', 'Documento excluído.')
      setDeleteId(null)
      queryClient.invalidateQueries({ queryKey: ['documents'] })
      queryClient.invalidateQueries({ queryKey: ['dashboard'] })
    },
    onError: () => {
      toast('error', 'Erro ao excluir documento.')
      setDeleteId(null)
    },
  })

  const handleSearch = () => {
    setParams((p) => ({ ...p, search: search || undefined, page: 1 }))
  }

  if (error) {
    return (
      <div className="space-y-6">
        <PageHeader title="Documentos" />
        <FeedbackAlert
          variant="destructive"
          title="Erro ao carregar documentos"
          description="Não foi possível conectar ao servidor. Verifique sua conexão."
        />
        <Button variant="outline" onClick={() => window.location.reload()}>Tentar novamente</Button>
      </div>
    )
  }

  return (
    <div className="space-y-6">
      <PageHeader
        title="Documentos"
        description="Gerencie e acompanhe seus documentos"
      >
        <Button onClick={() => navigate(ROUTES.UPLOAD)}>
          <Upload className="mr-2 h-4 w-4" />
          Enviar documento
        </Button>
      </PageHeader>

      <Card>
        <CardContent className="pt-6">
          <div className="flex flex-wrap gap-3 mb-4">
            <div className="flex-1 min-w-[200px]">
              <div className="relative">
                <Search className="absolute left-3 top-1/2 h-4 w-4 -translate-y-1/2 text-muted-foreground" />
                <Input
                  placeholder="Buscar por nome..."
                  value={search}
                  onChange={(e) => setSearch(e.target.value)}
                  onKeyDown={(e) => e.key === 'Enter' && handleSearch()}
                  className="pl-9"
                />
              </div>
            </div>
            <Select
              value={params.status ?? ''}
              onChange={(e: React.ChangeEvent<HTMLSelectElement>) => setParams((p) => ({ ...p, status: e.target.value || undefined, page: 1 }))}
              className="w-[160px]"
            >
              <option value="">Todos os status</option>
              <option value="Pending">Pendente</option>
              <option value="Processing">Processando</option>
              <option value="Processed">Processado</option>
              <option value="Failed">Falhou</option>
            </Select>
          </div>

          {isLoading ? (
            <TableSkeleton rows={5} />
          ) : !data?.items?.length ? (
            <EmptyState
              title="Nenhum documento enviado ainda"
              description="Faça upload do primeiro PDF para iniciar a análise documental."
              icon={FileText}
              action={
                <Button onClick={() => navigate(ROUTES.UPLOAD)}>
                  <Upload className="mr-2 h-4 w-4" /> Enviar documento
                </Button>
              }
            />
          ) : (
            <>
              <Table>
                <TableHeader>
                  <TableRow>
                    <TableHead>Nome do arquivo</TableHead>
                    <TableHead>Tipo do documento</TableHead>
                    <TableHead>Status</TableHead>
                    <TableHead>Data de envio</TableHead>
                    <TableHead>Ações</TableHead>
                  </TableRow>
                </TableHeader>
                <TableBody>
                  {data.items.map((doc) => (
                    <TableRow key={doc.id}>
                      <TableCell className="font-medium">{doc.fileName}</TableCell>
                      <TableCell><DocumentTypeLabel documentType={doc.documentType} /></TableCell>
                      <TableCell><StatusBadge status={doc.documentStatus} /></TableCell>
                      <TableCell className="text-muted-foreground">{formatDate(doc.createdAt)}</TableCell>
                      <TableCell>
                        <div className="flex items-center gap-1">
                          <Button variant="ghost" size="sm" onClick={() => navigate(`${ROUTES.DOCUMENTS}/${doc.id}`)}>
                            <Eye className="mr-1 h-4 w-4" />
                            Ver detalhes
                          </Button>
                          <Button variant="ghost" size="icon" onClick={async () => {
                            try {
                              const blob = await documentsService.download(doc.id)
                              triggerDownload(blob, doc.fileName)
                            } catch {
                              toast('error', 'Não foi possível baixar o documento.')
                            }
                          }}>
                            <Download className="h-4 w-4" />
                          </Button>
                          <Button variant="ghost" size="icon" onClick={() => setDeleteId(doc.id)}>
                            <Trash2 className="h-4 w-4 text-destructive" />
                          </Button>
                        </div>
                      </TableCell>
                    </TableRow>
                  ))}
                </TableBody>
              </Table>
              <div className="mt-4 flex justify-center">
                <Pagination
                  page={data.page}
                  totalPages={data.totalPages}
                  onPageChange={(page) => setParams((p) => ({ ...p, page }))}
                />
              </div>
            </>
          )}
        </CardContent>
      </Card>
      <ConfirmDialog
        open={!!deleteId}
        onOpenChange={(open) => { if (!open) setDeleteId(null) }}
        title="Excluir documento"
        description="O documento será removido da sua lista de documentos ativos."
        confirmLabel="Excluir"
        variant="destructive"
        isLoading={deleteMutation.isPending}
        onConfirm={() => deleteId && deleteMutation.mutate(deleteId)}
      />
    </div>
  )
}
