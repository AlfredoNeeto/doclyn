import { useState } from 'react'
import { useNavigate } from 'react-router-dom'
import { useMutation } from '@tanstack/react-query'
import { documentsService } from '@/services/documents.service'
import { UploadDropzone } from '@/components/shared/upload-dropzone'
import { PageHeader } from '@/components/shared/page-header'
import { Button } from '@/components/ui/button'
import { Card, CardContent } from '@/components/ui/card'
import { useToast } from '@/components/ui/toaster'
import { Loader2, CheckCircle, FileText } from 'lucide-react'
import { ROUTES } from '@/lib/constants/routes'

export function UploadPage() {
  const navigate = useNavigate()
  const { toast } = useToast()
  const [selectedFile, setSelectedFile] = useState<File | null>(null)

  const uploadMutation = useMutation({
    mutationFn: (file: File) => documentsService.upload(file),
    onSuccess: (data) => {
      toast('success', 'Documento enviado com sucesso!')
      setUploadedId(data.id)
    },
    onError: () => {
      toast('error', 'Erro ao enviar documento. Tente novamente.')
    },
  })

  const [uploadedId, setUploadedId] = useState<string | null>(null)

  if (uploadedId) {
    return (
      <div className="space-y-6">
        <PageHeader title="Enviar documento" description="Documento enviado com sucesso" />
        <Card>
          <CardContent className="flex flex-col items-center py-12 text-center">
            <div className="mb-4 flex h-16 w-16 items-center justify-center rounded-full bg-emerald-100 dark:bg-emerald-900">
              <CheckCircle className="h-8 w-8 text-emerald-600 dark:text-emerald-400" />
            </div>
            <h3 className="text-lg font-semibold">Documento enviado</h3>
            <p className="text-sm text-muted-foreground mt-1 mb-6">
              O processamento será iniciado automaticamente. Você pode acompanhar o status na página do documento.
            </p>
            <div className="flex gap-3">
              <Button onClick={() => navigate(`${ROUTES.DOCUMENTS}/${uploadedId}`)}>
                <FileText className="mr-2 h-4 w-4" />
                Ver documento
              </Button>
              <Button variant="outline" onClick={() => { setUploadedId(null); setSelectedFile(null) }}>
                Enviar outro
              </Button>
            </div>
          </CardContent>
        </Card>
      </div>
    )
  }

  return (
    <div className="space-y-6">
      <PageHeader title="Enviar documento" description="Faça upload de um PDF para análise documental" />
      <Card className="max-w-2xl mx-auto">
        <CardContent className="pt-6">
          <UploadDropzone
            onFileSelected={setSelectedFile}
            disabled={uploadMutation.isPending}
          />
          {selectedFile && (
            <div className="mt-6 flex justify-center">
              <Button
                onClick={() => uploadMutation.mutate(selectedFile)}
                disabled={uploadMutation.isPending}
                size="lg"
              >
                {uploadMutation.isPending && <Loader2 className="mr-2 h-4 w-4 animate-spin" />}
                {uploadMutation.isPending ? 'Enviando...' : 'Enviar documento'}
              </Button>
            </div>
          )}
        </CardContent>
      </Card>
    </div>
  )
}
