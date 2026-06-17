import api from './api'
import type { DashboardSummary } from '@/types/dashboard'

export const dashboardService = {
  async getData(): Promise<DashboardSummary> {
    const { data } = await api.get<DashboardSummary>('/dashboard/summary')
    return data
  },
}
