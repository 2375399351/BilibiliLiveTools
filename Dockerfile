# 使用Ubuntu 20.04作为基础镜像
FROM ubuntu:20.04 AS base
WORKDIR /app

# 安装.NET 6运行时和依赖
RUN apt-get update && \
    apt-get install -y wget && \
    wget https://packages.microsoft.com/config/ubuntu/20.04/packages-microsoft-prod.deb -O packages-microsoft-prod.deb && \
    dpkg -i packages-microsoft-prod.deb && \
    rm packages-microsoft-prod.deb && \
    apt-get update && \
    apt-get install -y dotnet-runtime-6.0 ffmpeg alsa-utils libasound2 && \
    rm -rf /var/lib/apt/lists/*

# 复制必要的文件
COPY . .

# 设置文件权限
RUN chmod -R 755 /app && \
    chmod +x BilibiliAutoLiver

# 暴露端口
EXPOSE 18686

# 设置启动命令
ENTRYPOINT ["./BilibiliAutoLiver"]
