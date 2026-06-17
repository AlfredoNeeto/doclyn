# AUTHENTICATION.md

## Visão geral

Este documento define como será implementada a autenticação do **Doclyn**, utilizando **JWT Access Token + Refresh Token**.

O objetivo é permitir que usuários façam login com e-mail e senha, recebam um token JWT para acessar endpoints protegidos e possam renovar a sessão de forma segura sem precisar reenviar credenciais a todo momento.

## Estratégia escolhida

A autenticação será baseada em:

```txt
E-mail + Senha
↓
Validação das credenciais
↓
Geração de Access Token JWT
↓
Geração de Refresh Token
↓
Access Token usado nas chamadas protegidas
↓
Refresh Token usado para renovar sessão
```

## Tokens

### Access Token

O Access Token será um JWT de curta duração.

Características:

- Usado para acessar endpoints protegidos.
- Enviado no header `Authorization`.
- Deve conter claims mínimas.
- Deve expirar rapidamente.

Tempo recomendado:

```txt
15 minutos
```

Exemplo de uso:

```http
Authorization: Bearer {accessToken}
```

### Refresh Token

O Refresh Token será um token opaco, aleatório e seguro.

Características:

- Usado apenas para gerar um novo Access Token.
- Deve ser salvo no banco de dados.
- Deve possuir data de expiração.
- Deve poder ser revogado.
- Deve ser rotacionado a cada uso.

Tempo recomendado:

```txt
7 dias
```

Para o MVP, pode começar com 7 dias. Futuramente, pode ser configurável por tenant ou política de segurança.

## Claims do JWT

O JWT deve conter apenas informações necessárias para autorização.

Claims recomendadas:

```txt
sub        → Id do usuário
email      → E-mail do usuário
name       → Nome do usuário
role       → Perfil do usuário
jti        → Identificador único do token
iat        → Data de emissão
```

Evitar colocar dados sensíveis no JWT.

Não incluir:

```txt
Senha
CPF
Dados extraídos de documentos
Informações sensíveis
Permissões muito detalhadas
```

## Perfis de usuário

Inicialmente o sistema terá dois perfis:

```txt
Admin
Operator
```

### Admin

Permissões:

- Gerenciar usuários.
- Visualizar todos os documentos.
- Visualizar dashboard global.
- Consultar logs de processamento.
- Reprocessar documentos futuramente.

### Operator

Permissões:

- Fazer upload de documentos.
- Visualizar apenas os próprios documentos.
- Visualizar dados extraídos dos próprios documentos.
- Acompanhar status dos próprios processamentos.

## Entidades necessárias

### User

Representa um usuário do sistema.

Campos sugeridos:

```csharp
public sealed class User : AuditableEntity
{
    public string Name { get; private set; } = string.Empty;
    public string Email { get; private set; } = string.Empty;
    public string PasswordHash { get; private set; } = string.Empty;
    public UserRole Role { get; private set; }
    public bool IsActive { get; private set; }
}
```

### RefreshToken

Representa uma sessão renovável de autenticação.

Campos sugeridos:

```csharp
public sealed class RefreshToken : AuditableEntity
{
    public Guid UserId { get; private set; }
    public string TokenHash { get; private set; } = string.Empty;
    public DateTime ExpiresAt { get; private set; }
    public DateTime? RevokedAt { get; private set; }
    public string? ReplacedByTokenHash { get; private set; }
    public bool IsRevoked => RevokedAt is not null;
    public bool IsExpired => DateTime.UtcNow >= ExpiresAt;
}
```

Importante: o Refresh Token puro não deve ser salvo diretamente no banco. Salvar apenas o hash.

## Endpoints

### Register

```http
POST /api/auth/register
```

Cria um novo usuário.

Request:

```json
{
  "name": "Alfredo Neto",
  "email": "alfredo@email.com",
  "password": "StrongPassword123!"
}
```

Response:

```json
{
  "id": "2f4f8f7e-6df2-4d24-9826-bab3f9f13711",
  "name": "Alfredo Neto",
  "email": "alfredo@email.com",
  "role": "Operator"
}
```

Regras:

