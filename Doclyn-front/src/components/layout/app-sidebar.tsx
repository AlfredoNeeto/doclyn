import { NavLink, useLocation } from 'react-router-dom'
import { mainNavItems } from '@/lib/constants/navigation'
import { Separator } from '@/components/ui/separator'
import { cn } from '@/lib/utils'

export function AppSidebar() {
  const location = useLocation()

  return (
    <aside className="hidden w-64 flex-col border-r bg-sidebar md:flex">
      <div className="flex h-16 items-center border-b px-5">
        <img src="/logo.png" alt="Doclyn" className="h-9" />
      </div>
      <nav className="flex-1 space-y-1 p-3">
        {mainNavItems.map((item) => {
          const isActive = location.pathname === item.href || location.pathname.startsWith(item.href + '/')
          return (
            <NavLink
              key={item.href}
              to={item.href}
              className={cn(
                'flex items-center gap-3 rounded-md px-3 py-2 text-sm font-medium transition-colors',
                isActive
                  ? 'bg-sidebar-accent text-sidebar-accent-foreground'
                  : 'text-sidebar-foreground hover:bg-sidebar-accent/50 hover:text-sidebar-accent-foreground'
              )}
            >
              <item.icon className="h-4 w-4" />
              {item.label}
            </NavLink>
          )
        })}
      </nav>
      <Separator />
      <div className="p-3">
        <p className="text-xs text-muted-foreground">Doclyn v1.0</p>
      </div>
    </aside>
  )
}
