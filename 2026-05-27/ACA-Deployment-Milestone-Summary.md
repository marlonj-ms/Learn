# ACA 部署里程碑 — temperature-sensor 上线 Azure Container Apps 🎯

> **Date**: 2026-05-27 (Day 48 — bonus evening session #3)
> **Mode**: 用户自己跑命令、mentor 当顾问（advisor-only mode）
> **Goal**: 把今早 CI 推到 GHCR 的镜像，**真正部署到云上 serverless 容器平台**，闭合 CI → CD 链路

---

## 🏆 已交付（live）

| 资源 | 名字 / 值 |
|---|---|
| Subscription | `2941a09d-7bcf-42fe-91ca-1765f521c829` (MySQL aaS) |
| Resource Group | `marlondb` (eastasia / Hong Kong) |
| Container Apps Environment | `cae-tempsensor-dev` |
| Container App | `temperature-sensor` |
| Revision | `temperature-sensor--jx1w89v` (single, Running, Healthy) |
| Image | `ghcr.io/marlonj-ms/temperature-sensor:latest` （**public**，无需 PAT） |
| FQDN | `https://temperature-sensor.thankfulplant-cb0fc5e9.eastasia.azurecontainerapps.io` |
| Scale | min=1, max=3, cpu=0.25, mem=0.5Gi |
| In-container config | `Sensor:Threshold = 30°C`（从 appsettings 加载，**未硬编码**） |

---

## ✅ 真正发生的事 — 端到端 4 次验证

| 步骤 | 命令 | 结果 |
|---|---|---|
| 1️⃣ GET / | `curl.exe https://<fqdn>/` | **200 "Hello World!"** ✅ |
| 2️⃣ POST 正常温度 | `POST /readings {"celsius":25}` | **200**，日志 `Recorded reading: 25°C` |
| 3️⃣ POST 超温 | `POST /readings {"celsius":100}` | **200** + 🔥 `THRESHOLD EXCEEDED at <time>: reading=100°C threshold=30°C` + "Beep Beep, call the fire department!" |
| 4️⃣ POST 畸形 | `POST /readings {"bad":"json"}` | **400** — handler 根本没被 invoke，model binding 阶段就拒了 |

> **关键证据**：超温日志在云端 stdout 出现，证明 (a) 镜像运行正常 (b) 配置链 `appsettings.json → env vars` 工作 (c) 组合根订阅 event 没漏。

---

## 🧠 新锁定口诀（lock-ins #10 → #14）

### #10 — ACA 是 3 层声明式金字塔

```
┌───────────────────────────────────────────────────┐
│ Container Apps Environment （cae-tempsensor-dev） │  ← Envoy + Log + Network 共享层
│  ┌────────────────────────────────────────────┐   │
│  │ Container App （temperature-sensor）       │   │  ← 1 个对外名字
│  │  ┌─────────────────────────────────────┐   │   │
│  │  │ Revision (--jx1w89v)                │   │   │  ← 1 个不可变快照
│  │  │  ┌────┐ ┌────┐ ┌────┐               │   │   │
│  │  │  │Pod1│ │Pod2│ │Pod3│ ...           │   │   │  ← 0..N 个真容器
│  │  │  └────┘ └────┘ └────┘               │   │   │
│  │  └─────────────────────────────────────┘   │   │
│  └────────────────────────────────────────────┘   │
└───────────────────────────────────────────────────┘
```

**锚句**："Environment 是房子，App 是户号，Revision 是不可变照片，Pod 是真活的人。"

---

### #11 — Revision 是 immutable，update 会产生新版本

```
你跑：az containerapp update --image ghcr.io/.../v2 ...
        ↓
ACA 不修改旧 revision，而是：
  1. 创建新 revision: temperature-sensor--abc123
  2. 等新 rev 健康 → 切 100% traffic 过去
  3. 旧 rev 保留（默认 retention），可秒级回滚
```

**锚句**："Revision 不可变是云原生回滚的物理基础。"
**反例**：传统 IIS 直接覆盖 wwwroot — 出问题只能祈祷有备份。

---

### #12 — Pod 是 ACA 的最小调度单位（≈ K8s pod ≈ 一个容器实例）

```
Revision 是"图纸"（不可变规格）
       ↓
autoscaler 看图纸 + 当前流量
       ↓
按需创建 N 个 Pod = N 个真容器进程 = N 个 Kestrel 在跑
```

**命名约定**：`<app>--<revision-suffix>-<replicaset-hash>-<pod-suffix>`
今天看到的：`temperature-sensor--jx1w89v-97c7dfc6-9vbb8`

**锚句**："Pod 数 = 你真正在花的钱（Consumption SKU）。Pod=0 = 零成本。"

---

### #13 — Autoscaler 不是即时的，有反应延迟 + cooldown

```
请求来了
   ↓
观察窗口 ~30s
   ↓
评估指标超阈值 → 决定扩
   ↓
启动新 pod ~10-20s
   ↓
~50 秒后才看得见新 pod
```

**Cooldown**：scale-out 后 ~5 分钟才会开始 scale-in（防抖动）。
**默认 HTTP 规则**：每个 replica 同时 10 个并发请求才扩。30 个 curl 串行返回太快，**根本没堆积到阈值**。

**锚句**："想看扩缩演示 → 要么 持续 60s+ 真并发，要么 直接调 `--min-replicas 2`（不依赖 autoscaler）。"

---

### #14 — PowerShell 并发的三种姿势（5.1 vs 7+）

| 方式 | 隔离 | 启动开销 | 适合 | 版本要求 |
|---|---|---|---|---|
| `Start-Job` | **进程**（fork pwsh） | 慢 ~1s | 重活、隔离 | PS 5.1+ |
| `Start-ThreadJob` | **线程**（同进程） | 快 ~50ms | I/O 密集（HTTP、SQL） | PS 5.1+（自带模块） |
| `ForEach-Object -Parallel` | **runspace** | 中 | 大量并发 PS 任务 | **PS 7+ only** |

**今天踩的坑**：`ForEach-Object -Parallel` 在 Windows PowerShell 5.1 报 `AmbiguousParameterSet`（参数集不识别）。**5.1 没有这个特性**，要么升 pwsh 7+，要么换 `Start-ThreadJob` / `Start-Process`。

---

## 🛠️ 走过的 6 个 az 命令模式（可复用）

```powershell
# 0. 设当前 sub
az account set --subscription 2941a09d-7bcf-42fe-91ca-1765f521c829

# 1. 注册 provider（一次性，可能已经做过）
az provider register --namespace Microsoft.App
az provider register --namespace Microsoft.OperationalInsights

# 2. 建 Container Apps Environment
az containerapp env create `
  --name cae-tempsensor-dev `
  --resource-group marlondb `
  --location eastasia

# 3. 部署 Container App（一句话起服务）
az containerapp create `
  --name temperature-sensor `
  --resource-group marlondb `
  --environment cae-tempsensor-dev `
  --image ghcr.io/marlonj-ms/temperature-sensor:latest `
  --target-port 8080 `
  --ingress external `
  --min-replicas 1 --max-replicas 3 `
  --cpu 0.25 --memory 0.5Gi

# 4. 看运行状态
az containerapp revision list -n temperature-sensor -g marlondb -o table
az containerapp replica list -n temperature-sensor -g marlondb `
  --revision temperature-sensor--jx1w89v -o table

# 5. 看 stdout 日志
az containerapp logs show -n temperature-sensor -g marlondb --type console --follow

# 6. 拿 FQDN
az containerapp show -n temperature-sensor -g marlondb `
  --query "properties.configuration.ingress.fqdn" -o tsv
```

---

## 💰 收尾：清理资源（避免持续计费）

> Consumption SKU 闲置时 pod=0 几乎不花钱。但保留 CAE 也会有少量 Log Analytics 接入费。

**A. 只删除 app，保留 env（继续学习用）**：
```powershell
az containerapp delete -n temperature-sensor -g marlondb --yes
```

**B. 删除 app + env**（彻底回到部署前）：
```powershell
az containerapp delete -n temperature-sensor -g marlondb --yes
az containerapp env delete -n cae-tempsensor-dev -g marlondb --yes
```

**C. ⚠️ NEVER**：`az group delete -n marlondb` — `marlondb` 是**共享团队 rg**，里面有别人的资源。

---

## 📂 文件清单（今天 evening session #3 没有新代码文件）

```
2026-05-27/
  ACA-Deployment-Milestone-Summary.md     ← NEW (this file)
  Current-Learning-Status.md              ← UPDATED (加 evening Part 3)
  CI-CD-Closeout-Summary.md               ← 早上的产物
  Kestrel-Docker-DeepDive-Summary.md      ← 傍晚的产物
```

**云端 artifacts（仍在 Azure 上跑，直到你 delete）：**
- `temperature-sensor` Container App in `marlondb` rg
- `cae-tempsensor-dev` Container Apps Environment

---

## 🏁 今天的 One-Line Win（3 段合起来）

> **早上**：CI 闭环 — 镜像自动进 GHCR registry。
> **傍晚**：理论闭环 — Kestrel 不是测试工具，反向代理在 Kestrel 前面。
> **晚上**：**CD 闭环 — 镜像真的从 GHCR 拉到 Azure，4 个 curl 在云端响应，从 commit 到 production 全链路打通** 🎯

---

## 🚀 下一步 — Pick One

| Path | Effort | Why |
|---|---|---|
| **A. Generics Session 1** | 1 day | 调试 pre-staged 文件的 exit-1，补语言深度短板 |
| **B. CD 自动化** | 1–2 days | 把今天手敲的 `az containerapp update --image` 接进 GitHub Actions，**真正的 CI/CD** |
| **C. Test doubles + Moq** | 1 day | 补 mocking 短板，配套 TDD 训练 |
| **D. ACA + App Insights** | 1 day | 加 telemetry/distributed tracing，看到云端 request 链路 |

**Recommended default**: A（Generics）。今天 0→1 production arc 的"CD 触达云"环已经闭合，可以暂时离开 infra 回到 C# 语言深度。
