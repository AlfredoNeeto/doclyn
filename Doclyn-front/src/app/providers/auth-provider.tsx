import { createContext, useContext, useState, useEffect, useCallback, type ReactNode } from 'react'
import { useQueryClient } from '@tanstack/react-query'
import type { User } from '@/types/auth'
import { authService } from '@/services/auth.service'
import { setAccessToken } from '@/services/api'

interface AuthContextType {
  user: User | null
  isAuthenticated: boolean
  isLoading: boolean
  login: (email: string, password: string) => Promise<void>
  logout: () => Promise<void>
  refreshSession: () => Promise<void>
}

const AuthContext = createContext<AuthContextType | null>(null)

export function AuthProvider({ children }: { children: ReactNode }) {
  const [user, setUser] = useState<User | null>(null)
  const [isLoading, setIsLoading] = useState(true)
  const queryClient = useQueryClient()

  const refreshSession = useCallback(async () => {
    const storedRefreshToken = localStorage.getItem('refreshToken')
    if (!storedRefreshToken) {
      setIsLoading(false)
      return
    }

    try {
      const tokenResponse = await authService.refreshToken(storedRefreshToken)
      setAccessToken(tokenResponse.accessToken)
      localStorage.setItem('refreshToken', tokenResponse.refreshToken)

      const meResponse = await authService.me()
      setUser(meResponse.user)
    } catch {
      setAccessToken(null)
      localStorage.removeItem('refreshToken')
      setUser(null)
    } finally {
      setIsLoading(false)
    }
  }, [])

  useEffect(() => {
    refreshSession()
  }, [refreshSession])

  const login = useCallback(async (email: string, password: string) => {
    const response = await authService.login({ email, password })
    setAccessToken(response.accessToken)
    localStorage.setItem('refreshToken', response.refreshToken)
    setUser(response.user)
  }, [])

  const logout = useCallback(async () => {
    const storedRefreshToken = localStorage.getItem('refreshToken')
    try {
      if (storedRefreshToken) {
        await authService.logout(storedRefreshToken)
      }
    } catch {
      // ignore
    } finally {
      setAccessToken(null)
      localStorage.removeItem('refreshToken')
      setUser(null)
      queryClient.clear()
    }
  }, [queryClient])

  return (
    <AuthContext.Provider
      value={{
        user,
        isAuthenticated: !!user,
        isLoading,
        login,
        logout,
        refreshSession,
      }}
    >
      {children}
    </AuthContext.Provider>
  )
}

export function useAuth() {
  const ctx = useContext(AuthContext)
  if (!ctx) {
    throw new Error('useAuth must be used within AuthProvider')
  }
  return ctx
}