- E-mail deve ser único.
- Senha deve ser armazenada com hash seguro.
- Usuário criado como `Operator` por padrão.
- Não retornar senha nem hash.

### Login

```http
POST /api/auth/login
```

Autentica o usuário e retorna os tokens.

Request:

```json
{
  "email": "alfredo@email.com",
  "password": "StrongPassword123!"
}
```

Response:

```json
{
  "accessToken": "jwt-token",
  "refreshToken": "refresh-token",
  "expiresIn": 900,
  "tokenType": "Bearer",
  "user": {
    "id": "2f4f8f7e-6df2-4d24-9826-bab3f9f13711",
    "name": "Alfredo Neto",
    "email": "alfredo@email.com",
    "role": "Operator"
  }
}
```

Regras:

- Validar se o usuário existe.
- Validar se o usuário está ativo.
- Validar senha usando hash.
- Gerar Access Token.
- Gerar Refresh Token.
- Salvar hash do Refresh Token no banco.
- Retornar tokens e dados básicos do usuário.

### Refresh Token

```http
POST /api/auth/refresh-token
```

Gera um novo Access Token usando um Refresh Token válido.

Request:

```json
{
  "refreshToken": "refresh-token"
}
```

Response:

```json
{
  "accessToken": "new-jwt-token",
  "refreshToken": "new-refresh-token",
  "expiresIn": 900,
  "tokenType": "Bearer"
}
```

Regras:

- Buscar Refresh Token pelo hash.
- Verificar se existe.
- Verificar se não expirou.
- Verificar se não foi revogado.
- Revogar token atual.
- Gerar novo Access Token.
- Gerar novo Refresh Token.
- Salvar novo Refresh Token.
- Registrar o token substituto em `ReplacedByTokenHash`.

### Logout

```http
POST /api/auth/logout
```

Revoga o Refresh Token atual.

Request:

```json
{
  "refreshToken": "refresh-token"
}
```

Response:

```http
204 No Content
```

Regras:

- Buscar token pelo hash.
- Revogar token se existir.
- Não retornar erro detalhado caso o token não exista.

### Me

```http
GET /api/auth/me
```

Retorna os dados do usuário autenticado.

Headers:

```http
Authorization: Bearer {accessToken}
```

Response:

```json
{
  "id": "2f4f8f7e-6df2-4d24-9826-bab3f9f13711",
  "name": "Alfredo Neto",
  "email": "alfredo@email.com",
  "role": "Operator"
}
```

## Segurança de senha

A senha nunca deve ser salva em texto puro.

Usar hash seguro com salt.

Opções recomendadas:

```txt
ASP.NET Core Identity PasswordHasher
BCrypt
Argon2
```

Para manter o MVP simples, pode usar:

```txt
Microsoft.AspNetCore.Identity.PasswordHasher<TUser>
```

## Configurações JWT

Adicionar no `appsettings.json`:

```json
{
  "Jwt": {
    "Issuer": "Doclyn",
    "Audience": "Doclyn",
    "Secret": "USE_USER_SECRETS_OR_ENVIRONMENT_VARIABLE",
    "AccessTokenExpirationMinutes": 15,
    "RefreshTokenExpirationDays": 7
  }
}
```

Em produção, nunca salvar `Secret` diretamente no `appsettings.json`.

Usar:

```txt
Environment Variables
User Secrets em desenvolvimento
Secret Manager
Vault futuramente
```

## Contratos de Application

Interfaces sugeridas no projeto `Doclyn.Application`:

```csharp
public interface ITokenService
{
    string GenerateAccessToken(User user);
    string GenerateRefreshToken();
    string HashRefreshToken(string refreshToken);
}

public interface IPasswordHasher
{
    string Hash(string password);
    bool Verify(string password, string passwordHash);
}

public interface ICurrentUserService
{
    Guid? UserId { get; }
    string? Email { get; }
    string? Role { get; }
}
```

Implementações devem ficar em `Doclyn.Infrastructure`.

## Organização sugerida

