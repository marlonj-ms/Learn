# Docker Fundamentals + AKS Teaser — 2026-05-20 (Evening)

> **Day type**: light conceptual intro (no code authored, no Dockerfile written yet).
> **Why today**: take a break from heavy C# pattern work; build mental model & vocabulary so that **Session N+3 (Docker for the REST API)** can focus purely on hands-on without re-learning concepts.
> **Tool state**: Docker Desktop 29.4.3 installed and verified working on this machine (WSL2 backend, Linux containers).

---

## 1. Concepts covered

| # | Concept | Status |
|---|---|---|
| 1 | Container vs Virtual Machine | ✅ |
| 2 | Image vs Container (blueprint vs instance) | ✅ |
| 3 | Image immutability + SHA-256 digest identity | ✅ |
| 4 | Docker client / daemon architecture | ✅ |
| 5 | WSL2 hosting Linux containers on Windows | ✅ |
| 6 | Layers + cache cascading rule | ✅ |
| 7 | The two-stage `COPY` pattern (manifest first, source last) | ✅ |
| 8 | Multi-stage builds (SDK build stage → slim runtime stage) | ✅ (preview only) |
| 9 | Kubernetes orchestration problem | ✅ |
| 10 | AKS = managed Kubernetes on Azure | ✅ |
| 11 | Pod / Node / Cluster / Deployment / Service vocabulary | ✅ |
| 12 | Control plane reconciliation loop | ✅ |

---

## 2. Vocabulary table (Chinese + English)

| 中文 | English | Meaning in production |
|---|---|---|
| 容器 | container | A running instance of an image. Has its own process, FS view, network |
| 镜像 | image | Immutable read-only blueprint — app + deps + OS userspace + start command |
| 内核 | kernel | OS core. Containers share the **host** kernel; VMs have their own |
| 守护进程 | daemon | The `dockerd` background service. CLI talks to it over a socket |
| 镜像仓库 | registry | Server that hosts images (Docker Hub, Azure ACR, GitHub GHCR) |
| 哈希指纹 / 哈希值 | digest / hash | SHA-256 that uniquely identifies an image's content |
| 退出码 | exit code | Process exit value. 0 = success, non-zero = failure. K8s uses it |
| 层 | layer | One filesystem diff in an image. Cached and shared across images |
| 可写层 | writable layer | The thin top layer unique to each running container |
| 多阶段构建 | multi-stage build | Build in SDK image, ship from slim runtime image |
| 容器编排 | container orchestration | The "run 150 containers, restart on failure, auto-scale" problem |
| 节点 | Node | A VM that runs Pods |
| 集群 | Cluster | All Nodes + the control plane managing them |
| 调和循环 | reconciliation loop | K8s control plane continuously matching actual state to desired state |
| 边车模式 | sidecar pattern | A Pod with multiple cooperating containers (rare case) |

---

## 3. Mental models / slogans to remember

These are the one-liners that lock concepts in place:

1. **"VM virtualizes hardware. Container virtualizes the OS."**
2. **"Image is the class; container is the instance."**
3. **"`latest` is a name; `sha256:...` is an identity."**
4. **"Treat containers like cattle, not pets."** (state lives *outside* the container)
5. **"Cache invalidation cascades downward."** (Dockerfile)
6. **"Put rarely-changing things at the top of the Dockerfile, frequently-changing things at the bottom."**
7. **"Manifest first, dependencies cached, source last."** (the two-stage COPY pattern)
8. **"You declare what you want. Kubernetes reconciles reality to match."**

---

## 4. The hands-on we did

Verified Docker works on this machine. Commands run + what they proved:

```powershell
docker --version          # CLI installed: 29.4.3
docker info               # Daemon running on WSL2 Linux backend
docker run hello-world    # Pulled an image, spawned a container, exited cleanly

docker ps                 # No running containers (hello-world exited)
docker ps -a              # Showed exited container "sharp_knuth" (auto-named)
docker images             # Showed cached image hello-world:latest, "U" = In Use
```

What this proved:
- A container is a discrete instance with its own ID, status, and exit code.
- An image persists even after the container that used it has exited.
- An image cannot be deleted while any container (even stopped) still references it.

---

## 5. The Dockerfile cache rule — the lesson

### Bad ordering (the beginner trap)

```dockerfile
FROM mcr.microsoft.com/dotnet/sdk:8.0     # layer 1: SDK
COPY . /src                                # layer 2: WHOLE REPO (includes Program.cs!)
RUN dotnet restore /src                    # layer 3
RUN dotnet publish /src -c Release         # layer 4
```

Change one line of `Program.cs` → layer 2 invalidates → cascades down → **`dotnet restore` re-runs every commit**. Slow CI, network thrash.

### Good ordering (the production pattern)

```dockerfile
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# Stage A: rarely-changing dependencies
COPY *.csproj ./                # only the project file
RUN dotnet restore              # CACHED unless .csproj changes

# Stage B: frequently-changing source
COPY . ./                       # rest of repo
RUN dotnet publish -c Release -o /app
```

Now `Program.cs` changes only invalidate the source-copy + publish layers. `dotnet restore` stays cached.

### Full multi-stage (production target — write this in Session N+3)

