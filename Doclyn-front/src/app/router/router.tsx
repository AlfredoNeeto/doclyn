import { createBrowserRouter, Navigate } from 'react-router-dom'
import { ROUTES } from '@/lib/constants/routes'
import { AuthGuard, GuestGuard } from './route-guards'
import { AppShell } from '@/components/layout/app-shell'
import { LoginPage } from '@/features/auth/pages/login-page'
import { RegisterPage } from '@/features/auth/pages/register-page'
import { ForgotPasswordPage } from '@/features/auth/pages/forgot-password-page'
import { VerifyResetCodePage } from '@/features/auth/pages/verify-reset-code-page'
import { ResetPasswordPage } from '@/features/auth/pages/reset-password-page'
import { DashboardPage } from '@/features/dashboard/pages/dashboard-page'
import { UploadPage } from '@/features/upload/pages/upload-page'
import { DocumentsPage } from '@/features/documents/pages/documents-page'
import { DocumentDetailPage } from '@/features/documents/pages/document-detail-page'
import { DocumentClassesPage } from '@/features/document-classes/pages/document-classes-page'
import { DocumentClassDetailPage } from '@/features/document-classes/pages/document-class-detail-page'
import { DocumentClassIndexersPage } from '@/features/document-classes/pages/document-class-indexers-page'
import { SettingsPage } from '@/features/settings/pages/settings-page'

export const router = createBrowserRouter([
  {
    element: <GuestGuard />,
    children: [
      { path: ROUTES.LOGIN, element: <LoginPage /> },
      { path: ROUTES.REGISTER, element: <RegisterPage /> },
      { path: ROUTES.FORGOT_PASSWORD, element: <ForgotPasswordPage /> },
      { path: ROUTES.VERIFY_RESET_CODE, element: <VerifyResetCodePage /> },
      { path: ROUTES.RESET_PASSWORD, element: <ResetPasswordPage /> },
    ],
  },
  {
    element: <AuthGuard />,
    children: [
      {
        element: <AppShell />,
        children: [
          { index: true, element: <Navigate to={ROUTES.DASHBOARD} replace /> },
          { path: ROUTES.DASHBOARD, element: <DashboardPage /> },
          { path: ROUTES.UPLOAD, element: <UploadPage /> },
          { path: ROUTES.DOCUMENTS, element: <DocumentsPage /> },
          { path: ROUTES.DOCUMENT_DETAIL, element: <DocumentDetailPage /> },
          { path: ROUTES.DOCUMENT_CLASSES, element: <DocumentClassesPage /> },
          { path: ROUTES.DOCUMENT_CLASS_DETAIL, element: <DocumentClassDetailPage /> },
          { path: ROUTES.DOCUMENT_CLASS_INDEXERS, element: <DocumentClassIndexersPage /> },
          { path: ROUTES.SETTINGS, element: <SettingsPage /> },
        ],
      },
    ],
  },
])
