import { useQuery } from '@tanstack/react-query'
import { useNavigate } from 'react-router-dom'
import { dashboardService } from '@/services/dashboard.service'
import { PageHeader } from '@/components/shared/page-header'
import { MetricCard } from '@/components/shared/metric-card'
import { StatusBadge } from '@/components/shared/status-badge'
import { EmptyState } from '@/components/shared/empty-state'
import { LoadingSkeleton } from '@/components/shared/loading-skeleton'
import { Table, TableBody, TableCell, TableHead, TableHeader, TableRow } from '@/components/ui/table'
import { Card, CardContent, CardHeader, CardTitle } from '@/components/ui/card'
import { Badge } from '@/components/ui/badge'
import { Button } from '@/components/ui/button'
import { formatDate } from '@/lib/formatters/date'
import { formatConfidence } from '@/lib/formatters/confidence'
import { SeverityBadge } from '@/components/shared/severity-badge'
import { FileText, CheckCircle, Clock, AlertTriangle, Upload, Eye, Tags, ShieldCheck, Lightbulb } from 'lucide-react'
import { ROUTES } from '@/lib/constants/routes'

export function DashboardPage() {
  const navigate = useNavigate()
  const { data, isLoading } = useQuery({
    queryKey: ['dashboard'],
    queryFn: () => dashboardService.getData(),
    refetchInterval: 5000,
    refetchIntervalInBackground: false,
  })

  if (isLoading) return <LoadingSkeleton />

  const docs = data?.documents
  const quality = data?.quality
  const insights = data?.insights
  const classes = data?.classes
  const attention = data?.attentionRequired ?? []

  return (
    <div className="space-y-6">
      <PageHeader title="Dashboard" description="Visão geral da plataforma" />

      <div className="grid gap-4 md:grid-cols-2 lg:grid-cols-4">
        <MetricCard title="Documentos enviados" value={docs?.total ?? 0} icon={FileText} />
        <MetricCard title="Pendentes" value={docs?.pending ?? 0} icon={Clock} description="aguardando processamento" />
        <MetricCard title="Processados" value={docs?.processed ?? 0} icon={CheckCircle} description="com sucesso" trend="up" />
        <MetricCard title="Falhou" value={docs?.failed ?? 0} icon={AlertTriangle} description="precisam de atenção" trend="down" />
      </div>

      {attention.length > 0 && (
        <Card className="border-amber-200 bg-amber-50/50 dark:border-amber-800 dark:bg-amber-950/20">
          <CardHeader className="flex flex-row items-center justify-between pb-2">
            <CardTitle className="text-base flex items-center gap-2">
              <AlertTriangle className="h-4 w-4 text-amber-600" />
              Atenção necessária ({attention.length})
            </CardTitle>
          </CardHeader>
          <CardContent>
            <div className="space-y-2">
              {attention.map((item) => (
                <div key={item.documentId} className="flex items-center justify-between rounded-md bg-background p-3 shadow-sm">
                  <div className="min-w-0 flex-1">
                    <p className="text-sm font-medium truncate">{item.fileName}</p>
                    <p className="text-xs text-muted-foreground">{item.reason}</p>
                  </div>
                  <div className="flex items-center gap-2 ml-3 shrink-0">
                    <SeverityBadge severity={item.severity} />
                    <Button variant="ghost" size="sm" onClick={() => navigate(`${ROUTES.DOCUMENTS}/${item.documentId}`)}>
                      <Eye className="h-4 w-4" />
                    </Button>
                  </div>
                </div>
              ))}
            </div>
          </CardContent>
        </Card>
      )}

      <div className="grid gap-6 lg:grid-cols-2">
        <Card>
          <CardHeader className="flex flex-row items-center justify-between">
            <CardTitle className="text-base">Últimos documentos</CardTitle>
            <Button variant="ghost" size="sm" onClick={() => navigate(ROUTES.DOCUMENTS)}>Ver todos</Button>
          </CardHeader>
          <CardContent>
            {!data?.recentDocuments?.length ? (
              <EmptyState
                title="Nenhum documento ainda"
                description="Envie seu primeiro documento para começar."
                icon={Upload}
                action={<Button size="sm" onClick={() => navigate(ROUTES.UPLOAD)}><Upload className="mr-1 h-4 w-4" /> Enviar documento</Button>}
              />
            ) : (
              <Table>
                <TableHeader>
                  <TableRow>
                    <TableHead>Arquivo</TableHead>
                    <TableHead>Status</TableHead>
                    <TableHead>Data</TableHead>
                    <TableHead></TableHead>
                  </TableRow>
                </TableHeader>
                <TableBody>
                  {data.recentDocuments.map((doc) => (
                    <TableRow key={doc.id}>
                      <TableCell className="font-medium max-w-[200px] truncate">{doc.fileName}</TableCell>
                      <TableCell><StatusBadge status={doc.documentStatus} /></TableCell>
                      <TableCell className="text-muted-foreground">{formatDate(doc.createdAt)}</TableCell>
                      <TableCell>
                        <Button variant="ghost" size="sm" onClick={() => navigate(`${ROUTES.DOCUMENTS}/${doc.id}`)}>
                          <Eye className="h-4 w-4" />
                        </Button>
                      </TableCell>
                    </TableRow>
                  ))}
                </TableBody>
              </Table>
            )}
          </CardContent>
        </Card>

        <div className="space-y-6">
          <Card>
            <CardHeader><CardTitle className="text-base flex items-center gap-2"><Tags className="h-4 w-4" /> Classes documentais</CardTitle></CardHeader>
            <CardContent>
              {!classes?.mostUsed?.length ? (
                <EmptyState title="Nenhuma classe identificada ainda." icon={Tags} />
              ) : (
                <div className="space-y-3">
                  {classes.mostUsed.map((cls) => (
                    <div key={cls.id || cls.name} className="flex items-center justify-between">
                      <div>
                        <p className="text-sm font-medium">{cls.displayName}</p>
                        <p className="text-xs text-muted-foreground font-mono">{cls.name}</p>
                      </div>
                      <Badge variant="secondary">{cls.documentsCount} docs</Badge>
                    </div>
                  ))}
                </div>
              )}
            </CardContent>
          </Card>

          <Card>
            <CardHeader><CardTitle className="text-base flex items-center gap-2"><ShieldCheck className="h-4 w-4" /> Qualidade dos dados</CardTitle></CardHeader>
            <CardContent className="grid grid-cols-2 gap-3">
              <div>
                <p className="text-xs text-muted-foreground">Confiança média</p>
                <p className="text-lg font-bold">{formatConfidence(quality?.averageConfidence)}</p>
              </div>
              <div>
                <p className="text-xs text-muted-foreground">Campos validados</p>
                <p className="text-lg font-bold text-emerald-600">{quality?.fieldsValidated ?? 0}</p>
              </div>
              <div>
                <p className="text-xs text-muted-foreground">Precisam de revisão</p>
                <p className="text-lg font-bold text-amber-600">{quality?.fieldsNeedsReview ?? 0}</p>
              </div>
              <div>
                <p className="text-xs text-muted-foreground">Rejeitados</p>
                <p className="text-lg font-bold text-red-600">{quality?.fieldsRejected ?? 0}</p>
              </div>
            </CardContent>
          </Card>

          <Card>
            <CardHeader><CardTitle className="text-base flex items-center gap-2"><Lightbulb className="h-4 w-4" /> Insights</CardTitle></CardHeader>
            <CardContent className="grid grid-cols-2 gap-3">
              <div>
                <p className="text-xs text-muted-foreground">Total</p>
                <p className="text-lg font-bold">{insights?.total ?? 0}</p>
              </div>
              <div>
                <p className="text-xs text-muted-foreground">Críticos</p>
                <p className="text-lg font-bold text-red-600">{insights?.critical ?? 0}</p>
              </div>
              <div>
                <p className="text-xs text-muted-foreground">Atenção</p>
                <p className="text-lg font-bold text-amber-600">{insights?.warning ?? 0}</p>
              </div>
              <div>
                <p className="text-xs text-muted-foreground">Informativos</p>
                <p className="text-lg font-bold text-blue-600">{insights?.info ?? 0}</p>
              </div>
            </CardContent>
          </Card>
        </div>
      </div>
    </div>
  )
}
