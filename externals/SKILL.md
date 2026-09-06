---
name: zongsoft-externals
description: 创建、修改、审查或调试 framework/externals 下的第三方适配器。适用于 SDK/REST/协议封装、连接和客户端生命周期、配置与插件注册、凭据和签名、回调、错误转换、可选 Web 项目及外部服务集成测试；不用于供应商无关的 Core 契约设计。
---

# Zongsoft 外部适配器工作流

## 定位

1. 阅读 [AGENTS.md](AGENTS.md) 和目标适配器就近的 `AGENTS.md`、README、项目文件、插件/选项。
2. 找到其实现的 Zongsoft 公共接口，再定位第三方 SDK 或协议对象的转换边界。
3. 优先沿用同项目现有 Provider、Factory、ConnectionSettings、Client/Service 和 Web/Gateway 模式。
4. 仅当多个生产适配器确实共享语义时，才提议修改 Core。

## 运行路由与提供者选择

- 表达式：Core `IExpressionEvaluator` → 服务匹配 → [Lua](lua/src/LuaExpressionEvaluator.cs)、[Python](python/src/PythonExpressionEvaluator.cs)、[Scriban](scriban/src/ScribanExpressionEvaluator.cs)。使用消费者接口与配置选定名字，语言语法并不因公共接口而相同。
- Lua 每次 Evaluate 创建/释放 NLua 状态；Python 复用 ScriptEngine 并切换 Runtime IO；Scriban 每次创建 TemplateContext 且当前读取 variables.Count。不能把“独立字典”概括成所有运行时均线程隔离。
- 缓存/序号/锁：[RedisServiceProvider](redis/src/RedisServiceProvider.cs) 注册别名 Redis，并用静态按名缓存供应实例；[EtcdServiceProvider](etcd/src/EtcdServiceProvider.cs) 的契约和别名须独立核对，不能假定存在 `@Etcd` 注册。
- Redis 普通服务可回退默认连接；可靠消息存储工厂要求严格匹配 Broker 名。修改任何回退或缓存键都要同时覆盖对应消费者。
- 先检查 `.plugin` 的程序集及扩展节点，再检查 ServiceAttribute/IServiceRegistration；某些对象只挂载在插件树，不存在对应容器注册，禁止编造 Resolve 示例。
- ClosedXml 的四个公共归档/模板服务按 Spreadsheet 格式匹配。内部表格名由 Spreadsheet.GetTableName 生成 `__{QualifiedName}__`，不是 Model.Name；类型描述器与 service.GetDescriptor() 的映射上下文不能互换。更改命名时同步生成、提取、手工模板说明及往返测试。
- 包含 Web/Gateway 的适配器要追踪“路由 → 请求解码 → 分发器 → Handler → 回复”全链路，不能把可扩展网关描述为自动验签或自动处理全部业务回调。

## 适配规则

- 第三方类型、错误码和配置细节留在适配层；公共调用方看到稳定的 Zongsoft 契约和异常语义。
- Client、Connection、Session、Stream、Subscription 与回调注册必须明确所有权、线程安全、取消和释放。
- SDK 默认重试、超时、分页、时区和序列化行为不能未经确认成为框架契约。
- REST 签名与回调按原始字节、规范化参数和供应商要求处理；验签成功前不分派业务 Handler。
- 核对实际实现是否提供验签；缺失时在 README 明确应用处理器的职责，不把应有安全约束写成框架已具备的保证。
- 凭据使用配置或测试环境注入，不写入源码、README 示例、日志或测试快照。

## 验证

- 构建目标 `.slnx`，先运行离线的解析、转换、签名和生命周期测试。
- 集成测试使用专用本地服务、容器或沙箱账号，设置唯一命名空间/资源前缀，并限制清理范围。
- 真实云调用、支付、短信、上传、证书操作或生产回调必须得到明确授权；无法验证时列出所需依赖。
- 宿主验证至少经过“加载清单 → 读取配置 → 解析契约 → 首次实际调用”。只验证插件列表会漏掉延迟连接与依赖版本问题；Redis 已出现传递部署覆盖新版 StackExchange.Redis 后首次调用缺少方法的情况，检查最终部署文件版本。
