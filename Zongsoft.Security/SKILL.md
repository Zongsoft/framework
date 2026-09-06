---
name: zongsoft-security
description: 修改或审查 Zongsoft.Security 的身份、凭据、认证器、权限持久化、Web 安全端点或 Authencode 验证码实现时使用；不用于一般 ASP.NET 管线配置。
---

# Zongsoft.Security 实现协作

先阅读 [AGENTS.md](AGENTS.md)；使用者场景、配置与示例见 [README](README.zh-Hans.md)。本技能聚焦实现边界，不替代公开 API 文档。

## 分层入口

- [Core Security](../Zongsoft.Core/src/Security) 拥有公共模型和认证授权契约；[src](src) 提供持久化服务、`CredentialProvider`、身份和权限实现。
- [Privileges](src/Privileges) 中 `Authenticators.Identity` 与 `Authenticators.Secretor` 分别接入身份和秘密验证；不能将身份验证成功等同于所有资源授权。
- [api](api) 负责 HTTP 参数、结果和路由；[captcha](captcha) 独立实现人机挑战；[database](database) 与映射决定持久化兼容。
- 修改用户/角色/成员/权限字段时同步检查 `Zongsoft.Security.mapping`、各数据库脚本、服务筛选及 Web 暴露字段，不只改模型。

## 认证配置与高风险不变量

- `src/Configuration` 绑定 identity/authentication/authorization；选项默认值以 `src/Zongsoft.Security.option` 和类型共同确认。场景过期、失败次数与锁定窗口是外部可感知语义。
- 凭据签发、续期、撤销和缓存读取应作为完整链路验证；不要仅测试解析成功。
- 密码/Secret/凭据比较及异常不得暴露原值；对不存在用户与失败凭据的结果需评估枚举风险。
- 权限继承、成员类型、租户/命名空间筛选和特殊安全角色必须保持一致；Data 查询变化可能扩大授权范围。
- Web 层复用服务，不复制密码校验或权限计算。变更响应负载需检查信息泄漏和旧客户端兼容。

## Authencode 的两阶段状态

[AuthencodeCaptcha](captcha/AuthencodeCaptcha.cs) 的挑战图像与最终确认不是同一个令牌：

1. 签发随机挑战 Token 和答案，缓存有效期 10 分钟；格式化器返回 PNG，响应头为 `X-Captcha`。
2. 答案验证接受 `token:code` 或 `token=code`，成功产生有效期 5 分钟的确认令牌。
3. 最终字符串确认先原子删除确认缓存项，再删除挑战；确认令牌只能消费一次。

当前挑战答案验证本身不立即消费挑战，可在最终消费前产生多个确认令牌。不要在文档或测试中将它描述为全链路单次挑战。并发、缓存过期和删除失败应分开测试；不得用进程内字典替代分布式缓存的原子移除语义。

## 最小验证

- 本仓没有独立 Security 测试项目；先选 `src/Zongsoft.Security.csproj`、`api/Zongsoft.Security.Web.csproj` 或 `captcha/Zongsoft.Security.Captcha.csproj`。
- 使用固定身份、内存/替身缓存与脱敏签名向量验证拒绝、撤销、过期、重复确认和并发。
- 数据库验证明确选择脚本与隔离库，不能默认触发真实短信、邮件或支付验证。
- 不恢复已删除的 Postman 文件；现有 HTTP 文档按实际端点检查。
