# Integration Tests + Docker Summary — 2026-05-26

> **Day 47 (Part B)**｜C# Learning Journey ｜Mode：C# Mentor walk-through (mode A — mentor reviews, learner writes)  
> **Focus**：Session N+3 of the 0→1 production arc — integration tests against the live API + multi-stage Docker build  
> **Folder**：`d:\AITriage\2026-05-26\` (tests) ＋ `d:\AITriage\2026-05-25\Dockerfile` (build context constraint)  
> **Final state**：3/3 integration tests green ✅ ＋ Docker image `temperature-sensor:dev` 跑起来，curl.exe 实测 API 响应正常 ✅

---

## 🎯 Mission Today

把昨天的 Minimal API 推过最后一公里：
1. **集成测试（integration tests）**：用 `WebApplicationFactory<T>` 在内存里启动整个 API，端到端测真实 HTTP 路径。
2. **容器化（containerization）**：写 multi-stage `Dockerfile`，构建镜像，跑容器，用 `curl.exe` 实测。

这意味着同一份代码现在既能：通过单元测试 → 通过集成测试 → 跑在容器里被外部 HTTP 客户端访问。

---

## 📚 Part 1 — Integration Tests（集成测试）

### 1️⃣ Unit test vs Integration test 的真正边界

| 维度 | Unit Test（单元测试） | Integration Test（集成测试） |
|---|---|---|
| **目标** | 一个 class / 一个方法 | 多个组件协作（routing + DI + JSON + 业务） |
| **依赖** | 都 mock 掉 | 真实组件协作（除了外部数据库） |
| **入口** | 直接调方法 | 走 HTTP（`HttpClient`） |
| **速度** | 极快（毫秒） | 较慢（需要 启动 host） |
| **回答的问题** | "这个零件对吗？" | "拼起来还转吗？" |

**锁定心法**：单元测试是**显微镜**，集成测试是**风洞**。两个都要有。

### 2️⃣ `WebApplicationFactory<T>` — 在测试进程里启动整个 Web 应用

```csharp
public class TemperatureSensorAPITests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly HttpClient _client;

    public TemperatureSensorAPITests(WebApplicationFactory<Program> factory)
    {
        _client = factory.CreateClient();
    }
    // ...
}
```

**关键点**：
- `WebApplicationFactory<TEntryPoint>`：来自 `Microsoft.AspNetCore.Mvc.Testing` NuGet 包。  
  作用：在**同一个进程**里跑你的 `Program.cs`，但**不绑真实端口**——用一个内存 `HttpClient` 直接戳进去。
- `IClassFixture<T>`：xUnit 提供的**类级共享生命周期（class-level fixture）**——一个测试类只 `new` 一次 factory，所有测试共用。
- `factory.CreateClient()`：拿到一个已配置好 `BaseAddress` 的 `HttpClient`，直接 `GetAsync("/")` 就能命中你的路由。

> 💡 **省钱点**：不开端口、不启动 Kestrel TCP listener，纯走 in-memory pipeline。**比 dotnet run + 真 HTTP 快 10x+**。

### 3️⃣ `Program.cs` 必须开一道"小窗户"——`public partial class Program { }`

ASP.NET Core 的顶级语句（top-level statements）默认把 `Program` 类做成 `internal`，测试项目（不同 assembly）拿不到。

**修复**：在 `Program.cs` 末尾加一行——

```csharp
public partial class Program { }   // ← 测试可见的"挂钩点"
```

**这是 standard pattern**，微软官方推荐。没有这行，`WebApplicationFactory<Program>` 编译都过不去。

### 4️⃣ 三个测试覆盖的真实路径

| 测试名 | 路径 | 预期 | 实测 |
|---|---|---|---|
| `GetRoot_ReturnsHelloWorld` | `GET /` | 200 + `"Hello World!"` | ✅ |
| `PostReading_WithValidValue_Returns200OK` | `POST /readings` + `{ Celsius = 25.0 }` | 200 | ✅ |
| `PostReading_WithMalformedBody_Returns400BadRequest` | `POST /readings` + `{ Celsius = "not-a-number" }` | 400 | ✅ |

### 5️⃣ `EnsureSuccessStatusCode()` 是把双刃剑

```csharp
// 正路径：好用
response.EnsureSuccessStatusCode();   // 非 2xx 就 throw

