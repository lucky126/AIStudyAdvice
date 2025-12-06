# AI 学习助手 (AI Study Advice)

AI 学习助手是一个基于 .NET 9 和 Blazor Server 构建的智能学习辅助平台。利用 Coze AI 工作流强大的分析能力，帮助学生通过试卷图像识别、错题管理和个性化练习生成，高效提升学习成绩。

## ✨ 功能特性

- **📄 试卷智能解析**：上传试卷图片，自动识别题目、选项及解析，并结构化存储。
- **❌ 错题本管理**：自动记录错题，支持按知识点、题型查看和复习。
- **🎯 个性化练习**：基于错题和薄弱知识点，利用 AI 生成针对性的强化练习题。
- **📊 学习数据分析**：可视化展示学习进度和知识点掌握情况。
- **💡 智能学习顾问**：根据学习数据提供个性化建议，支持随时查看与关闭。

### 🎨 界面与体验优化 (New)

- **沉浸式布局**：移除传统侧边栏，采用顶部导航栏，支持快速切换年级/学科，界面更加清爽聚焦。
- **可视化反馈**：知识点详情页采用红绿配色直观区分正误，题目解析布局优化，阅读体验更佳。
- **快捷操作**：学习中心首页集成上传、练习、顾问快捷入口，空状态下智能引导。

## 🛠 技术栈

- **框架**: .NET 9.0 (Blazor Server)
- **数据库**: PostgreSQL (使用 Entity Framework Core)
- **AI 服务**: [Coze (扣子)](https://www.coze.cn/) API
- **UI 组件**: Bootstrap / Blazor Native / Open Iconic

## 🚀 快速开始

### 环境要求

- [.NET 9.0 SDK](https://dotnet.microsoft.com/download/dotnet/9.0)
- [PostgreSQL](https://www.postgresql.org/) 数据库

### 安装步骤

1. **克隆仓库**
   ```bash
   git clone https://github.com/lucky126/AIStudyAdvice.git
   cd AIStudyAdvice
   ```

2. **配置数据库与 API**
   复制示例配置文件并重命名为 `appsettings.json`：
   ```bash
   cp appsettings.Example.json appsettings.json
   ```
   
   编辑 `appsettings.json`，填入你的 PostgreSQL 连接字符串和 Coze API 密钥：
   ```json
   {
     "ConnectionStrings": {
       "Postgres": "Host=localhost;Port=5432;Database=studydb;Username=postgres;Password=your_password"
     },
     "Coze": {
       "BaseUrl": "https://api.coze.cn/v1",
       "WorkflowIdParse": "你的试卷解析工作流ID",
       "WorkflowIdGenerate": "你的题目生成工作流ID",
       "WorkflowIdAdvice": "你的学习建议工作流ID",
       "ApiKey": "你的Coze API Key"
     }
   }
   ```

3. **运行项目**
   ```bash
   dotnet run
   ```
   项目启动后，支持内网访问：
   - 本机访问：`http://localhost:8099`
   - 内网访问：`http://<本机IP>:8099` (例如 `http://192.168.1.100:8099`)

   *注意：如果需要内网访问，请确保防火墙允许 8099 端口通信。*

## 📖 使用说明

1. **首页配置**：首次进入选择年级和学科，系统将记住你的选择。
2. **上传试卷**：在学习中心点击“上传作业”，系统将自动解析题目。
3. **学情分析**：查看知识点掌握情况，点击知识点可进入题目详情页查看错题解析。
4. **专项练习**：点击“专项练习”基于薄弱点生成强化训练。

## 🤝 贡献

欢迎提交 Issue 或 Pull Request 来改进这个项目！

## 📄 许可证

[MIT License](LICENSE)
