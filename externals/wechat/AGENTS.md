## 概述

本目录遵循 [../AGENTS.md](../AGENTS.md)。本项目提供微信身份、公众平台、支付等 REST 能力，并包含 Web API 与外部回调网关。

修改账户、支付、平台接口、回调或插件配置前，阅读 [SKILL.md](SKILL.md) 中的代码入口、协议边界和下游适配流程。

## 开发与设计约束

- 始终遵循本机 `D:\Zongsoft\guidelines` 的[开发协作规范](../../../guidelines/AGENTS.md)、[C# 开发规范](../../../guidelines/zongsoft.csharp.guidelines.md)和[规则列表](../../../guidelines/RULES.zh-Hans.md)；Web API 同时遵循 [REST API 规范](../../../guidelines/zongsoft.rest-api.guidelines.md)。规范与分析器由 guidelines 维护，不在本项目复制维护。
- 保持 Zongsoft 插件化架构和实现模式：通过已有服务注册、模块容器、提供者和插件扩展节点装配能力，插件、配置与部署清单共同构成运行契约；宿主只负责承载，不承接支付业务逻辑。
- 遵守 SOLID，保持高内聚、低耦合；公共流程复用已有基类，变化通过职责明确的窄扩展点表达，保持派生实现可替换，不为单一实现或测试增加无必要公共抽象。
- 紧密关联的子服务与辅助类型优先采用嵌套类，复杂实现按稳定职责拆成 `partial` 文件；仅在当前方法内使用的辅助逻辑优先采用局部函数，无捕获时使用 `static`。确有声明与实现分离需求时采用 `partial` 方法，不为语法形式拆散职责。

## 接口范围

- 以本项目已实现的功能为基线，逐项核对同一产品、账号/商户角色与业务场景的官方接口；有官方更新版本时升级到最新可用版本，不保留被替代版本的兼容入口。
- 官方没有更新版本的功能继续采用现有版本，不因版本号较低或使用 XML 等协议而删除；保留必要的签名、加解密及配置依赖，并完善实现缺陷。
- 不以统一迁移 APIv3 为由缩减现有能力，也不将尚未实现的产品自动纳入升级范围。版本选择应有官方依据，并追踪 Automao 的直接调用。
- SaaS 扩展明确包含商户公众号/小程序的第三方授权管理和微信支付服务商的特约商户接入，按[实施计划](.plan.md#refactoring-plan)分阶段进行；二者不混用身份、令牌、权限或回调。个人测试资格和真实支付准入见[SaaS 开发与测试账号](docs/implementation.zh-Hans.md#saas-development-and-test-accounts)。

## 下游范围

- 本项目仅关注 Automao 调用方，按 [Automao 下游适配](SKILL.md#automao-下游适配)核查代码、配置、插件及部署影响。
- 仓库根文档中的 Yuanshan 适配要求不适用于本项目，不扫描 Yuanshan 调用方或维护其适配清单。
- Automao HTTP 请求位于独立仓库 [documents/http](../../../../Automao/documents/http)，使用前阅读[站点请求说明](../../../../Automao/documents/http/sites/README.zh-Hans.md)，不从 `server/documents/http` 推断请求文件缺失。

## 工作边界

- 本项目及 Automao 尚未正式商用，直接维护当前代码、配置与完整建表脚本，不保留历史版本兼容入口或独立升迁脚本。文档只描述当前契约，不记录重构或升迁过程；修改 SQL 时同步对应表结构设计文档。

- `src` 承载账户、选项、安全、平台和支付适配，`api/src` 提供人工 API 接入，`gateway/src` 处理微信回调。
- 每个生产项目的单元测试放在与其 `src` 同级的 `test` 目录，分别为 `test`、`api/test`、`gateway/test`，均加入 `Zongsoft.Externals.Wechat.slnx`；测试项目仅通过 `ProjectReference` 引用各自的被测项目。
- 保持签名、消息加解密、时间戳/随机数校验、支付金额和幂等边界；回调必须先验证再分派。
- 不输出 AppSecret、API Key、证书、OpenId、支付签名或真实交易数据。
- 修改平台或支付能力时同步主插件、Web/gateway 插件、选项、部署文件和项目引用。

## 账户查询

- 账户提供者通过 IAccountProvider.GetAccountAsync 查询；AppletManager、ChannelManager 的 Get…Async 为必需获取，Find…Async 为可空查找。向提供者传递末尾取消令牌，不缓存未完成或失败的任务。
- 从配置初始化的账户与动态查询的自有账户共用实例缓存；委托账户不缓存实例，并在每次调用时验证当前授权；认证、Web API 和消息发送均等待账户准备完成。
- 本地账户解析、签名、加解密与已有对象转换保持同步；不能在这些入口中引入数据库或网络阻塞等待。

## 验证

- 构建 `Zongsoft.Externals.Wechat.slnx` 并运行脱敏、网络无关测试。
- 使用固定测试向量验证签名和加解密；真实微信请求、支付或回调只在明确授权下执行。
- Automao 支付业务集成验证通过其实际插件宿主与业务入口进行，覆盖服务解析、支付/退款、回调及持久化状态；与框架单元测试分离，使用隔离的测试环境和业务数据，执行范围按用户授权及[实施计划](.plan.md#refactoring-plan)落实。
