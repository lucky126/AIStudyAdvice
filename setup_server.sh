#!/bin/bash

# 颜色定义
GREEN='\033[0;32m'
NC='\033[0m'

echo -e "${GREEN}开始配置服务器环境...${NC}"

# 1. 更新系统
echo "更新系统软件包..."
if [ -f /etc/debian_version ]; then
    # Debian/Ubuntu
    sudo apt-get update && sudo apt-get upgrade -y
    sudo apt-get install -y curl git
elif [ -f /etc/redhat-release ]; then
    # CentOS/RHEL/Volcano Engine Linux
    sudo yum update -y
    sudo yum install -y curl git
fi

# 2. 安装 Docker (如果未安装)
if ! command -v docker &> /dev/null; then
    echo "正在安装 Docker..."
    curl -fsSL https://get.docker.com | sh
    sudo systemctl start docker
    sudo systemctl enable docker
else
    echo "Docker 已安装"
fi

# 3. 安装 Docker Compose (如果未安装)
if ! command -v docker-compose &> /dev/null; then
    echo "正在安装 Docker Compose..."
    sudo curl -L "https://github.com/docker/compose/releases/download/v2.23.0/docker-compose-$(uname -s)-$(uname -m)" -o /usr/local/bin/docker-compose
    sudo chmod +x /usr/local/bin/docker-compose
else
    echo "Docker Compose 已安装"
fi

# 4. 检查项目文件
if [ ! -f "docker-compose.prod.yml" ]; then
    echo "错误：未找到 docker-compose.prod.yml 文件"
    echo "请确保您已将项目代码上传到服务器并在项目根目录下运行此脚本"
    exit 1
fi

# 5. 设置环境变量并启动
echo -e "${GREEN}请输入配置信息 (按回车使用默认值):${NC}"

read -p "请输入 Coze API Key: " COZE_KEY
if [ -z "$COZE_KEY" ]; then
    echo "错误: Coze API Key 是必须的"
    exit 1
fi

read -p "设置数据库密码 (默认: postgres_secure_pass): " DB_PASS
DB_PASS=${DB_PASS:-postgres_secure_pass}

# 创建 .env 文件
echo "COZE_API_KEY=$COZE_KEY" > .env
echo "DB_PASSWORD=$DB_PASS" >> .env

echo -e "${GREEN}正在启动服务...${NC}"
sudo docker-compose -f docker-compose.prod.yml up -d --build

echo -e "${GREEN}=====================================${NC}"
echo -e "${GREEN}部署完成！${NC}"
echo -e "应用正在后台运行，监听端口: 80"
echo -e "请确保火山云安全组已开放 TCP:80 端口"
echo -e "${GREEN}=====================================${NC}"
