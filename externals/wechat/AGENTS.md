## 概述

本目录遵循 [../AGENTS.md](../AGENTS.md)。本项目提供微信身份、公众平台、支付等 REST 能力，并包含 Web API 与外部回调网关。

## 工作边界

- `src` 承载账户、选项、安全、平台和支付适配，`api` 提供人工 API 接入，`gateway` 处理微信回调。
- 保持签名、消息加解密、时间戳/随机数校验、支付金额和幂等边界；回调必须先验证再分派。
- 不输出 AppSecret、API Key、证书、OpenId、支付签名或真实交易数据。
- 修改平台或支付能力时同步主插件、Web/gateway 插件、选项、部署文件和项目引用。

## 账户查询

- 账户提供者通过 IAccountProvider.GetAccountAsync 查询；AppletManager、ChannelManager 的 Get…Async 为必需获取，Find…Async 为可空查找。向提供者传递末尾取消令牌，不缓存未完成或失败的任务。
- 从配置初始化的账户与动态查询账户共用实例缓存；认证、Web API 和消息发送均等待账户准备完成。
- 本地账户解析、签名、加解密与已有对象转换保持同步；不能在这些入口中引入数据库或网络阻塞等待。

## 验证

- 构建 `Zongsoft.Externals.Wechat.slnx` 并运行脱敏、网络无关测试。
- 使用固定测试向量验证签名和加解密；真实微信请求、支付或回调只在明确授权下执行。
