# save-day.ps1 — C# 学习每日收尾 hook
#
# 用法：
#   .\save-day.ps1                            # 用今天日期
#   .\save-day.ps1 -Date 2026-05-19           # 指定日期
#   .\save-day.ps1 -Title "LINQ Drills" -Desc "Where/Select 等"
#
# 功能：
#   1. 检查当日学习目录是否存在
#   2. 检查 dailyread/dailyread-YYYY-MM-DD.html 是否存在；不存在则提示
#   3. 检查首页 dailyread.html 是否已有当日卡片；缺则自动插入到顶部
#   4. 自动把上一个 "today" 卡片降级为普通卡片
#
# 注：这个脚本只做"链接管理"，不会自动生成 dailyread 子页的内容。
# 内容仍然由 mentor 在当日生成，因为内容需要梳理学习重点和易错点。

param(
    [string]$Date,
    [string]$Title,
    [string]$Desc
)

$ErrorActionPreference = 'Stop'

# 1. 解析日期
if (-not $Date) { $Date = (Get-Date).ToString('yyyy-MM-dd') }
if ($Date -notmatch '^\d{4}-\d{2}-\d{2}$') {
    Write-Error "Date must be YYYY-MM-DD, got: $Date"
}

$root        = Split-Path -Parent $PSCommandPath
$indexFile   = Join-Path $root 'dailyread.html'
$dayDir      = Join-Path $root $Date
$dayReadFile = Join-Path $root "dailyread/dailyread-$Date.html"

# 2. 检查当日学习目录
if (-not (Test-Path $dayDir)) {
    Write-Warning "No study folder for $Date at $dayDir."
}

# 3. 检查 dailyread 子页是否存在
if (-not (Test-Path $dayReadFile)) {
    Write-Warning "Missing $dayReadFile."
    Write-Host "  → Ask the mentor to generate today's dailyread first, then re-run this script."
    return
}

# 4. 读首页 HTML
$html = Get-Content $indexFile -Raw -Encoding UTF8

# 5. 已存在当日卡片？跳过插入
if ($html -match "dailyread-$Date\.html") {
    Write-Host "[OK] dailyread.html already references $Date — nothing to do."
    return
}

# 6. 提示标题/描述
if (-not $Title) {
    $Title = Read-Host "Card title for $Date"
}
if (-not $Desc) {
    $Desc  = Read-Host "Short desc (one line, key bullets)"
}

# 7. 把现有的 .card.today 降级为普通 .card
$html = $html -replace 'class="card today"', 'class="card"'
# 同时移除 "今天 · 最新" 标记（保留日期）
$html = $html -replace '(<div class="date">\d{4}-\d{2}-\d{2})\s*·\s*今天\s*<span class="tag">最新</span>', '$1'

# 8. 构造新卡片片段
$card = @"

<a class="card today" href="dailyread/dailyread-$Date.html">
  <div class="date">$Date · 今天 <span class="tag">最新</span></div>
  <div class="title">$Title</div>
  <div class="desc">$Desc</div>
</a>
"@

# 9. 插入到 subtitle 段落之后
$anchor = '<p class="subtitle">每日学习复习首页 — 每个子页面 5 分钟内读完</p>'
if ($html -notmatch [regex]::Escape($anchor)) {
    Write-Error "Cannot find subtitle anchor in dailyread.html. Aborting."
}
$html = $html -replace [regex]::Escape($anchor), ($anchor + $card)

# 10. 写回（保留 UTF-8 无 BOM）
[System.IO.File]::WriteAllText($indexFile, $html, (New-Object System.Text.UTF8Encoding $false))

Write-Host "[OK] Inserted card for $Date into dailyread.html."
Write-Host "     Title: $Title"
Write-Host "     Desc : $Desc"
