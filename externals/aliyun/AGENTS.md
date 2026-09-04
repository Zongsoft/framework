## 概述

本目录遵循 [../AGENTS.md](../AGENTS.md)。本项目通过阿里云 REST API 提供存储、推送、电信和 MQTT 等能力，并有独立回调网关。

## 工作边界

- 保持服务专属签名、Endpoint、Region、错误码和选项封装在对应子域。
- `gateway` 只负责接收和分派回调；每种服务必须按官方规则验签、限制访问并防止重复处理。
- 不记录 AccessKey、Secret、短信/号码认证凭据或完整回调签名。
- 新增服务时同步插件、选项、部署文件及 gateway 项目引用。

## 验证

- 构建 `Zongsoft.Externals.Aliyun.slnx`；回调变化同时构建 gateway。
- 使用固定脱敏向量验证签名和请求形状；真实云调用只在明确授权和测试凭据可用时进行。
