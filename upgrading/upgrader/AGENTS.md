## 概述

本目录遵循 [../AGENTS.md](../AGENTS.md)。本项目运行在宿主内，负责发布发现、选择、下载、校验、解压和向 deployer 交接；详细规则见 [SKILL.md](SKILL.md)。

## 工作边界

- 保持 File/Web 通道、版本分支和 Fully+Delta 选择顺序确定。
- 下载复用必须重新校验大小与 checksum，解压完成后才写入 `.deployment`。
- worker 防止并发重入；取消、下载失败和解压失败不得产生可部署的假成功状态。
- 启动 deployer 后的宿主关闭属于运行时副作用，不在普通测试中触发真实宿主。

## 验证

- 构建 `Zongsoft.Upgrading.Upgrader.slnx`。
- 使用临时目录覆盖无更新、完整/增量链、指定版本、损坏包、取消、并发 tick 和描述文件生成。
