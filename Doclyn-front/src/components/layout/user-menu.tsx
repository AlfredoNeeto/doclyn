import { useAuth } from '@/app/providers/auth-provider'
import { DropdownMenu, DropdownMenuItem, DropdownMenuSeparator } from '@/components/ui/dropdown-menu'
import { LogOut, Sun, Moon, Settings } from 'lucide-react'
import { useTheme } from '@/app/providers/theme-provider'
import { useNavigate } from 'react-router-dom'
import { getUserInitials, formatRole } from '@/lib/auth/permissions'
import { ROUTES } from '@/lib/constants/routes'

export function UserMenu() {
  const { user, logout } = useAuth()
  const { resolvedTheme, setTheme } = useTheme()
  const navigate = useNavigate()

  const handleLogout = async () => {
    await logout()
    navigate('/login')
  }

  const initials = getUserInitials(user)

  return (
    <DropdownMenu
      trigger={
        <button className="flex items-center gap-2 rounded-md px-2 py-1.5 text-sm hover:bg-accent">
          <div className="flex h-7 w-7 items-center justify-center rounded-full bg-primary/10 text-xs font-semibold text-primary">
            {initials}
          </div>
          <span className="hidden sm:inline font-medium">{user?.name ?? 'Usuário'}</span>
        </button>
      }
    >
      <div className="px-3 py-2">
        <div className="flex items-center gap-3">
          <div className="flex h-10 w-10 items-center justify-center rounded-full bg-primary/10 text-sm font-semibold text-primary">
            {initials}
          </div>
          <div>
            <p className="text-sm font-medium">{user?.name}</p>
            <p className="text-xs text-muted-foreground">{user?.email}</p>
            <p className="text-xs text-muted-foreground mt-0.5">{formatRole(user)}</p>
          </div>
        </div>
      </div>
      <DropdownMenuSeparator />
      <DropdownMenuItem onClick={() => navigate(ROUTES.SETTINGS)}>
        <Settings className="mr-2 h-4 w-4" />
        Configurações
      </DropdownMenuItem>
      <DropdownMenuItem onClick={() => setTheme(resolvedTheme === 'dark' ? 'light' : 'dark')}>
        {resolvedTheme === 'dark' ? <Sun className="mr-2 h-4 w-4" /> : <Moon className="mr-2 h-4 w-4" />}
        {resolvedTheme === 'dark' ? 'Modo claro' : 'Modo escuro'}
      </DropdownMenuItem>
      <DropdownMenuSeparator />
      <DropdownMenuItem onClick={handleLogout}>
        <LogOut className="mr-2 h-4 w-4" />
        Sair
      </DropdownMenuItem>
    </DropdownMenu>
  )
}
