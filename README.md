# AI 学习助手 (AI Study Advice)

AI 学习助手是一个基于 .NET 9 和 Blazor Server 构建的智能学习辅助平台。利用 Coze AI 工作流强大的分析能力，帮助学生通过试卷图像识别、错题管理和个性化练习生成，高效提升学习成绩。

## ✨ 功能特性

- **📄 试卷智能解析**：上传试卷图片，自动识别题目（支持复杂父子题结构）、选项及解析。
- **❌ 错题本管理**：自动记录错题，支持按知识点、题型查看和复习。
- **🎯 个性化练习**：基于错题和薄弱知识点，利用 AI 生成针对性的强化练习题。
- **📊 学习数据分析**：可视化展示学习进度和知识点掌握情况。
- **💡 智能学习顾问**：根据学习数据提供个性化建议，内置智能缓存机制。

### 🚀 最新优化
- **性能优化**：采用单容器 Host 网络部署，减少网络开销，提升数据库连接稳定性。
- **会话隔离**：支持多角色（用户/管理员）并发登录。

## 🛠 技术栈

- **框架**: .NET 9.0 (Blazor Server)
- **数据库**: PostgreSQL (Entity Framework Core)
- **AI 服务**: [Coze (扣子)](https://www.coze.cn/) API
- **部署**: Docker (Host Network Mode)

## 🚀 快速开始

### 环境要求

- [.NET 9.0 SDK](https://dotnet.microsoft.com/download/dotnet/9.0)
- [PostgreSQL](https://www.postgresql.org/) 数据库

### 本地运行

1. **克隆仓库**
   ```bash
   git clone https://github.com/lucky126/AIStudyAdvice.git
   cd AIStudyAdvice
   ```

2. **配置应用**
   复制 `appsettings.Example.json` 为 `appsettings.json` 并填入配置：
   - 数据库连接字符串
   - Coze API Key 和 Workflow IDs
   - 管理员账号密码 Hash

3. **启动项目**
   ```bash
   dotnet run
   ```
   访问地址：`http://localhost:8099`

## 🐳 Docker 部署

项目使用单一 Docker 容器配合 Host 网络模式部署，简化了网络配置并提高了性能。

### 部署步骤

1. **准备环境**
   确保服务器已安装 Docker，且 PostgreSQL 数据库已运行。

2. **配置与部署**
   直接运行项目根目录下的部署脚本：

   **Linux / Mac:**
   ```bash
   chmod +x deploy.sh
   ./deploy.sh
   ```

   **Windows (PowerShell):**
   您可以参考 `deploy.sh` 的内容，执行相应的 `docker build` 和 `docker run` 命令。

   *注意：脚本默认使用 Host 网络模式，容器将直接使用宿主机的网络栈，数据库连接可直接指向 `127.0.0.1`。*

## 📖 文档

- [用户手册 (USER_GUIDE.md)](USER_GUIDE.md)
- [项目维基 (WIKI.md)](WIKI.md)

## 🤝 贡献

欢迎提交 Issue 或 Pull Request 来改进这个项目！

## 📄 许可证

[MIT License](LICENSE)
