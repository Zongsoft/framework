## 硬性要求

- 保持已有文件的换行符；新建文本文件使用 CRLF，代码文件使用 Tab 缩进。
- `.cmd` 文件必须使用 CRLF；`.sh` 文件遵循 `.gitattributes` 使用 LF。
- `.cs` 文件的 `#endregion` 前不留空行。
- README 面向使用者，只描述功能、公开用法、配置、运行要求和限制；实现细节放在对应 SKILL.md 或实现文档。所有文档只记录最终契约与当前约束，不记录重构经过、前后对比、任务进度或历史验证结果。
- 任务结束时清理本任务的临时脚本、探针、备份、日志和中间生成文件；正式源码、测试、资源生成文件与交付产物不作为临时文件删除。
- 如果 `AGENTS.md`、`SKILL.md` 或 `README*.md` 与实际代码不符，在任务范围允许时同步文档；否则在结果中指出差异。
- 不要为了方便单元测试就把不需要的类、结构、方法、属性、字段、事件等作用域扩大化（即将 `private` 改成 `internal` 或 `public`）。

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
- `*.deploy` 不引用 `Zongsoft.Core`、`Zongsoft.Web`、`Zongsoft.Plugins`、`Zongsoft.Plugins.Web`；这四个基础库由宿主统一提供，编译依赖在项目文件中声明。
- 修改公开类型、插件入口或配置模型时，检查项目文件、插件/部署产物、双语 README 和资源文件是否需要同步。
- 保持 XML 节点顺序、命名、大小写和局部格式；不要无关重排。

## Yuanshan 适配

Zongsoft 重构涉及 Yuanshan 调用方适配时，在同一任务中维护 [Yuanshan 适配清单](../../Yuanshan/server/docs/zongsoft-adaptation.md)，按清单中的约定写明受影响文件、目标行为和验收条件。

## 操作边界

- 未经明确要求，不运行发布、推包、部署、升级、安装、容器启停或会接触真实外部服务的脚本。
- 不提交或输出真实密钥、令牌、证书私钥、连接字符串和云平台凭据。
- 分析器、构建规则和 C# 诊断配置由 guidelines 维护并通过 `Zongsoft.CodeAnalysis` NuGet 包分发；升级中央包版本即可更新，见 [代码规范检查](README.zh-Hans.md#code-analysis)。
- `Zongsoft.Diagnostics/proto` 是 Git 子模块；不要把生成物或无关上游变更写入其中。
- 保留工作区中的用户修改，不重置、不覆盖与当前任务无关的差异。

## 验证

- 编码风格与构建诊断按 [代码规范检查](README.zh-Hans.md#code-analysis) 执行；保留只读检查选项，自动修正仅限本次修改文件。

- 单元测试不验证本地化资源的文案、翻译、标点或语言切换结果；异常检查使用类型、参数名、错误码和业务状态，避免依赖 `Exception.Message`。测试自有数据的传递、编解码和格式化行为不受此限制。

- 文档改动检查 `git diff --check`、链接、CRLF 和内容差异。
- 代码改动先构建受影响的具体 `.csproj` 或 `.slnx`；公共契约变化再验证直接下游。
- 测试项目只通过 `ProjectReference` 引用本项目的被测项目；其它 Zongsoft 项目使用 `Reference` 与 `HintPath` 引用已构建的程序集，所需第三方依赖显式声明，并在构建流程中先生成依赖程序集。
- 除 Zongsoft.Plugins 和 Zongsoft.Plugins.Web 的单元测试与集成测试项目外，测试项目不引用 Zongsoft.Plugins，也不通过插件宿主装配被测对象。Diagnostics 协议服务端集成测试直接装配本地 WebApplication、gRPC 服务及处理器，独立于单元测试项目。
- 多目标框架问题分别验证 `net8.0`、`net9.0`、`net10.0`。
- 集成测试先确认环境变量、端口、容器和外部服务就绪；不能满足时明确说明，不以临时代码绕过。
- 仓库完整 CI 通过 `cake.ps1`/`cake.sh` 串联各项目；普通定向修改无需默认运行全仓构建。
