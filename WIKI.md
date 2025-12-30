# AI Study Advice 项目文档

## 1. 项目概况

AI Study Advice 是一个基于 .NET 9 和 Blazor Server 的智能学习辅助系统。它利用 Coze AI 工作流能力，提供试卷解析、错题管理、个性化练习生成和智能学习建议等功能。

### 1.1 核心功能
*   **试卷解析**：上传图片，AI 识别题目内容及解析。
*   **错题管理**：自动归纳错题，统计知识点掌握情况。
*   **专项练习**：基于薄弱点生成针对性练习题。
*   **智能顾问**：分析学习数据，提供个性化学习建议。
*   **多角色系统**：支持普通用户（学生）和管理员（邀请码管理、用户管理）。

## 2. 技术架构

### 2.1 技术栈
*   **后端/前端**: .NET 9.0 (Blazor Server)
*   **数据库**: PostgreSQL (Entity Framework Core)
*   **AI 集成**: Coze API (HTTP Client)
*   **身份认证**: 自定义 AuthenticationStateProvider + Cookie Session
*   **部署**: Docker (Host Network Mode)

### 2.2 目录结构
*   `Pages/`: Blazor 页面组件
*   `Models/`: 数据库实体模型 (User, Question, Paper, etc.)
*   `Services/`: 业务逻辑服务 (CozeService, AuthService)
*   `Data/`: EF Core 数据库上下文
*   `wwwroot/`: 静态资源 (CSS, JS)
*   `deploy.sh`: Docker 部署脚本

## 3. 数据库设计

主要实体关系如下：

*   **Users**: 用户账户信息 (Username, PasswordHash, etc.)
*   **Papers**: 试卷记录 (关联 User)
*   **Questions**: 题目详情 (关联 Paper)
*   **KnowledgeStats**: 知识点掌握统计 (关联 User)
*   **PracticeQuestions**: 生成的练习题记录
*   **InvitationCodes**: 注册邀请码
*   **AdviceHistories**: 学习建议历史记录

## 4. 配置说明

配置文件 `appsettings.json` 结构：

```json
{
  "ConnectionStrings": {
    "Postgres": "Host=127.0.0.1;Port=5432;Database=studydb;Username=postgres;Password=..."
  },
  "Admin": {
    "Username": "admin",
    "Password": "..." // SHA256 Hash
  },
  "Coze": {
    "BaseUrl": "https://api.coze.cn/v1",
    "ApiKey": "...",
    "WorkflowIdParse": "...",
    "WorkflowIdGenerate": "...",
    "WorkflowIdAdvice": "..."
  }
}
```

## 5. 部署指南

项目推荐使用 Docker 单容器部署，利用 Host 网络模式连接宿主机数据库。

### 5.1 环境准备
*   Docker Engine
*   PostgreSQL 数据库 (运行在宿主机或网络可达位置)

### 5.2 部署步骤

1.  **配置环境变量**
    修改 `deploy.sh` 中的环境变量，或者在运行时传递。关键变量：
    *   `ConnectionStrings__Postgres`: 数据库连接字符串
    *   `Coze__*`: Coze API 相关配置

2.  **运行部署脚本**
    在 Linux/Mac 环境下：
    ```bash
    chmod +x deploy.sh
    ./deploy.sh
    ```
    
    脚本会自动构建镜像 `study-app` 并以 host 网络模式启动容器。

3.  **访问应用**
    部署成功后，访问宿主机 IP 的 80 端口（或脚本中配置的端口）。

## 6. 常见问题

*   **数据库连接失败**：检查 `pg_hba.conf` 是否允许连接，以及 `ConnectionStrings__Postgres` 中的 IP 是否正确（Host 模式下通常使用 `127.0.0.1` 连接宿主机）。
*   **AI 响应超时**：`Program.cs` 中已配置 5 分钟超时时间，确保 Coze 工作流能在该时间内返回。
