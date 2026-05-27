# Kestrel + SDK + Docker — 概念深挖（晚场）

> **Date**: 2026-05-27 (Day 48 · evening session)
> **Topic**: 从 Dockerfile 出发，把 SDK / Runtime / Kestrel / 反向代理 / 云原生扩缩容理顺
> **Mode**: 纯概念问答（无代码改动）
> **Trigger**: 早上 CI/CD 收尾完成后，学习者问 "什么是 SDK? docker restore / build / run 时为啥有的用 SDK 有的用 runtime"
> **Status**: ✅ 8 个 lock-in 全部当场跑题验证通过

---

## 🎯 主线串联

```
   "什么是 SDK?"
        ↓
  SDK vs Runtime vs ASP.NET 框架
        ↓
  三层镜像（sdk ⊃ aspnet ⊃ runtime）
        ↓
  Dockerfile 多阶段：Stage 1 用 sdk，Stage 2 用 aspnet
        ↓
  "为什么不直接用 runtime?"
        ↓
  ASP.NET Core 是独立 shared framework，runtimeconfig.json 启动期检查
        ↓
  "那 Kestrel 是干嘛的?"  ← 学习者第一个大误区
        ↓
  纠正：Kestrel 是真生产 web server，不是测试工具
        ↓
  反向代理 vs Kestrel 的边界（4 个作用）
        ↓
  Azure 三大服务的映射（App Service / ACA / Front Door）
        ↓
  "10 个 docker run 都听 8080 怎么不打架?"
        ↓
  容器网络 namespace 隔离 + 反向代理分发
        ↓
  "把 HTTP 处理放到 code 里会不会太 heavy?"  ← 第二个大误区
        ↓
  纠正：Kestrel 反而比 IIS 更轻更快
        ↓
  云原生终极哲学：代码层稳定，元数据层动态
```

---

## ✅ 8 个 Lock-in 口诀

| # | 口诀 | 一行解释 |
|---|---|---|
| **1** | **sdk 编译，aspnet 运行，runtime 缺 ASP.NET 框架** | 三层镜像：`sdk:9.0` ⊃ `aspnet:9.0` ⊃ `runtime:9.0`。web app 不能用纯 runtime 跑 |
| **2** | **FROM 是 stage 边界** | 多阶段构建里每个 `FROM` 启动新临时容器；ENV/WORKDIR/RUN 都不跨阶段 |
| **3** | **`COPY --from=build` 是唯一桥梁** | Stage 2 没有编译器、没有源码，只能通过这一行把 Stage 1 的产物拿过来 |
| **4** | **Kestrel 是真 web server，不是测试工具** | 生产里它是直接接 TCP / 解析 HTTP / 跑你 C# 代码的那个家伙 |
| **5** | **反向代理的 4 个作用** | TLS termination、负载均衡、缓存、边缘安全 — 它在 Kestrel **前面**，不替代 Kestrel |
| **6** | **网络 namespace 隔离** | 10 个容器都听 8080 互不打架，因为每个容器有独立 network namespace（独立 lo / eth0 / 端口空间） |
| **7** | **Kestrel 比 IIS 更轻** | async I/O + 同进程（无 IPC）+ buffer 池。TechEmpower benchmark 全球前 5 |
| **8** | **代码层稳定，元数据层动态** | 扩缩容时变的是反向代理路由表 + replica 数量；不变的是镜像 + 8080 + C# 代码 |

---

## 🍽️ 餐厅类比（今天最有效的锚点）

| 餐厅 | 软件 |
|---|---|
| 厨师 | **Kestrel**（真正做菜=跑 C# 代码） |
| 前台 / 接待 | **反向代理**（接客、分桌、安检 = TLS / LB / 缓存 / WAF） |
| 餐厅租赁公司 | **Azure 服务**（App Service / ACA / AKS — 提供"运营平台" + 自带前台） |

> **关键洞察**：餐厅租赁公司换品牌（Envoy / nginx / IIS / Front Door / ALB）都行，**厨师永远是 Kestrel**。

---

## 🌐 三种部署场景下的 "8080" 含义

| 场景 | 8080 是什么 | 怎么分发 |
|---|---|---|
| 本地 `docker run -p 5000:8080` | host 5000 → 容器 8080 | host 一对一映射；想跑第二个就 `-p 5001:8080` |
| Kubernetes / ACA 多 replica | 每个 pod 自己的 8080（互不知情） | Service / Ingress / Envoy 负载均衡到 N 个 pod IP |
| Azure App Service（自动平台） | 平台自动映射 | App Service 前置 IIS-as-proxy 自动处理 |

---

## ⚖️ Kestrel vs IIS（反直觉对比）

| 维度 | IIS + Worker | Kestrel (in-process) |
|---|---|---|
| 进程数 | 2（IIS + worker） | 1（自带） |
| 请求路径 | client → IIS → IPC → worker | client → Kestrel（同进程） |
| 跨平台 | Windows only | Win / Linux / macOS |
| TechEmpower 排名 | 中等 | **全球前 5** |
| 启动时间 | 慢（IIS 冷启） | 快（~1 秒） |
| 内存开销 | 几百 MB（IIS 本身） | Kestrel 框架部分 ~20–30 MB |