```txt
Doclyn.Application/
  Auth/
    Register/
      RegisterUserCommand.cs
      RegisterUserHandler.cs
      RegisterUserValidator.cs
      RegisterUserResponse.cs

    Login/
      LoginCommand.cs
      LoginHandler.cs
      LoginValidator.cs
      LoginResponse.cs

    RefreshToken/
      RefreshTokenCommand.cs
      RefreshTokenHandler.cs
      RefreshTokenValidator.cs
      RefreshTokenResponse.cs

    Logout/
      LogoutCommand.cs
      LogoutHandler.cs

    Me/
      GetCurrentUserQuery.cs
      GetCurrentUserHandler.cs
      CurrentUserResponse.cs

  Common/
    Interfaces/
      ITokenService.cs
      IPasswordHasher.cs
      ICurrentUserService.cs
```

```txt
Doclyn.Infrastructure/
  Security/
    JwtOptions.cs
    JwtTokenService.cs
    PasswordHasherService.cs
    CurrentUserService.cs
```

```txt
Doclyn.Api/
  Controllers/
    AuthController.cs
```

## Autorização

Endpoints públicos:

```txt
POST /api/auth/register
POST /api/auth/login
POST /api/auth/refresh-token
```

Endpoints protegidos:

```txt
POST /api/auth/logout
GET  /api/auth/me
POST /api/documents/upload
GET  /api/documents
GET  /api/documents/{id}
GET  /api/documents/{id}/extracted-data
GET  /api/documents/{id}/logs
GET  /api/dashboard/summary
```

Endpoints restritos a Admin:

```txt
GET /api/users
POST /api/users
PUT /api/users/{id}
DELETE /api/users/{id}
GET /api/dashboard/admin-summary
```

## Fluxo de autenticação no front-end React

```txt
Usuário informa e-mail e senha
↓
Front chama POST /api/auth/login
↓
API retorna accessToken + refreshToken
↓
Front armazena tokens
↓
Access Token é enviado nas próximas requisições
↓
Se API retornar 401 por token expirado
↓
Front chama POST /api/auth/refresh-token
↓
Front atualiza os tokens
↓
Requisição original é repetida
```

Para o MVP, os tokens podem ser armazenados em memória ou localStorage.

Para produção com maior segurança, considerar:

```txt
HttpOnly Secure Cookies
SameSite
CSRF Protection
```

## Erros esperados

### Credenciais inválidas

```http
401 Unauthorized
```

```json
{
  "message": "Invalid email or password."
}
```

### Usuário inativo

```http
403 Forbidden
```

```json
{
  "message": "User is inactive."
}
```

### Refresh Token inválido

```http
401 Unauthorized
```

```json
{
  "message": "Invalid refresh token."
}
```

### Acesso negado por perfil

```http
403 Forbidden
```

```json
{
  "message": "Access denied."
}
```

## Testes recomendados

### UnitTests

Testar:

- Registro com e-mail válido.
- Registro com e-mail duplicado.
- Login com senha correta.
- Login com senha incorreta.
- Login com usuário inativo.
- Refresh Token expirado.
- Refresh Token revogado.
- Geração de claims esperadas.

### IntegrationTests

Testar:

- `POST /api/auth/register`
- `POST /api/auth/login`
- `GET /api/auth/me` com token válido.
- `GET /api/auth/me` sem token.
- `POST /api/auth/refresh-token`
- `POST /api/auth/logout`

## Decisões importantes

- Access Token será curto para reduzir impacto de vazamento.
- Refresh Token será persistido no banco para permitir revogação.
- Refresh Token será salvo como hash, não em texto puro.
- Refresh Token será rotacionado a cada uso.
- O JWT não deve carregar dados sensíveis.
- A autorização será baseada inicialmente em roles simples.
- Permissões mais granulares podem ser adicionadas futuramente.

## Fora do escopo inicial

Não implementar agora:

- Login social.
- MFA.
- SSO.
- OAuth externo.
- Recuperação de senha.
- Confirmação de e-mail.
- Bloqueio por tentativas falhas.
- Detecção de dispositivos.
- Gerenciamento avançado de sessões.

Esses recursos podem ser adicionados depois que o MVP estiver funcional.
