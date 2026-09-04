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

## 适配规则

- 第三方类型、错误码和配置细节留在适配层；公共调用方看到稳定的 Zongsoft 契约和异常语义。
- Client、Connection、Session、Stream、Subscription 与回调注册必须明确所有权、线程安全、取消和释放。
- SDK 默认重试、超时、分页、时区和序列化行为不能未经确认成为框架契约。
- REST 签名与回调按原始字节、规范化参数和供应商要求处理；验签成功前不分派业务 Handler。
- 凭据使用配置或测试环境注入，不写入源码、README 示例、日志或测试快照。

## 验证

- 构建目标 `.slnx`，先运行离线的解析、转换、签名和生命周期测试。
- 集成测试使用专用本地服务、容器或沙箱账号，设置唯一命名空间/资源前缀，并限制清理范围。
- 真实云调用、支付、短信、上传、证书操作或生产回调必须得到明确授权；无法验证时列出所需依赖。
