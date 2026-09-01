# TmsApi — Training Management System API

A production-grade ASP.NET Core 10 REST API built with Clean Architecture, serving the CoTBE Training Management System. Built as a school project covering enterprise patterns: versioning, CQRS, caching, real-time push, resilience, and security.

---

## 📸 Screenshots

> Add screenshots of Scalar UI, health checks, and SignalR demo here.

---

## 📁 Project Structure

```
TmsApi/
├── TmsApi.sln
├── TmsApi.Api/                        ← Web layer (startup project)
│   ├── Controllers/
│   │   ├── V1/
│   │   │   └── CoursesController.cs   ← Frozen V1 contract
│   │   ├── V2/
│   │   │   ├── CoursesController.cs   ← V2 envelope (data/meta/links)
│   │   │   ├── EnrollmentsController.cs
│   │   │   ├── TranscriptsController.cs
│   │   │   └── CertificatesController.cs
│   │   └── AuthController.cs
│   ├── ExceptionHandlers/
│   │   └── GlobalExceptionHandler.cs  ← RFC 7807 ProblemDetails
│   ├── Filters/
│   │   └── AuditLogFilter.cs
│   ├── Hubs/
│   │   └── TmsHub.cs                  ← SignalR typed hub
│   ├── Middleware/
│   │   ├── RequestLoggingMiddleware.cs
│   │   └── V1DeprecationMiddleware.cs
│   ├── RateLimiting/
│   │   └── ApiKeyTier.cs
│   ├── appsettings.json
│   ├── appsettings.Development.json
│   └── Program.cs
│
├── TmsApi.Application/                ← Business logic (no EF, no ASP.NET)
│   ├── Behaviors/
│   │   ├── LoggingBehavior.cs         ← CorrelationId + elapsed ms
│   │   └── ValidationBehavior.cs      ← FluentValidation pipeline
│   ├── Common/
│   │   ├── Result.cs                  ← Result<TValue, TError>
│   │   └── EnrollmentError.cs
│   ├── DTOs/
│   │   ├── CourseDto.cs               ← + CourseDtoFields whitelist
│   │   ├── CourseResponseDto.cs
│   │   ├── CourseDetailDto.cs
│   │   ├── CreateCourseRequest.cs
│   │   ├── EnrollmentResponseDto.cs
│   │   ├── EnrollStudentRequest.cs
│   │   ├── LinkDto.cs
│   │   ├── PagedRequest.cs
│   │   └── PagedResponse.cs
│   ├── Enrollments/
│   │   ├── Commands/
│   │   │   ├── EnrollStudentCommand.cs
│   │   │   ├── EnrollStudentHandler.cs
│   │   │   └── EnrollStudentValidator.cs
│   │   └── Queries/
│   │       ├── GetStudentScheduleQuery.cs
│   │       └── GetStudentScheduleHandler.cs
│   ├── Hubs/
│   │   └── ITmsHubClient.cs           ← Strongly-typed hub contract
│   ├── Interfaces/
│   │   ├── ICourseService.cs
│   │   ├── IEnrollmentService.cs
│   │   ├── ICourseRepository.cs
│   │   ├── IEnrollmentRepository.cs
│   │   └── ICertificateService.cs
│   ├── Transcripts/
│   │   └── TranscriptModels.cs        ← State machine: Queued→Processing→Ready|Failed
│   └── Utilities/
│       └── DataShaper.cs              ← Field whitelist shaping
│
├── TmsApi.Infrastructure/             ← EF Core, external services, workers
│   ├── Caching/
│   │   ├── CacheKeys.cs               ← Schema-versioned keys
│   │   ├── CachedCourseService.cs     ← HybridCache hit/miss logging
│   │   └── TmsMeters.cs               ← OpenTelemetry cache counters
│   ├── ExternalServices/
│   │   └── CertificateService.cs      ← Polly v8 resilience pipeline
│   ├── Persistence/
│   │   ├── Configurations/
│   │   │   ├── StudentConfiguration.cs
│   │   │   ├── CourseConfiguration.cs
│   │   │   └── EnrollmentConfiguration.cs
│   │   ├── TmsDbContext.cs
│   │   └── DataSeeder.cs              ← 25 deterministic courses
│   ├── Services/
│   │   ├── CourseService.cs
│   │   ├── EnrollmentService.cs
│   │   ├── TokenService.cs            ← JWT generation
│   │   └── CryptoDemoService.cs
│   ├── Transcripts/
│   │   ├── ITranscriptStatusStore.cs
│   │   └── InMemoryTranscriptStatusStore.cs
│   └── Workers/
│       └── TranscriptWorker.cs        ← BackgroundService + Channel<T>
│
└── TmsApi.Domain/                     ← Pure C# entities, zero dependencies
    └── Entities/
        ├── Student.cs
        ├── Course.cs
        ├── Enrollment.cs
        ├── Assessment.cs
        ├── Certificate.cs
        ├── TmsUser.cs                 ← Extends IdentityUser
        └── RefreshToken.cs
```

