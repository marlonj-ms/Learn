# push-day.ps1 — 每日收尾 hook 的 Step 5
#
# 用途：
#   把当日（或指定日期）学习相关的文件白名单 stage + commit + push 到 origin/main。
#   绝不 `git add .` —— 因为 d:\AITriage 里还有 orcasql / understand-anything 等非学习项目。
#
# 用法：
#   .\push-day.ps1                                # 默认今天 + 默认 message
#   .\push-day.ps1 -Date 2026-05-27               # 指定日期（影响 commit message）
#   .\push-day.ps1 -Message "Day 49 generics+park" # 自定义 message
#   .\push-day.ps1 -DryRun                        # 只看会做什么，不真改
#
# 白名单（永远只 stage 这些）：
#   2026-*/                          ← 所有学习日的内容
#   dailyread/                       ← dailyread 子页目录
#   dailyread.html                   ← 主索引页
#   csharp-syntax-cheatsheet.html    ← cheatsheet
#   DAILY-WRAPUP-HOOK.md             ← 流程文档
#   save-day.ps1                     ← 半自动脚本
#   push-day.ps1                     ← 此脚本自身
#   git-worktree-handbook.md         ← C# 学习配套笔记

param(
    [string]$Date,
    [string]$Message,
    [switch]$DryRun
)

$ErrorActionPreference = 'Stop'

# ---- 1. 日期解析 ----
if (-not $Date) { $Date = (Get-Date).ToString('yyyy-MM-dd') }
if ($Date -notmatch '^\d{4}-\d{2}-\d{2}$') {
    Write-Error "Date must be YYYY-MM-DD, got: $Date"
}

$root = Split-Path -Parent $PSCommandPath
Push-Location $root

try {
    # ---- 2. Sanity check: 是 git 仓库吗 ----
    $null = git rev-parse --is-inside-work-tree 2>$null
    if ($LASTEXITCODE -ne 0) {
        Write-Error "Not a git repository: $root"
    }

    $branch = (git branch --show-current).Trim()
    Write-Host "[INFO] Repo: $root"
    Write-Host "[INFO] Branch: $branch"

    # ---- 3. 白名单 pathspec ----
    # 注：`2026-*/` 在 git 默认 pathspec 下 `*` 不跨目录，必须用 magic glob 语法 `:(glob)2026-*/**`
    # 才能匹配 `2026-MM-DD/<any nested file>`。
    $pathspecs = @(
        ':(glob)2026-*/**',
        'dailyread/',
        'dailyread.html',
        'csharp-syntax-cheatsheet.html',
        'DAILY-WRAPUP-HOOK.md',
        'save-day.ps1',
        'push-day.ps1',
        'git-worktree-handbook.md'
    )

    Write-Host ""
    Write-Host "[STAGE] White-listed pathspecs:"
    foreach ($spec in $pathspecs) {
        Write-Host "         $spec"
    }
    Write-Host ""

    # ---- 4. 干运行 vs 真 stage ----
    if ($DryRun) {
        Write-Host "[DRY-RUN] Would run:" -ForegroundColor Yellow
        foreach ($spec in $pathspecs) {
            Write-Host "  git add -- $spec"
        }
        Write-Host ""
        Write-Host "[DRY-RUN] What WOULD be staged (running git add --intent-to-add equivalent):" -ForegroundColor Yellow

        # 用 git status 显示当前会被加入的内容（按 pathspec 过滤）
        $statusOut = git status --short -- $pathspecs 2>$null
        if ($statusOut) {
            $statusOut | ForEach-Object { Write-Host "  $_" }
        } else {
            Write-Host "  (nothing changed under white-listed paths)"
        }

        Write-Host ""
        $defaultMsg = if ($Message) { $Message } else { "daily: $Date learning save" }
        Write-Host "[DRY-RUN] Would commit with message:" -ForegroundColor Yellow
        Write-Host "         $defaultMsg"
        Write-Host "[DRY-RUN] Would push to: origin/$branch" -ForegroundColor Yellow
        Write-Host ""
        Write-Host "[DRY-RUN] No changes made. Re-run without -DryRun to actually push." -ForegroundColor Yellow
        return
    }

    # ---- 5. 实际 stage ----
    foreach ($spec in $pathspecs) {
        # `git add -- <spec>` 即使 spec 没有匹配项也不会报错（除非 spec 不存在）
        # 用 try-catch 容错
        try {
            git add -- $spec 2>$null
        } catch {
            # 静默跳过 —— 比如 push-day.ps1 还没存在的时候
        }
    }

    # ---- 6. 检查是否有东西被 stage ----
    $staged = git diff --cached --name-only
    if (-not $staged) {
        Write-Host "[OK] Nothing to commit. Hook outputs already pushed." -ForegroundColor Green
        return
    }

    Write-Host "[STAGED] Files about to be committed:"
    $staged | ForEach-Object { Write-Host "  $_" }
    Write-Host ""

    # ---- 7. 安全检查：staged 列表里不应该出现非学习路径 ----
    $suspicious = $staged | Where-Object {
        $_ -notmatch '^(2026-\d{2}-\d{2}/|dailyread/|dailyread\.html$|csharp-syntax-cheatsheet\.html$|DAILY-WRAPUP-HOOK\.md$|save-day\.ps1$|push-day\.ps1$|git-worktree-handbook\.md$)'
    }
    if ($suspicious) {
        Write-Host "[ABORT] Unexpected paths in staged list:" -ForegroundColor Red
        $suspicious | ForEach-Object { Write-Host "  $_" -ForegroundColor Red }
        Write-Host ""
        Write-Host "Run 'git reset' to unstage, then investigate." -ForegroundColor Red
        Write-Error "Refusing to commit suspicious paths."
    }

    # ---- 8. Commit ----
    if (-not $Message) {
        $Message = "daily: $Date learning save"
    }
    git commit -m $Message
    if ($LASTEXITCODE -ne 0) { Write-Error "git commit failed." }

    # ---- 9. Push ----
    Write-Host ""
    Write-Host "[PUSH] git push origin $branch"
    git push origin $branch
    if ($LASTEXITCODE -ne 0) {
        Write-Error "git push failed. Commit is local; re-run push later."
    }

    Write-Host ""
    Write-Host "[OK] $Date wrap-up pushed to origin/$branch ✅" -ForegroundColor Green

} finally {
    Pop-Location
}