// 反路径：不能用！会因为 400 抛出，反而让测试假失败
// 正确写法：
Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
```

**锁定心法**：**测错误路径时，永远直接断言 StatusCode，不要用 EnsureSuccessStatusCode。**

### 6️⃣ `PostAsJsonAsync` — 不用手写 JSON 序列化

```csharp
var reading = new { Celsius = 25.0 };          // 匿名对象（anonymous object）
var response = await _client.PostAsJsonAsync("/readings", reading);
```

`PostAsJsonAsync` 来自 `System.Net.Http.Json`，做三件事：
1. 把对象序列化成 JSON
2. 设置 `Content-Type: application/json`
3. 发送 POST 请求

这是 **2026 年生产级测试代码的写法**，比手搓 `StringContent + Encoding.UTF8 + "application/json"` 干净 5 倍。

---

## 🐳 Part 2 — Docker Multi-Stage Build

### 1️⃣ 容器化（containerization）到底解决什么问题？

| 没容器时 | 有容器后 |
|---|---|
| "在我机器上跑得好好的" | "镜像（image）就是一切——可移植、可复制" |
| 部署 = 装一堆依赖 + 调环境变量 | 部署 = `docker run` 一行 |
| 开发/测试/生产环境漂移 | 同一个二进制（镜像）跑在所有环境 |

**锁定心法**：容器 = **一个把 OS、运行时、应用、配置都封进去的盒子**。

### 2️⃣ Multi-stage build（多阶段构建）哲学

**问题**：如果用 `dotnet/sdk:9.0` 镜像直接做最终镜像，体积 ~1.5 GB（SDK 含编译器、NuGet 缓存、调试符号）。生产**根本不需要**这些。

**解决方案**：分两阶段
- **Stage 1（build）**：用 SDK 镜像编译 → 产出 publish 目录
- **Stage 2（runtime）**：用 aspnet 镜像（只含 .NET 运行时，~200MB）只装 publish 产物

**锁定比喻**：
- "**FROM 是 stage 分界线**" — 每个 FROM 启动一个全新的临时容器
- "**只有 `COPY --from=<stage>` 是桥**" — ENV / WORKDIR / 安装的包都不跨 stage
- "**Stage 2 没编译器**" — 想跑 `dotnet build` 都没工具，只能跑预编译好的 `.dll`
- "**92.9 MB 证明 Stage 2 是 runtime 不是 SDK**" — 体积本身就是 multi-stage 正确性的证据

### 3️⃣ 最终的 Dockerfile

文件位置：**`d:\AITriage\2026-05-25\Dockerfile`**（不是今天文件夹！）

```dockerfile
# Stage 1: Build
FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src

# 先拷贝 csproj —— 利用 layer cache：只要 csproj 不变，restore 就走缓存
COPY TemperatureSensor.Api/TemperatureSensor.Api.csproj TemperatureSensor.Api/
COPY TemperatureSensor.Core/TemperatureSensor.Core.csproj TemperatureSensor.Core/
RUN dotnet restore TemperatureSensor.Api/TemperatureSensor.Api.csproj

# 再拷贝全部源码 + publish
COPY . .
RUN dotnet publish TemperatureSensor.Api/TemperatureSensor.Api.csproj -c Release -o /app/publish

# Stage 2: Runtime
FROM mcr.microsoft.com/dotnet/aspnet:9.0
WORKDIR /app
COPY --from=build /app/publish .
EXPOSE 8080
ENV ASPNETCORE_URLS=http://+:8080
ENTRYPOINT ["dotnet", "TemperatureSensor.Api.dll"]
```

### 4️⃣ 为什么 Dockerfile 在 `2026-05-25/` 而不是 `2026-05-26/`？

**Docker 构建上下文（build context）**：`docker build .` 的 `.` 是 Docker daemon 能看到的所有文件的根目录。  
Dockerfile 里 `COPY TemperatureSensor.Api/...` 是**相对 context 根**的。

我们的项目结构：
```
2026-05-25/
├── Dockerfile          ← 必须在这一层
├── TemperatureSensor.Api/
└── TemperatureSensor.Core/
```

把 Dockerfile 放在两个项目的**共同父目录**（common ancestor），COPY 才能同时拿到两个项目。  
**锁定心法**：**Dockerfile 必须看得见所有它要 COPY 的项目。**

### 5️⃣ Layer cache 优化套路：先 csproj，后源码

```dockerfile
# ✅ 高效
COPY *.csproj ./
RUN dotnet restore
COPY . .
RUN dotnet publish

