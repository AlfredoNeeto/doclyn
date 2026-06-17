import { useParams, useNavigate } from 'react-router-dom'
import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query'
import { documentsService } from '@/services/documents.service'
import { PageHeader } from '@/components/shared/page-header'
import { StatusBadge } from '@/components/shared/status-badge'
import { DocumentTypeLabel } from '@/components/shared/document-type-label'
import { DocumentInsightCard } from '@/components/shared/document-insight-card'
import { ExtractedFieldTable } from '@/components/shared/extracted-field-table'
import { ProcessingTimeline } from '@/components/shared/processing-timeline'
import { JsonViewer } from '@/components/shared/json-viewer'
import { LoadingSkeleton } from '@/components/shared/loading-skeleton'
import { ConfirmDialog } from '@/components/shared/confirm-dialog'
import { Table, TableBody, TableCell, TableHead, TableHeader, TableRow } from '@/components/ui/table'
import { Card, CardContent } from '@/components/ui/card'
import { Tabs, TabsContent, TabsList, TabsTrigger } from '@/components/ui/tabs'
import { Button } from '@/components/ui/button'
import { useToast } from '@/components/ui/toaster'
import { formatDateTime } from '@/lib/formatters/date'
import { ArrowLeft, RefreshCw, Trash2, Download, Loader2, AlertTriangle, Lightbulb, Database, Clock, FileCode, FileSearch } from 'lucide-react'
import { useState } from 'react'
import { ROUTES } from '@/lib/constants/routes'
import { normalizeExtractedData } from '@/features/documents/lib/normalize-extracted-data'
import { extractDocumentSummary } from '@/features/documents/lib/extract-document-summary'
import { triggerDownload } from '@/lib/download-file'

