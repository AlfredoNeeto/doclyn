import { useCallback, useState } from 'react'
import { Upload, FileText, X } from 'lucide-react'
import { cn } from '@/lib/utils'
import { Button } from '@/components/ui/button'

interface UploadDropzoneProps {
  onFileSelected: (file: File) => void
  disabled?: boolean
}

export function UploadDropzone({ onFileSelected, disabled }: UploadDropzoneProps) {
  const [dragOver, setDragOver] = useState(false)
  const [selectedFile, setSelectedFile] = useState<File | null>(null)
  const fileInputId = 'file-upload-input'

  const handleDrop = useCallback(
    (e: React.DragEvent) => {
      e.preventDefault()
      setDragOver(false)
      const file = e.dataTransfer.files[0]
      if (file) {
        setSelectedFile(file)
        onFileSelected(file)
      }
    },
    [onFileSelected]
  )

  const handleChange = useCallback(
    (e: React.ChangeEvent<HTMLInputElement>) => {
      const file = e.target.files?.[0]
      if (file) {
        setSelectedFile(file)
        onFileSelected(file)
      }
    },
    [onFileSelected]
  )

  const clearFile = () => {
    setSelectedFile(null)
  }

  return (
    <div className="space-y-4">
      <div
        className={cn(
          'relative flex flex-col items-center justify-center rounded-lg border-2 border-dashed p-12 transition-colors',
          dragOver ? 'border-primary bg-primary/5' : 'border-border',
          disabled && 'opacity-50 pointer-events-none'
        )}
        onDragOver={(e) => { e.preventDefault(); setDragOver(true) }}
        onDragLeave={() => setDragOver(false)}
        onDrop={handleDrop}
      >
        {selectedFile ? (
          <div className="flex flex-col items-center gap-2">
            <FileText className="h-10 w-10 text-primary" />
            <p className="text-sm font-medium">{selectedFile.name}</p>
            <p className="text-xs text-muted-foreground">
              {(selectedFile.size / 1024 / 1024).toFixed(2)} MB
            </p>
            <Button variant="ghost" size="sm" onClick={(e) => { e.stopPropagation(); clearFile() }}>
              <X className="mr-1 h-3 w-3" /> Remover
            </Button>
          </div>
        ) : (
          <>
            <Upload className="h-10 w-10 text-muted-foreground mb-3" />
            <p className="text-sm font-medium">Arraste um PDF aqui ou clique para selecionar</p>
            <p className="text-xs text-muted-foreground mt-1">Apenas arquivos PDF, máximo 10 MB</p>
          </>
        )}
      </div>

      {!selectedFile && (
        <div className="flex justify-center">
          <label htmlFor={fileInputId} className="inline-flex items-center justify-center whitespace-nowrap rounded-md text-sm font-medium border border-input bg-background hover:bg-accent hover:text-accent-foreground h-10 px-4 py-2 cursor-pointer">
            <Upload className="mr-2 h-4 w-4" />
            Selecionar arquivo
          </label>
          <input
            id={fileInputId}
            type="file"
            accept=".pdf"
            className="hidden"
            onChange={handleChange}
            disabled={disabled}
          />
        </div>
      )}

      <p className="text-sm text-muted-foreground text-center">
        Envie um PDF digitalizado. O Doclyn irá ler, classificar, extrair os principais dados e gerar insights automaticamente.
      </p>
    </div>
  )
}
