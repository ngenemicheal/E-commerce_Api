# E-commerce Web API — Complete Development Roadmap

> .NET 9 · Clean Architecture · DDD · EF Core · PostgreSQL · Docker · GitHub Actions CI/CD

---

## 1. Project Overview

**What we are building:** A production-style **E-commerce Web API** using .NET 9 that exposes RESTful endpoints for managing a realistic online store. The system will handle product catalog management, customer accounts, authentication/authorization, shopping carts, and order processing.

### Scope (realistic for an assignment)

- **Product Catalog:** CRUD for Products and Categories
- **Customers:** Registration, login, profile (using ASP.NET Core Identity)
- **Roles:** Admin vs. Customer authorization
- **Shopping Cart:** Add/remove items, adjust quantities
- **Orders:** Checkout (cart → Order with OrderItems), order status tracking
- **Validation & Error Handling:** Proper HTTP status codes, FluentValidation
- **API Docs:** Swagger/OpenAPI

### Explicitly out of scope (for now)

Payment gateway integration, real inventory/warehouse management, shipping carriers, email notifications, reviews/ratings, multi-vendor, search engines, caching layers, CDN, frontend.

---

## 2. Architecture — Clean Architecture + Practical DDD

### Recommended Solution Structure

```
ECommerce.sln
├── src/
│   ├── ECommerce.Domain/          ← Core: no dependencies on anything external
│   │   ├── Entities/              Aggregate roots + entities
│   │   ├── ValueObjects/          Immutable value types
│   │   ├── Enums/                 OrderStatus, Role etc.
│   │   ├── Interfaces/            Repository abstractions (IProductRepository…)
│   │   ├── Services/              Domain services (only if logic spans aggregates)
│   │   └── Exceptions/            Domain exceptions
│   │
│   ├── ECommerce.Application/     ← Depends ONLY on Domain
│   │   ├── UseCases/              Application services / CQRS handlers
│   │   │   ├── Products/
│   │   │   ├── Categories/
│   │   │   ├── Cart/
│   │   │   ├── Orders/
│   │   │   └── Identity/
│   │   ├── DTOs/                  Data Transfer Objects (in/out of API)
│   │   ├── Interfaces/            ICurrentUserService, IDateTimeProvider etc.
│   │   ├── Validators/            FluentValidation validators
│   │   ├── Mappings/              Mapster/AutoMapper profiles
│   │   ├── Behaviors/             MediatR pipelines (validation, logging, txn)
│   │   └── Exceptions/            Application-level exceptions
│   │
│   ├── ECommerce.Infrastructure/  ← Depends on Domain + Application
│   │   ├── Data/
│   │   │   ├── AppDbContext.cs         EF Core DbContext
│   │   │   ├── Configurations/         EF Core IEntityTypeConfiguration classes
│   │   │   ├── Migrations/             EF Core migrations (auto-generated)
│   │   │   └── SeedData/               Database initializer
│   │   ├── Repositories/               Concrete implementations of IRepository<T>
│   │   ├── Identity/                   ASP.NET Core Identity integration
│   │   └── Services/                   IDateTimeProvider etc. implementations
│   │
│   └── ECommerce.Api/                 ← Depends on Application (+ loosely Infra for DI wiring)
│       ├── Controllers/                Minimal APIs or controller-based endpoints
│       ├── Middleware/                 Exception handling, request logging
│       ├── Extensions/                 ServiceCollection extensions (DI wiring)
│       └── Program.cs                  Composition root
│
├── tests/
│   ├── ECommerce.Domain.UnitTests/
│   ├── ECommerce.Application.UnitTests/
│   └── ECommerce.Api.IntegrationTests/
│
├── docker/
│   ├── Dockerfile
│   ├── docker-compose.yml
│   └── docker-compose.override.yml
│
└── .github/workflows/
    ├── ci.yml
    └── cd.yml
```

### Layer Responsibilities & Dependency Direction