export function DocumentDetailPage() {
  const { id } = useParams<{ id: string }>()
  const navigate = useNavigate()
  const { toast } = useToast()
  const queryClient = useQueryClient()
  const [reprocessOpen, setReprocessOpen] = useState(false)
  const [deleteOpen, setDeleteOpen] = useState(false)

  const { data: document, isLoading, error } = useQuery({
    queryKey: ['document', id],
    queryFn: () => documentsService.getById(id!),
    enabled: !!id,
    refetchInterval(query) {
      const doc = query.state.data as { documentStatus: string } | undefined
      if (!doc) return 5000
      return (doc.documentStatus === 'Pending' || doc.documentStatus === 'Processing') ? 5000 : false
    },
    refetchIntervalInBackground: false,
  })

  const { data: extractedData } = useQuery({
    queryKey: ['document-extracted-data', id],
    queryFn: () => documentsService.getExtractedData(id!),
    enabled: !!id,
    refetchInterval: 5000,
    refetchIntervalInBackground: false,
  })

  const { data: reviewFields } = useQuery({
    queryKey: ['document-review-fields', id],
    queryFn: () => documentsService.getReviewFields(id!),
    enabled: !!id,
    refetchInterval: 5000,
    refetchIntervalInBackground: false,
  })

  const { data: insights } = useQuery({
    queryKey: ['document-insights', id],
    queryFn: () => documentsService.getInsights(id!),
    enabled: !!id,
    refetchInterval: 5000,
    refetchIntervalInBackground: false,
  })

  const { data: logs } = useQuery({
    queryKey: ['document-logs', id],
    queryFn: () => documentsService.getLogs(id!),
    enabled: !!id,
    refetchInterval: 5000,
    refetchIntervalInBackground: false,
  })

  const reprocessMutation = useMutation({
    mutationFn: () => documentsService.reprocess(id!),
    onSuccess: () => {
      toast('success', 'Reprocessamento iniciado.')
      setReprocessOpen(false)
      queryClient.invalidateQueries({ queryKey: ['document', id] })
      queryClient.invalidateQueries({ queryKey: ['document-logs', id] })
      queryClient.invalidateQueries({ queryKey: ['document-extracted-data', id] })
      queryClient.invalidateQueries({ queryKey: ['document-review-fields', id] })
      queryClient.invalidateQueries({ queryKey: ['document-insights', id] })
      queryClient.invalidateQueries({ queryKey: ['documents'] })
      queryClient.invalidateQueries({ queryKey: ['dashboard'] })
    },
    onError: () => {
      toast('error', 'Erro ao reprocessar documento.')
      setReprocessOpen(false)
    },
  })

  const deleteMutation = useMutation({
    mutationFn: () => documentsService.delete(id!),
    onSuccess: () => {
      toast('success', 'Documento excluído.')
      setDeleteOpen(false)
      queryClient.invalidateQueries({ queryKey: ['documents'] })
      queryClient.invalidateQueries({ queryKey: ['dashboard'] })
      navigate(ROUTES.DOCUMENTS)
    },
    onError: () => {
      toast('error', 'Erro ao excluir documento.')
      setDeleteOpen(false)
    },
  })

  if (isLoading) return <LoadingSkeleton />

  if (error || !document) {
    return (
      <div className="space-y-6">
        <PageHeader title="Documento" />
        <Card>
          <CardContent className="flex flex-col items-center py-12 text-center">
            <FileSearch className="h-12 w-12 text-muted-foreground/40 mb-4" />
            <h3 className="text-lg font-semibold">Documento não encontrado</h3>
            <p className="text-sm text-muted-foreground mt-1 mb-4">O documento solicitado não existe ou você não tem permissão para acessá-lo.</p>
            <Button onClick={() => navigate(ROUTES.DOCUMENTS)}><ArrowLeft className="mr-2 h-4 w-4" /> Voltar para documentos</Button>
          </CardContent>
        </Card>
      </div>
    )
  }

  const summaryText = extractDocumentSummary(extractedData, insights)
  const extractedFields = normalizeExtractedData(extractedData?.data)
  const reviewItems = reviewFields?.fields?.filter(
    (f) => f.validationStatus === 'NeedsReview' || f.validationStatus === 'Rejected'
  ) ?? []

  const hasSummary = !!summaryText
  const hasInsights = !!(insights && insights.length > 0)
  const hasExtractedData = extractedFields.length > 0
  const hasReviewFields = reviewItems.length > 0
  const hasLogs = !!(logs && logs.length > 0)

  const defaultTab = hasSummary ? 'summary'
    : hasInsights ? 'insights'
    : hasExtractedData ? 'data'
    : hasReviewFields ? 'review'
    : hasLogs ? 'logs'
    : 'info'

  return (
    <div className="space-y-6">
      <PageHeader title={document.fileName}>
        <Button variant="outline" onClick={() => navigate(ROUTES.DOCUMENTS)}>
          <ArrowLeft className="mr-2 h-4 w-4" /> Voltar
        </Button>
        <Button variant="outline" onClick={async () => {
          try {
            const blob = await documentsService.download(id!)
            triggerDownload(blob, document.fileName)
          } catch {
            toast('error', 'Não foi possível baixar o documento.')
          }
        }}>
          <Download className="mr-2 h-4 w-4" /> Baixar
        </Button>
        <Button variant="outline" onClick={() => setReprocessOpen(true)} disabled={reprocessMutation.isPending}>
          {reprocessMutation.isPending ? <Loader2 className="mr-2 h-4 w-4 animate-spin" /> : <RefreshCw className="mr-2 h-4 w-4" />}
          Reprocessar
        </Button>
        <Button variant="outline" onClick={() => setDeleteOpen(true)} disabled={deleteMutation.isPending}>
          {deleteMutation.isPending ? <Loader2 className="mr-2 h-4 w-4 animate-spin" /> : <Trash2 className="mr-2 h-4 w-4" />}
          Excluir
        </Button>
      </PageHeader>

      <Card>
        <CardContent className="grid grid-cols-2 md:grid-cols-4 gap-4 p-6">
          <div>
            <p className="text-xs text-muted-foreground">Status</p>
            <StatusBadge status={document.documentStatus} />
          </div>
          <div>
            <p className="text-xs text-muted-foreground">Tipo identificado</p>
            <p className="text-sm font-medium"><DocumentTypeLabel documentType={document.documentType} /></p>
          </div>
          <div>
            <p className="text-xs text-muted-foreground">Data de envio</p>
            <p className="text-sm">{formatDateTime(document.createdAt)}</p>
          </div>
          <div>
            <p className="text-xs text-muted-foreground">Processado em</p>
            <p className="text-sm">{formatDateTime(document.processedAt)}</p>
          </div>
        </CardContent>
      </Card>

      <Tabs defaultValue={defaultTab}>
        <TabsList>
          {hasSummary && <TabsTrigger value="summary"><FileSearch className="mr-1 h-4 w-4" /> Resumo</TabsTrigger>}
          {hasInsights && <TabsTrigger value="insights"><Lightbulb className="mr-1 h-4 w-4" /> Insights ({insights!.length})</TabsTrigger>}
          {hasExtractedData && <TabsTrigger value="data"><Database className="mr-1 h-4 w-4" /> Dados extraídos</TabsTrigger>}
          {hasReviewFields && <TabsTrigger value="review"><AlertTriangle className="mr-1 h-4 w-4" /> Revisão ({reviewItems.length})</TabsTrigger>}
          {hasLogs && <TabsTrigger value="logs"><Clock className="mr-1 h-4 w-4" /> Logs ({logs!.length})</TabsTrigger>}
          <TabsTrigger value="info"><FileCode className="mr-1 h-4 w-4" /> Informações</TabsTrigger>
        </TabsList>

        {hasSummary && (
          <TabsContent value="summary" className="mt-4">
            <Card>
              <CardContent className="p-6">
                <p className="text-sm leading-relaxed">{summaryText}</p>
              </CardContent>
            </Card>
          </TabsContent>
        )}

        {hasInsights && (
          <TabsContent value="insights" className="mt-4">
            <div className="grid gap-4 md:grid-cols-2">
              {insights!.map((insight) => (
                <DocumentInsightCard key={insight.id} insight={insight} />
              ))}
            </div>
          </TabsContent>
        )}

        {hasExtractedData && (
          <TabsContent value="data" className="mt-4">
            <Card>
              <CardContent className="p-4">
                <Table>
                  <TableHeader>
                    <TableRow>
                      <TableHead>Campo</TableHead>
                      <TableHead>Valor</TableHead>
                    </TableRow>
                  </TableHeader>
                  <TableBody>
                    {extractedFields.map((field) => (
                      <TableRow key={field.name}>
                        <TableCell className="font-medium">{field.name}</TableCell>
                        <TableCell className="max-w-lg whitespace-normal break-words">{field.value}</TableCell>
                      </TableRow>
                    ))}
                  </TableBody>
                </Table>
              </CardContent>
            </Card>
          </TabsContent>
        )}

        {hasReviewFields && (
          <TabsContent value="review" className="mt-4">
            <Card>
              <CardContent className="p-4">
                <ExtractedFieldTable fields={reviewItems} />
              </CardContent>
            </Card>
          </TabsContent>
        )}

        {hasLogs && (
          <TabsContent value="logs" className="mt-4">
            <Card>
              <CardContent className="p-4">
                <ProcessingTimeline logs={logs!} />
              </CardContent>
            </Card>
          </TabsContent>
        )}

        <TabsContent value="info" className="mt-4">
          <Card>
            <CardContent className="p-4 space-y-3">
              <div>
                <p className="text-xs text-muted-foreground">ID do documento</p>
                <p className="text-sm font-mono">{document.id}</p>
              </div>
              <div>
                <p className="text-xs text-muted-foreground">Hash do arquivo</p>
                <p className="text-sm font-mono text-xs">{document.fileHash}</p>
              </div>
              {!hasExtractedData && !hasReviewFields && (
                <div>
                  <p className="text-xs text-muted-foreground">Dados extraídos</p>
                  <p className="text-sm text-muted-foreground">O documento ainda não foi processado ou os dados ainda não estão disponíveis.</p>
                </div>
              )}
              {extractedData && <JsonViewer data={{ document, extractedData, reviewFields, insights }} />}
            </CardContent>
          </Card>
        </TabsContent>
      </Tabs>

      <ConfirmDialog
        open={deleteOpen}
        onOpenChange={setDeleteOpen}
        title="Excluir documento"
        description="O documento será removido da sua lista de documentos ativos."
        confirmLabel="Excluir"
        variant="destructive"
        isLoading={deleteMutation.isPending}
        onConfirm={() => deleteMutation.mutate()}
      />

      <ConfirmDialog
        open={reprocessOpen}
        onOpenChange={setReprocessOpen}
        title="Reprocessar documento"
        description="O documento será enviado novamente para o pipeline de processamento. Esta ação pode levar alguns minutos."
        confirmLabel="Reprocessar"
        variant="default"
        isLoading={reprocessMutation.isPending}
        onConfirm={() => reprocessMutation.mutate()}
      />
    </div>
  )
}