**为什么 Kestrel 反而轻？**
1. **异步 I/O 模型**：少量线程 + `async/await` 处理大量连接（不是 1 连接 1 线程）
2. **无 IPC 开销**：跟你的 C# 代码同进程，函数调用就完事
3. **buffer 池 + `Span<T>` / `ArrayPool<T>`**：减少 GC 压力
4. **HTTP/2 + HTTP/3 原生支持**

---

## 🧭 云原生设计哲学（今天最有价值的洞察）

```
┌─ 稳定层（"不变"的部分）──────────────┐
│  • 客户端入口     (443)              │
│  • 容器内部端口   (8080)             │
│  • 业务代码      (Program.cs)        │  ← 横向扩展跟代码无关
└──────────────────────────────────────┘
                  ⇅
┌─ 动态层（"会变"的部分）──────────────┐
│  • Replica 数量    (3 → 10 → 3)      │
│  • 路由表后端 IP   (Envoy 自动更新)   │
│  • CPU/内存分配    (平台 auto scale)  │
└──────────────────────────────────────┘
```

> **范式翻转：以前靠"把 server 做大"扛流量；现在靠"把 app 做小、做多、做独立"扛流量。**
>
> 这种"代码无感知扩容"叫 **stateless（无状态）**，是横向扩展的前提。
> 跟今天上午 CI 把镜像推到 GHCR 完美闭环：一个不可变 artifact → 平台可以随便复制 N 份。

---

## 🧪 验证的两道跑题

### Q1：ACA Envoy ingress 部署路径
A. Front Door → App Service → Kestrel
B. nginx → IIS → Kestrel
**C. Envoy → Kestrel → 你的代码 ✅**
D. Kestrel 直接对外

> 学习者答 **C**。锁住"ACA 用 Envoy 当 ingress"这个事实。

### Q2：ACA 从 3 replica 扩到 10 replica，谁不变？
**A. Envoy 路由表里登记的后端 IP 列表** ✅（需要加新机器）
B. host 上对外暴露的 443 端口（不变）
C. 每个 Kestrel replica 监听的内部端口 8080（不变）
D. 你的 C# 代码（不变）

> 学习者答 "BCD 不变，A 要加新机器" — **完全正确**，闭环了"代码层稳定，元数据层动态"。

---

## 📦 关联的 cheatsheet 新坑（35–37）

- **坑 35**：`runtime:9.0` 没有 ASP.NET Core 框架 → web app 启动**立刻失败**（runtimeconfig.json 检查 → `framework not found`）
- **坑 36**：容器内部端口 ≠ host 端口。10 个容器都用 8080 是合法的，本地多开必须改 host 端口（`-p 5001:8080`）
- **坑 37**：反向代理 ≠ Kestrel 的替代品。即使有 Envoy/nginx/IIS-as-proxy，**Kestrel 永远还在跑**

---

## 🪜 资源链路汇总（从客户端到代码）

```
 客户端 (HTTPS:443)
     ↓
 [Azure Front Door]            ← 全球边缘（可选）
     ↓
 [Application Gateway]         ← 区域 + WAF（可选）
     ↓
 [ACA Envoy ingress]           ← TLS 解开、按 host 路由、负载均衡到 N 个 replica
     ↓ HTTP:8080 (cluster 内 plain)
 [Container N]                 ← 选中的某个 replica
     ↓ TCP:8080 (容器内)
 [Kestrel]                     ← 同进程 web server
     ↓ 函数调用
 [你的 C# Minimal API handler]
```

---

## 🎯 一句话总结

> **早上学到的是工具链（CI/CD），晚上学到的是底层物理（容器里到底跑了什么）。**
> 两层结合：commit push → CI 跑测试 → 镜像进 registry → ACA 拉 N 份 → Envoy 分流到各 Kestrel → 你的代码运行。
> **从此 "云原生" 不再是 buzzword。**

---

## 📂 文件状态（晚场结束）

```
2026-05-27/
  CI-CD-Closeout-Summary.md            ← 早上的（不动）
  Kestrel-Docker-DeepDive-Summary.md   ← NEW（这份）
  Current-Learning-Status.md           ← 待更新（加晚场段）
  Generics-*                            ← 仍然 DEFERRED

dailyread/
  dailyread-2026-05-27.html            ← 追加"晚场补充"小节

dailyread.html                          ← 卡片 desc 加 "晚场: Kestrel/SDK/反向代理深挖"
csharp-syntax-cheatsheet.html           ← 追加坑 35–37
```

---

## 🛣️ 下次开始

仍然是三选一：A. Generics（pre-staged 文件 exit 1 待 debug）/ B. ACA 部署（CD 链路）/ C. Test doubles + Moq。
晚上的概念深挖**没有改变**下一步计划，只是把"为什么要部署到 ACA"的底层逻辑彻底想通了 — 真去做 B 路径时会顺很多。