```
┌─────────────────────────────────────────────────────────┐
│                 Presentation (API)                      │  ← ASP.NET Core, Controllers, Swagger, Middleware
│  Composes everything. Knows about ALL layers ONLY      │  Knows how to wire DI containers
└───────────────────────────┬─────────────────────────────┘
                            │ uses (references)
                            ▼
┌─────────────────────────────────────────────────────────┐
│               Application (Use Cases)                   │  ← MediatR, DTOs, Validation, CQRS
│  Orchestrates the Domain.                              │  Defines WHAT the app DOES. Never HOW.
│  Depends on abstractions defined in Domain             │  Contracts for data access, external services
└───────────────────────────┬─────────────────────────────┘
                            │ uses (references)
                            ▼
┌─────────────────────────────────────────────────────────┐
│                     Domain (Core)                       │  ← Entities, ValueObjects, Enums
│  Business rules. Pure POCOs.                           │  ZERO external dependencies.
│  Defines repository interfaces (abstractions)          │  The "heart" — stable center
└─────────────────────────────────────────────────────────┘
                            ▲
                            │ implements (depends inward)
                            │
┌───────────────────────────┴─────────────────────────────┐
│                  Infrastructure                         │  ← EF Core, PostgreSQL, Identity
│  Implements Domain + Application abstractions          │  Concrete repositories, DbContext, DB specifics
│  "Dirty" layer — knows about databases & external      │  Can be replaced without changing Domain/App
└─────────────────────────────────────────────────────────┘
```

**Key principle: Dependencies point inward** — toward the Domain/Application. Infrastructure implements interfaces defined in the inner layers.

### DDD Concepts (practical, not dogmatic)

| DDD Concept | Where it applies in our system |
|---|---|
| **Entity** | `Product`, `Category`, `Customer`, `Order`, `Cart`, `CartItem`, `OrderItem` — have IDs and mutable state |
| **Value Object** | `Money` (Amount + Currency), `Address` (Street/City/Zip) — immutable, no ID, equality by value |
| **Aggregate Root** | `Product`, `Category`, `Order` (controls OrderItems), `Cart` (controls CartItems) — consistency boundary |
| **Repository** | Abstractions in Domain, implementations in Infrastructure |
| **Domain Service** | Only if needed (e.g. complex pricing across entities) |
| **Application Service** | Each Use Case handler via MediatR |
| **DTO** | In Application layer — what crosses API ↔ Application boundary |

---

## 3. Development Roadmap (Sequential, with Reasoning)

---

### Phase 1 — Solution Skeleton & Git Repository Initialization

1. **What we are building:** The empty .NET solution structure with 4 source projects + 3 test projects. Solution file, project references, initial `.gitignore`, first commit.
2. **Why at this point:** The architectural skeleton must exist before any logic. Project references *enforce* the dependency rule. Retrofitting architecture later creates chaos.
3. **Concepts to understand first:**
   - .NET solution (.sln) vs project (.csproj)
   - Project-to-project references in .NET
   - `classlib` vs `webapi` project types
   - Basic git: `init`, `commit`, `.gitignore`
4. **Technologies/tools introduced:** .NET 9 CLI, Git + GitHub, `.gitignore` for C# / Rider / VS
5. **Depends on:** Nothing. This is Day 1.
6. **Expected milestone:** `dotnet build ECommerce.sln` succeeds. Project references are correct (Infra → Domain + App; App → Domain; Api → App + Domain). Git repo has first commit.
7. **Time estimate (learner):** ½–1 day.
8. **How to verify:**
   ```bash
   dotnet restore ECommerce.sln
   dotnet build ECommerce.sln        # SUCCESS
   git log --oneline                 # shows initial commit
   ```
   **Manual check:** `Domain.csproj` has ZERO `<PackageReference>` entries. `Application.csproj` only references Domain + MediatR/Mapster/FluentValidation later in Phase 3.
9. **Deliberately NOT doing (yet):** EF Core, PostgreSQL, Docker, Swagger customization, authentication.

---

### Phase 2 — Domain Layer (The Heart of the System)

1. **What:** Implement entities, value objects, enums, domain exceptions, and **repository interface abstractions** (interfaces only — no implementations).
2. **Why before EF Core / DB:** The domain is the most stable layer. We model business concepts first with plain C# — no database influence. The model dictates persistence, not the reverse (core DDD).
3. **Concepts before starting:**
   - POCOs
   - Entity (has identity) vs Value Object (equality by value)
   - Aggregate = consistency boundary
   - Interface Segregation Principle (ISP)
   - Why repository interfaces live in Domain
