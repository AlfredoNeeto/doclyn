import type { User } from '@/types/auth'

export function isAdmin(user: User | null): boolean {
  return user?.role === 'Admin'
}

export function isOperator(user: User | null): boolean {
  return user?.role === 'Operator'
}

export function canManageIndexers(user: User | null): boolean {
  return isAdmin(user)
}

export function canApproveSuggestions(user: User | null): boolean {
  return isAdmin(user)
}

export function getUserInitials(user: User | null): string {
  if (!user?.name) return 'U'
  const parts = user.name.trim().split(/\s+/)
  if (parts.length === 1) return parts[0].slice(0, 2).toUpperCase()
  return (parts[0][0] + parts[parts.length - 1][0]).toUpperCase()
}

export function formatRole(user: User | null): string {
  if (isAdmin(user)) return 'Administrador'
  if (isOperator(user)) return 'Operador'
  return 'Usuário'
}
