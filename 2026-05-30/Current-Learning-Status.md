# C# Learning Journey — Current Status

> **Last updated**: 2026-05-30 (Day 53 — EF Core 基础起步)
> **Mode**: C# Mentor walk-through（概念→学员复述→纠正→下一步 + 边读 code 边问）

---

## 🎯 Today's Topic (Day 53)

主题：**EF Core 基础（Entity Framework Core）** —— ORM 是什么、`DbContext` / `DbSet<T>`、亲眼看 `IQueryable` 链子被翻译成真实 SQL。

出发点：Day 52 关掉 LINQ 时摸到 `IQueryable<T>` → `Expression<Func<T,bool>>` → 被翻译成 SQL。Day 53 第一次见真的 `DbSet<T>` 干这件事。

状态：核心概念全收 ✅ · demo 跑通 ✅ · 变更追踪讲到一半（明天验证）。

---

## ✅ Day 53 Done

- [x] ORM 心智模型（表↔DbSet、行↔实体对象、列↔属性、SELECT↔Where）
- [x] `DbContext` = 数据库会话总管（连接 + 变更追踪 + SaveChanges）
- [x] `DbSet<Reading> Readings => Set<Reading>();` 整行逐片段拆解
- [x] 澄清：`Set<T>()` 是 **DbContext 基类方法**，不是 DbSet 的初始化
- [x] `DbSet` 两种写法对比（auto-property 反射注入 vs `Set<T>()` 现代写法）
- [x] 跑通 `EFCore-FirstQuery` demo，`LogTo` 打印 EF 生成的 CREATE / INSERT / SELECT
- [x] 解码输出：约定主键、复数表名、decimal→TEXT、参数化防注入、远程过滤只回 2 行
- [x] 理解 `AsEnumerable()` 翻译截止线（线后全在内存 = 性能 bug）

---

## 🅿️ Day 54 Resume Point（明天第一件事）

1. 先回答昨天留的检查题：`Set<Reading>()` 定义在哪个类（→ `DbContext` 基类，继承来的，所以裸调）。
2. **变更追踪（change tracking）实操**：跑「只改属性不调 `Update` → SaveChanges 是否自动 UPDATE」，验证快照对比机制。
3. **`AsNoTracking()`** 只读优化对比。
4. （Day 55+ 落地）把 EF Core 用 `AddDbContext` 接进 `TemperatureSensor.Api`，假数据 → 真 SQLite。

详见今天的 summary：`2026-05-30/EFCore-Fundamentals-Summary.md`。

---

## 📌 口诀进度

Day 53 新增 #74–#78（EF Core 5 条），累计 **78** 条。详见 summary。

---

## 🏁 Today's One-Line Win

> **"`.Where(r => r.Celsius > 30m)` 在控制台变成真实 `SELECT ... WHERE ...` —— Day 52 的 `IQueryable` 理论落地。"**
