import { useLocation } from 'react-router-dom'
import { mainNavItems } from '@/lib/constants/navigation'
import { UserMenu } from './user-menu'
import { Menu } from 'lucide-react'
import { Button } from '@/components/ui/button'
import { useState } from 'react'
import { MobileSidebar } from './mobile-sidebar'

export function AppHeader() {
  const location = useLocation()
  const [mobileOpen, setMobileOpen] = useState(false)

  const currentNav = mainNavItems.find(
    (item) => location.pathname === item.href || location.pathname.startsWith(item.href + '/')
  )

  return (
    <header className="flex h-14 shrink-0 items-center justify-between border-b px-4 lg:px-6">
      <div className="flex items-center gap-3">
        <Button variant="ghost" size="icon" className="md:hidden" onClick={() => setMobileOpen(true)}>
          <Menu className="h-5 w-5" />
        </Button>
        <h1 className="text-sm font-medium">{currentNav?.label ?? 'Doclyn'}</h1>
      </div>
      <div className="flex items-center gap-2">
        <UserMenu />
      </div>
      <MobileSidebar open={mobileOpen} onClose={() => setMobileOpen(false)} />
    </header>
  )
}
