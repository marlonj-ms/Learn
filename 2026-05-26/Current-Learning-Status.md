# C# Learning Journey — Current Status

> **Last updated**: 2026-05-26 (Day 47 — End of Day)  
> **Mode**: C# Mentor walk-through (mode A — mentor reviews, learner writes)

---

## 🎯 Big Picture

**Original goal**: 0→1 production C# software — deployable, tested, observable, packaged mini-service.

**Status as of today**: ✅ **GOAL ACHIEVED.**

The original 3-Session arc (DI → API → Tests+Docker) is **fully complete**. You now have a `.NET 9` Web API that is:
- Unit-tested (16 unit tests in `TemperatureSensor.Core.Tests`)
- Integration-tested (3 in-process HTTP tests in `TemperatureSensor.Api.Tests`)
- Containerized (multi-stage Docker image, 92.9 MB)
- Verifiably running (curl.exe round-trips through the container)
- Cloud-portable (Linux base, same binary runs on Azure / AWS / GCP)

---

## ✅ 3-Session Arc Status — COMPLETE

### Session N+1: DI + ILogger + Lifetimes — ✅ DONE (2026-05-24)
- Built `DI-Logging-MinHost` with constructor injection + `ILogger<T>`
- Built `Lifetime-Drill` showing Transient / Singleton / Scoped side-by-side
- Locked-in: "Registration ≠ construction", "ctors fire in PHASE 3", "scope = HTTP request in ASP.NET Core"

### Session N+2: Minimal REST API — ✅ DONE (2026-05-25)
- Built `TemperatureSensor.Api` using `dotnet new web`
- 2 endpoints: `GET /` + `POST /readings`
- Wired `TemperatureSensor.Core` as project reference
- `IOptions<T>` + `appsettings.json` for threshold config
- Event handlers fire on threshold exceeded → ILogger warnings

### Session N+3: Integration tests + Docker — ✅ DONE (2026-05-26)
- 3 integration tests with `WebApplicationFactory<Program>`
- Multi-stage Dockerfile (SDK build → aspnet runtime)
- Image built (92.9 MB content size), container ran, curl.exe verified
- Three-test trio: GET /, valid POST, malformed POST → all green

---

## 📚 Skills Acquired (Cumulative)

### Language Fundamentals
- ✅ Delegates, events, event handlers (Day 6–10)
- ✅ Lambda expressions, closures
- ✅ Async/await, Task, `SemaphoreSlim` throttling
- ✅ LINQ (Where/Select/GroupBy/OrderBy/Aggregate)
- ✅ Pattern matching (basic)
- ✅ Nullable reference types

### Production Patterns
- ✅ Dependency Injection (Microsoft.Extensions.DependencyInjection)
- ✅ ILogger\<T\> + structured logging
- ✅ Lifetimes (Transient / Singleton / Scoped)
- ✅ Configuration system (`appsettings.json` + `IOptions<T>`)
- ✅ Minimal API hosting

### Testing
- ✅ xUnit fundamentals (`[Fact]`, `[Theory]`, `[InlineData]`, `[MemberData]`)
- ✅ AAA pattern (Arrange-Act-Assert)
- ✅ `Assert.Throws<T>` for exception testing
- ✅ Object spy pattern with captured event args
- ✅ `WebApplicationFactory<T>` for in-process integration tests
- ✅ `PostAsJsonAsync` for clean HTTP test bodies

### Packaging & Infrastructure
- ✅ `.csproj` project files + `.slnx` solution
- ✅ Multi-targeting (`<TargetFrameworks>`)
- ✅ NuGet packaging (`dotnet pack`, SemVer, `.nuspec` fields)
- ✅ Docker multi-stage build
- ✅ Container layer cache optimization
- ✅ Docker Desktop on Windows = WSL2 Linux VM behind the scenes

---

## 📂 Project Layout (Final)

```
2026-05-20/
  TemperatureSensor.slnx
  TemperatureSensor.Core/             ← Library (16 unit tests pass)
  TemperatureSensor.Tests/            ← Unit tests
2026-05-25/
  Dockerfile                          ← Multi-stage build context
  TemperatureSensor.Api/              ← Minimal Web API (net9.0)
  TemperatureSensor.Core/             ← Symlinked / referenced
2026-05-26/
  TemperatureSensor.Core.Tests/       ← Refreshed unit tests (13 green)
  TemperatureSensor.Api.Tests/        ← NEW integration tests (3 green)
  Tests-Refresher-Summary.md
  Integration-Tests-and-Docker-Summary.md
```

---

## 🪜 Next Steps (Backlog)

The 3-session arc is complete. Below is the next-wave backlog, in suggested priority order:

### Priority 1 — Real-world deploy (extends today's work)
- ⬜ Push image to a registry (Docker Hub free tier or Azure Container Registry)
- ⬜ Deploy container to **Azure Container Apps** (the natural next home for a Dockerized .NET API)
- ⬜ Add a **GitHub Actions CI/CD pipeline**: build → test → docker build → push → deploy

### Priority 2 — Production-grade observability
- ⬜ Add **Application Insights** (or OpenTelemetry) for distributed tracing
- ⬜ Health checks (`/health`) endpoint
- ⬜ Structured logging in JSON format for cloud log aggregation

### Priority 3 — Language deep-dive
- ⬜ **Generics** (constraints, variance, covariance/contravariance)
- ⬜ **Records** + pattern matching deep dive
- ⬜ **Test doubles**: mock / stub / fake / spy distinctions + Moq or NSubstitute
- ⬜ **TDD discipline** drill (red → green → refactor)
- ⬜ `TheoryData<T1,T2>` + `[ClassData]`
- ⬜ EF Core basics
- ⬜ Clean Architecture practical refactor

### Priority 4 — Deferred from earlier days
- ⬜ IL revisit (deferred from 2026-05-20)
- ⬜ Local NuGet feed round-trip (deferred from 2026-05-20)
- ⬜ E2E testing with Playwright

---

## 🧠 Locked-in Mental Models (running list)

| Topic | Locked Phrase |
|---|---|
| DI registration | "Registration ≠ construction — ctors fire only in PHASE 3" |
| DI scope | "Scope = HTTP request in ASP.NET Core" |
| DI anti-pattern | "Never inject Scoped into Singleton" |
| HTTP idempotency | "GET/PUT/DELETE 幂等, POST 不幂等" |
| HTTP status codes | "2xx 我做了. 4xx 你错了. 5xx 我错了. 4xx 不重试, 5xx 看幂等" |
| REST | "URL 名词 + HTTP 动词 + stateless 让服务横向扩展" |
| Testing scope | "Unit test 是显微镜, integration test 是风洞" |
| Test errors | "测错误路径永远 Assert.Equal(StatusCode), 不用 EnsureSuccessStatusCode" |
| WebAppFactory | "不开端口, 纯走 in-memory pipeline" |
| Docker multi-stage | "FROM 是 stage 分界线, 只有 COPY --from 是桥, Stage 2 没编译器" |
| Docker context | "Dockerfile 必须看得见所有它要 COPY 的项目" |
| Docker cache | "先 csproj 后源码 — layer cache 的黄金顺序" |
| Windows + Docker | "Docker Desktop 后面藏了个 WSL2 Linux VM" |
| PowerShell HTTP | "PS 的 curl 是别名陷阱, 调试 HTTP 永远用 curl.exe -i" |

---

## 🏁 Today's One-Line Win

> "我今天写了一个有单元测试、集成测试、跑在容器里的 .NET 9 Web API——同一个二进制，明天就能扔上云。"

That's 0→1 production. **Done.** 🚀
