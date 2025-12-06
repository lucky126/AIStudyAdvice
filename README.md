# AI 学习助手 (AI Study Advice)

AI 学习助手是一个基于 .NET 9 和 Blazor Server 构建的智能学习辅助平台。利用 Coze AI 工作流强大的分析能力，帮助学生通过试卷图像识别、错题管理和个性化练习生成，高效提升学习成绩。

## ✨ 功能特性

- **📄 试卷智能解析**：上传试卷图片，自动识别题目、选项及解析，并结构化存储。
- **❌ 错题本管理**：自动记录错题，支持按知识点、题型查看和复习。
- **🎯 个性化练习**：基于错题和薄弱知识点，利用 AI 生成针对性的强化练习题。
- **📊 学习数据分析**：可视化展示学习进度和知识点掌握情况。

## 🛠 技术栈

- **框架**: .NET 9.0 (Blazor Server)
- **数据库**: PostgreSQL (使用 Entity Framework Core)
- **AI 服务**: [Coze (扣子)](https://www.coze.cn/) API
- **UI 组件**: Bootstrap / Blazor Native

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
   项目启动后，访问 `https://localhost:7196` (或控制台显示的端口) 即可使用。

## 📖 使用说明

1. **上传试卷**：在首页或上传页面选择试卷图片，选择年级和科目，系统将自动解析题目。
2. **查看错题**：解析完成后，题目会自动加入错题库，可在错题本中查看。
3. **生成练习**：点击“生成练习”按钮，AI 将根据你的错题生成相似题目的练习卷。

## 🤝 贡献

欢迎提交 Issue 或 Pull Request 来改进这个项目！

## 📄 许可证

[MIT License](LICENSE)
