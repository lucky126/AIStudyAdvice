# AI 学习助手 (AI Study Advice)

AI 学习助手是一个基于 .NET 9 和 Blazor Server 构建的智能学习辅助平台。利用 Coze AI 工作流强大的分析能力，帮助学生通过试卷图像识别、错题管理和个性化练习生成，高效提升学习成绩。

## ✨ 功能特性

- **📄 试卷智能解析**：上传试卷图片，自动识别题目、选项及解析，并结构化存储。
- **❌ 错题本管理**：自动记录错题，支持按知识点、题型查看和复习。
- **🎯 个性化练习**：基于错题和薄弱知识点，利用 AI 生成针对性的强化练习题。
- **📊 学习数据分析**：可视化展示学习进度和知识点掌握情况。
- **💡 智能学习顾问**：根据学习数据提供个性化建议，内置智能缓存机制，秒级响应，支持随时查看。

### 🚀 性能与稳定性优化 (Latest)

- **智能缓存策略**：学习建议采用稳定的哈希算法进行缓存，避免重复消耗 AI Token，且自动过滤 API 错误响应，确保内容准确。
- **长连接支持**：针对 AI 生成长文本场景优化了 HTTP 超时设置，确保生成复杂内容时不中断。

### 🎨 界面与体验优化 (New)

- **沉浸式布局**：移除传统侧边栏，采用顶部导航栏，支持快速切换年级/学科，界面更加清爽聚焦。
- **用户交互升级**：顶部导航栏新增用户个人中心下拉菜单，采用现代化的胶囊式设计，提供更便捷的密码修改与注销入口。
- **可视化反馈**：知识点详情页采用红绿配色直观区分正误，题目解析布局优化，阅读体验更佳。
- **快捷操作**：学习中心首页集成上传、练习、顾问快捷入口，并支持自动平滑滚动定位，操作更流畅。
- **LaTeX 支持**：全站支持复杂的数学公式渲染，针对中文环境优化了公式识别算法，完美显示 $m^2$ 等特殊符号。

### 🔐 安全与管理 (New)

- **独立会话管理**：实现了前端用户与后端管理员的会话隔离，支持在同一浏览器中同时登录两种角色，互不干扰。
- **密码安全策略**：注册与修改密码时强制执行强密码规则（8位以上，需包含大小写字母及数字），并提供自助修改密码功能，修改后自动注销以确保安全。
- **持久化登录**：支持 24 小时登录状态保持，避免频繁登录困扰。
- **用户体系**：支持用户注册、登录，数据（上传记录、练习历史、学情分析）按用户隔离。
- **注册机制**：采用邀请码注册机制，有效控制用户准入。
- **管理后台**：
  - **用户管理**：查看用户列表，支持停用/启用用户账号。
  - **邀请码管理**：批量生成邀请码，查看使用状态。

## 🛠 技术栈

- **框架**: .NET 9.0 (Blazor Server)
- **数据库**: PostgreSQL (使用 Entity Framework Core)
- **身份认证**: Blazor Custom Authentication State Provider + Cookie Session
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
   
   编辑 `appsettings.json`，填入你的 PostgreSQL 连接字符串、Coze API 密钥以及管理员账号配置：
   ```json
   {
     "ConnectionStrings": {
       "Postgres": "Host=localhost;Port=5432;Database=studydb;Username=postgres;Password=your_password"
     },
     "Admin": {
       "Username": "admin",
       "Password": "（SHA256哈希后的密码）"
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
   *注意：默认管理员密码 `admin_password` 的哈希值为 `bUUlwqIfm+HMqeQfOqQC4HZe5fzD5/6jShabFzCuOG4=`*

3. **运行项目**
   ```bash
   dotnet run
   ```
   项目启动后，支持内网访问：
   - 本机访问：`http://localhost:8099`
   - 内网访问：`http://<本机IP>:8099` (例如 `http://192.168.1.100:8099`)

   *注意：如果需要内网访问，请确保防火墙允许 8099 端口通信。*

## 📖 使用说明

### 用户端
1. **注册登录**：使用管理员分发的邀请码注册账号。
2. **首页配置**：首次进入选择年级和学科，系统将记住你的选择。
3. **上传试卷**：在学习中心点击“上传作业”，系统将自动解析题目。
4. **学情分析**：查看知识点掌握情况，点击知识点可进入题目详情页查看错题解析。
5. **专项练习**：点击“专项练习”基于薄弱点生成强化训练。

### 管理端
1. 访问 `/admin/login` 进入管理后台登录页。
2. 默认账号：`admin` / `admin_password`（请及时修改配置文件中的密码哈希）。
3. **邀请码管理**：生成邀请码分发给用户。
4. **用户管理**：监控用户状态，必要时停用账号。

## 🤝 贡献

欢迎提交 Issue 或 Pull Request 来改进这个项目！

## 📄 许可证

[MIT License](LICENSE)
