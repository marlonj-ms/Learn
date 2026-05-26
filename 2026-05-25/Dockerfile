# ============================================================
# TemperatureSensor.Api — Multi-stage Dockerfile
# Build context: d:\AITriage\2026-05-25\
# Author: Marlon (Day 47, 2026-05-26)
# ============================================================


# ------------ Stage 1: build ------------
# 基镜像 base image: .NET 9 SDK (有编译器、CLI、所有工具)
FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build

# 容器内的工作目录 working directory
WORKDIR /src

# --- 步骤 A: 先复制两个项目的 .csproj，做依赖层缓存 ---
# TODO[1]: 把 Api 的 csproj 复制到容器里 TemperatureSensor.Api/ 下
# 提示：源路径是构建上下文里的 "TemperatureSensor.Api/TemperatureSensor.Api.csproj"
#       目标路径是容器里的 "TemperatureSensor.Api/"
COPY TemperatureSensor.Api/TemperatureSensor.Api.csproj TemperatureSensor.Api/


# TODO[2]: 把 Core 的 csproj 复制到容器里 TemperatureSensor.Core/ 下
# 提示：和 TODO[1] 同样的套路，把 "Api" 换成 "Core"
COPY TemperatureSensor.Core/TemperatureSensor.Core.csproj TemperatureSensor.Core/


# --- 步骤 B: 还原 NuGet 包 (这一层会被 Docker 缓存，下次没改 csproj 就直接跳过) ---
RUN dotnet restore TemperatureSensor.Api/TemperatureSensor.Api.csproj

# --- 步骤 C: 现在才复制全部源代码 (改代码不会让 restore 那层失效) ---
COPY . .

# --- 步骤 D: 发布 publish (编译 + 收集运行时所需所有文件 → /app/publish) ---
# TODO[3]: 发布 Api 项目，Release 配置，输出目录 /app/publish
# 提示：dotnet publish <csproj路径> -c Release -o <输出路径>
RUN dotnet publish TemperatureSensor.Api/TemperatureSensor.Api.csproj -c Release -o /app/publish


# ------------ Stage 2: runtime ------------
# 基镜像 base image: ASP.NET 9 runtime (只有运行时，没有编译器，~200 MB)
# TODO[4]: 写出 Stage 2 的起点。基镜像是 mcr.microsoft.com/dotnet/aspnet:9.0
# 提示：和 Stage 1 的 FROM 一样的格式，但这一行不需要 AS 别名（这就是最终 stage）
FROM mcr.microsoft.com/dotnet/aspnet:9.0

WORKDIR /app

# 把 Stage 1 编译好的 publish 目录搬过来 (这是唯一跨越 stage 边界的桥)
COPY --from=build /app/publish .

# 容器内监听 8080 端口；这只是声明，不是真正的端口映射
EXPOSE 8080
ENV ASPNETCORE_URLS=http://+:8080

# 容器启动时跑的命令
# TODO[5]: 启动命令应该是 dotnet 跑 TemperatureSensor.Api.dll
# 提示：ENTRYPOINT 用 JSON 数组语法 ["可执行文件", "参数1", "参数2"...]
ENTRYPOINT ["dotnet", "TemperatureSensor.Api.dll"]
