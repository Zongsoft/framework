## 概述

本目录遵循 [../AGENTS.md](../AGENTS.md)。本项目提供 Hangfire 后台作业、daemon 插件、Web Dashboard 和可选 Redis Storage。

## 工作边界

- 保持 Zongsoft 调度/命令契约与 Hangfire Job、Server、Storage 的映射稳定。
- 作业参数必须可序列化；重试、并发和重复执行下的业务副作用应由处理器保证幂等。
- daemon 负责后台 Server 生命周期，web 仅承载 Dashboard，storage 子项目只提供存储适配。
- 修改服务注册时同步主插件、daemon 插件、选项、Web/Storage 插件和部署产物。

## 验证

- 构建主解决方案及受影响的 web、storages 或 samples 解决方案。
- 优先用临时存储验证入队、调度、取消、重试和关闭；不默认连接共享 Hangfire 数据库。
