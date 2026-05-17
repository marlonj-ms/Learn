# Git Worktree Handbook

## Setup & Creation

```bash
# List all worktrees
git worktree list
///condition ? valueIfTrue : valueIfFalse
# Create worktree with an EXISTING branch
git worktree add <path> <branch>
git worktree add ../feature-a feature-a

# Create worktree with a NEW branch
git worktree add -b <new-branch> <path>
git worktree add -b hotfix-99 ../hotfix-99

# Create worktree from a remote branch (auto-tracks it)
git worktree add <path> origin/release-2.0
# or explicitly:
git worktree add -b release-2.0 ../release-2.0 origin/release-2.0

# Create a detached HEAD worktree (no branch, just a commit)
git worktree add --detach <path> <commit-hash>
git worktree add --detach ../investigate abc123f
```

## Inspection

```bash
# List all worktrees with branch and commit info
git worktree list

# Verbose output
git worktree list --porcelain
```

Example output:

```
d:/AITriage          abc1234 [main]
d:/AITriage-feature  def5678 [feature-a]
d:/AITriage-hotfix   ghi9012 [hotfix-99]
```

## Cleanup & Removal

```bash
# Remove a worktree (working directory must be clean)
git worktree remove <path>
git worktree remove ../hotfix-99

# Force remove (discards uncommitted changes)
git worktree remove --force ../hotfix-99

# Prune stale worktree references (folder was manually deleted)
git worktree prune

# Dry-run prune (see what would be cleaned)
git worktree prune --dry-run
```

## Locking (prevent accidental removal)

```bash
# Lock a worktree (e.g., on a network drive that's sometimes offline)
git worktree lock <path>
git worktree lock ../feature-a

# Lock with a reason
git worktree lock --reason "on external drive" ../feature-a

# Unlock
git worktree unlock ../feature-a
```

## Moving

```bash
# Move a worktree to a different directory
git worktree move <old-path> <new-path>
git worktree move ../feature-a ../work/feature-a
```

## Repair

```bash
# Fix broken links (e.g., after moving the main repo manually)
git worktree repair

# Repair from within a worktree
cd ../feature-a
git worktree repair
```

---

## Common Workflows

### Quick feature branch

```bash
git worktree add -b feature-login ../login
cd ../login
# work, commit, push
git push -u origin feature-login
# done — clean up
cd ../AITriage
git worktree remove ../login
git branch -d feature-login
```

### Review a PR while working on something else

```bash
git fetch origin
git worktree add ../pr-review origin/pr-branch
cd ../pr-review
# review, test, comment
cd ../AITriage
git worktree remove ../pr-review
```

### Hotfix without disrupting current work

```bash
git worktree add -b hotfix ../hotfix main
cd ../hotfix
# fix, commit, push
git push -u origin hotfix
cd ../AITriage
git worktree remove ../hotfix
```

---

## Rules & Gotchas

| Rule | Detail |
|---|---|
| **No duplicate branches** | Two worktrees cannot have the same branch checked out |
| **Always clean up** | Use `git worktree remove`, don't just delete the folder (or run `prune` after) |
| **Shared objects** | Commits/stashes made in any worktree are visible to all |
| **Independent index** | Each worktree has its own staging area and uncommitted changes |
| **Submodules** | Each worktree needs its own `git submodule update --init` |
| **Hooks** | Shared — all worktrees use the same `.git/hooks` from the main repo |

## Quick Reference Card

| Task | Command |
|---|---|
| New worktree + new branch | `git worktree add -b <branch> <path>` |
| New worktree + existing branch | `git worktree add <path> <branch>` |
| List all | `git worktree list` |
| Remove | `git worktree remove <path>` |
| Force remove | `git worktree remove --force <path>` |
| Clean stale refs | `git worktree prune` |
| Move | `git worktree move <old> <new>` |
| Lock | `git worktree lock <path>` |
| Unlock | `git worktree unlock <path>` |
| Repair links | `git worktree repair` |
