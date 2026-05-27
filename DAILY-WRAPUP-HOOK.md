# Daily Wrap-up Hook — 每日收尾流程

## 触发关键词

跟 mentor 说任何一个：

- "**保存今天的学习**" / "**记录学习状态**" / "**记录今天的学习**" / "**记录一下学习**"
- "**记录进度**" / "**保存进度**" / "**更新学习状态**" / "**update learning status**"
- "**收尾**" / "**收工**" / "**wrap up**"
- "**save session**" / "**save today**"
- "**完成**" / "**搞定**" / "**done**" / "**done for today**"
- "**结束今天**" / "**今天到这**"

> mentor 看到上述关键词（或任何**语义等价**的"把今天的学习沉淀下来"类说法）就必须**完整跑完下面 5 步**，不能只做第 1 步就停。漏一步算失误。

## 自动化做的事（5 步必须全做完，不可只做部分）

1. **总结 markdown**：在 `YYYY-MM-DD/` 目录下生成 `[Topic]-Summary.md`。
2. **dailyread 子页**：在 `dailyread/` 下生成 `dailyread-YYYY-MM-DD.html`（5 分钟内能读完的版本）。
3. **首页 dailyread.html**：插入当日卡片，自动把昨日的 "今天 · 最新" 标记降级。
4. **cheatsheet 易错点**：把今天反复踩的坑追加到 `csharp-syntax-cheatsheet.html` 的"反复踩的坑"章节。
5. **Git 推送到远端** 🆕：用 `push-day.ps1` 把当日学习相关文件白名单 stage + commit + push 到 `origin/main`。**严禁 `git add .`** —— 仓库里有 `orcasql-mysqlflex-onboarding/`、`Understand-Anything/` 等非学习项目，必须用 pathspec 白名单过滤。

### Step 5 白名单（push-day.ps1 内置）

```
2026-*/                          ← 所有学习日的内容
dailyread/                       ← dailyread 子页目录
dailyread.html                   ← 主索引页
csharp-syntax-cheatsheet.html    ← cheatsheet
DAILY-WRAPUP-HOOK.md             ← 此流程文档
save-day.ps1                     ← 半自动脚本
push-day.ps1                     ← 此脚本自身
git-worktree-handbook.md         ← C# 学习配套笔记
```

staged 列表里出现任何非白名单路径 → `push-day.ps1` 会 **abort**，不会 commit。

## 半自动脚本

`save-day.ps1` 可以独立做第 3 步（链接管理）。用法：

```powershell
cd d:\AITriage
.\save-day.ps1                             # 今天
.\save-day.ps1 -Date 2026-05-19            # 指定
.\save-day.ps1 -Date 2026-05-19 -Title "LINQ Drills" -Desc "Where/Select/GroupBy/TryGetValue"
```

`push-day.ps1` 做第 5 步（git 推送）。用法：

```powershell
cd d:\AITriage
.\push-day.ps1                                # 今天 + 默认 commit message
.\push-day.ps1 -DryRun                        # 干运行，看会做什么
.\push-day.ps1 -Date 2026-05-27 -Message "Day 49 Generics+park"
```

mentor 在跑完 1–4 步后**必须**调一次 `push-day.ps1`（默认参数即可），不能漏。

## 历史补齐

如果发现某天的 dailyread 没生成（之前漏过 05-15、05-18），告诉 mentor：

> "把 05-15 的 dailyread 补上"

mentor 会基于该日的 `*-Summary.md` 重新生成 HTML 子页，再用 `save-day.ps1` 把卡片插回主页。

## 设计原则

- **链接管理可脚本化** → 由 `save-day.ps1` 负责
- **学习内容需要人脑梳理** → 由 mentor 当天生成总结
- **易错点累积** → 写进 cheatsheet 的"反复踩的坑"章节，长期沉淀
- **推送 = 异地备份** → `push-day.ps1` 用白名单 commit，避免与无关项目串台；本机磁盘坏了也不丢学习记录
