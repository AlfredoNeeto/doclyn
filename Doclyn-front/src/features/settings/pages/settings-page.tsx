import { useAuth } from '@/app/providers/auth-provider'
import { useTheme } from '@/app/providers/theme-provider'
import { PageHeader } from '@/components/shared/page-header'
import { Card, CardContent, CardHeader, CardTitle, CardDescription } from '@/components/ui/card'
import { Button } from '@/components/ui/button'
import { Badge } from '@/components/ui/badge'
import { User, Moon, Sun, Monitor } from 'lucide-react'

export function SettingsPage() {
  const { user } = useAuth()
  const { theme, setTheme } = useTheme()

  return (
    <div className="space-y-6">
      <PageHeader title="Configurações" description="Gerencie suas preferências" />

      <div className="grid gap-6 max-w-2xl">
        <Card>
          <CardHeader>
            <CardTitle className="text-base flex items-center gap-2"><User className="h-4 w-4" /> Informações da conta</CardTitle>
          </CardHeader>
          <CardContent className="space-y-3">
            <div><p className="text-xs text-muted-foreground">Nome</p><p className="text-sm font-medium">{user?.name}</p></div>
            <div><p className="text-xs text-muted-foreground">E-mail</p><p className="text-sm">{user?.email}</p></div>
            <div><p className="text-xs text-muted-foreground">Função</p><Badge variant={user?.role === 'Admin' ? 'default' : 'secondary'}>{user?.role === 'Admin' ? 'Administrador' : 'Operador'}</Badge></div>
          </CardContent>
        </Card>

        <Card>
          <CardHeader>
            <CardTitle className="text-base flex items-center gap-2"><Monitor className="h-4 w-4" /> Aparência</CardTitle>
            <CardDescription>Escolha o tema da interface</CardDescription>
          </CardHeader>
          <CardContent>
            <div className="flex gap-2">
              <Button variant={theme === 'light' ? 'default' : 'outline'} size="sm" onClick={() => setTheme('light')}>
                <Sun className="mr-2 h-4 w-4" /> Claro
              </Button>
              <Button variant={theme === 'dark' ? 'default' : 'outline'} size="sm" onClick={() => setTheme('dark')}>
                <Moon className="mr-2 h-4 w-4" /> Escuro
              </Button>
              <Button variant={theme === 'system' ? 'default' : 'outline'} size="sm" onClick={() => setTheme('system')}>
                <Monitor className="mr-2 h-4 w-4" /> Sistema
              </Button>
            </div>
          </CardContent>
        </Card>
      </div>
    </div>
  )
}
