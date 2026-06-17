import React, { useState } from 'react'
import { useParams, useNavigate } from 'react-router-dom'
import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query'
import { indexersService, documentClassesService } from '@/services/document-classes.service'
import type { CreateIndexerPayload } from '@/types/document-classes'
import { PageHeader } from '@/components/shared/page-header'
import { EmptyState } from '@/components/shared/empty-state'
import { LoadingSkeleton } from '@/components/shared/loading-skeleton'
import { ConfirmDialog } from '@/components/shared/confirm-dialog'
import { Table, TableBody, TableCell, TableHead, TableHeader, TableRow } from '@/components/ui/table'
import { Card, CardContent } from '@/components/ui/card'
import { Button } from '@/components/ui/button'
import { Input } from '@/components/ui/input'
import { Label } from '@/components/ui/label'
import { Badge } from '@/components/ui/badge'
import { Select } from '@/components/ui/select'
import { Dialog, DialogHeader, DialogTitle, DialogDescription, DialogFooter } from '@/components/ui/dialog'
import { useToast } from '@/components/ui/toaster'
import { useAuth } from '@/app/providers/auth-provider'
import { formatIndexerDataType } from '@/lib/formatters/labels'
import { ArrowLeft, Plus, Trash2, List } from 'lucide-react'
import { ROUTES } from '@/lib/constants/routes'