```dockerfile
# ----- BUILD STAGE -----
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src
COPY *.csproj ./
RUN dotnet restore
COPY . ./
RUN dotnet publish -c Release -o /app

# ----- RUNTIME STAGE -----
FROM mcr.microsoft.com/dotnet/aspnet:8.0
WORKDIR /app
COPY --from=build /app ./
ENTRYPOINT ["dotnet", "MyApp.dll"]
```

Result: ~900 MB SDK image is discarded, final image is ~220 MB slim runtime + your DLLs. Smaller, faster pulls, no compilers in production.

---

## 6. The full deployment picture

```
[Your C# code]
      ↓
[Dockerfile]                                          ← Session N+3
      ↓
   docker build
      ↓
   [Image]
      ↓
   docker push
      ↓
[Container Registry — ACR / Docker Hub]               ← bridge
      ↓
   kubectl apply -f deployment.yaml
      ↓
[AKS Cluster]                                         ← backlog
   ├─ Control Plane (managed by Azure, free)
   └─ Worker Nodes (your VMs, you pay):
        ├─ Node 1: [Pod A] [Pod B]
        ├─ Node 2: [Pod A] [Pod C]
        └─ Node 3: [Pod B] [Pod C]

[Service] ─ stable DNS / virtual IP ─ load-balances across all live Pod copies
```

One-sentence flow:
> Build an image → push it to a registry → declare a Deployment → AKS schedules Pods onto Nodes → a Service exposes them to the network.

---

## 7. AKS in one paragraph

Kubernetes is the open-source container orchestrator that won the industry. You declare desired state ("3 replicas of `temperature-api:v1.2`, 256 MB each"), and K8s' **control plane** runs a continuous reconciliation loop to make actual state match. AKS is Microsoft's managed K8s: **Azure runs the control plane for you for free, you only pay for the worker VMs**. Equivalents elsewhere: AWS = EKS, GCP = GKE. Same Kubernetes underneath.

---

## 8. Review questions (for tomorrow's quiz)

### A. Vocabulary recall
1. What is the precise difference between a container and a VM regarding the kernel?
2. In the analogy "image is to container as ___ is to ___", what's the C# pair?
3. What does the `sha256:...` digest of an image guarantee?
4. What's the difference between an image *tag* (like `:latest`) and an image *digest*?
5. On Windows, what is actually hosting your Linux containers when you use Docker Desktop?

### B. Docker CLI
6. Which command lists only running containers? Which lists all containers, including stopped?
7. What does the exit code `(0)` in `docker ps -a` STATUS column mean?
8. If `docker images` shows `EXTRA: U` next to an image, what does that mean and what's the consequence?

### C. Dockerfile cache reasoning
9. State the cache invalidation cascading rule in one sentence.
10. Given this Dockerfile, if `Program.cs` changes, which layers rebuild?
    ```dockerfile
    FROM mcr.microsoft.com/dotnet/sdk:8.0
    COPY . /src
    RUN dotnet restore /src
    RUN dotnet publish /src
    ```
11. Why is the "two-stage COPY" pattern (`COPY *.csproj` first, then `COPY . .`) faster than copying everything at once?
12. In a multi-stage build, what concretely lives in the final image and what gets thrown away?

### D. Kubernetes / AKS
13. Define each in one sentence: Pod, Node, Cluster, Deployment, Service.
14. What does Kubernetes' control plane *continuously do*, and what's that loop called?
15. What's the practical division of labor between Azure and you when running AKS — who runs what?
16. A Pod dies because its Node's hardware fails. Who notices, and who decides where the replacement Pod runs?
17. In the deployment pipeline `code → image → registry → cluster`, where does `docker build` happen vs `docker push` vs `kubectl apply`?

### E. Production judgment
18. Why is it dangerous to store user data inside a container's writable layer?
19. Why do production teams pin to image digests rather than the `:latest` tag?
20. Why do we use a *slim runtime image* (not the SDK image) as the final stage of a Dockerfile?

---

## 9. Where today fits in the plan

- **Original committed arc** (locked 2026-05-20 morning):
  - Session N+1: ILogger + DI + service lifetimes
  - Session N+2: Minimal REST API
  - Session N+3: Integration tests + **Docker** ← today moved the *conceptual* prerequisite of N+3 forward
- **Effect**: When you reach Session N+3, you no longer need to learn Docker concepts — you just write the Dockerfile and run it. That session shrinks from "learn Docker + write Dockerfile + integration test" to "write Dockerfile + integration test."
- **AKS** stays in the backlog, after the 3-session arc + GitHub Actions + Azure deploy.

---

## 10. Quick-reference cheat sheet (one-screen)

```
Container vs VM:    VM = own kernel    Container = shares host kernel
Image vs Container: class             vs instance
Identity:           tag = name        sha256:... = identity
Cattle vs pets:     state lives OUTSIDE the container

Dockerfile cache rule:  changes invalidate the changed layer AND every layer below
Production pattern:     COPY manifest → restore deps → COPY source → build
Multi-stage:            FROM sdk AS build  →  FROM runtime  → COPY --from=build

K8s vocab (5):  Pod (instance) · Node (VM) · Cluster (all of it)
                Deployment (desired N replicas) · Service (stable endpoint)
K8s mantra:     "Declare desired state. Control plane reconciles."
AKS:            Azure runs the control plane (free). You pay for worker VMs.
```
