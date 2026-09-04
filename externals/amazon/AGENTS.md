## 概述

本目录遵循 [../AGENTS.md](../AGENTS.md)。本项目基于 AWS SDK 提供 Zongsoft 文件系统等 Amazon 服务适配。

## 工作边界

- 保持 Bucket、Key、Region、Endpoint 和凭据解析与 Zongsoft IO 抽象的语义一致。
- 流式上传下载应保留取消、长度、定位和释放所有权，不无界缓冲大对象。
- 兼容 S3 及文档声明的 S3-compatible 端点，不将供应商客户端暴露为公共契约。
- 不输出 AccessKey、SecretKey、SessionToken 或预签名 URL 中的敏感查询参数。

## 验证

- 构建 `Zongsoft.Externals.Amazon.slnx`，优先运行网络无关测试。
- 对象存储集成验证优先使用本地兼容端点；真实 AWS 操作需明确授权。
