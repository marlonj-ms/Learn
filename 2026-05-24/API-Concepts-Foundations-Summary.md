# API / HTTP / REST 概念基础 — May 24, 2026 (Afternoon Session)

## 这一段在做什么

下午本来想直接开 Session N+2（动手造 REST API），临到要写代码时**主动叫停**，
先把三个绕不开的基础概念吃透：**API → HTTP → REST**。代码已删（之前 agent 演示版的
`TemperatureSensor.Api/` 已清场），明天**自己从零 scaffold** 重建。

## 三层概念，一句话总结

```text
API   = 契约（contract）              ← 任何层都成立
HTTP  = 远程传输契约的协议（信件格式 + 状态码）
REST  = 用 HTTP 时的"资源 + 动词"设计风格
```

---

## Concept 1 — API 到底是什么

### 定义
> **API = 调用方和被调用方之间的契约（contract）。**
> 契约 = **签名（syntactic / shape）** + **语义（semantic / behavior）**

| 我的两部分理解 | 产线术语 | 谁能检查 |
|---|---|---|
| "外部需要提供什么，以及返回什么" | **签名 / 形态契约**（signature / shape contract） | **编译器** |
| "一段说明解释这个 api 能干什么" | **语义契约 / 规约**（semantic contract / specification） | **人 + 测试** |

**记忆锚句**：
> Compiler checks the signature. Humans (and tests) check the semantics.

### API 在每一层都成立（不是只有 Web 才叫 API）

```text
┌─────────────────────────────────────────────────────────────┐
│  网络 API（network API） — HTTP / gRPC / GraphQL              │  ← 今天目标
├─────────────────────────────────────────────────────────────┤
│  进程间 API（IPC）— pipe / shared memory / unix socket        │
├─────────────────────────────────────────────────────────────┤
│  库 API（library API）— TemperatureSensor.Core.dll 的 public  │  ← 已做
├─────────────────────────────────────────────────────────────┤
│  类的公开 API（class public surface）— public 成员             │  ← 已做
├─────────────────────────────────────────────────────────────┤
│  方法 API（method API）— signature + 行为                      │  ← 已做
└─────────────────────────────────────────────────────────────┘
```

每往上一层：**传输距离变远 + 失败方式变多**。

---

## Concept 2 — HTTP

### Method call vs HTTP call 的根本差异

> Method call: 要么抛异常，要么返回值。**两种结局。**
> HTTP call: 要么抛异常，要么返回值，**要么消息送达但响应丢了**。**三种结局。**

第三种叫**部分失败（partial failure）**：服务器成功记录了，但响应在网上丢了 → 客户端
以为失败 → 重试 → 同一笔被记两次。所以 HTTP 世界需要：

- **幂等性**（idempotency）— 调一次和调一百次结果一样
- **重试 + 去重**（retry + deduplication）
- **`Idempotency-Key` 头**（生产支付 / 订单系统的标配）

### HTTP 风险清单 — 方法调用没有，HTTP 全有

| 风险 | 关键词 |
|---|---|
| **部分失败 / 传输失败** | timeout, connection reset, 502/504, DNS, TLS |
| **并发 / 可扩展性** | shared state, thread safety, rate limiting |
| **序列化 / 反序列化** | 类型擦除 — C# 类型变成字符串化 JSON |
| **不可信输入** | 验证 / 注入攻击 |
| **认证 / 授权** | 谁在调？能调吗？ |
| **版本化** | 已发布契约不能随便改 |
| **延迟** | 微秒 → 毫秒（1000× 慢） |

**记忆锚句**：
> Same logic. 1000× more failure modes.

### 信件结构

```text
请求信（HTTP request）
  起始行：POST /readings HTTP/1.1           ← 动词 + 路径 + 版本
  头部：  Host, Content-Type, Authorization
  空行
  正文：  { "Value": 75.5 }                  ← 可选

响应信（HTTP response）
  起始行：HTTP/1.1 202 Accepted              ← 版本 + 状态码 + 文字
  头部：  Content-Type, Location
  空行
  正文：  { "status": "accepted" }
```

### 四个核心动词

| 动词 | 含义 | 幂等？ |
|---|---|---|
| `GET` | 读 | ✅ |
| `POST` | 创建 / 提交（通常改状态） | ❌ |
| `PUT` | 整体替换 | ✅ |
| `DELETE` | 删除 | ✅ |

### 三段状态码