# ❌ 低效
COPY . .
RUN dotnet restore
RUN dotnet publish
```

**为什么**：Docker 按行做 layer，行命中 cache 就跳过。  
只改 C# 代码时，`csproj` 没变 → `dotnet restore` 这一层走缓存（~5 秒省成 0 秒）。

### 6️⃣ 5 个 Dockerfile 关键指令对照表

| 指令 | 作用 | 类比 |
|---|---|---|
| `FROM` | 选基础镜像，分 stage | 选地基 |
| `WORKDIR` | 设当前工作目录（不存在自动建） | `cd` |
| `COPY src dst` | 把本地文件拷进镜像 | `cp` |
| `RUN cmd` | 在构建时执行命令 | shell 命令 |
| `EXPOSE 8080` | 声明容器内监听的端口（**注释作用**，不真开） | "我用 8080 这扇门" |
| `ENV K=V` | 设环境变量（运行时生效） | `export K=V` |
| `ENTRYPOINT [...]` | 容器启动时跑的命令 | `main()` |

### 7️⃣ Windows 上开发，为什么镜像是 Linux？

**真相**：
1. **.NET 5+ 是跨平台的**：IL 字节码跑在任何平台的 `dotnet` 运行时上。
2. **云上容器几乎全是 Linux**：Azure Container Apps / AWS ECS / GCP Cloud Run 默认 Linux base，价钱便宜、生态成熟。
3. **Docker Desktop on Windows 后面是 WSL2 Linux VM**：`docker version` 里 `context: desktop-linux` 揭穿这一点——你的 "Linux 容器" 其实跑在 Windows 里偷藏的一个轻量 Linux 虚拟机里。

**结论**：写 Linux Dockerfile = 写**到处都能跑的 Dockerfile**，这是 2026 年的标准操作。

---

## 🧪 实测验证三连击

### Build → Run → Curl

```powershell
# 1. Build the image
cd d:\AITriage\2026-05-25
docker build -t temperature-sensor:dev .
# => Image ID 6730928d3084, Content Size 92.9 MB ✅

# 2. Run the container in detached mode
docker run -d -p 8080:8080 --name temp-sensor temperature-sensor:dev
# => CONTAINER ID 33631bccb28e, Status: Up ✅

# 3. Test with curl.exe (NOT PowerShell's curl alias!)
curl.exe -i http://localhost:8080/
# => HTTP/1.1 200 OK + "Hello World!" ✅

curl.exe -i -X POST http://localhost:8080/readings `
              -H "Content-Type: application/json" `
              -d '{\"celsius\":25}'
# => HTTP/1.1 200 OK ✅

curl.exe -i -X POST http://localhost:8080/readings `
              -H "Content-Type: application/json" `
              -d '{this is not json'
# => HTTP/1.1 400 Bad Request ✅
```

### PowerShell 测试踩坑提醒

| 坑 | 表现 | 正解 |
|---|---|---|
| `curl` 是 `Invoke-WebRequest` 别名 | 响应空、看不见 status code | 用 `curl.exe`（强制走真正的 curl） |
| `Invoke-RestMethod` 在 200 空 body 返回 `$null` | 看着像"挂了" | 加 `-i` 看完整 HTTP 头 |
| 单引号里写 JSON | `'{"celsius":25}'` 在传给 curl.exe 时变 `{celsius:25}` | 写 `'{\"celsius\":25}'` 才能保住双引号 |

---

## 🧠 锁定的心法（拿来记一辈子）

