import axios from 'axios'

const api = axios.create({
  baseURL: '/api',
  headers: { 'Content-Type': 'application/json' },
})

let accessToken: string | null = null
let refreshPromise: Promise<string | null> | null = null

export function setAccessToken(token: string | null) {
  accessToken = token
}

export function getAccessToken(): string | null {
  return accessToken
}

api.interceptors.request.use((config) => {
  if (accessToken) {
    config.headers.Authorization = `Bearer ${accessToken}`
  }
  return config
})

api.interceptors.response.use(
  (response) => response,
  async (error) => {
    const originalRequest = error.config

    if (error.response?.status === 401 && !originalRequest._retry) {
      originalRequest._retry = true

      const storedRefreshToken = localStorage.getItem('refreshToken')
      if (!storedRefreshToken) {
        setAccessToken(null)
        localStorage.removeItem('refreshToken')
        window.location.href = '/login?sessionExpired=1'
        return Promise.reject(error)
      }

      try {
        if (!refreshPromise) {
          refreshPromise = (async () => {
            try {
              const response = await axios.post('/api/Auth/refresh-token', {
                refreshToken: storedRefreshToken,
              })
              const { accessToken: newToken, refreshToken: newRefresh } = response.data
              setAccessToken(newToken)
              localStorage.setItem('refreshToken', newRefresh)
              return newToken
            } catch {
              setAccessToken(null)
              localStorage.removeItem('refreshToken')
              window.location.href = '/login?sessionExpired=1'
              return null
            } finally {
              refreshPromise = null
            }
          })()
        }

        const newToken = await refreshPromise
        if (newToken) {
          originalRequest.headers.Authorization = `Bearer ${newToken}`
          return api(originalRequest)
        }
      } catch {
        // redirect handled in refreshPromise
      }
    }

    return Promise.reject(error)
  }
)

export default api
