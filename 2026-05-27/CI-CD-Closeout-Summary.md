# CI/CD Close-out — GitHub Actions + GHCR

> **Date**: 2026-05-27 (Day 48)
> **Topic**: GitHub Actions CI/CD pipeline 收官 — 把昨天 push 的 yml 跑完整，加上 docker build + push to GHCR
> **Mode**: C# Mentor walk-through (mode A — mentor 边讲边改 yml)
> **Status**: ✅ DONE — Run #3 (commit `dafde3d`) `conclusion: success`，镜像 `ghcr.io/marlonj-ms/temperature-sensor:<sha>` 已在 registry

---

## 🎯 今天的主线

昨天（2026-05-26）虽然把 `temperature-sensor-ci.yml` 推到了远端，但**只跑了 restore + build + test**，没有镜像推送，CI 故事没闭环。今天的目标只有一个：

> **让 `git push` → image 自动出现在 registry**。

这就是 **CI/CD 的 CI 部分** 完整意义：每次 push，自动产出可部署的 artifact。

---

## ✅ 今天达成

```text
git push main
  ↓
GitHub Actions (build-test-and-image job)
  ↓
checkout → setup-dotnet → restore → build
  ↓
unit tests (13) → integration tests (3)
  ↓
docker/login-action → ghcr.io 认证
  ↓
docker/build-push-action → build + push (一步完成)
  ↓
ghcr.io/marlonj-ms/temperature-sensor:<commit-sha>   ← 可追溯的不可变 tag
ghcr.io/marlonj-ms/temperature-sensor:latest        ← 方便部署的 floating tag
```

**证据：**
- Run #3 URL: https://github.com/marlonj-ms/Learn/actions/runs/26487205739
- Commit: `dafde3defe1e2d109af8d7fdd06cec813d365b00` ("CI: push image to GHCR with commit-sha tag")
- Image visibility: **private** (默认), 在 https://github.com/marlonj-ms?tab=packages 能看到

---

## 📜 最终的 `temperature-sensor-ci.yml`

```yaml
name: Temperature Sensor CI

on:
  push:
    branches: [ main ]
  pull_request:
    branches: [ main ]

jobs:
  build-test-and-image:
    runs-on: ubuntu-latest
    permissions:
      contents: read
      packages: write          # ← 给 GITHUB_TOKEN 写 GHCR 的权限
    steps:
      - name: Checkout source
        uses: actions/checkout@v4

      - name: Setup .NET
        uses: actions/setup-dotnet@v4
        with:
          dotnet-version: '9.0.x'

      - name: Restore API
        run: dotnet restore 2026-05-25/TemperatureSensor.Api/TemperatureSensor.Api.csproj

      - name: Build API
        run: dotnet build 2026-05-25/TemperatureSensor.Api/TemperatureSensor.Api.csproj --configuration Release --no-restore

      - name: Run core unit tests
        run: dotnet test 2026-05-26/TemperatureSensor.Core.Tests/TemperatureSensor.Core.Tests.csproj --configuration Release

      - name: Run API integration tests
        run: dotnet test 2026-05-26/TemperatureSensor.Api.Tests/TemperatureSensor.Api.Tests.csproj --configuration Release

      - name: Log in to GHCR
        uses: docker/login-action@v3
        with:
          registry: ghcr.io
          username: ${{ github.actor }}
          password: ${{ secrets.GITHUB_TOKEN }}

      - name: Build & push image
        uses: docker/build-push-action@v6
        with:
          context: 2026-05-25
          push: ${{ github.event_name == 'push' }}     # PR 不推，只 main 推
          tags: |
            ghcr.io/marlonj-ms/temperature-sensor:${{ github.sha }}
            ghcr.io/marlonj-ms/temperature-sensor:latest
```

---

## 🧠 今天 5 个新锁定心法（locked口诀）

| # | 口诀 | 一句话注释 |
|---|---|---|
| 1 | **`:latest` 是生产毒药** | CI/CD 永远给具体的 immutable tag（commit sha / version）。`:latest` 只能作为"方便人记"的 floating alias |
| 2 | **`GITHUB_TOKEN` 的作用域 = job** | 它是每个 job 自动注入的 ephemeral token。`permissions:` 只能写在 workflow 顶层或 job 顶层，**没有 step 级 permissions** |
| 3 | **PR build, but only main push** | `push: ${{ github.event_name == 'push' }}` 让 PR 验证完整 pipeline 又不污染 registry |
| 4 | **build-push-action = 一站式** | 不要分"build" + "push" 两步。`docker/build-push-action@v6` 内部直接走 buildx，比 CLI 的 `docker build && docker push` 更快、有 layer cache、能 multi-arch |
| 5 | **CI runner FS ≠ Docker build context** | runner 上 checkout 出来的目录 vs. daemon 看到的 build context 是两个独立世界。`context: 2026-05-25` 告诉 daemon **从 runner 的哪个子目录抓 build context** |

---

## 📚 概念拆解（按今天讲过的顺序）

### 1. 为什么 yml 文件本身要 commit 进仓库？

> **基础设施即代码（Infrastructure as Code）。**

`.github/workflows/*.yml` 跟你的源码住在同一个仓库里。意味着：
- ✅ pipeline 变更走 PR review
- ✅ 历史可追溯（git log）
- ✅ 谁动了 CI 一目了然
- ✅ 不需要在 GitHub UI 上点点点配置