4. **Technologies:** None new — just C# types. 0 NuGet deps.
5. **Depends on:** Phase 1 (skeleton + correct references).
6. **Milestone:** `Domain.csproj` builds. 0 NuGet package references. Entities enforce basic invariants via constructor guards / methods (not public set-all).
7. **Time estimate:** 2–3 days.
8. **Verify:**
   ```bash
   dotnet build src/ECommerce.Domain/ECommerce.Domain.csproj
   ```
   Manual code review: e.g. `Product.Price < 0` throws; stock changed via `DecreaseStock(qty)` method.
9. **Deliberately NOT doing:** `[Table]` attributes, DbContext, anything database-related.

---

### Phase 3 — Application Layer (Use Cases & DTOs)

1. **What:**
   - DTOs (CreateProductDto, ProductResponse…)
   - Use cases as MediatR Commands/Queries
   - Validators (FluentValidation)
   - Mapping profiles (Mapster — preferred for speed/simplicity)
   - Pipeline Behaviors (ValidationBehavior wraps each MediatR request)
   - Application exceptions (NotFoundException, ValidationException, ForbiddenException)
   - Abstractions: `ICurrentUserService`, `IDateTimeProvider`
2. **Why before Infrastructure/DB:** Application defines *what* the system does, independent of storage. Writing use cases first reveals exactly what the repository interfaces need to provide (Dependency Inversion).
3. **Concepts:**
   - CQRS (simple version)
   - MediatR: Request → Handler → Response
   - DTO vs Entity (never expose entities)
   - FluentValidation
   - Pipeline behaviors for cross-cutting concerns
   - Object mapping
