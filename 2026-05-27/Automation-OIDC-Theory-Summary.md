# 自动化 vs 脚本化 + OIDC 信任模型 — 理论闭环

> **Date**: 2026-05-27 (Day 48 — bonus session #4，纯理论 / advisor mode)
> **Why**: 昨晚 ACA 是手动 `az containerapp create`，被自己问到"那昨晚算自动化吗？" → 暴露概念漏洞
> **Goal**: 把"自动化 / 脚本化 / OIDC 信任 / 纵深防御"理论说穿，**为下一步真正的 CD 自动化打地基**

---

## 🎯 起点 — 一个困惑

> "昨晚我手敲了 `az containerapp create ... ghcr.io/.../latest`，这算自动化吗？"

**不算。** 这是 manual operation。脚本化和自动化都没沾边。

---

## 🧭 三层概念金字塔（从下往上）

```
┌────────────────────────────────────────────────┐
│ 自动化 automation                                │  ← 触发器是事件 / 时钟 / 上游产物
│  例：git push → CI 自动跑                         │     人不在回路里
│  例：cron 2 AM → 自动重启服务                     │
├────────────────────────────────────────────────┤
│ 脚本化 scripting                                 │  ← 把多步合并成一步
│  例：deploy.ps1 包含所有 az 命令                  │     但要人按
│  例：Ctrl+B 跑 dotnet build                      │
├────────────────────────────────────────────────┤
│ 手动 manual                                      │  ← 你一行一行敲命令
│  例：昨晚晚上的 az containerapp create + curl    │
└────────────────────────────────────────────────┘
```

**锚句**：**"自动化 ≠ 脚本化。判断标准 = 触发器是不是机器自己产生的。"**

---

## 🔑 OIDC 信任模型 — 4 层纵深防御

### 整体链路

```
你 push / 同事 PR
      ↓
┌─────────────────────────────────────┐
│ 护栏 0: yml on: 触发器                │  ← yml 决定 workflow 跑不跑
│  branches: [main]                    │
└─────────────────────────────────────┘
      ↓ 通过
┌─────────────────────────────────────┐
│ 护栏 1: job 的 if: 条件               │  ← yml 决定具体 job 跑不跑
│  event_name == 'push'                │
│  && ref == refs/heads/main           │
└─────────────────────────────────────┘
      ↓ 通过
┌─────────────────────────────────────┐
│ 护栏 2: OIDC subject 匹配             │  ← Azure 决定 token 给不给
│  federated cred 名单                  │
└─────────────────────────────────────┘
      ↓ 通过
┌─────────────────────────────────────┐
│ 护栏 3: Azure RBAC                   │  ← Azure 决定 token 能干啥
│  SP 在 rg 上的角色                    │
└─────────────────────────────────────┘
      ↓ 通过
   实际改 ACA
```

> **任何一道护栏挡住都进不去 production。这就是"defense in depth"。**

---

## 🤝 OIDC handshake 流程（关键 7 步）

```
1. setup（一次性）：你在 Azure 配 federated credential
   "我信任 GitHub OIDC issuer，并且 sub 必须 = repo:marlonj-ms/Learn:ref:refs/heads/main"

2. runner 启动 azure/login 步
3. runner 找 GitHub OIDC service 要 JWT token
4. GitHub 签发 JWT（用 GitHub 私钥签名）：
   { "iss": "...githubusercontent.com",
     "sub": "repo:marlonj-ms/Learn:ref:refs/heads/main",
     "aud": "api://AzureADTokenExchange",
     "exp": <10 分钟> }

5. runner 把 JWT 递给 Azure
6. Azure 验签 + 查名单：
   (a) 拉 GitHub 公钥（从 .well-known/openid-configuration）
   (b) 验签名 → ✅ 真是 GitHub 签的
   (c) 查 sub 字段 → ✅ 在 federated cred 名单里

7. Azure 发回 access token（短期 ~1h）
   → runner 用它 az containerapp update ...
```

**关键认知**：
- **没有长期 secret**（不是把 GitHub 的 client-secret 存进 Azure）
- **Azure 信任的是 GitHub 公钥，不是某个共享密钥**
- **JWT 10 分钟过期**，跑完就废 → 泄露面接近零

类比：**"中国护照 + 美国海关查签证条件" 而不是 "美国海关给你一把钥匙"**。

---

## 🚨 今晚最关键的认知纠偏 — "pipeline 跑" ≠ "az login 成功"

### 我答错的那道题（B 选项 = PR）

> **问**："federated cred subject = `repo:.../ref:refs/heads/main`，PR 触发能成功 az login 吗？"
> **我答 ✅，实际 ❌。**

### 错在哪里

我把"pipeline 能不能跑"和"az login 能不能成功"混成同一件事。它们其实在**两个不同的层**：

| | 谁说了算 | 判断依据 |
|---|---|---|
| **pipeline 触发** | **GitHub server** | yml 的 `on:` 写了什么 |
| **az login 成功** | **Azure AD** | OIDC token 的 `sub` 字段对得上 federated cred 吗 |

### PR 场景实际发生的事

```
1. PR 开了 → GitHub 检测到 PR 事件 → workflow 启动 ✅
2. runner 跑到 azure/login 这步
3. GitHub 发的 JWT 里 sub = "repo:marlonj-ms/Learn:pull_request"  ⚠️ 注意 sub 长啥样
4. Azure 查名单：subject = "repo:.../pull_request" → ❌ 没这条
5. Azure 拒绝：AADSTS70021 No matching federated identity record found
6. azure/login 这一步红 ❌
7. 后续 az containerapp update 根本没机会跑
```

**结论**：pipeline **确实跑了**，但 `az login` **单个步骤**在 Azure 那端被拒。

> **锚句**：**"被触发 ≠ 被授权。GitHub 决定能不能跑，Azure 决定能不能登。"**

---

## ⏭️ Skipped ≠ Failed — 优雅的护栏 1 行为

如果 yml 加了 `if: github.event_name == 'push' && github.ref == 'refs/heads/main'`，PR 场景下：

```
✅ build-and-push      (PR 也跑，测试通过)
⏭️ deploy-to-aca       (Skipped — if 不满足)

总评: ✅ All checks passed → PR 可以 merge
```

**`Skipped` 算 success，不算 fail**。整个 workflow 仍然绿，PR merge 顺畅。

### 但有个坑 — Required check 不能给 deploy job 用

如果你在 branch protection 把 `deploy-to-aca` 设成 "Required status check"，PR 会卡在 "Waiting for status to be reported"，因为 Skipped 状态被视作 "missing"。

**正确做法**：
- ✅ `build-and-push` 设 Required（PR 时一定会跑）
- ⬜ `deploy-to-aca` **绝不**设 Required（只在 main push 后跑，PR 时是 Skipped）

---

## 📋 OIDC subject 的安全范围对照

| Subject 写法 | 信任范围 | 风险 |
|---|---|---|
| `repo:marlonj-ms/Learn:ref:refs/heads/main` ⭐ | 只有 main 分支 | **最小，推荐** |
| `repo:marlonj-ms/Learn:environment:production` | 走 GitHub Environment "production" gate 的 job | 最小+审批墙 |
| `repo:marlonj-ms/Learn:pull_request` | **任何 PR**（包括 fork 的） | ⚠️ 危险 |
| `repo:marlonj-ms/Learn:*` | 所有 ref / PR / tag | 🚨 几乎等于裸奔 |

**多场景** = **多 federated credential**，不要为图省事用通配符。

---

## 🆕 今晚锁定的口诀 #16 → #20

| # | 口诀 |
|---|---|
| **16** | 自动化 ≠ 脚本化 — 双击 deploy.ps1 / Ctrl+B 不算自动化，触发器必须是机器自己产生的事件 |
| **17** | OIDC subject 是白名单字符串，默认拒绝；**PR 拿不到 prod 凭据是 feature**，不是 bug |
| **18** | "pipeline 跑" ≠ "az login 成功" — GitHub 决定前者，Azure 决定后者；**被触发 ≠ 被授权** |
| **19** | 纵深防御 4 层：`on:` → `if:` → OIDC subject → RBAC，**任一层兜底都能救你 production** |
| **20** | Required status check **只给 PR 时一定会跑的 job**（build/test）；deploy job 永远别 required，否则 Skipped 会卡 PR merge |

---

## ⚠️ 为什么今晚没真做实操 — 权限受限

配 federated credential 需要的 Azure 权限并不轻：

| 步骤 | 需要的权限 | 共享 marlondb sub 能不能动 |
|---|---|---|
| `az ad app create` | Entra ID Application Developer（tenant 级） | 大概率没有 |
| `az ad sp create` | 同上 | 同上 |
| `az ad app federated-credential create` | App owner | 同上 |
| `az role assignment create` 到 marlondb rg | User Access Administrator / Owner | 共享 rg，几乎没有 |

**这就是为什么生产 CI/CD 设置往往要找 IT / DevOps team**——跨应用层 + 身份层 + 资源层，安全收口必须在中央。

### 可行的下一步

| 选项 | 可行性 |
|---|---|
| A. 找 personal Azure sub（MSDN / 试用）做实验 | 看你账户 |
| B. 在 fork repo 上配 OIDC 指向 personal sub | 看 A |
| C. 跟公司 DevOps 提 ticket，让他们帮你给 marlondb 配 OIDC | 长流程 |
| D. **理论先扎实，实操延后** ⭐ | 今晚选的路径 |

---

## 🧩 一份接近生产可用的完整 yml（理论参考）

```yaml
# .github/workflows/temperature-sensor-cicd.yml
name: Temperature Sensor CI/CD

on:
  push:
    branches: [main]
  pull_request:
    branches: [main]

permissions:
  contents: read

jobs:
  build-and-push:
    runs-on: ubuntu-latest
    permissions:
      contents: read
      packages: write
    steps:
      - uses: actions/checkout@v4
      - uses: actions/setup-dotnet@v4
        with:
          dotnet-version: '9.0.x'
      - run: dotnet test 2026-05-25/TemperatureSensor.slnx
      - uses: docker/login-action@v3
        with:
          registry: ghcr.io
          username: ${{ github.actor }}
          password: ${{ secrets.GITHUB_TOKEN }}
      - uses: docker/build-push-action@v6
        with:
          context: 2026-05-25
          push: ${{ github.event_name == 'push' }}
          tags: |
            ghcr.io/marlonj-ms/temperature-sensor:${{ github.sha }}
            ghcr.io/marlonj-ms/temperature-sensor:latest

  deploy-to-aca:
    needs: build-and-push
    if: github.event_name == 'push' && github.ref == 'refs/heads/main'   # 🛡️ 护栏 1
    runs-on: ubuntu-latest
    permissions:
      id-token: write
      contents: read
    steps:
      - uses: azure/login@v2                                              # 🛡️ 护栏 2 在这步生效
        with:
          client-id:       ${{ secrets.AZURE_CLIENT_ID }}
          tenant-id:       ${{ secrets.AZURE_TENANT_ID }}
          subscription-id: ${{ secrets.AZURE_SUBSCRIPTION_ID }}
      - run: |
          az containerapp update `
            -n temperature-sensor -g marlondb `
            --image ghcr.io/marlonj-ms/temperature-sensor:${{ github.sha }}
```

---

## 🏁 今晚 One-Line Win

> "**昨晚那句'CI/CD 全程自动化'是吹牛——CI 闭环了，CD 还是手动的**。
> 今晚把'自动化 vs 脚本化'分清楚，把 OIDC 4 层纵深防御看穿，
> **理论先扎实，实操等权限到位再补。结果是底气更足，下次说'自动化'三个字时不会再含糊。**"

---

## 🚀 下一步选项

| Path | 内容 |
|---|---|
| **A. Generics（pre-staged 文件）** | 回到语言深度，明天的硬骨头 |
| **B. 申请 personal Azure sub** | 把今晚理论落地实操 |
| **C. 跟 DevOps 提 ticket** | 用公司 sub 做 OIDC |
| **D. 别的方向** | 你定 |