对比：Jenkins 时代的 pipeline 经常住在 Jenkins server 自己的数据库里，跟代码脱钩，每次改动靠口头通知。

### 2. `permissions:` 块的真正意义

```yaml
permissions:
  contents: read       # 读源代码
  packages: write      # 写 GHCR
```

GitHub Actions 给每个 job 自动注入一个**短命 token**（`GITHUB_TOKEN`），它的能力**默认是只读**。要写 GHCR、写 Issues、写 PR comments，都要在 `permissions:` 里**显式 opt-in**。

> **锚句：最小权限原则。token 默认啥都不能干，你要哪个权限就声明哪个。**

### 3. `docker/login-action@v3` 在做什么

它其实就跑了一条命令：

```bash
echo $TOKEN | docker login ghcr.io -u marlonj-ms --password-stdin
```

把 token 喂给 docker daemon，让后续的 `docker push` 知道用谁的身份去推。

### 4. `docker/build-push-action@v6` 在做什么

```yaml
uses: docker/build-push-action@v6
with:
  context: 2026-05-25            # ← 等价于 `docker build 2026-05-25`
  push: true                     # ← 构建完之后立刻 push
  tags: |
    ghcr.io/.../...:<sha>
    ghcr.io/.../...:latest
```

它做了：
1. 找 build context 根目录下的 `Dockerfile`（除非用 `file:` override）
2. 启 buildx engine（比 legacy builder 快，支持 layer cache + multi-arch）
3. build 出镜像，**同时贴上多个 tag**（多 tag 不重新构建，只是给同一个 image id 多个名字）
4. push 所有 tag 到 registry

### 5. `${{ github.sha }}` vs `${{ github.event_name }}`

GitHub Actions 暴露了一堆 context 变量：

| 变量 | 值 | 用途 |
|---|---|---|
| `github.sha` | 触发本次 run 的 commit SHA（不是 yml 文件的 sha！） | 给镜像打**不可变** tag |
| `github.actor` | 触发本次 run 的 GitHub 用户名 | 给 docker login 当 username |
| `github.event_name` | `push` / `pull_request` / `schedule` ... | 用来分支判断（PR 不推、只 main 推） |
| `secrets.GITHUB_TOKEN` | 自动注入的 job-scoped token | 不用自己生成 PAT |

### 6. GHCR 包默认是 private —— 不是 push 失败

工作流跑成功后，访问 `https://api.github.com/users/marlonj-ms/packages/container/temperature-sensor/versions` 返 **401**。

**原因：包默认私有。** 公开的未认证 REST 端点看不到私有包，所以返 401。这不是推送失败。

3 种查看方式：
1. **UI**：`https://github.com/marlonj-ms?tab=packages` → `temperature-sensor`
2. **认证 REST**：用 PAT (`read:packages` scope) 加 `Authorization: Bearer <PAT>` header
3. **docker pull**：`docker login ghcr.io -u marlonj-ms` → `docker pull ghcr.io/marlonj-ms/temperature-sensor:latest`

---

## 🪤 反复踩的坑（追加进 cheatsheet）

### 坑 30 — CI 里 yml 的 `permissions:` 没有 step 级
```yaml
# ❌ 不存在 step 级 permissions
steps:
  - uses: docker/login-action@v3
    permissions: { packages: write }   # 这块根本不会被识别

# ✅ 写在 job 顶层
jobs:
  build:
    permissions:
      packages: write
    steps:
      - uses: docker/login-action@v3
```

### 坑 31 — `:latest` 看着方便，生产是定时炸弹
```bash
# ❌ 生产 deployment 引用 :latest
docker pull myapp:latest    # 今天和明天可能是完全不同的二进制

# ✅ 引用具体的 immutable tag
docker pull myapp:dafde3defe1e2d109af8d7fdd06cec813d365b00
```
出了 bug 想 roll back，`:latest` 已经飘到下一版了；具体 sha tag 永远指向同一个二进制。

### 坑 32 — GHCR 推成功 ≠ 公开可见
匿名 REST endpoint 返 401 **不代表推失败**。GHCR 包默认 visibility 是 `private`。要么去 Package settings 改 Public，要么用 PAT 认证查。

### 坑 33 — `${{ github.sha }}` 不是 yml 文件的 sha
它是**触发本次 run 的 commit** 的 sha。改 yml 文件 push 一次 → 该次 commit sha 就是 `github.sha`。改源代码 push 一次 → 那次的 commit sha 才是新的 `github.sha`。

---

## 🪜 下一步

- ⬜ **Next session (优先)**: Generics Session 1（昨天 pre-staged 的 `Generics-Constraints-Variance.cs` 还 exit 1，正好当调试开场题）
- ⬜ **Or alternatively**: Azure Container Apps 部署 GHCR 镜像（CD 部分）
- ⬜ 把 GHCR 包改成 public（如果想匿名 pull）
- ⬜ 给 image 加 [OCI labels](https://github.com/opencontainers/image-spec/blob/main/annotations.md)（`org.opencontainers.image.source` 等）方便 registry 显示链接

---

## 🏁 一句话总结

> "代码 push → 测试 → 镜像 → registry 自动完成。今天我有了一条真正的 CI pipeline，下次只剩把镜像拉到云上跑。"

CD 还没上，但 CI 闭环了。0→1 production arc 现在带上了"每次提交都验证、都产出 artifact" 的工程能力。🎯
