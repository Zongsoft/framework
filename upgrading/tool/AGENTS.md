## 概述

本目录遵循 [../AGENTS.md](../AGENTS.md)。`dotnet-upgrade` 提供 pack、checksum、publish 命令；详细规则见 [SKILL.md](SKILL.md)。

## 工作边界

- 保持命令语法、变量替换、文件选择/排除、ZIP 条目、runtime 命名和 manifest 生成兼容。
- checksum 必须与 web/upgrader/deployer 使用的算法及 manifest 字段一致。
- publish 支持的通道别名、URL 标准化和上传顺序不得无意改变。
- 不记录 secret、access、authorization、credential；不默认向真实 Web 或 S3 发布。

## 验证

- 构建 `Zongsoft.Tools.Upgrader.slnx`。
- 在临时目录验证代表性 pack/checksum；publish 使用本地测试端点，并同时检查退出码、日志、包和 manifest。
