# 使用官方.NET运行时镜像作为基础
FROM mcr.microsoft.com/dotnet/runtime:6.0-focal AS base
WORKDIR /app

# 安装依赖
RUN apt-get update && \
    apt-get install -y ffmpeg libasound2 && \
    rm -rf /var/lib/apt/lists/*

# 复制必要的文件
COPY BilibiliAutoLiver .
COPY libe_sqlite3.so .
COPY libSkiaSharp.so .
COPY appsettings.json .
COPY nlog.config .
COPY wwwroot ./wwwroot

# 设置执行权限
RUN chmod +x BilibiliAutoLiver

# 暴露端口
EXPOSE 18686

# 设置启动命令
ENTRYPOINT ["./BilibiliAutoLiver"]
