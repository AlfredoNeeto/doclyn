# FORGOT_PASSWORD_UI_TASK.md

# Task — Implementar Fluxo de Esqueci Minha Senha no Front-end

## Contexto

O Doclyn possui recuperação de senha baseada em código de verificação enviado por e-mail.

Endpoints esperados:

```txt
POST /api/auth/forgot-password
POST /api/auth/verify-reset-code
POST /api/auth/reset-password
```

O usuário deverá conseguir redefinir sua senha sem suporte técnico.

---

# Objetivo

Implementar no front-end React o fluxo completo de recuperação de senha:

```txt
Informar e-mail
↓
Receber código de 6 dígitos
↓
Validar código
↓
Definir nova senha
↓
Voltar para login
```

---

# Telas

## 1. Solicitar Recuperação

Rota:

```txt
/forgot-password
```

Campos:

```txt
E-mail
```

Botão:

```txt
Enviar código
```

Mensagem de sucesso:

```txt
Se o e-mail existir, um código de recuperação foi enviado.
```

Importante: nunca informar se o e-mail existe ou não.

---

## 2. Verificar Código

Rota:

```txt
/verify-reset-code
```

Campos:

```txt
Código de 6 dígitos
```

Validações:

```txt
Obrigatório
Apenas números
Exatamente 6 dígitos
```

Botão:

```txt
Validar código
```

Após sucesso, armazenar temporariamente o `resetToken` apenas em memória ou state.

---

## 3. Redefinir Senha

Rota:

```txt
/reset-password
```

Campos:

```txt
Nova senha
Confirmar nova senha
```

Validações:

```txt
Senha obrigatória
Mínimo 8 caracteres
Confirmação deve ser igual à nova senha
```

Botão:

```txt
Redefinir senha
```

Após sucesso:

```txt
Limpar estado temporário
Redirecionar para /login
Exibir toast de sucesso
```

Mensagem:

```txt
Senha redefinida com sucesso. Faça login novamente.
```

---

# UX Esperada

Criar uma experiência em etapas, simples e clara.

Exemplo de título por etapa:

```txt
Recuperar senha
Verificar código
Criar nova senha
```

Adicionar texto explicativo:

```txt
Informe seu e-mail para receber um código de recuperação.
```

```txt
Digite o código de 6 dígitos enviado para seu e-mail.
```

```txt
Crie uma nova senha segura para acessar sua conta.
```

---

# Estados

Implementar:

```txt
Loading
Erro
Sucesso
Botões desabilitados durante requisição
Toast de feedback
```

---

# Erros

Código inválido:

```txt
Código inválido ou expirado.
```

Erro de rede:

```txt
Não foi possível conectar ao servidor. Tente novamente.
```

Senha inválida:

```txt
A senha deve conter pelo menos 8 caracteres.
```

---

# Services

Criar em:

```txt
src/services/auth.service.ts
```

Métodos:

```ts
forgotPassword(email: string): Promise<void>

verifyResetCode(payload: {
  email: string
  code: string
}): Promise<{ resetToken: string }>

resetPassword(payload: {
  resetToken: string
  newPassword: string
}): Promise<void>
```

---

# Schemas Zod

Criar:

```ts
forgotPasswordSchema
verifyResetCodeSchema
resetPasswordSchema
```

---

# Componentes

Criar:

```txt
ForgotPasswordForm
VerifyResetCodeForm
ResetPasswordForm
```

---

# Segurança

Não armazenar `resetToken` em:

```txt
localStorage
sessionStorage
URL
query string
```

Preferir manter em memória durante o fluxo.

Se o usuário atualizar a página na etapa final, solicitar reinício do fluxo.

---

# Critérios de Aceite

A task estará concluída quando:

* Usuário conseguir solicitar código por e-mail.
* Usuário conseguir validar código de 6 dígitos.
* Usuário conseguir redefinir a senha.
* Mensagens forem claras.
* Não houver enumeração de usuário.
* Reset token não for exposto na URL.
* Fluxo redirecionar corretamente para login.
* Código estiver tipado com TypeScript.
* Formulários usarem React Hook Form + Zod.
* UI usar Shadcn/UI.
