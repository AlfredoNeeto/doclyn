import api from './api'
import type { LoginCredentials, LoginResponse, RefreshTokenResponse, CurrentUserResponse } from '@/types/auth'

export const authService = {
  async login(credentials: LoginCredentials): Promise<LoginResponse> {
    const { data } = await api.post<LoginResponse>('/Auth/login', credentials)
    return data
  },

  async refreshToken(refreshToken: string): Promise<RefreshTokenResponse> {
    const { data } = await api.post<RefreshTokenResponse>('/Auth/refresh-token', { refreshToken })
    return data
  },

  async logout(refreshToken: string): Promise<void> {
    await api.post('/Auth/logout', { refreshToken })
  },

  async me(): Promise<CurrentUserResponse> {
    const { data } = await api.get<CurrentUserResponse>('/Auth/me')
    return data
  },

  async forgotPassword(email: string): Promise<void> {
    await api.post('/Auth/forgot-password', { email })
  },

  async verifyResetCode(email: string, code: string): Promise<{ resetToken: string }> {
    const { data } = await api.post<{ resetToken: string }>('/Auth/verify-reset-code', { email, code })
    return data
  },

  async resetPassword(resetToken: string, newPassword: string): Promise<void> {
    await api.post('/Auth/reset-password', { resetToken, newPassword })
  },

  async register(payload: { name: string; email: string; password: string }): Promise<void> {
    await api.post('/Auth/register', { name: payload.name, email: payload.email, password: payload.password })
  },
}