---

## 🛠️ Tech Stack

| Concern | Technology |
|---|---|
| Framework | ASP.NET Core 10 |
| Database | PostgreSQL 18 |
| ORM | Entity Framework Core 10 (Npgsql) |
| Architecture | Clean Architecture — 4 projects |
| CQRS / Mediator | MediatR |
| Validation | FluentValidation |
| Authentication | ASP.NET Core Identity + JWT Bearer |
| Password Hashing | BCrypt.Net-Next |
| Caching | Microsoft.Extensions.Caching.Hybrid |
| Real-time | ASP.NET Core SignalR |
| Resilience | Polly v8 (Timeout → CircuitBreaker → Retry) |
| Rate Limiting | ASP.NET Core RateLimiting (Token Bucket) |
| API Versioning | Asp.Versioning.Http |
| API Docs | Scalar (OpenAPI) |
| Observability | OpenTelemetry (OTLP) + Health Checks |
| Background Jobs | BackgroundService + System.Threading.Channels |

---

## ✨ Features

### API & Contract
- **URL-segment versioning** — V1 (deprecated) and V2 served simultaneously
- **V1 deprecation headers** — `Deprecation: true`, `Sunset`, `Link` on every V1 response
- **RFC 7807 ProblemDetails** — every error response is structured JSON
- **Data shaping** — `?fields=id,title` with per-DTO whitelist security
- **HATEOAS** — self/next/prev on collections, one action link on detail
- **Scalar explorer** — Development only, hidden in Production

### Architecture
- **CQRS with MediatR** — commands write, queries read, controllers are HTTP-only
- **Typed Result<T,E>** — no exceptions for business failures
- **LoggingBehavior** — correlation ID + elapsed ms on every MediatR request
- **ValidationBehavior** — FluentValidation in the pipeline, not in controllers
- **GlobalExceptionHandler** — central RFC 7807 translation

### Security
- **ASP.NET Core Identity** — BCrypt passwords, lockout after 5 failed attempts
- **JWT Bearer** — 15-minute access tokens, HMAC-SHA256 signed
- **Refresh Token Rotation** — single-use tokens, theft detection revokes all sessions
- **Resource-based authorization** — instructors can only edit their own courses
- **Security headers** — X-Frame-Options, X-Content-Type-Options, CSP
- **CORS** — named policy, AllowCredentials, no wildcard origin
- **XSRF protection** — double-submit cookie pattern

### Performance & Reliability
- **HybridCache** — stampede protection, tag invalidation, hit/miss observable in logs
- **Tier-aware rate limiting** — Anonymous / Free / Paid token buckets per API key
- **Polly v8** — Timeout → CircuitBreaker → Retry, scoped to transient failures only
- **Pagination** — mandatory bounded collections, page size capped at 50

### Real-time & Background
- **SignalR** — typed hub `ITmsHubClient`, group dispatch, auto-reconnect
- **Async transcript** — 202 Accepted + status URL + BackgroundService worker
- **Idempotency keys** — double-click safe, same key returns same report ID
- **State machine** — `Queued → Processing → Ready | Failed`

### Observability
- **Health checks** — `/health/live` (liveness) and `/health/ready` (DB readiness)
- **OpenTelemetry** — traces + metrics exported via OTLP (Aspire / Jaeger)
- **Custom meters** — `tms.cache.hits` and `tms.cache.misses`
- **Structured logging** — JSON console with TraceId on every line

---

## 🚀 Getting Started

### Prerequisites

| Tool | Version |
|---|---|
| .NET SDK | 10.x |
| PostgreSQL | 18.x |
| dotnet-ef CLI | 10.x |

```bash
# Verify .NET version
dotnet --version

# Install EF CLI tool (if missing)
dotnet tool install --global dotnet-ef

# Verify EF tool
dotnet ef --version
```

### 1. Clone and configure