4. **Technologies:** MediatR, FluentValidation, Mapster (NuGet in `Application.csproj`)
5. **Depends on:** Phase 2 (entities + repo interfaces exist so use cases compile).
6. **Milestone:** All use case handlers compile. Handlers inject repo interfaces (still unimplemented — that's fine).
7. **Time estimate:** 3–5 days.
8. **Verify:**
   ```bash
   dotnet build src/ECommerce.Application/ECommerce.Application.csproj
   ```
   Handlers follow: `_repository.AddAsync(entity)` → `_unitOfWork.SaveChangesAsync(ct)`. Validators check required/length/etc. No EF Core references anywhere in Application.
9. **Deliberately NOT doing:** DB, auth implementation, controllers.

---

### Phase 4 — API Layer (Controllers & DI Wiring, *No DB Yet!*)

1. **What:** ASP.NET Core endpoints (Controllers or Minimal APIs). Register MediatR, FluentValidation, Swagger. Exception handling middleware. DI wiring in Program.cs. Correct HTTP status codes. **Twist:** Inject *fake/in-memory repository implementations* (Dictionary-based).
2. **Why before DB:** The API is the entry point. Running with fakes validates the end-to-end chain: HTTP → MediatR → Handler → Fake Repo → Response. Catches architectural mismatches *before* adding DB complexity. Also forces learning the DI container — Dependency Injection clicks here.
3. **Concepts:**
   - ASP.NET Core Web API fundamentals
   - `Program.cs` as Composition Root (all services registered here)
   - `ActionResult<T>`, status codes
   - Middleware pipeline
   - Swagger/OpenAPI
   - Scoped vs Transient vs Singleton
   - DI container (IServiceCollection)
4. **Technologies:** ASP.NET Core Web API, Swashbuckle (Swagger)
5. **Depends on:** Phase 3 (use cases + DTOs + MediatR)
6. **Milestone:** API runs, Swagger at `/swagger` works, all endpoints functional with in-memory fake data.
7. **Time estimate:** 2–3 days.
8. **Verify:**
   ```bash
   dotnet run --project src/ECommerce.Api
   curl http://localhost:5000/api/products    # → JSON list of 3 fake products
   ```
   Invalid input → 400 ProblemDetails; not-found → 404.
9. **Deliberately NOT doing:** Database, real authentication.

---

### Phase 5 — Infrastructure Layer (EF Core + PostgreSQL + Identity)

1. **What:**
   - EF Core packages in `Infrastructure.csproj` (Npgsql provider + Tools)
   - `AppDbContext : DbContext` with DbSets
   - `IEntityTypeConfiguration` per entity (Fluent API, *not* data annotations)
   - Concrete Repository + UnitOfWork implementations (EF Core)
   - ASP.NET Core Identity integration (`AppUser` extending `IdentityUser`)
   - JWT authentication
   - Migrations + seeding
2. **Why NOW (not earlier):** Only after the whole system works with fakes do we swap in the real database. This proves Open/Closed — we *extend* by adding new implementations without modifying Domain/Application. Tables now map to stable domain entities (domain dictates DB, not reverse).
3. **Concepts:**
   - DbContext, DbSet, Change Tracker
   - Fluent API vs Data Annotations (prefer Fluent)
   - Migrations (`add-migration`, `update-database`, `script-migration`)
   - Connection strings, `appsettings.json` vs user secrets
   - ASP.NET Core Identity (UserManager, SignInManager) + JWT
   - Repository + Unit of Work with EF Core
   - Transactions, database seeding
4. **Technologies:** Npgsql EF Core provider, ASP.NET Core Identity.EntityFrameworkCore, JWT Bearer
5. **Depends on:** Phases 2–4 (entities + interfaces + DI container + app working with fakes)
6. **Milestone:** Local PostgreSQL running. Migration applied. Repo impls swapped in. Auth works. API against real DB.
7. **Time estimate:** 4–7 days (largest single phase — EF Core + Identity + JWT is the biggest lift).
8. **Verify:**
   ```bash
   dotnet ef migrations list       # shows InitialCreate
   psql/pgAdmin → tables visible
   /register → 200 with JWT; admin-only endpoint → 401 → 200 with token
   ```

---

### Phase 6 — Testing

1. **What:**
   - **Domain.UnitTests:** Entity invariants
   - **Application.UnitTests:** Handler logic with mocked repos
   - **Api.IntegrationTests:** Full pipeline via `WebApplicationFactory<Program>` + Testcontainers (throwaway Postgres per run)
2. **Why at this point:** Architecture is now stable. Writing tests earlier would churn with layer changes. Tests now catch bugs before containerization/CI.
3. **Concepts:**
   - xUnit (more common in modern .NET)
   - NSubstitute/Moq for mocking
   - FluentAssertions for readable assertions
   - `WebApplicationFactory<TProgram>` for integration testing
   - Testcontainers for disposable DB per test run
   - AAA pattern (Arrange-Act-Assert)
4. **Technologies:** xunit, NSubstitute, FluentAssertions, Microsoft.AspNetCore.Mvc.Testing, Testcontainers.PostgreSql
5. **Depends on:** Phases 2–5.
6. **Milestone:** 50+ tests passing. Integration tests against real disposable Postgres work.
7. **Time estimate:** 2–4 days.
8. **Verify:**
   ```bash
   dotnet test ECommerce.sln   # All green
   ```
   Key scenarios tested: create product → 201; checkout → Order+OrderItems created; unauth → 401; invalid DTO → 400 with errors.

---

### Phase 7 — Docker Containerization

1. **What:**
   - Dockerfile for API (multi-stage build: SDK publish → ASP.NET runtime)
   - `docker-compose.yml` with 2 services: `postgres:17-alpine` + `ecommerce-api`
   - `docker-compose.override.yml` for dev config
   - Connection string from env vars: `ConnectionStrings__DefaultConnection=Host=postgres;…`
   - Volume for Postgres data persistence; health checks
2. **Why after everything works locally:** Docker is *deployment packaging*. You only containerize an app that already works. Dockerizing first makes every local dev iteration painful (longer feedback loops).
3. **Concepts:**
   - Dockerfile instructions (FROM, WORKDIR, COPY, RUN, ENTRYPOINT)
   - Multi-stage builds (why SDK not in final image)
   - Docker Compose services, networks, volumes, `depends_on`
   - Environment variables overriding `IConfiguration`
   - Image vs Container
4. **Technologies:** Docker Engine, docker-compose
5. **Depends on:** Phases 5+6 (app builds locally, tests pass locally, connection string env-configurable)
6. **Milestone:** `docker-compose up --build` → API at `http://localhost:8080/swagger`; all endpoints work; down/up → data persists (volume).
7. **Time estimate:** 1–2 days.
8. **Verify:**
   ```bash
   docker-compose up --build
   curl http://localhost:8080/api/products
   docker-compose down; docker-compose up -d   # verify data persists
   ```

---

### Phase 8 — GitHub Actions — Continuous Integration (CI)

1. **What:** `.github/workflows/ci.yml` — triggered on push/PR to main:
   1. `actions/checkout`
   2. Setup .NET 9 SDK
   3. `dotnet restore`
   4. `dotnet build --no-restore`
   5. (Spin up Postgres service container)
   6. `dotnet test --no-build`
   7. On success → `docker build` + tag + push to GHCR (GitHub Container Registry) or Docker Hub
   Failure at any step → red X on PR/commit.
2. **Why after Docker:** Every build+test+docker step must *first* work locally. CI = automated verification — you can't debug CI if local doesn't work.
3. **Concepts:**
   - GitHub Actions: YAML, jobs, steps, runners (`ubuntu-latest`)
   - GitHub Actions services (Postgres container for tests)
   - CI = Build + Test automation
   - GitHub Repo → Settings → Secrets and variables
   - GHCR vs Docker Hub
4. **Technologies:** GitHub Actions workflows
5. **Depends on:** Git/GitHub (Phase 1), Docker (Phase 7)
6. **Milestone:** Every push/PR → Actions tab shows green checkmark. PRs blocked until CI passes.
7. **Time estimate:** 1–2 days (YAML debugging; test flakiness on CI runner).
8. **Verify:** Badge on repo shows CI: ✅. Purposefully break a test → commit → red X; fix → green.

---

### Phase 9 — Continuous Deployment (CD) & Deployment Considerations

1. **What:** `cd.yml` (separate workflow or job after CI). Trigger on release tag or push to main:
   1. Wait for CI to pass
   2. Log in to container registry
   3. `docker push` tagged image
   4. Deployment target options (conceptual for assignment):
      - Azure App Service / AWS ECS / VPS with Docker
      - `docker-compose pull && docker-compose up -d`
      - Orchestrators: Kubernetes / Azure Container Apps
   - Deployment considerations: production config, managed identity, reverse proxy (nginx/Caddy), HTTPS (Let's Encrypt), domain, DB backups, migrations in startup vs out-of-process.
   **For the assignment, CI (build+test+docker push) is sufficient.** CD is conceptual documentation unless doing extra credit.
2. **Why last:** CD = putting it live. You only go live when CI is reliably green. No point "deploying" an app whose CI is broken.
3. **Concepts:**
   - **CI vs CD:** CI = every commit verified. CD = every verified commit ready/shipped to prod.
   - Trunk-based vs release branching
   - Blue-green / rolling / canary deployments (conceptual)
   - Secrets, connection strings, HTTPS in production
4. **Technologies:** (Conceptual: Azure, AWS, SSH + Docker Compose)
5. **Depends on:** CI (Phase 8 passes reliably)
6. **Milestone:** README documents deployment steps OR a working CD pipeline that pushes image to GHCR.
7. **Time estimate:** 1 day (write-up); 2–3 days if actually deploying live.
8. **Verify:** Docker pull from registry → image runs. (If live deployment: `https://api.yourdomain.com/swagger` reachable.)

---

## 4. Dependency Map

```
 ╔══════════════════════════════════════════════════════════════════╗
 ║                  ARCHITECTURAL (code layers)                     ║
 ╠══════════════════════════════════════════════════════════════════╣
 ║                                                                  ║
 ║   ┌──────────────┐                                               ║
 ║   │   DOMAIN     │◄───────────────┐                              ║
 ║   │ (entities,   │                │ defines abstractions         ║
 ║   │  VOs, enums, │                │ (IProductRepository…)        ║
 ║   │  IRepo IFs)  │                │                              ║
 ║   └──────┬───────┘                │                              ║
 ║          │ references             │                              ║
 ║          ▼                        │                              ║
 ║   ┌──────────────┐                │                              ║
 ║   │ APPLICATION  │────────────────┤                              ║
 ║   │ (use cases,  │                │ concrete implementations     ║
 ║   │  DTOs, valid.│                │                              ║
 ║   └──────┬───────┘                │                              ║
 ║          │ references             │                              ║
 ║          ▼                        │                              ║
 ║   ┌──────────────┐          ┌─────┴──────────────┐               ║
 ║   │     API      │─────────►│  INFRASTRUCTURE    │               ║
 ║   │ (controllers,│          │  (EF Core repos,   │               ║
 ║   │  middleware, │          │   Identity impl,   │               ║
 ║   │  Swagger,    │          │   DbContext)       │               ║
 ║   │  DI wiring)  │          └────────────────────┘               ║
 ║   └──────────────┘                                               ║
 ║                                                                  ║
 ╚══════════════════════════════════════════════════════════════════╝

 ╔══════════════════════════════════════════════════════════════════╗
 ║               TECHNOLOGY (infrastructure stack)                  ║
 ╠══════════════════════════════════════════════════════════════════╣
 ║                                                                  ║
 ║   PostgreSQL (database engine)                                   ║
 ║        │ used by                                                 ║
 ║        ▼                                                         ║
 ║   Npgsql EF Core Provider                                        ║
 ║        │ used by                                                 ║
 ║        ▼                                                         ║
 ║   EF Core (DbContext + Fluent Configurations)                    ║
 ║        │ used in                                                 ║
 ║        ▼                                                         ║
 ║   Infrastructure (Repository + Identity Implementations)         ║
 ║        │ injected via                                            ║
 ║        ▼                                                         ║
 ║   .NET DI Container (IServiceCollection in Program.cs)           ║
 ║        │ resolves for                                            ║
 ║        ▼                                                         ║
 ║   Application Use Cases (MediatR handlers)                       ║
 ║                                                                  ║
 ╚══════════════════════════════════════════════════════════════════╝

 ╔══════════════════════════════════════════════════════════════════╗
 ║                 DEVOPS (delivery pipeline)                       ║
 ╠══════════════════════════════════════════════════════════════════╣
 ║                                                                  ║
 ║   Local Dev: `dotnet run` / `docker-compose up`                  ║
 ║        │ commit & push                                           ║
 ║        ▼                                                         ║
 ║   GitHub Repository (remote origin)                              ║
 ║        │ triggers (push / PR to main)                            ║
 ║        ▼                                                         ║
 ║   GitHub Actions — CI Workflow                                   ║
 ║   ├─ Checkout → Restore → Build → Test                           ║
 ║   └─ (on success) Docker Build → Push to Registry                ║
 ║        │ (optional deploy trigger)                               ║
 ║        ▼                                                         ║
 ║   GitHub Actions — CD Workflow / Deploy                          ║
 ║   ├─ Pull new image → `docker-compose up -d` on server           ║
 ║   └─ (or) deploy to AKS / ECS / Azure App Service                ║
 ║        │ result in                                               ║
 ║        ▼                                                         ║
 ║   Production: Running Containers (API + Postgres)                ║
 ║                                                                  ║
 ╚══════════════════════════════════════════════════════════════════╝
```

---

## 5. Timeline

| Phase | Work | Min (fast learner, full-time) | Comfortable (learning, part-time ~2h/day + wknds) |
|-------|------|--------:|--------:|
| 1. Solution + Git init  | Skeleton + refs + commit | ½ day | 1 day |
| 2. Domain               | Entities/VOs/Interfaces  | 2 days | 3 days |
| 3. Application          | UseCases/DTOs/Validators | 3 days | 5 days |
| 4. API (Fakes)          | Controllers/DI/Fakes     | 2 days | 3 days |
| 5. Infra (EF+PG+Identity) | Repos/DbContext/Auth/Migrations | 4 days | 7 days |
| 6. Testing              | Unit + Integration tests | 2 days | 4 days |
| 7. Docker               | Dockerfile + Compose     | 1 day | 2 days |
| 8. CI                   | GitHub Actions           | 1 day | 2 days |
| 9. CD (concept/live)    | Write-up or actual deploy | ½ day | 2 days |
| **Buffer** (debugging, learning curve issues, rework) | | **+ 2 days** | **+ 5 days** |
| **TOTAL** | | **~18 days (~3.5 weeks full-time)** | **~34 days (~7–8 weeks)** |

- **Minimum realistic timeline (full-time, motivated):** 3–4 weeks
- **Comfortable learning timeline (part-time):** 7–8 weeks

---

## 6. Milestones

| ID | Milestone | Definition of Done |
|----|-----------|---------------------|
| **M1** | Architecture Established | Solution builds. 4-layer project layout + correct reference directions. Git initialized. `dotnet build` succeeds. |
| **M2** | Domain Implemented | All entities, VOs, enums, domain exceptions, repository interfaces. Domain has 0 NuGet deps. Compiles. |
| **M3** | Use Cases Implemented | All use case handlers, DTOs, validators compile. MediatR + FluentValidation + Mapster wired. |
| **M4** | API Works (Fakes) | API runs. Swagger works. All CRUD endpoints functional with in-memory fake data. Correct HTTP codes. DI container wired. |
| **M5** | Database Persistence Works | PostgreSQL + EF Core. Migrations applied. Repo implementations swapped in. Identity + JWT auth works. Data persists across restarts. Seed data present. |
| **M6** | Tests Passing | 50+ tests. Domain unit tests, Application unit tests (mocked repos), API Integration tests (WebApplicationFactory + Testcontainers). All green. |
| **M7** | Dockerized | Dockerfile + docker-compose work. `docker-compose up --build` succeeds. API ↔ Postgres containers communicate. Volume persistence works. |
| **M8** | CI Working | GitHub Actions workflow. Every push/PR → restore → build → test → docker build. Green checkmark. Failures reported. |
| **M9** | CD/Deployment Ready | Docker images pushed to GHCR/Docker Hub. Deployment docs written. Optional: actual deploy working on server. |

---

## 7. Phase 1 — Exact Starting Point

### What Phase 1 covers (from the roadmap)

### Deliverables for Phase 1

1. **GitHub repository** — create repo on GitHub, clone locally (already at `/home/michael/projects/learning/c#/E-commerce_Api`)
2. **Solution structure** — create 7 projects under `src/` and `tests/`
3. **Project references** — wire them to enforce the inward dependency rule
4. **Clean up** scaffolded default files (Class1.cs, UnitTest1.cs, etc.)
5. **First git commit**

### What to learn BEFORE starting Phase 1

- Re-read Section 2 (Architecture) above — know what each project's responsibility is and the dependency arrows.
- Know `dotnet` CLI basics: `dotnet new`, `dotnet sln`, `dotnet add reference`, `dotnet build`.
- Know basic git workflow.

### What to DELIBERATELY NOT do in Phase 1

- ❌ NO Entity Framework / Npgsql packages
- ❌ NO PostgreSQL setup
- ❌ NO MediatR / FluentValidation / Mapster (Phase 3)
- ❌ NO authentication / Identity setup
- ❌ NO Swagger customization
- ❌ NO Docker
- ❌ NO GitHub Actions files
- ❌ NO entity classes / enums / interfaces (Phase 2!)
- ❌ NO controller modifications in API project — just delete the default WeatherForecast later, but not yet

### Verification checklist for Phase 1 "done"

- [ ] `dotnet build ECommerce.sln` → `Build succeeded. 0 Errors.`
- [ ] `ECommerce.Domain.csproj` has `<PackageReference>` count = 0
- [ ] `ECommerce.Application.csproj` references → `ECommerce.Domain` only
- [ ] `ECommerce.Infrastructure.csproj` references → `ECommerce.Domain` + `ECommerce.Application`
- [ ] `ECommerce.Api.csproj` references → `ECommerce.Application` + `ECommerce.Domain` (Infra added later when DI extension exists)
- [ ] Each test project references its corresponding source project
- [ ] No `Class1.cs` / `UnitTest1.cs` / `WeatherForecast.cs` / scaffolded leftovers
- [ ] `git log --oneline` shows one initial commit with the skeleton
