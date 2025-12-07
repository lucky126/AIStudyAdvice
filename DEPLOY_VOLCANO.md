# 部署到火山引擎 (Volcano Engine) 指南

本指南将帮助您将 AI 学习助手部署到火山引擎的云服务器 (ECS) 上。

## 1. 准备工作

1.  **购买云服务器**
    *   登录火山引擎控制台，购买 ECS 实例。
    *   操作系统建议选择 **Ubuntu 20.04/22.04 LTS** 或 **CentOS 7/Stream 8**。
    *   配置公网 IP。

2.  **配置安全组**
    *   在实例的安全组规则中，确保开放以下端口：
        *   **TCP: 80** (HTTP 服务)
        *   **TCP: 22** (SSH 远程连接)

## 2. 部署步骤

### 方法一：使用 Git 拉取代码 (推荐)

1.  **登录服务器**
    ```bash
    ssh root@您的服务器公网IP
    ```

2.  **拉取代码**
    如果服务器未安装 Git，请先安装：
    ```bash
    # Ubuntu/Debian
    apt-get update && apt-get install -y git
    # CentOS
    yum install -y git
    ```
    
    克隆项目代码：
    ```bash
    git clone https://github.com/lucky126/AIStudyAdvice.git
    cd AIStudyAdvice
    ```

3.  **运行安装脚本**
    ```bash
    chmod +x setup_server.sh
    ./setup_server.sh
    ```
    *   脚本会自动安装 Docker 和 Docker Compose。
    *   按提示输入您的 `Coze API Key` 和数据库密码。
    *   脚本将自动构建镜像并启动服务。

### 方法二：手动上传代码

如果不方便使用 Git，也可以将本地代码上传到服务器。

1.  **上传代码**
    ```bash
    # 在本地终端执行
    scp -r ./AIStudyAdvice root@您的服务器IP:/root/
    ```

2.  **登录并运行**
    ```bash
    ssh root@您的服务器IP
    cd /root/AIStudyAdvice
    chmod +x setup_server.sh
    ./setup_server.sh
    ```

## 3. 验证部署

部署完成后，在浏览器中访问服务器的公网 IP：
`http://您的服务器IP`

如果看到登录页面，说明部署成功！

## 4. 后续维护

- **查看日志**
  ```bash
  docker-compose -f docker-compose.prod.yml logs -f
  ```

- **停止服务**
  ```bash
  docker-compose -f docker-compose.prod.yml down
  ```

- **更新代码**
  ```bash
  git pull
  docker-compose -f docker-compose.prod.yml up -d --build
  ```