```bash
git clone <your-repo-url>
cd TmsApi
```

### 2. Set connection string

Edit `TmsApi.Api/appsettings.Development.json`:

```json
{
  "ConnectionStrings": {
    "TmsDatabase": "Host=localhost;Database=TmsDb;Username=postgres;Password=yourpassword"
  },
  "AllowedOrigins": ["http://localhost:4200"],
  "Jwt": {
    "Issuer": "https://localhost:5001",
    "Audience": "tms-client",
    "ExpiryMinutes": 15
  },
  "TmsApi": {
    "PublicBaseUrl": "https://localhost:5001"
  }
}
```

### 3. Set JWT signing key (user secrets)

```bash
dotnet user-secrets set "Jwt:Key" "A-Very-Long-Secret-Key-Min-32-Chars-2026" \
  --project TmsApi.Api
```

### 4. Apply database migrations

```bash
dotnet ef database update \
  --project TmsApi.Infrastructure/TmsApi.Infrastructure.csproj \
  --startup-project TmsApi.Api/TmsApi.Api.csproj
```

### 5. Run

```bash
dotnet run --project TmsApi.Api/TmsApi.Api.csproj
```

### 6. Open API explorer

```
https://localhost:5001/scalar/v1
```

> The deterministic seeder runs on startup in Development and inserts 25 courses automatically.

---

## 📡 API Endpoints

### Courses

| Method | Route | Description |
|---|---|---|
| `GET` | `/api/v1/courses` | V1 paged list (deprecated) |
| `GET` | `/api/v2/courses` | V2 paged list — `data/meta/links` envelope |
| `GET` | `/api/v2/courses?fields=id,title` | Shaped response |
| `GET` | `/api/v2/courses?search=fund&page=1&pageSize=10` | Filter + paginate |
| `GET` | `/api/v2/courses/{id}` | Detail with HATEOAS links |
| `POST` | `/api/v2/courses` | Create (409 on duplicate code) |

### Enrollments

| Method | Route | Description |
|---|---|---|
| `POST` | `/api/v2/enrollments` | Enroll student via CQRS command |
| `GET` | `/api/v2/enrollments/{studentId}/schedule` | Student schedule query |
| `GET` | `/api/v2/courses/{courseId}/enrollments` | List enrollments for course |
| `GET` | `/api/v2/courses/{courseId}/enrollments/{id}` | Single enrollment |

### Transcripts

| Method | Route | Description |
|---|---|---|
| `POST` | `/api/v2/transcripts` | Request transcript (returns 202 immediately) |
| `GET` | `/api/v2/transcripts/{id}/status` | Poll status: Queued → Processing → Ready |

### Certificates

| Method | Route | Description |
|---|---|---|
| `POST` | `/api/v2/certificates` | Issue certificate (Polly-protected) |

### Auth

| Method | Route | Description |
|---|---|---|
| `POST` | `/api/auth/register` | Register user with role |
| `POST` | `/api/auth/login` | Login → `{accessToken, refreshToken}` |
| `POST` | `/api/auth/refresh` | Rotate refresh token |

### Infrastructure

| Method | Route | Description |
|---|---|---|
| `GET` | `/health/live` | Liveness probe (always fast) |
| `GET` | `/health/ready` | Readiness probe (checks DB) |
| `GET` | `/scalar/v1` | API explorer (Development only) |
| WebSocket | `/hubs/tms` | SignalR hub |

---

## 🔐 Authentication Flow

```bash
# 1. Register
curl -X POST https://localhost:5001/api/auth/register \
  -H "Content-Type: application/json" \
  -d '{
    "email": "liya@cotbe.edu.et",
    "password": "SecurePass123!",
    "firstName": "Liya",
    "lastName": "Kebede",
    "role": "Student"
  }'

# 2. Login — get tokens
curl -X POST https://localhost:5001/api/auth/login \
  -H "Content-Type: application/json" \
  -d '{"email":"liya@cotbe.edu.et","password":"SecurePass123!"}'
# Response: { "accessToken": "...", "refreshToken": "..." }

# 3. Call protected endpoint
curl https://localhost:5001/api/v2/courses \
  -H "Authorization: Bearer <accessToken>"

# 4. Rotate tokens before expiry (15 min)
curl -X POST https://localhost:5001/api/auth/refresh \
  -H "Content-Type: application/json" \
  -d '{"refreshToken":"<refreshToken>"}'
```

---

## 🧪 Running Tests

