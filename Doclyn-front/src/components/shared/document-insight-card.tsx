import type { DocumentInsight } from '@/types/insights'
import { SeverityBadge } from './severity-badge'
import { Card, CardContent } from '@/components/ui/card'

interface DocumentInsightCardProps {
  insight: DocumentInsight
}

export function DocumentInsightCard({ insight }: DocumentInsightCardProps) {
  return (
    <Card>
      <CardContent className="p-4">
        <div className="flex items-start justify-between gap-2">
          <div className="flex-1">
            <div className="flex items-center gap-2 mb-1">
              <SeverityBadge severity={insight.severity} />
              <span className="text-sm text-muted-foreground">{insight.type}</span>
            </div>
            <p className="font-medium">{insight.title}</p>
            <p className="text-sm text-muted-foreground mt-1">{insight.message}</p>
          </div>
        </div>
      </CardContent>
    </Card>
  )
}
