# GitHub Actions CI Summary (2026-05-26)

## 今日进度
- 新建 `.github/workflows/temperature-sensor-ci.yml`，实现主线 push/PR 自动化：restore → build → unit test → integration test → docker build。
- 本地全流程验证通过：API build、Core 13 个单测、API 3 个集成测试、Docker 镜像构建全部成功。
- 已 push 到 GitHub，Actions 页面成功触发并通过。

## 关键知识点
- `uses:` 调用 action，`run:` 执行 shell 命令。
- CI 只保护 main 分支，feature 分支合 PR 时才触发。
- Docker build context 必须指向 Dockerfile 所在目录。
- 所有测试通过后才构建镜像，保证产物质量。

## 明日计划
- 继续 CI/CD 流程（如 docker push、自动部署），或切换到泛型/测试等新专题。