| 范围 | 含义 | 常见 | 客户端该重试吗？ |
|---|---|---|---|
| `2xx` | 我做了 | `200 OK`, `201 Created`, `202 Accepted`, `204 No Content` | — |
| `4xx` | **你错了** | `400 Bad Request`, `401`, `404`, `409 Conflict` | ❌ **不要** |
| `5xx` | **我错了** | `500`, `502 Bad Gateway`, `503` | ✅ **可以**（但前提：操作幂等） |

**2xx 内部要分清楚**：

| 码 | 含义差别 |
|---|---|
| `200 OK` | 做完了，结果在响应里 |
| `201 Created` | 已创建资源，新地址在 `Location` 头里 |
| `202 Accepted` | **我收下了，但还在处理**（异步） |

**记忆锚句**：
> 2xx = 我做了。 4xx = 你错了。 5xx = 我错了。
> 4xx 不要重试。 5xx 重试看幂等。

---

## Concept 3 — REST

### 定义
> REST 是**设计 HTTP API 的一种风格**。核心：
> **URL 表达名词（资源），HTTP 动词表达动作。**

### RPC 风格 vs REST 风格

| 操作 | RPC 风格（远程方法调用） | REST 风格（资源 + 动词） |
|---|---|---|
| 记一个读数 | `POST /addReading` | `POST /readings` |
| 看全部 | `POST /getAllReadings` | `GET /readings` |
| 看第 7 条 | `POST /getReadingById?id=7` | `GET /readings/7` |
| 删第 7 条 | `POST /deleteReading?id=7` | `DELETE /readings/7` |
| 改第 7 条 | `POST /updateReading` | `PUT /readings/7` |

### REST 的硬规矩 — 无状态（stateless）

> 每个 HTTP 请求必须自带所有必要信息（认证 token、资源 ID 等）。
> **服务端在两个请求之间不记你是谁。**

这条让服务**可以横向扩展** — 1 台或 100 台服务器对客户端透明，每台都能独立处理任何请求。

---

## 把三个概念缝起来 — 用 `TemperatureSensor.Api` 印证

| 概念 | 在我们项目里的样子 |
|---|---|
| API（库层） | `TemperatureSensor.Core.dll` 的 `public TemperatureSensor` 类 + `public RecordReading()` |
| HTTP | `POST http://localhost:5000/readings` 带 `{ "Value": 50 }` 正文 |
| REST | 资源 `/readings`（名词）+ 动词 `POST`（动作）。`GET /health` 探活 |
| 已识别失败模式 | NaN → `400`（4xx 不重试）；触发阈值 → `202 Accepted`（异步语义）|

---

## 做过的小检查题（自己答对的）

1. **Q**: HTTP 那一层会冒出哪些方法调用根本没有的失败？
   **A**: ✅ 1) 网络问题导致的调用失败；2) 大量请求导致的并发问题。
   （后来补充：序列化、不可信输入、认证、版本化、延迟。）

2. **Q**: `POST /readings` body `{"Value":50}` → 服务器回 `202 Accepted` —
   动词 / 路径 / 正文分别是什么？`202 Accepted` 告诉客户端两件事？
   **A**: ✅ POST / `/readings` / `{"Value":50}`；成功 + Accepted（已收下还在处理）。

3. **Q**: 服务器回 `400 Bad Request` — 该不该重试？
   **A**: ✅ **不该** — 客户端请求正文有问题，被 reject 了，重试还是会被 reject。
   （强化：4xx 不重试，5xx 看幂等性。）

---

## 明天的起点（N+2 实战）

**目录**：`d:\AITriage\2026-05-25\`

**Step 0**（已完成）：清场 ✅ — 之前 agent 演示版已删

**Step 1**：自己 scaffold
```powershell
cd d:\AITriage\2026-05-25
dotnet new web -n TemperatureSensor.Api --framework net9.0 -o TemperatureSensor.Api
```

后续步骤会按 **7-step 通用配方** 一步一步走：
1. **Scaffold**（dotnet new web）
2. **Wire deps**（dotnet add reference 到 Core）
3. **Register**（builder.Services.Add...）
4. **Build**（var app = builder.Build()）
5. **Configure**（app.MapPost / app.MapGet）
6. **Run**（app.Run）
7. **Test**（Invoke-WebRequest 冒烟测试）

每一步完成后 ping mentor，确认后再开下一步。

---

## Anchor Sentences（要记住的几句）

1. **API = signature + semantics.** Compiler checks the first, humans + tests check the second.
2. **HTTP = three outcomes, not two.** The third one is "delivered but response lost" — partial failure.
3. **2xx 我做了。 4xx 你错了。 5xx 我错了。**
4. **4xx 不重试。 5xx 重试看幂等。**
5. **REST = URL noun + HTTP verb.** No new verbs invented; stateless between requests.
