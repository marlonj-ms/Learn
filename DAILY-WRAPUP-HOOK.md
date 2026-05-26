# Daily Wrap-up Hook — 每日收尾流程

## 触发关键词

跟 mentor 说任何一个：

- "**保存今天的学习**" / "**记录学习状态**" / "**记录今天的学习**" / "**记录一下学习**"
- "**记录进度**" / "**保存进度**" / "**更新学习状态**" / "**update learning status**"
- "**收尾**" / "**收工**" / "**wrap up**"
- "**save session**" / "**save today**"
- "**完成**" / "**搞定**" / "**done**" / "**done for today**"
- "**结束今天**" / "**今天到这**"

> mentor 看到上述关键词（或任何**语义等价**的"把今天的学习沉淀下来"类说法）就必须**完整跑完下面 4 步**，不能只做第 1 步就停。漏一步算失误。

## 自动化做的事（4 步必须全做完，不可只做部分）

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