1. **"Unit test 是显微镜，integration test 是风洞"** — 两个都要有，不可替代。
2. **"`WebApplicationFactory` = 不开端口，纯走 in-memory pipeline"** — 比真 HTTP 快 10x+。
3. **"测错误路径用 `Assert.Equal(HttpStatusCode.BadRequest, ...)`，永远不用 `EnsureSuccessStatusCode`"**。
4. **"FROM 是 stage 分界线，只有 `COPY --from` 是桥"** — multi-stage 的核心。
5. **"Stage 2 没编译器"** — 想 build 都不行，强制你做正确的事。
6. **"92.9 MB 证明 Stage 2 是 runtime 不是 SDK"** — 体积本身就是正确性的证据。
7. **"Dockerfile 必须看得见所有它要 COPY 的项目"** — context 决定 Dockerfile 该放哪。
8. **"先 csproj 后源码"** — layer cache 的黄金顺序。
9. **"PowerShell 的 `curl` 是别名陷阱，调试 HTTP 永远用 `curl.exe -i`"**。
10. **"写 Linux Dockerfile = 写到处都能跑的 Dockerfile"** — Docker Desktop 后面藏了个 WSL2 Linux VM。

---

## 🎉 0→1 Production Arc 完结

| Session | 目标 | 状态 |
|---|---|---|
| N+1 | DI + ILogger + Lifetimes | ✅ Done (2026-05-24) |
| N+2 | Minimal REST API | ✅ Done (2026-05-25) |
| **N+3** | **Integration tests + Docker** | ✅ **Done (2026-05-26)** |

**你现在拥有的**：
- 一个有 **单元测试** + **集成测试** 双重保险的 .NET 9 Web API
- 一个可以 `docker build` + `docker run` 的 **329 MB 容器镜像**
- 一份**完全可移植**的代码——明天扔给 Azure Container Apps / AWS ECS / GCP Cloud Run，**一行部署命令**就能上云

**0→1 production 的目标，正式达成。** 🚀

---

## 📝 复习题（下次 quiz 用）

### Integration Tests
1. `Microsoft.AspNetCore.Mvc.Testing` 这个包提供了什么核心类？它的作用是什么？
2. `IClassFixture<T>` 和 `IAsyncLifetime` 有什么区别？为什么 fixture 是"类级共享"的？
3. 为什么必须在 `Program.cs` 加 `public partial class Program { }`？没加会怎样？
4. 测一个会返回 400 的 endpoint，可以用 `EnsureSuccessStatusCode` 吗？为什么？
5. `PostAsJsonAsync` 帮你做了哪三件事？
6. `WebApplicationFactory.CreateClient()` 拿到的 `HttpClient` 跟 `new HttpClient()` 有啥本质区别？

### Docker
7. Multi-stage build 解决什么问题？没有它最终镜像大概多大？
8. `COPY --from=build` 和普通 `COPY` 的区别是？
9. 为什么先 COPY csproj、做 restore，再 COPY 源码？
10. Dockerfile 应该放在项目目录里还是项目的父目录里？为什么？
11. `EXPOSE 8080` 真的会打开端口吗？真正打开端口是哪个命令？
12. 为什么在 Windows 上开发可以用 Linux base image？背后机制是什么？
13. `docker run -d` 和不加 `-d` 有什么区别？
14. PowerShell 里 `curl` 和 `curl.exe` 是同一个东西吗？哪个能看到 HTTP 状态码？
15. 用 multi-stage 后镜像只有 92.9 MB，如果不用 multi-stage 直接用 SDK 镜像，大约会是多大？

---

## 📂 今日产出文件清单

| 文件 | 路径 | 状态 |
|---|---|---|
| 集成测试代码 | `d:\AITriage\2026-05-26\TemperatureSensor.Api.Tests\TemperatureSensorAPITests.cs` | ✅ 3 tests green |
| 集成测试项目 | `d:\AITriage\2026-05-26\TemperatureSensor.Api.Tests\TemperatureSensor.Api.Tests.csproj` | ✅ |
| Dockerfile | `d:\AITriage\2026-05-25\Dockerfile` | ✅ Multi-stage, built |
| Docker image | `temperature-sensor:dev` (ID `6730928d3084`, 92.9 MB) | ✅ Built |
| 已运行容器 | `temp-sensor` on port 8080 | ✅ Tested via curl.exe |
| Tests refresher summary | `d:\AITriage\2026-05-26\Tests-Refresher-Summary.md` | ✅ |
| 本文件 | `d:\AITriage\2026-05-26\Integration-Tests-and-Docker-Summary.md` | ✅ |
