# Daily Wrap-up Hook — 每日收尾流程

## 触发关键词
跟 mentor 说："**保存今天的学习**" / "**收尾**" / "**save session**"。

## 自动化做的事

1. **总结 markdown**：在 `YYYY-MM-DD/` 目录下生成 `[Topic]-Summary.md`。
2. **dailyread 子页**：在 `dailyread/` 下生成 `dailyread-YYYY-MM-DD.html`（5 分钟内能读完的版本）。
3. **首页 dailyread.html**：插入当日卡片，自动把昨日的 "今天 · 最新" 标记降级。
4. **cheatsheet 易错点**：把今天反复踩的坑追加到 `csharp-syntax-cheatsheet.html` 的"反复踩的坑"章节。

## 半自动脚本

`save-day.ps1` 可以独立做第 3 步（链接管理）。用法：

```powershell
cd d:\AITriage
.\save-day.ps1                             # 今天
.\save-day.ps1 -Date 2026-05-19            # 指定
.\save-day.ps1 -Date 2026-05-19 -Title "LINQ Drills" -Desc "Where/Select/GroupBy/TryGetValue"
```

## 历史补齐

如果发现某天的 dailyread 没生成（之前漏过 05-15、05-18），告诉 mentor：

> "把 05-15 的 dailyread 补上"

mentor 会基于该日的 `*-Summary.md` 重新生成 HTML 子页，再用 `save-day.ps1` 把卡片插回主页。

## 设计原则

- **链接管理可脚本化** → 由 `save-day.ps1` 负责
- **学习内容需要人脑梳理** → 由 mentor 当天生成总结
- **易错点累积** → 写进 cheatsheet 的"反复踩的坑"章节，长期沉淀
