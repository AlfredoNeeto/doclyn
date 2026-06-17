import type { DocumentLog } from '@/types/insights'
import { formatDateTime } from '@/lib/formatters/date'
import { formatProcessingStep } from '@/lib/formatters/labels'
import { CheckCircle, XCircle, Clock, Loader2, Upload, FileText, Search, Brain, Lightbulb, RefreshCw, Trash2 } from 'lucide-react'

interface ProcessingTimelineProps {
  logs: DocumentLog[]
}

const stepIcons: Record<string, typeof CheckCircle> = {
  Upload: Upload,
  ProcessingStarted: Loader2,
  OcrCompleted: FileText,
  TextExtracted: FileText,
  DocumentClassified: Search,
  AiClassificationCompleted: Brain,
  DocumentClassFound: Search,
  AiExtractionCompleted: Brain,
  ClassGuidedExtractionCompleted: Brain,
  InsightsGenerated: Lightbulb,
  ProcessingCompleted: CheckCircle,
  ProcessingFailed: XCircle,
  ReprocessRequested: RefreshCw,
  DocumentDeleted: Trash2,
}

function getStepIcon(step: string) {
  const Icon = stepIcons[step]
  if (Icon) return <Icon className="h-3.5 w-3.5" />
  return <Clock className="h-3.5 w-3.5" />
}

function getStepColor(status: string): string {
  const s = status?.toLowerCase() ?? ''
  if (s.includes('success') || s.includes('completed') || s.includes('processed')) return 'border-emerald-500 text-emerald-600 bg-emerald-50 dark:border-emerald-600 dark:text-emerald-400 dark:bg-emerald-950/50'
  if (s.includes('error') || s.includes('failed')) return 'border-red-500 text-red-600 bg-red-50 dark:border-red-600 dark:text-red-400 dark:bg-red-950/50'
  if (s.includes('processing') || s.includes('started') || s.includes('requested')) return 'border-blue-500 text-blue-600 bg-blue-50 dark:border-blue-600 dark:text-blue-400 dark:bg-blue-950/50'
  return 'border-muted-foreground/30 text-muted-foreground bg-muted/30'
}

export function ProcessingTimeline({ logs }: ProcessingTimelineProps) {
  if (!logs.length) {
    return <p className="text-sm text-muted-foreground py-4">Nenhum log de processamento disponível.</p>
  }

  return (
    <div className="space-y-0">
      {logs.map((log, index) => {
        const isLast = index === logs.length - 1
        const colorClass = getStepColor(log.status)

        return (
          <div key={log.id} className="flex gap-3">
            <div className="flex flex-col items-center">
              <div className={`flex h-7 w-7 items-center justify-center rounded-full border-2 ${colorClass}`}>
                {getStepIcon(log.step)}
              </div>
              {!isLast && <div className="w-0.5 flex-1 bg-border/50 min-h-[12px]" />}
            </div>
            <div className={isLast ? '' : 'pb-5'}>
              <p className="text-sm font-medium">{formatProcessingStep(log.step)}</p>
              <p className="text-xs text-muted-foreground leading-relaxed">{log.message}</p>
              <p className="text-xs text-muted-foreground/60 mt-0.5">{formatDateTime(log.createdAt)}</p>
            </div>
          </div>
        )
      })}
    </div>
  )
}
