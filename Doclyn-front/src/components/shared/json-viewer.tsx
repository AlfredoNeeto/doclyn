import { useState } from 'react'
import { Button } from '@/components/ui/button'
import { ChevronDown, ChevronRight } from 'lucide-react'

interface JsonViewerProps {
  data: unknown
}

export function JsonViewer({ data }: JsonViewerProps) {
  const [expanded, setExpanded] = useState(false)

  return (
    <div>
      <Button variant="outline" size="sm" onClick={() => setExpanded(!expanded)} className="mb-2">
        {expanded ? <ChevronDown className="mr-1 h-4 w-4" /> : <ChevronRight className="mr-1 h-4 w-4" />}
        {expanded ? 'Ocultar dados técnicos' : 'Ver dados técnicos'}
      </Button>
      {expanded && (
        <pre className="rounded-md bg-muted p-4 text-xs overflow-auto max-h-96">
          {JSON.stringify(data, null, 2)}
        </pre>
      )}
    </div>
  )
}