export function DocumentClassIndexersPage() {
  const { id } = useParams<{ id: string }>()
  const navigate = useNavigate()
  const { toast } = useToast()
  const queryClient = useQueryClient()
  const { user } = useAuth()
  const isAdmin = user?.role === 'Admin'

  const [createOpen, setCreateOpen] = useState(false)
  const [deleteId, setDeleteId] = useState<string | null>(null)
  const [form, setForm] = useState<CreateIndexerPayload>({
    name: '', displayName: '', description: '', dataType: 0,
    isRequired: false, isMultiple: false, extractionHint: null, regexPattern: null,
  })

  const { data: cls } = useQuery({
    queryKey: ['document-class', id],
    queryFn: () => documentClassesService.getById(id!),
    enabled: !!id,
  })

  const { data: indexers, isLoading } = useQuery({
    queryKey: ['document-class-indexers', id],
    queryFn: () => indexersService.getByDocumentClass(id!),
    enabled: !!id,
  })

  const createMutation = useMutation({
    mutationFn: () => indexersService.create(id!, form),
    onSuccess: () => {
      toast('success', 'Indexador criado com sucesso.')
      setCreateOpen(false)
      setForm({ name: '', displayName: '', description: '', dataType: 0, isRequired: false, isMultiple: false, extractionHint: null, regexPattern: null })
      queryClient.invalidateQueries({ queryKey: ['document-class-indexers', id] })
    },
    onError: () => toast('error', 'Erro ao criar indexador.'),
  })

  const deleteMutation = useMutation({
    mutationFn: (indexerId: string) => indexersService.disable(id!, indexerId),
    onSuccess: () => {
      toast('success', 'Indexador desativado.')
      setDeleteId(null)
      queryClient.invalidateQueries({ queryKey: ['document-class-indexers', id] })
    },
    onError: () => toast('error', 'Erro ao desativar indexador.'),
  })

  if (isLoading) return <LoadingSkeleton />

  return (
    <div className="space-y-6">
      <PageHeader title={`Indexadores - ${cls?.displayName ?? 'Carregando...'}`}>
        <Button variant="outline" onClick={() => navigate(`${ROUTES.DOCUMENT_CLASSES}/${id}`)}>
          <ArrowLeft className="mr-2 h-4 w-4" /> Voltar
        </Button>
        {isAdmin && (
          <Button onClick={() => setCreateOpen(true)}>
            <Plus className="mr-2 h-4 w-4" /> Novo indexador
          </Button>
        )}
      </PageHeader>

      <Card>
        <CardContent className="pt-6">
          {!indexers?.length ? (
            <EmptyState title="Nenhum indexador cadastrado." description="Indexadores definem quais campos devem ser extraídos desta classe documental." icon={List} />
          ) : (
            <Table>
              <TableHeader>
                <TableRow>
                  <TableHead>Nome</TableHead>
                  <TableHead>Tipo</TableHead>
                  <TableHead>Obrigatório</TableHead>
                  <TableHead>Múltiplo</TableHead>
                  <TableHead>Regex</TableHead>
                  <TableHead>Status</TableHead>
                  {isAdmin && <TableHead>Ações</TableHead>}
                </TableRow>
              </TableHeader>
              <TableBody>
                {indexers.map((idx) => (
                  <TableRow key={idx.id}>
                    <TableCell>
                      <p className="font-medium">{idx.displayName}</p>
                      <p className="text-xs text-muted-foreground font-mono">{idx.name}</p>
                    </TableCell>
                    <TableCell>{formatIndexerDataType(String(idx.dataType))}</TableCell>
                    <TableCell>{idx.isRequired ? <Badge variant="warning">Sim</Badge> : 'Não'}</TableCell>
                    <TableCell>{idx.isMultiple ? 'Sim' : 'Não'}</TableCell>
                    <TableCell>{idx.hasRegexPattern ? <Badge variant="success">Sim</Badge> : 'Não'}</TableCell>
                    <TableCell><Badge variant={idx.isActive ? 'success' : 'secondary'}>{idx.isActive ? 'Ativo' : 'Inativo'}</Badge></TableCell>
                    {isAdmin && (
                      <TableCell>
                        <Button variant="ghost" size="icon" onClick={() => setDeleteId(idx.id)} disabled={!idx.isActive}>
                          <Trash2 className="h-4 w-4 text-destructive" />
                        </Button>
                      </TableCell>
                    )}
                  </TableRow>
                ))}
              </TableBody>
            </Table>
          )}
        </CardContent>
      </Card>

      <Dialog open={createOpen} onOpenChange={setCreateOpen}>
        <DialogHeader><DialogTitle>Novo indexador</DialogTitle><DialogDescription>Defina um novo campo para extração nesta classe documental.</DialogDescription></DialogHeader>
        <div className="space-y-4 py-4">
          <div className="grid grid-cols-2 gap-4">
            <div className="space-y-2"><Label>Nome</Label><Input value={form.name} onChange={(e) => setForm({ ...form, name: e.target.value })} placeholder="numeroProcesso" /></div>
            <div className="space-y-2"><Label>Nome de exibição</Label><Input value={form.displayName} onChange={(e) => setForm({ ...form, displayName: e.target.value })} placeholder="Número do Processo" /></div>
          </div>
          <div className="space-y-2"><Label>Descrição</Label><Input value={form.description} onChange={(e) => setForm({ ...form, description: e.target.value })} /></div>
          <div className="space-y-2"><Label>Tipo</Label>
            <Select value={String(form.dataType)} onChange={(e: React.ChangeEvent<HTMLSelectElement>) => setForm({ ...form, dataType: Number(e.target.value) })}>
              <option value={0}>Texto</option><option value={1}>Número</option><option value={2}>Decimal</option>
              <option value={3}>Data</option><option value={4}>Sim/Não</option><option value={5}>CPF</option>
              <option value={6}>CNPJ</option><option value={7}>E-mail</option><option value={8}>Telefone</option>
              <option value={9}>CEP</option><option value={10}>Moeda</option>
            </Select>
          </div>
          <div className="space-y-2"><Label>Dica de extração</Label><Input value={form.extractionHint ?? ''} onChange={(e) => setForm({ ...form, extractionHint: e.target.value || null })} /></div>
          <div className="space-y-2"><Label>Regex</Label><Input value={form.regexPattern ?? ''} onChange={(e) => setForm({ ...form, regexPattern: e.target.value || null })} /></div>
          <div className="flex gap-4">
            <label className="flex items-center gap-2"><input type="checkbox" checked={form.isRequired} onChange={(e) => setForm({ ...form, isRequired: e.target.checked })} /><span className="text-sm">Obrigatório</span></label>
            <label className="flex items-center gap-2"><input type="checkbox" checked={form.isMultiple} onChange={(e) => setForm({ ...form, isMultiple: e.target.checked })} /><span className="text-sm">Múltiplo</span></label>
          </div>
        </div>
        <DialogFooter>
          <Button variant="outline" onClick={() => setCreateOpen(false)}>Cancelar</Button>
          <Button onClick={() => createMutation.mutate()} disabled={createMutation.isPending}>Criar</Button>
        </DialogFooter>
      </Dialog>

      <ConfirmDialog
        open={!!deleteId}
        onOpenChange={(open) => { if (!open) setDeleteId(null) }}
        title="Desativar indexador"
        description="O indexador será desativado e não será mais usado nas extrações futuras."
        confirmLabel="Desativar"
        variant="destructive"
        isLoading={deleteMutation.isPending}
        onConfirm={() => deleteId && deleteMutation.mutate(deleteId)}
      />
    </div>
  )
}
