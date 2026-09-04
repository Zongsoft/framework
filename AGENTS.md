## 硬性要求

- 保持已有文件的换行符；新建文本文件使用 CRLF，代码文件使用 Tab 缩进。
- `.cmd` 文件必须使用 CRLF；`.sh` 文件遵循 `.gitattributes` 使用 LF。
- 如果 `AGENTS.md`、`SKILL.md` 或 `README*.md` 与实际代码不符，在任务范围允许时同步文档；否则在结果中指出差异。

## 仓库概览

本仓库是 Zongsoft 可插拔应用开发框架，公共目标框架由 `Directory.Build.props` 定义为 .NET 8、9、10，NuGet 版本由 `Directory.Packages.props` 集中管理。

- `Zongsoft.Core`：最底层公共抽象和通用实现。
- `Zongsoft.Data`：数据引擎及数据库驱动。
- `Zongsoft.Plugins`、`Zongsoft.Plugins.Web`：插件装配、宿主和 Web 集成。
- `Zongsoft.Web`、`Zongsoft.Security`：Web 与安全能力。
- `messaging`：消息队列驱动和消息存储。
- `externals`：第三方平台、协议和基础设施适配器。
- `upgrading`：打包、发现、下载、部署和发布组成的升级链路。

开始工作前先读目标目录就近的 `AGENTS.md`、`SKILL.md`、`README*.md`、解决方案和项目文件。行为应放在拥有该契约的最窄项目中；不要为单个适配器需求随意扩充 Core 公共接口。

## 插件与产物

- `*.plugin` 声明插件、依赖和组件，`*.option` 声明选项，`*.mapping` 声明数据映射，`*.deploy` 控制部署复制。
- 修改公开类型、插件入口或配置模型时，检查项目文件、插件/部署产物、双语 README 和资源文件是否需要同步。
- 保持 XML 节点顺序、命名、大小写和局部格式；不要无关重排。

## 操作边界

- 未经明确要求，不运行发布、推包、部署、升级、安装、容器启停或会接触真实外部服务的脚本。
- 不提交或输出真实密钥、令牌、证书私钥、连接字符串和云平台凭据。
- `Zongsoft.Diagnostics/proto` 是 Git 子模块；不要把生成物或无关上游变更写入其中。
- 保留工作区中的用户修改，不重置、不覆盖与当前任务无关的差异。

## 验证

- 文档改动检查 `git diff --check`、链接、CRLF 和内容差异。
- 代码改动先构建受影响的具体 `.csproj` 或 `.slnx`；公共契约变化再验证直接下游。
- 多目标框架问题分别验证 `net8.0`、`net9.0`、`net10.0`。
- 集成测试先确认环境变量、端口、容器和外部服务就绪；不能满足时明确说明，不以临时代码绕过。
- 仓库完整 CI 通过 `cake.ps1`/`cake.sh` 串联各项目；普通定向修改无需默认运行全仓构建。
