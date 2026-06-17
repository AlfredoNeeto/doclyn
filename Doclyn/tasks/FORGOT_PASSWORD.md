# FORGOT_PASSWORD.md

# Recuperação de Senha - Doclyn

## Objetivo

Permitir que um usuário redefina sua senha de forma segura quando esquecer suas credenciais de acesso.

---

# Estratégia Escolhida

A recuperação de senha será baseada em um código temporário de verificação enviado para o e-mail do usuário.

Fluxo:

```txt
Usuário informa o e-mail
↓
Sistema gera código temporário
↓
Código enviado por e-mail
↓
Usuário informa código recebido
↓
Sistema valida código
↓
Usuário define nova senha
↓
Senha é atualizada
```

---

# Avaliação de Segurança

Um código de 6 dígitos é uma boa solução para o MVP.

Exemplo:

```txt
482193
```

Possibilidades:

```txt
000000 até 999999
```

Total:

```txt
1.000.000 combinações
```

Porém, sozinho, um código de 6 dígitos pode sofrer ataques de força bruta se não houver controles adicionais.

Portanto, a implementação deve incluir proteções obrigatórias.

---

# Recomendação

Utilizar:

- Código numérico de 6 dígitos.
- Expiração curta.
- Limite de tentativas.
- Hash do código no banco.
- Revogação automática após uso.

Essa abordagem oferece excelente equilíbrio entre:

- Segurança
- Facilidade de uso
- Rapidez de implementação

---

# Fluxo Completo

## Etapa 1 - Solicitar recuperação

Endpoint:

```http
POST /api/auth/forgot-password
```

Request:

```json
{
  "email": "alfredo@email.com"
}
```

Resposta:

```http
204 No Content
```

Importante:

O sistema nunca deve informar se o e-mail existe ou não.

Sempre retornar:

```http
204 No Content
```

Isso evita enumeração de usuários.

---

## Etapa 2 - Geração do código

Gerar código aleatório:

```txt
582914
```

Características:

- 6 dígitos
- Uso único
- Expiração curta

Validade recomendada:

```txt
10 minutos
```

---

## Etapa 3 - Envio do e-mail

Assunto:

```txt
Código de recuperação de senha
```

Conteúdo:

```txt
Olá, Alfredo.

Seu código para redefinição de senha é:

582914

Este código expira em 10 minutos.

Caso você não tenha solicitado esta operação,
ignore este e-mail.
```

---

## Etapa 4 - Validação do código

Endpoint:

```http
POST /api/auth/verify-reset-code
```

Request:

```json
{
  "email": "alfredo@email.com",
  "code": "582914"
}
```

Resposta:

```json
{
  "resetToken": "temporary-reset-token"
}
```

Após validação:

- Código é invalidado.
- Sistema gera um Reset Token temporário.

---

# Sugestão de Segurança Melhor

Em vez de redefinir a senha diretamente após informar o código, utilize um token temporário.

Fluxo:

```txt
E-mail
↓
Código
↓
Validação
↓
Reset Token
↓
Nova senha
```

Isso reduz riscos de manipulação da operação.

---

## Etapa 5 - Definir nova senha

Endpoint:

```http
POST /api/auth/reset-password
```

Request:

```json
{
  "resetToken": "temporary-reset-token",
  "newPassword": "NovaSenha@123"
}
```

Response:

```http
204 No Content
```

---

# Entidade

## PasswordResetRequest

```csharp
public sealed class PasswordResetRequest
{
    public Guid Id { get; private set; }

    public Guid UserId { get; private set; }

    public string CodeHash { get; private set; } = string.Empty;

    public DateTime ExpiresAt { get; private set; }

    public int Attempts { get; private set; }

    public bool IsUsed { get; private set; }

    public DateTime CreatedAt { get; private set; }
}
```

---

# Regras de Segurança

## Não salvar código em texto puro

ERRADO:

```txt
582914
```

CORRETO:

```txt
SHA256(582914)
```

Salvar apenas o hash.

---

## Limite de tentativas

Máximo:

```txt
5 tentativas
```

Após isso:

```txt
Código bloqueado
```

Novo processo de recuperação deve ser iniciado.

---

## Expiração

Recomendado:

```txt
10 minutos
```

Máximo:

```txt
15 minutos
```

---

## Uso único

Após validação:

```txt
IsUsed = true
```

O código não pode ser reutilizado.

---

## Rate Limit

Limitar solicitações:

```txt
3 solicitações por hora
por usuário
```

e

```txt
10 solicitações por hora
por IP
```

---

# Endpoints

```http
POST /api/auth/forgot-password
POST /api/auth/verify-reset-code
POST /api/auth/reset-password
```

---

# Casos de Erro

Código inválido:

```http
400 Bad Request
```

```json
{
  "message": "Invalid verification code."
}
```

Código expirado:

```http
400 Bad Request
```

```json
{
  "message": "Verification code expired."
}
```

Muitas tentativas:

```http
429 Too Many Requests
```

```json
{
  "message": "Too many attempts."
}
```

---

# Testes Recomendados

## Unit Tests

- Deve gerar código de 6 dígitos.
- Deve salvar hash do código.
- Deve expirar após 10 minutos.
- Deve bloquear após 5 tentativas.
- Deve invalidar após uso.

## Integration Tests

- Forgot Password.
- Verify Reset Code.
- Reset Password.
- Código expirado.
- Código inválido.
- Rate Limit.

---

# Decisão Final

Para o Doclyn, a melhor estratégia é:

```txt
JWT + Refresh Token
↓
Recuperação de Senha
↓
Código de 6 dígitos enviado por e-mail
↓
Código salvo com hash
↓
Expiração de 10 minutos
↓
Máximo de 5 tentativas
↓
Geração de Reset Token temporário
↓
Definição da nova senha
```

Essa abordagem é simples para o MVP, segura para produção e compatível com futuras evoluções como MFA, autenticação por aplicativo e login social.
