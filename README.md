# E-Commerce Web API — Clean Architecture (.NET 10)

> Production-style e-commerce REST API built with **ASP.NET Core 10**, following Clean Architecture +
> practical Domain-Driven Design (DDD). Includes EF Core / PostgreSQL persistence, optional MongoDB
> side-by-side provider toggle, ASP.NET Core Identity + JWT auth, Swagger UI, 161 automated tests
> (domain / application / integration with Testcontainers), a 5-service Docker Compose stack,
> **GitHub Actions CI (build + test on every push / PR) and CD (tag → SSH deploy to AWS EC2 + managed RDS)**.
> Live deployment at http://13.246.197.212:8080/swagger.

---

## 🎯 Feature Scope

| Area | Implemented |
|---|---|
| **Catalog** | Categories + Products CRUD (with slug-based lookup) |
| **Pricing** | `Money` value object with currency guards (USD/EUR/etc.) |
| **Stock** | `IncreaseStock / DecreaseStock` with domain invariants |
| **Identity** | Register / Login / Get Profile (ASP.NET Core Identity, JWT) |
| **Roles** | `Admin`, `Customer` role-based authorization |
| **Shopping Cart** | Add item / update qty / remove item / clear / sum totals |
| **Orders** | Checkout (Cart → Order + OrderItems), state machine, Cancel |
| **Order State Machine** | `Pending → Paid → Shipped → Delivered`; Cancel valid from `Pending` / `Paid` |
| **Validation** | FluentValidation pipeline behavior via MediatR |
| **Error Handling** | Global middleware → proper HTTP status codes + problem details |
| **API Documentation** | Swagger UI + OpenAPI 3.0 JSON (always on, not dev-only) |
| **Persistence Toggles** | `Database:Provider = Postgres | Mongo` (Identity always stays on Postgres) |
| **Containerization** | Dockerfile (multi-stage) + docker-compose (5 services + volumes) |
| **CI (GitHub Actions)** | Workflow runs build + 3 test layers on every push / PR. GHCR image push on main. See [.github/workflows/ci.yml](file:///home/michael/projects/learning/c#/E-commerce_Api/.github/workflows/ci.yml) |
| **CD (GitHub Actions)** | Tag `v*` → SSH into EC2 → `docker compose pull && up -d` → health curl against `/api/categories`. See [.github/workflows/cd.yml](file:///home/michael/projects/learning/c#/E-commerce_Api/.github/workflows/cd.yml) |

Explicitly out of scope (for now): payment gateway, shipping carriers, email, reviews, search, CDN, frontend.

---

## 🏗️ Architecture

Clean Architecture — dependencies always point **inward**.

```
ECommerce.slnx
├── src/
│   ├── ECommerce.Domain/             ← No external deps. Pure business rules.
│   │   ├── Entities/                 Category, Product, Cart, CartItem, Order, OrderItem
│   │   ├── ValueObjects/             Money (immutable, currency-aware)
│   │   ├── Enums/                    OrderStatus, Role
│   │   ├── Interfaces/               I{Aggregate}Repository + IUnitOfWork (ISP)
│   │   └── Exceptions/               InvalidPriceException, CurrencyMismatchException…
│   │
│   ├── ECommerce.Application/        ← Depends ONLY on Domain
│   │   ├── UseCases/                 MediatR IRequest / IRequestHandler pairs
│   │   │   ├── Categories/           Create / List / GetById / Update / Delete
│   │   │   ├── Products/             Create / List (paged) / GetById / GetBySlug /…
│   │   │   ├── Carts/                AddItemToCart, UpdateQuantity, ClearCart…
│   │   │   ├── Orders/               Checkout, ListMyOrders, GetById, CancelOrder…
│   │   │   └── Identity/             Register, Login, GetCurrentUserProfile
│   │   ├── DTOs/                     CategoryDtos, ProductDtos, CartDtos, OrderDtos, IdentityDtos
│   │   ├── Mappings/                 CatalogMappingProfile, CommerceMappingProfile (Mapster IRegister)
│   │   ├── Behaviors/                ValidationBehavior<TRequest,TResponse>
│   │   ├── Validators/               FluentValidation AbstractValidator per command
│   │   ├── Interfaces/               ICurrentUserService, IDateTimeProvider, IIdentityService
│   │   └── Exceptions/               NotFoundException<T>, ConflictException, ForbiddenException…
│   │
│   ├── ECommerce.Infrastructure/     ← Depends on Domain + Application
│   │   ├── Persistence/
│   │   │   ├── ECommerceDbContext.cs          EF Core (PostgreSQL / Identity tables)
│   │   │   ├── Configurations/                IEntityTypeConfiguration per aggregate
│   │   │   ├── Migrations/                    20260912041737_InitialCreate (EF auto)
│   │   │   ├── Repositories/                  Ef*Repository.cs (Postgres impls)
│   │   │   └── MongoDb/                       Mongo*Repository.cs + MongoUnitOfWork
│   │   ├── Identity/                  AppUser, IdentityService, JwtSettings
│   │   ├── Extensions/                InfrastructureServiceCollectionExtensions, DatabaseSeeder
│   │   └── Services/                  DateTimeProvider
│   │
│   └── ECommerce.Api/                 ← Composition root. Depends on Infrastructure + Application
│       ├── Controllers/               Categories, Products, Carts, Orders, Identity
│       ├── Middleware/                ExceptionHandlingMiddleware
│       ├── Services/                  CurrentUserService
│       └── Program.cs                 DI, Auth, Swagger, MigrateAndSeed, pipeline
│
└── tests/
    ├── ECommerce.Domain.UnitTests/           116 tests — pure domain invariants
    ├── ECommerce.Application.UnitTests/       32 tests — handlers with NSubstitute mocks
    └── ECommerce.Api.IntegrationTests/       13 tests  — WebApplicationFactory + Testcontainers.PostgreSql
```

### Persistence — Dual provider toggle

Identity (users / roles) **always runs on Postgres**. For catalog + commerce data, switch provider with a single key in `appsettings.json`:

```json
{ "Database": { "Provider": "Postgres" } }
```

Supported values: `Postgres` (default) or `Mongo`. Both implementations have the same repository surface.

---

## ⚙️ Prerequisites

| Tool | Min Version | Notes |
|---|---|---|
| .NET SDK | **10.0.100+** | `dotnet --list-sdks` to check |
| Docker Engine | **24+** | `docker version` |
| Docker Compose | **v2+** | `docker compose version` (v2, not python docker-compose) |
| (Optional) dotnet-ef | **10.0.x** | `dotnet tool install --global dotnet-ef` — only for adding new migrations |

---

## 🚀 Quick start (3 ways)

### Option 1: Docker Compose (recommended — one command, 5 services)

```bash
cd <repo-root>

# Builds + pulls images, starts all services.
docker compose up --build -d
```

Wait ~60 seconds for migrations + identity seed to run, then open in your browser:

| URL | What |
|---|---|
| **http://localhost:8080/swagger** | ✅ Swagger UI (interactive API docs) |
| http://localhost:8080/api/products | REST endpoint — returns `PagedResponse<ProductResponse>` |
| http://localhost:8082 | Adminer (PostgreSQL UI; server = `ecommerce-postgres`, user = postgres, pw = postgres, db = ecommerce) |
| http://localhost:8081 | Mongo Express (basic auth: admin / admin) |

To stop while preserving data:

```bash
docker compose down
# data lives in named Docker volumes:
#   e-commerce_api_ecommerce_pg_data
#   e-commerce_api_ecommerce_mongo_data
#   e-commerce_api_ecommerce_aspnet_keys

docker compose up -d   # resumes with persisted data + identities
```

To fully reset: `docker compose down -v` (destroys volumes).

---

### Option 2: Run API locally, DBs in Docker

```bash
# 1. Start only Postgres + MongoDB containers:
docker compose up -d postgres mongodb

# 2. Restore + build + run the API:
dotnet restore
dotnet build
dotnet run --project src/ECommerce.Api/ECommerce.Api.csproj
```

Swagger UI: https://localhost:5001/swagger (or whatever ASP.NET Core picks via launchSettings.json).
Migrations + seed run on startup — database is ready before first request.

---

### Option 3: Everything native (Postgres installed locally)

1. Set up PostgreSQL, create db `ecommerce`, user `postgres` / pw `postgres`
2. Update `src/ECommerce.Api/appsettings.Development.json` → `ConnectionStrings:DefaultConnection`
3. Run `dotnet run --project src/ECommerce.Api/ECommerce.Api.csproj`

---

## 🔐 Seeded credentials (auto-created on first run)

| Email | Password | Role |
|---|---|---|
| `admin@example.com` | `Admin123!` | Admin + Customer |

For registration, use `POST /api/identity/register`:

```json
{
  "email": "you@example.test",
  "password": "Strong@123",
  "firstName": "Jane",
  "lastName": "Doe"
}
```

New users are automatically assigned the `Customer` role. Elevate to `Admin` via direct SQL or a custom command.

---

## 🧪 Testing

Phase 6 delivered **161/161 tests across 3 layers**. Run them:

```bash
# All 3 test projects (sequential):
dotnet test tests/ECommerce.Domain.UnitTests/ECommerce.Domain.UnitTests.csproj
dotnet test tests/ECommerce.Application.UnitTests/ECommerce.Application.UnitTests.csproj
dotnet test tests/ECommerce.Api.IntegrationTests/ECommerce.Api.IntegrationTests.csproj
```

Current scoreboard:

| Layer | # Tests | Run time | Technology |
|---|---|---|---|
| **Domain.UnitTests** | 116 | ~1 s | xUnit + FluentAssertions. No I/O allowed. Tests entity/value-object invariants. |
| **Application.UnitTests** | 32 | ~15 s | xUnit + FluentAssertions + **NSubstitute 6.x**. Handlers invoked directly with substituted repositories, ICurrentUser, IDateTimeProvider. Mapster config cloned per class (isolation). |
| **Api.IntegrationTests** | 13 | ~25 s | xUnit + `WebApplicationFactory<Program>` + **Testcontainers.PostgreSql** (throwaway postgres:16-alpine per collection). Full HTTP pipeline incl. JWT auth, EF migrations, Identity seed. Shared via `[CollectionDefinition]` so 3 test classes reuse ONE container. |

### Educational highlights in the tests

- **Money tests expose a subtle bug** we fixed: Money ctor validated currency length *before* `Trim()` → `InvalidCurrencyException` on `"  eur  "`. All value objects now **normalize first, then validate**.
- **Order tests expose a Guid getter anti-pattern**: `static Guid ValidCustomerId => Guid.NewGuid()` created a *different id each access*, so `CreateFromCart` got mismatched customerIds. Pattern fixed by capturing once in a helper factory.
- **NSubstitute sequential Returns gotcha**: mixing lambdas and values in `Returns(value, lambda, lambda)` is a `CS1503`. Either go all-lambda or use a closure call-counter. See `CartAndOrderHandlerTests.cs`.
- **Mapster test isolation**: `TypeAdapterConfig.GlobalSettings.Clone()` + `Scan(ApplicationAssembly)` per test class, so GlobalSettings singleton isn't polluted across classes.
- **Integration tests use IClassFixture** but the Program class is `internal` from top-level statements, so `[InternalsVisibleTo("ECommerce.Api.IntegrationTests")]` lives in `ECommerce.Api.csproj`.

---

## 🐳 Docker deep-dive (Phase 7 deliverables)

### `Dockerfile` — 3-stage multi-stage

```
mcr.microsoft.com/dotnet/sdk:10.0   AS build    (restore + build Release)
  └─► publish stage                    (dotnet publish /p:UseAppHost=false → /app/publish)
       └─► mcr.microsoft.com/dotnet/aspnet:10.0 AS final   (runtime only, ~250 MB)
```

Key details:
- `/p:UseAppHost=false` mirrors the `ECommerce.Api.csproj` property — produces a framework-dependent DLL instead of a native binary.
- **Layer caching optimization**: `COPY *.csproj → RUN dotnet restore` FIRST, then `COPY src/` second. Reordering code means Docker hits the cache from restore onward.
- `.dockerignore` strips `bin/`, `obj/`, `tests/`, `.git/`, `*.md`, so build context is ~50 KB (not 300+ MB).

### `docker-compose.yml` — 5 services with health-based ordering

| Service | Image | Ports | Healthcheck |
|---|---|---|---|
| `postgres` | postgres:17-alpine | 5432:5432 | `pg_isready -U postgres` every 5 s |
| `mongodb` | mongo:7 | 27017:27017 | `mongosh ping` every 10 s |
| `adminer` | adminer:latest | 8082:8080 | — |
| `mongo-express` | mongo-express:1.0.2-20-alpine3.19 | 8081:8081 | — |
| ✨ **api** (new) | `ecommerce-api:local` (build from ./Dockerfile) | 8080:8080 | `wget --spider /api/categories` every 15 s, start_period=60 s |

Startup ordering is guaranteed with `depends_on: condition: service_healthy` — the API container doesn't even start until both DBs accept connections, so `MigrateAndSeedAsync` can never fail with "Npgsql connection refused".

### Environment variables as config (docker-compose)

The compose file uses the standard Docker double-underscore convention to override nested `IConfiguration` sections:

| env var | replaces appsettings key |
|---|---|
| `Database__Provider=Postgres` | `{ "Database": { "Provider": "Postgres" } }` |
| `ConnectionStrings__DefaultConnection=Host=postgres;…` | `ConnectionStrings:DefaultConnection` |
| `MongoDb__ConnectionString=mongodb://…` | `MongoDb:ConnectionString` |
| `Jwt__SecretKey=THIS_IS_A_…` | `Jwt:SecretKey` |

This is the canonical way to provide config to a container — you never have to mount or rebuild for env-specific settings.

---

## 🌱 Adding new entities / migrations (dotnet-ef)

When you change domain entities or `ECommerceDbContext`:

```bash
# Create migration:
dotnet ef migrations add NewFeatureName \
  --project src/ECommerce.Infrastructure/ECommerce.Infrastructure.csproj \
  --startup-project src/ECommerce.Api/ECommerce.Api.csproj

# Apply migrations (or just restart API — MigrateAndSeed runs on startup):
dotnet ef database update \
  --project src/ECommerce.Infrastructure/ECommerce.Infrastructure.csproj \
  --startup-project src/ECommerce.Api/ECommerce.Api.csproj
```

---

## 🛠️ Troubleshooting

| Symptom | Likely cause | Fix |
|---|---|---|
| `NETSDK1064 Package Microsoft.CodeAnalysis.Analyzers 3.11.0 was not found` during Docker build | Using `--no-restore` on `dotnet build` in SDK 10.0.401 (NuGet cache path issue) | Never pass `--no-restore` to `dotnet build / publish` inside Dockerfile |
| `TRAIEngine memfd_create` stderr warning with `dotnet run` | TRAE sandbox kernel capability | Cosmetic only. Suppress with `<UseAppHost>false</UseAppHost>` (already done in Api csproj) |
| DataProtection keys re-generated → logged-out users | Keys written to `$HOME/.aspnet/DataProtection-Keys` by default which isn't a volume | Already handled: Program.cs redirects keys to `/tmp/ecommerce-aspnet-keys`. Docker compose mounts it as named volume `ecommerce_aspnet_keys`. |
| 401 Unauthorized on POST /api/categories with valid JWT | Check "Bearer " scheme in Authorization header, and that `role=Admin` is in the token's `role` claims | Login → inspect jwt.io → confirm role claim present |
| `CurrencyMismatchException` when adding cart items | Each cart product price's `Currency` must match all other lines | Catalog seed uses `USD`; keep it consistent when creating test products |
| `InvalidOrderStatusTransitionException: Cannot Cancel from Shipped` | Order state machine allows Cancel from `Pending` or `Paid` only | Work around in tests by not advancing past `Paid` before calling Cancel |

---

## 🗺️ Roadmap phases: 0 → 9

See [ROADMAP.md](file:///home/michael/projects/learning/c#/E-commerce_Api/ROADMAP.md) for the complete phase-by-phase plan.

Phase legend — **Completed phases (9/9)**:
- ✅ 0 Environment, 1 Solution structure, 2 Domain, 3 Application, 4 Infrastructure, 5 Presentation (API + Mongo), 6 Testing (161 tests), 7 Docker Containerization
- ✅ **8 GitHub Actions — CI** · Build + 3 test layers on every push / PR + GHCR image push on main → [ci.yml](file:///home/michael/projects/learning/c#/E-commerce_Api/.github/workflows/ci.yml)
- ✅ **9 CD + Deployment Considerations** · SSH tag-deploy to AWS EC2 + managed RDS Postgres (free tier) → live endpoint http://13.246.197.212:8080/swagger · [cd.yml](file:///home/michael/projects/learning/c#/E-commerce_Api/.github/workflows/cd.yml)


## 🔵 PHASE 8 — GitHub Actions: Continuous Integration

### Goal (per ROADMAP.md lines 328–351)

Push to `main` (or open a PR) → GitHub **automatically** runs:
1. `actions/checkout`
2. Setup .NET 9/10 SDK
3. `dotnet restore`
4. `dotnet build --no-restore`
5. (Optional but required for integration tests) spin up a PostgreSQL service container
6. `dotnet test --no-build`
7. ✅ On SUCCESS → `docker build` + tag image + push to **GHCR** (GitHub Container Registry) or Docker Hub
❌ Failure at any step → red ✖️ on the commit / PR.

### Step-by-step plan for you (open a PR and go step by step)

---

### 🔹 Step 1 — Prepare your GitHub repo

1. Create a new GitHub repo (if you haven't already):
   ```bash
   cd /home/michael/projects/learning/c#/E-commerce_Api
   git remote add origin https://github.com/<your-username>/E-commerce_Api.git
   ```
2. `git push -u origin main` (or whatever your default branch is)
3. Browse to repo → **Settings → Secrets and variables → Actions** — we'll add secrets later in step 7.

---

### 🔹 Step 2 — Create the workflow file

Create `.github/workflows/ci.yml`:

```
E-commerce_Api/
├── .github/
│   └── workflows/
│       └── ci.yml         ← HERE
├── src/
├── tests/
└── docker-compose.yml
```

Every GitHub Actions workflow needs 5 core concepts:
- **on**: when does it run? (push, pull_request, manual)
- **jobs**: logical units of work (run on a machine called a "runner")
- **runs-on**: what OS? (`ubuntu-latest` is standard / free / Docker-compatible)
- **steps**: sequential commands inside a job (each runs in the same shell unless `uses:` a composite action)
- **services**: long-running containers attached to the job (like PostgreSQL!)

#### Suggested workflow skeleton you can fill out:

```yaml
# <repo-root>/.github/workflows/ci.yml
name: CI

on:
  push:
    branches: [ main ]            # build on every push to main
  pull_request:
    branches: [ main ]            # build every PR targeting main
  workflow_dispatch:              # manual trigger button from Actions tab

jobs:
  build_and_test:
    name: Build + Test
    runs-on: ubuntu-latest

    # ---- STEP 5 (below) will require a PostgreSQL service container
    services:
      postgres:
        image: postgres:16-alpine
        env:
          POSTGRES_DB: ecommerce_test
          POSTGRES_USER: postgres
          POSTGRES_PASSWORD: postgres
        ports:
          - 5432:5432
        options: >-
          --health-cmd "pg_isready -U postgres"
          --health-interval 5s
          --health-timeout 5s
          --health-retries 10

    steps:
      - name: 1. Checkout code
        uses: actions/checkout@v4

      - name: 2. Setup .NET SDK
        uses: actions/setup-dotnet@v4
        with:
          dotnet-version: '10.0.x'     # SDK 10 latest — matches project's <TargetFramework>net10.0</TargetFramework>

      - name: 3. Restore NuGet packages
        run: dotnet restore   # will pick up ECommerce.slnx; if not, specify each project path

      - name: 4. Build (no-restore)
        run: dotnet build --no-restore -c Release /p:UseAppHost=false

      # Before running tests: Integration tests use Testcontainers (their own Postgres).
      # To avoid confusion we run them *separately* from Domain + Application unit tests.
      # Unit tests DON'T need PostgreSQL, so they can use localhost service only for Integration tests.

      - name: 5a. Domain Unit Tests
        run: dotnet test tests/ECommerce.Domain.UnitTests/ECommerce.Domain.UnitTests.csproj --no-build -c Release -v minimal --logger "trx;LogFileName=domain.trx"

      - name: 5b. Application Unit Tests
        run: dotnet test tests/ECommerce.Application.UnitTests/ECommerce.Application.UnitTests.csproj --no-build -c Release -v minimal --logger "trx;LogFileName=app.trx"

      - name: 5c. API Integration Tests (use the GitHub services postgres? keep Testcontainers?)
        # CHOICE A (simpler! Recommended for now): keep Testcontainers.
        #   The workflow simply needs Docker available → ubuntu-latest runners have Docker preinstalled.
        #   Testcontainers auto-pulls postgres:16-alpine at test time — no service config needed.
        # CHOICE B: pass ConnectionStrings__DefaultConnection pointing at ${{ job.services.postgres }}
        #   You'd refactor ECommerceApiFactory to NOT spin up Testcontainers but read from the env var.
        run: dotnet test tests/ECommerce.Api.IntegrationTests/ECommerce.Api.IntegrationTests.csproj --no-build -c Release -v minimal --logger "trx;LogFileName=integration.trx"

      # Optional: upload test results → visible in Actions UI's "Summary" page
      - name: Publish test results
        uses: actions/upload-artifact@v4
        with:
          name: test-results
          path: '**/TestResults/*.trx'
        if: always()        # even if tests fail, still show artifacts

  docker_push:
    name: Docker Build + Push to GHCR
    needs: build_and_test       # only runs if build_and_test succeeded
    runs-on: ubuntu-latest
    if: github.event_name == 'push' && github.ref == 'refs/heads/main'

    permissions:
      contents: read
      packages: write           # REQUIRED for GHCR push

    steps:
      - uses: actions/checkout@v4

      - name: Log in to GitHub Container Registry
        uses: docker/login-action@v3
        with:
          registry: ghcr.io
          username: ${{ github.actor }}
          password: ${{ secrets.GITHUB_TOKEN }}   # auto-provided by Actions, no need to create

      - name: Extract metadata (tags, labels)
        id: meta
        uses: docker/metadata-action@v5
        with:
          images: ghcr.io/${{ github.repository }}
          tags: |
            type=sha,format=long,prefix=
            type=ref,event=branch
            type=raw,value=latest,enable={{is_default_branch}}

      - name: Build + push
        uses: docker/build-push-action@v6
        with:
          context: .
          file: ./Dockerfile
          push: true
          tags: ${{ steps.meta.outputs.tags }}
          labels: ${{ steps.meta.outputs.labels }}
```

---

### 🔹 Step 3 — Commit + push, watch it fail on purpose

```bash
git add .github/workflows/ci.yml
git commit -m "ci: add GitHub Actions workflow"
git push
```

Open your repo → **Actions** tab. You should see the `build_and_test` job kick off.
Common **first-run failures** to expect (and learn from):

| Failure | Fix |
|---|---|
| `Could not find any project to restore` | `dotnet restore` needs an explicit project/sln argument. Your `.slnx` file isn't a real MSBuild solution — pass the 3 project paths separated by space, or pass `src/ECommerce.Api/ECommerce.Api.csproj` (it transitively restores Domain / App / Infra). |
| `Unknown package source` or 401 from nuget | No action needed — default nuget.org works. |
| Integration tests hang for 10 minutes then timeout | Docker daemon not started on runner? Testcontainers pulling too slow? Add verbosity `-v n` to the test step. |
| `NSubstitute throws` on CI but not locally | Transitive version differences. Our csproj pins NSubstitute 5.3.5 which resolves to 6.0.0 — OK, but keep an eye on it. |

---

### 🔹 Step 4 — Make it green

Iterate on the workflow until you see:

```
✅ build_and_test  —  build + test  succeeded (all 161 tests passing)
✅ docker_push     —  skipped on PR, runs on push to main only
```

Once green, test the failure path by **intentionally breaking one line** (e.g. change a `dotnet build` parameter to a non-existent config) and push. You must see the **red X** badge. Revert the break and confirm green again. This confirms the workflow actually validates commits.

---

### 🔹 Step 5 — Verify the GHCR image was actually pushed

After a green push to `main`:
1. Go to your GitHub profile → **Packages**
2. You should see a package called `ghcr.io/<your-user>/e-commerce_api:latest`
3. Copy the pull command shown on the page and run it locally:
   ```bash
   docker pull ghcr.io/<your-user>/e-commerce_api:latest
   docker run --rm -it -p 9090:8080 ghcr.io/<your-user>/e-commerce_api:latest
   ```
4. Navigate to http://localhost:9090/swagger → you should see Swagger UI.

> 💡 If the container refuses to start: Did you set env vars for `ConnectionStrings__DefaultConnection`? Without it the app tries to use `Host=localhost` and there is no Postgres inside the image. Use the local docker-compose file with Postgres + the GHCR image as your `api` service `image:` instead of `build:`.

---

### 🔹 Step 6 — Add a status badge to the README

Once CI is stable, repo → Actions → ⋮ menu on `CI` workflow → **Create status badge**.
Copy the markdown and paste it at the **top of your README** so every visitor sees a green `CI: passing` flag.

```markdown
[![CI](https://github.com/<you>/E-commerce_Api/actions/workflows/ci.yml/badge.svg)](https://github.com/<you>/E-commerce_Api/actions)
```

---

### 🔹 Step 7 (Extra credit) — Secrets + Docker Hub push

If you prefer **Docker Hub** instead of GHCR:

1. Create a Docker Hub account → Settings → Security → **New Access Token** (store it, it's shown once)
2. Go to GitHub repo → **Settings → Secrets and variables → Actions → New repository secret**:
   - Name: `DOCKERHUB_USERNAME`, Value: `<your docker id>`
   - Name: `DOCKERHUB_TOKEN`, Value: `<access token>`
3. Replace the `docker/login-action` block in your workflow:
   ```yaml
   - uses: docker/login-action@v3
     with:
       username: ${{ secrets.DOCKERHUB_USERNAME }}
       password: ${{ secrets.DOCKERHUB_TOKEN }}
   ```
4. Change `metadata-action@v5` `images:` to `<your-docker-id>/ecommerce-api`
5. Push and watch it push to hub.docker.com.

---

### ✅ Phase 8 self-checklist — you're done when:

- [ ] Every push / PR → Actions tab runs `build_and_test` with 0 errors
- [ ] 161 tests (or the number you have) show **Passed** in the step log
- [ ] Breaking a test → red ✖️; fixing → green ✔️
- [ ] On success of a push to `main`, workflow pushes an image to `ghcr.io` / Docker Hub
- [ ] README shows CI status badge with live state

---

## 🟢 PHASE 9 — Continuous Deployment + Deployment Considerations

### Goal (per ROADMAP.md lines 354–366)

CI passes → **automatic release / delivery** of the verified image. Concepts:

- **CI vs CD**: CI = every commit *verified* (build + test + docker push). CD = every *verified commit* is ready/shipped to production.
- **Deployment triggers**: release tag push (`v1.0.0`) vs push to `main` vs manual approval.
- **Deployment targets** (real-world options — you don't have to set them all up):
  - **VPS with Docker + docker-compose**: SSH in → `docker compose pull && docker compose up -d`
  - **Azure App Service / AWS ECS Fargate**: managed PaaS for containers
  - **Kubernetes / Azure Container Apps**: orchestrators for scaling
- **Deployment concerns** (think through each — you don't have to implement them, but know them):
  - Migrations: run *inside startup* (our current approach) vs an out-of-process pre-deploy job? What if migration fails?
  - Secrets in production: use managed identity + Azure KeyVault / AWS SSM. Never hard-code JWT secret or DB password in the image.
  - Reverse proxy: nginx / Caddy with Let's Encrypt HTTPS in front of Kestrel.
  - Domain + DNS: point `api.yourdomain.com` at VPS / load-balancer IP.
  - Backups: automatic pg_dump snapshots, retention policy, test that restoring them actually works.

### Suggested minimal CD you can implement for practice (pick ONE target):

#### Option A — "Tag = deploy to VPS" (cheapest, full-stack learning)

1. Get the cheapest VPS you can (DigitalOcean droplet, Hetzner CX11, Oracle free tier). Install Docker.
2. `scp` your `docker-compose.yml` to it (strip the `build:` block under `api:` and replace with `image: ghcr.io/<you>/ecommerce-api:latest`).
3. Create `cd.yml` workflow that:
   - Runs **only on `v*` tags** (`on: push: tags: ['v*']`)
   - Waits for CI (jobs: `needs: [build_and_test]`)
   - SSHes into VPS via `appleboy/ssh-action@v1` with secrets `VPS_HOST`, `VPS_USER`, `VPS_SSH_KEY`
   - Runs on VPS:
     ```bash
     set -e
     cd /opt/ecommerce
     echo ${{ secrets.GITHUB_TOKEN }} | docker login ghcr.io -u ${{ github.actor }} --password-stdin
     docker compose pull api
     docker compose up -d api
     docker image prune -af
     ```
4. Create a git tag `git tag v0.1.0 && git push origin v0.1.0` → Actions triggers CD → VPS now runs the new image.

#### Option B — "Azure App Service for Containers" (most PaaS-like, no ssh)

- Azure Portal → Create **App Service** → Publish = **Docker Container** → Image source = `GHCR`.
- Use `azure/webapps-deploy@v3` GitHub Action with publish profile secret.
- Every tag push deploys to the slot you define.

#### Option C — Concepts only (no infra needed, just docs for extra credit)

For the assignment (roadmap line 365: *"For the assignment, CI is sufficient. CD is conceptual documentation unless doing extra credit."*), you can simply **document the deployment options** in a `docs/deployment.md` file. Write 3 paragraphs for each option: "What it is / How would I set it up / Pros vs cons".

### ✅ Phase 9 self-checklist

- [x] You can explain the difference between CI and CD to another learner
- [x] You know at least 2 ways to trigger a deployment (tag push / push-to-main / manual / release)
- [x] You've practiced with ONE deployment target OR documented 3 conceptual options
- [x] You understand the 5 production concerns listed above (migrations, secrets, TLS, domain, backups)
- [x] `git tag v1.0.0 && git push origin v1.0.0` → app auto-deploys to live AWS URL (see below)

---

## 🌐 Live Deployment (Phase 9 — AWS EC2 + RDS)

As of `v1.0.0`, the API runs on real **AWS Free Tier** infrastructure in `af-south-1` (Cape Town):

| URL | Purpose |
|---|---|
| **http://13.246.197.212:8080/swagger/index.html** | ✅ Public Swagger UI (HTTP only; TLS + custom domain = Phase 10/extension work) |
| http://13.246.197.212:8080/api/products | REST endpoint → `PagedResponse<ProductResponse>` JSON |

Seeded credentials on the live deployment (RDS-persisted, identical to local compose):

| Email | Password | Role |
|---|---|---|
| `admin@example.com` | `Admin123!` | Admin + Customer |
| `customer@example.com` | `Customer@example.com!` | Customer |

### AWS stack used (both Free Tier eligible)

| AWS Service | Instance name | Shape / engine | Why chosen |
|---|---|---|---|
| **RDS** | `ecommerce-pg` | PostgreSQL SECOND 18.3, `db.t4g.micro`, 20 GB gp2 | Persistent managed DB. Backups + patching handled. Identity + catalog data survive any number of container restarts, `docker compose down`, or EC2 re-images. |
| **EC2** | `ecommerce-vm` | Ubuntu 24.04 LTS, `t3.micro` (x86), 8 GB gp3 | Single "cloud laptop" running Docker CE 29 + compose-plugin v2. Image pulled from `ghcr.io/ngenemicheal/e-commerce_api:latest` on every CD run. SSH-key only (RSA 2048 `.pem`) — password login disabled. |

### 5-command smoke test against the live endpoint

Run these from any internet-connected machine (no VPN, no AWS creds, no local Docker required):

```bash
# 1. Anon category list (proves API → RDS read path works)
curl -s http://13.246.197.212:8080/api/categories | python3 -m json.tool

# 2. Anon admin-only POST → expect HTTP 401 (auth middleware guarding admin routes)
curl -s -o /dev/null -w "HTTP %{http_code}\n" \
  -X POST -H "Content-Type: application/json" \
  -d '{"name":"anon-test","slug":"anon-test","description":"x"}' \
  http://13.246.197.212:8080/api/categories

# 3. Admin login → get JWT (proves Identity + ASP.NET Core Identity tables on RDS)
JWT=$(curl -s -X POST -H "Content-Type: application/json" \
  -d '{"email":"admin@example.com","password":"Admin123!"}' \
  http://13.246.197.212:8080/api/identity/login \
  | python3 -c "import sys,json; print(json.load(sys.stdin)['token'])")
echo "Got ${#JWT}-char JWT"

# 4. Authenticated category create → 201, persisted to RDS
CID=$(curl -s -X POST \
  -H "Authorization: Bearer $JWT" -H "Content-Type: application/json" \
  -d '{"name":"Live Deploy Test","slug":"live-deploy-test","description":"created via curl against live AWS"}' \
  http://13.246.197.212:8080/api/categories \
  | python3 -c "import sys,json; print(json.load(sys.stdin)['id'])")
echo "Created category id=$CID"

# 5. Retrieve it back anonymously (proves persistence across HTTP calls)
curl -s "http://13.246.197.212:8080/api/categories/$CID" | python3 -m json.tool
```

### CD workflow (how tags become live deployments)

Trigger deployments with a Git annotated tag:

```bash
# After a feature has been merged to main through the dev branch:
git checkout main && git pull origin main
git tag -a v1.1.0 -m "v1.1.0 — one-line summary of what changed"
git push origin v1.1.0
```

GitHub Actions → **CD — Deploy to AWS EC2 (SSH pull)** workflow runs these exact steps on the EC2 box:

1. Open an SSH session using secret `SSH_PRIVATE_KEY` (RSA 2048 `.pem`) → host `13.246.197.212` → user `ubuntu`.
2. `docker compose pull` against GHCR image `ghcr.io/ngenemicheal/e-commerce_api:latest`.
3. `docker compose up -d --remove-orphans` → rolling-recreate the single `ecommerce-api` container.
4. Prune old images to save disk on the 8 GB EC2 root.
5. Sleep 25 s → wait for ASP.NET boot + MigrateAndSeed → loop 9 attempts → `curl -sf http://127.0.0.1:8080/api/categories` → reports ✅ / ❌.

### Anti-drift branching rules (enforced from v1.0.0 onward)

The exact merge chain used in this project. Do **not** commit directly on `dev` or `main`:

```
feat/phase-X-new-thing  (daily work here. CI runs on push.)
        ↓ merge (PR or local --no-ff)
       dev              (integration branch. CI + manual CD runs here.)
        ↓ merge --no-ff (after phase passes all tests & manual QA)
      main              (release line. CI + docker_push to GHCR runs here.)
        ↓ merge --no-edit (CLOSE THE GAP. Prevents drift like main 1-commit ahead.)
       dev              (now points at the same commit as main again.)
```

Commands to produce a release tag without leaving `dev` behind:

```bash
# Precondition: the phase feature branch was already merged to dev.
git checkout main && git pull origin main
git merge --no-ff dev -m "Merge dev → main: v1.1.0 release"
git push origin main
git tag -a v1.1.0 -m "v1.1.0 — summary of deliverables"
git push origin v1.1.0
# ===== MISSING STEP THAT CAUSES DRIFT — DO NOT SKIP =====
git checkout dev && git pull origin dev
git merge --no-edit main
git push origin dev
```

### Hard-won Phase 8 + 9 lessons (not from tutorials)

Documented here so future-you doesn't re-learn them the hard way:

1. **EC2 launch screen "SSH from My IP" is a trap for SSH CD.** GitHub Actions runners come from a huge, ever-changing pool of Azure public IPs. You must add a *second* SSH security-group rule of `0.0.0.0/0 + ::/0` alongside the My IP rule, and rely on RSA `.pem` key strength (not on source-IP restriction) for SSH auth. Symptom if you forget: `nc -zv $host 22` works from home but GitHub Actions runner reports `dial tcp :22: i/o timeout`.
2. **appleboy/ssh-action's `envs_format: auto` line prints two harmless `bash: auto: command not found` warnings.** Remove the `envs_format:` line entirely — environment forwarding still works, secrets still mask, and the two noise lines go away.
3. **Inside the remote SSH script, `set -euo pipefail` will crash on `${GITHUB_SHA:0:7}` slice + default.** The runner env vars are in the runner process, not auto-forwarded to the non-interactive EC2 bash. Use the action's `envs:` list to pass them explicitly, and either drop `-u` or add explicit `VAR="${GITHUB_SHA:-fallback}"` defaults for every injected variable before the first use.
4. **Use a 4-step connectivity DEBUG step before troubleshooting SSH keys.** Add a temporary step that runs `getent hosts $SSH_HOST` + `nc -zv 22` + key length checks from the runner. It costs ~10 s per run, and immediately distinguishes "SG is blocking TCP 22" (nc timeout) from "I accidentally pasted a truncated private key into secrets" (nc reachable, key length <1 000 chars).
5. **Skip ECS/Fargate on a first learning deployment.** The ECS Console wizard wraps service creation in a CloudFormation stack that rolls back with *"Stack rollback paused"* banners and zero actionable error messages. Go EC2 Ubuntu + `docker compose` first (exact same pattern as your laptop), then add the complexity of ECS only if you need auto-scaling, spot fleets, or scheduled tasks.
6. **`workflow_dispatch:` button is the primary iteration loop for CD debugging.** Don't burn 20 tag pushes per session. Fix bugs on a chore branch, merge up the chain to main, then use **Actions → Workflow → Run workflow → pick branch=main** 20 times. Only when it's clean, push a real annotated `v*` tag for the production history.

---

## 📚 Learning map (files you should look at first if you want to deepen each concept)

| Concept | Files to study |
|---|---|
| DDD value objects + equality | [Money.cs](file:///home/michael/projects/learning/c#/E-commerce_Api/src/ECommerce.Domain/ValueObjects/Money.cs) |
| Entity invariants + state machine | [Order.cs](file:///home/michael/projects/learning/c#/E-commerce_Api/src/ECommerce.Domain/Entities/Order.cs) |
| Repository abstraction (interface segregation) | [ICartRepository.cs](file:///home/michael/projects/learning/c#/E-commerce_Api/src/ECommerce.Domain/Interfaces/ICartRepository.cs) |
| MediatR + CQRS handler | [Checkout.cs](file:///home/michael/projects/learning/c#/E-commerce_Api/src/ECommerce.Application/UseCases/Orders/Checkout.cs) |
| FluentValidation pipeline behavior | [ValidationBehavior.cs](file:///home/michael/projects/learning/c#/E-commerce_Api/src/ECommerce.Application/Behaviors/ValidationBehavior.cs) |
| Dual persistence (EF vs Mongo same interface) | Compare `EfProductRepository.cs` → `MongoProductRepository.cs` under Infrastructure/Persistence/ |
| JWT auth wiring (Program.cs) | [Program.cs](file:///home/michael/projects/learning/c#/E-commerce_Api/src/ECommerce.Api/Program.cs#L30-L53) |
| Testcontainers + WAF<Program> | [ECommerceApiFactory.cs](file:///home/michael/projects/learning/c#/E-commerce_Api/tests/ECommerce.Api.IntegrationTests/ECommerceApiFactory.cs) |
| Docker multi-stage + compose depends_on | [Dockerfile](file:///home/michael/projects/learning/c#/E-commerce_Api/Dockerfile), [docker-compose.yml](file:///home/michael/projects/learning/c#/E-commerce_Api/docker-compose.yml) |

---
