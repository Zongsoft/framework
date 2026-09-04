## 概述

本目录遵循 [../AGENTS.md](../AGENTS.md)。`upgrading` 由共享协议、打包工具、Web 包管理器、应用内升级器和进程外部署器组成一条完整升级链路。

## 跨项目契约

- `.shared` 中的 Manifest、Release、Runtime、Executor 和 Deployment 类型是四个项目共同协议，修改时必须检查全部消费者。
- 标准数据流为：tool 生成包和 manifest，web 导入并发布，upgrader 发现/下载/校验/解压并生成 `.deployment`，deployer 独占读取后部署和重启。
- 保持包名、checksum、runtime、完整/增量发布、执行器事件和 `.deployment` 生命周期兼容。
- 发布、上传、文件部署、服务重启和容器脚本具有外部副作用，未经明确要求不得执行。
- `.ai` 和 README 是流程说明，不是运行时状态；实际行为以代码和共享协议为准。

## 验证

- 普通改动构建受影响解决方案；共享协议变化依次构建 tool、web、upgrader、deployer。
- 端到端测试使用临时仓库、临时应用目录和本地服务，不覆盖真实应用或发布源。
- Native AOT 和 Linux 容器发布仅在相关代码/配置变化且环境可用时执行。
