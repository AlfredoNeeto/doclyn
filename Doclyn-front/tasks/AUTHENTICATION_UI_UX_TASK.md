# AUTHENTICATION_UI_UX_TASK.md

# Task — Implementar Fluxo de Autenticação e Comportamento do Login

## Contexto

O Doclyn possui autenticação baseada em JWT e Refresh Token.

Endpoints disponíveis:

```txt
POST /api/auth/login
POST /api/auth/refresh-token
POST /api/auth/logout
GET  /api/auth/me

POST /api/auth/forgot-password
POST /api/auth/verify-reset-code
POST /api/auth/reset-password
```

O objetivo desta task é implementar toda a experiência de autenticação do usuário no Front-end React.

---

# Objetivo

Criar um fluxo de autenticação moderno, simples e intuitivo.

O usuário deve conseguir:

```txt
Entrar no sistema
↓
Manter sessão ativa
↓
Recuperar senha
↓
Redefinir senha
↓
Sair do sistema
```

Sem precisar entender JWT ou Refresh Tokens.

---

# Jornada do Usuário

## Primeiro acesso

Fluxo:

```txt
Abrir aplicação
↓
Tela de Login
↓
Informar email
↓
Informar senha
↓
Entrar
↓
Dashboard
```

---

# Login

## Tela

Campos:

```txt
Email
Senha
```

Ações:

```txt
Entrar
Esqueci minha senha
```

---

## Validações

### Email

Obrigatório.

Formato válido:

```txt
usuario@email.com
```

---

### Senha

Obrigatória.

Mínimo:

```txt
8 caracteres
```

---

## Comportamento

Ao clicar em:

```txt
Entrar
```

Executar:

```txt
POST /api/auth/login
```

---

## Loading

Durante autenticação:

```txt
Desabilitar botão
Mostrar spinner
```

Texto:

```txt
Entrando...
```

---

## Sucesso

API retorna:

```json
{
  "accessToken": "jwt",
  "refreshToken": "token",
  "expiresIn": 3600,
  "user": {
    "id": "guid",
    "name": "Alfredo Neto",
    "email": "alfredo@email.com",
    "role": "Admin"
  }
}
```

---

## Após login

Salvar:

```txt
Access Token
Refresh Token
Dados do usuário
```

Atualizar contexto global.

Redirecionar:

```txt
/dashboard
```

Exibir toast:

```txt
Login realizado com sucesso.
```

---

## Erro

Mensagem amigável:

```txt
Email ou senha inválidos.
```

Não exibir:

```txt
Stack trace
Erro técnico
Mensagem da API
```

---

# Persistência da Sessão

Ao atualizar a página:

```txt
F5
```

O usuário deve continuar autenticado.

Fluxo:

```txt
Ler tokens
↓
Executar /api/auth/me
↓
Carregar usuário
```

---

# Refresh Token

Implementar automaticamente.

Fluxo:

```txt
JWT expirou
↓
POST /api/auth/refresh-token
↓
Receber novo JWT
↓
Continuar navegação
```

O usuário não deve perceber.

---

# Logout

Ao clicar:

```txt
Sair
```

Executar:

```txt
POST /api/auth/logout
```

Depois:

```txt
Limpar tokens
Limpar cache
Limpar usuário
```

Redirecionar:

```txt
/login
```

---

# Expiração de Sessão

Caso refresh token expire:

```txt
401
↓
Sessão expirada
↓
Redirecionar login
```

Toast:

```txt
Sua sessão expirou. Faça login novamente.
```

---

# Recuperação de Senha

Fluxo:

```txt
Login
↓
Esqueci minha senha
↓
Informar email
↓
Receber código
↓
Validar código
↓
Nova senha
↓
Login
```

---

# Tela Esqueci Minha Senha

Campo:

```txt
Email
```

Botão:

```txt
Enviar código
```

---

## Sucesso

Mensagem:

```txt
Se o email existir, um código de recuperação foi enviado.
```

Nunca informar:

```txt
Usuário existe
Usuário não existe
```

---

# Tela Verificar Código

Campo:

```txt
Código de 6 dígitos
```

Validação:

```txt
Apenas números
6 caracteres
```

Botão:

```txt
Validar código
```

---

# Tela Nova Senha

Campos:

```txt
Nova senha
Confirmar senha
```

Validações:

```txt
Mínimo 8 caracteres
Senhas iguais
```

---

# Proteção de Rotas

Criar:

```txt
PublicRoute
ProtectedRoute
```

---

## Rotas Públicas

```txt
/login
/forgot-password
/reset-password
```

---

## Rotas Protegidas

```txt
/dashboard
/upload
/documents
/document-classes
/suggestions
/settings
```

---

# Contexto Global

Criar:

```txt
AuthProvider
```

Responsável por:

```txt
Usuário atual
Login
Logout
Refresh token
Permissões
Estado autenticado
```

---

# Hook

Criar:

```ts
useAuth()
```

Retornar:

```ts
{
  user,
  isAuthenticated,
  login,
  logout,
  refreshSession
}
```

---

# Controle de Permissões

Roles:

```txt
Admin
Operator
```

---

## Admin

Pode:

```txt
Gerenciar classes
Gerenciar indexadores
Aprovar sugestões
Ver todos os documentos
```

---

## Operator

Pode:

```txt
Enviar documentos
Consultar documentos
Consultar insights
Reprocessar documentos próprios
```

---

# Header

Quando autenticado:

Mostrar:

```txt
Nome
Email
Cargo
Avatar
```

Menu:

```txt
Minha conta
Configurações
Sair
```

---

# Estados de UX

## Loading Inicial

Ao abrir sistema:

```txt
Verificando autenticação...
```

Mostrar:

```txt
Tela de loading
ou skeleton
```

---

## Usuário não autenticado

Redirecionar:

```txt
/login
```

---

## Erro de rede

Mensagem:

```txt
Não foi possível conectar ao servidor.
Tente novamente.
```

---

# Segurança

Não armazenar JWT em:

```txt
URL
Query String
```

Nunca exibir:

```txt
Access Token
Refresh Token
```

na interface.

---

# Estrutura Sugerida

```txt
src/
  features/
    auth/
      pages/
        LoginPage.tsx
        ForgotPasswordPage.tsx
        VerifyCodePage.tsx
        ResetPasswordPage.tsx

      hooks/
        useAuth.ts

      services/
        auth.service.ts

      context/
        AuthProvider.tsx

      components/
        LoginForm.tsx
        ForgotPasswordForm.tsx
        ResetPasswordForm.tsx
```

---

# Critérios de Aceite

A task será considerada concluída quando:

* Login funcionar.
* Logout funcionar.
* Sessão persistir após refresh da página.
* Refresh token funcionar automaticamente.
* Recuperação de senha funcionar.
* Rotas protegidas estiverem implementadas.
* Contexto global de autenticação existir.
* Hook useAuth existir.
* Tratamento de erro estiver implementado.
* UX estiver amigável.
* Código respeitar React + TypeScript + Shadcn/UI.

---

# Resultado Esperado

Ao final desta task o usuário poderá acessar o Doclyn de forma segura e intuitiva, mantendo sua sessão ativa automaticamente e recuperando sua senha sem necessidade de suporte técnico.
