import { FileText, LayoutDashboard, Settings, Upload } from 'lucide-react'
import type { LucideIcon } from 'lucide-react'
import { ROUTES } from './routes'

export interface NavItem {
  label: string
  href: string
  icon: LucideIcon
}

export const mainNavItems: NavItem[] = [
  { label: 'Dashboard', href: ROUTES.DASHBOARD, icon: LayoutDashboard },
  { label: 'Enviar documento', href: ROUTES.UPLOAD, icon: Upload },
  { label: 'Documentos', href: ROUTES.DOCUMENTS, icon: FileText },
  { label: 'Configurações', href: ROUTES.SETTINGS, icon: Settings },
]