```bash
dotnet test
```

---

## 🗄️ Database Migrations

```bash
# Add a migration
dotnet ef migrations add <MigrationName> \
  --project TmsApi.Infrastructure/TmsApi.Infrastructure.csproj \
  --startup-project TmsApi.Api/TmsApi.Api.csproj

# Apply migrations
dotnet ef database update \
  --project TmsApi.Infrastructure/TmsApi.Infrastructure.csproj \
  --startup-project TmsApi.Api/TmsApi.Api.csproj

# List applied migrations
dotnet ef migrations list \
  --project TmsApi.Infrastructure/TmsApi.Infrastructure.csproj \
  --startup-project TmsApi.Api/TmsApi.Api.csproj
```

---

## ⚙️ Configuration Reference

| Key | Description | Example |
|---|---|---|
| `ConnectionStrings:TmsDatabase` | PostgreSQL connection string | `Host=localhost;Database=TmsDb;...` |
| `Jwt:Key` | HMAC-SHA256 signing key (use user-secrets) | min 32 chars |
| `Jwt:Issuer` | Token issuer | `https://localhost:5001` |
| `Jwt:Audience` | Token audience | `tms-client` |
| `Jwt:ExpiryMinutes` | Access token lifetime | `15` |
| `TmsApi:PublicBaseUrl` | Base URL for internal HttpClient | `https://localhost:5001` |
| `AllowedOrigins` | CORS allowed origins | `["http://localhost:4200"]` |
| `ASPNETCORE_ENVIRONMENT` | Runtime environment | `Development` / `Production` |

---

## 🔬 Verify the Stack

```bash
# V1 deprecation headers
curl -i https://localhost:5001/api/v1/courses

# V2 envelope (data/meta/links)
curl https://localhost:5001/api/v2/courses

# Field shaping
curl "https://localhost:5001/api/v2/courses?fields=id,title"

# Invalid field → 400
curl -i "https://localhost:5001/api/v2/courses?fields=id,passwordHash"

# Health probes
curl https://localhost:5001/health/live
curl https://localhost:5001/health/ready

# Rate limiting — anonymous tier (first 10 succeed, then 429)
for i in {1..15}; do curl -s -o /dev/null -w "%{http_code}\n" https://localhost:5001/api/v2/courses; done

# Transcript with idempotency
curl -X POST https://localhost:5001/api/v2/transcripts \
  -H "Content-Type: application/json" \
  -H "Idempotency-Key: 11111111-2222-3333-4444-555555555555" \
  -d '{"studentId": 1}'
```

---

## 📊 Database Schema

```
Students          Courses           Enrollments
──────────        ───────           ───────────
Id (PK)           Id (PK)           Id (PK)
RegistrationNo    Code (UQ)         StudentId (FK)
Name              Title             CourseId (FK)
GPA               MaxCapacity       Grade (nullable)
IsActive          InstructorId      EnrolledAt
IsDeleted                           IsArchived

Assessments       Certificates      AspNetUsers (Identity)
───────────       ────────────      ─────────────────────
Id (PK)           Id (PK)           Id (PK)
Title             SerialNumber (UQ) Email
MaxScore          IssuedAt          FirstName
Weight            StudentId (FK)    LastName
CourseId (FK)     CourseId (FK)     Department

RefreshTokens
─────────────
Id (PK)
Token
UserId (FK)
ExpiresAt
IsUsed
IsRevoked
```

---

## 🏗️ Architecture Decisions

**Why Clean Architecture (4 projects)?**
Domain knows nothing. Application knows Domain. Infrastructure knows both. Api knows all. The compiler enforces this — a Domain class cannot accidentally import EF Core.

**Why Result<T,E> instead of exceptions?**
"Course is full" is an expected business outcome, not a bug. Typed results force callers to handle both paths. Exceptions stay loud for actual failures.

**Why MediatR?**
Controllers become HTTP-only (receive → dispatch → return). Business logic lives in handlers that can be unit tested without spinning up ASP.NET.

**Why HybridCache over IMemoryCache?**
Stampede protection — only one factory call fires under concurrent load. Tag-based invalidation — one call clears all related entries. Drop-in Redis upgrade — one line of config.

**Why separate /health/live and /health/ready?**
Liveness restarts the pod. Readiness removes it from the load balancer pool. A pod with a broken DB connection should stop receiving traffic, not be restarted.

---

## 📄 License

School project — CoTBE Software Engineering Programme 2026.