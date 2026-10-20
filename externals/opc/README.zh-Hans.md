# Zongsoft.Externals.Opc 扩展插件库

![License](https://img.shields.io/github/license/Zongsoft/framework)
![NuGet Version](https://img.shields.io/nuget/v/Zongsoft.Externals.Opc)
![NuGet Downloads](https://img.shields.io/nuget/dt/Zongsoft.Externals.Opc)
![GitHub Stars](https://img.shields.io/github/stars/Zongsoft/framework?style=social)

[English](README.md) |
[简体中文](README.zh-Hans.md)

-----

## 连接设置（OpcConnectionSettings）

连接设置（[`OpcConnectionSettings`](src/Configuration/OpcConnectionSettings.cs)）不带 `TOptions` 约束，允许任意主参数键；`Server` 与别名 `Url` 访问同一参数。`OpcClient` 读取服务器、超时、身份和安全相关的类型化属性建立会话，不通过通用 `GetOptions()` 生成 SDK 选项。

原始字典只返回显式参数，默认值由类型化读取提供。`Value` 保留全部显式主参数与扩展属性；保留未知项不意味着 OPC 客户端会使用它们。完整规则见 [Core 设置契约](../../Zongsoft.Core/docs/settings.zh-Hans.md)。

## 通知缓冲验证

[SubscriberSpoolingTest](test/SubscriberSpoolingTest.cs) 使用未连接的实际 `Subscriber` 和 OPC UA SDK `MonitoredItem.SaveValueInCache` 产生通知，经过条目查找、值解包、消费者回调和 `Spooler` 批处理。测试不创建会话、不打开端口，也不生成证书。

测试由单个可等待的生产者控制通知源：同步消费者仅捕获本次 `PutAsync`，生产者在通知回调返回后等待投递完成，再发送下一条，最多保留一项未完成投递。覆盖同节点连续值和重复值、多节点标识与值的配对、满载慢消费、刷新等待、取消和异常传播、解绑后的显式最终刷新、释放不排空，以及周期回调的取消、清理等待和故障通知。

在仓库根目录先构建当前 Core，再运行 OPC 测试。使用 Debug 使适配器引用工作区 Core 程序集；Release 配置引用发布包。

```shell
dotnet build Zongsoft.Core/src/Zongsoft.Core.csproj -c Debug -p:GeneratePackageOnBuild=false
dotnet build externals/opc/Zongsoft.Externals.Opc.slnx -c Debug -p:GeneratePackageOnBuild=false
dotnet test externals/opc/test/Zongsoft.Externals.Opc.Tests.csproj -c Debug -f net8.0 --no-build --filter FullyQualifiedName~SubscriberSpoolingTest
```

分别将 `net8.0` 替换为 `net9.0`、`net10.0` 验证其余目标。关闭时是否显式刷新剩余数据由所有者决定，`DisposeAsync` 本身不排空缓冲区。

测试中的可等待生产者不是生产环境同步订阅回调的异步背压方案；实际 `Consume(Action)` 不会等待异步投递。测试不覆盖服务器采样、网络 Publish 分派、重连和 SDK 内部队列溢出。生产环境必须由应用明确满载策略，不能丢弃 `PutAsync` 返回值或使用 `async void` 回调；协议端到端验证使用 [samples 的本地 client/server](samples/README.zh-Hans.md)。

## 证书生成

如果采用 X509 证书进行身份验证，需要使用 [OpenSSL 工具](https://docs.openssl.org) 生成 X509 证书文件。

> 💡 **注意：** 请根据需要调整命令中的相应参数值。

### 生成私钥

- 使用 [`openssl genpkey`](https://docs.openssl.org/master/man1/openssl-genpkey) 命令生成未加密的私钥文件。

```bash
openssl genpkey -algorithm RSA -pkeyopt rsa_keygen_bits:2048 -out certificate.private.pem -outform PEM
```

- 使用 [`openssl genpkey`](https://docs.openssl.org/master/man1/openssl-genpkey) 命令生成加密的私钥文件，密码由 `-pass` 参数指定。

```bash
openssl genpkey -algorithm RSA -aes256 -pkeyopt rsa_keygen_bits:2048 -out certificate.private.pem -outform PEM -pass pass:"password"
```

### 创建自签名证书

命令的 `-subj` 参数包含的字段：
> - CN = 公共名称
> - C  = 国家地区
> - ST = 省/州/邦
> - L  = 城市 _(**L**ocality)_
> - O  = 组织机构
> - OU = 分支机构 _(部门)_

- 使用 [`openssl req`](https://docs.openssl.org/master/man1/openssl-req) 命令创建自签名证书，私钥为未加密。

```bash
openssl req -new -x509 -key certificate.private.pem -days 3650 -out certificate.der -outform DER -subj "/C=CN/ST=Province|State/L=City/O=Organization/OU=Branch|Department/CN=Common Name/emailAddress=certificate@zongsoft.com"
```

- 使用 [`openssl req`](https://docs.openssl.org/master/man1/openssl-req) 命令创建自签名证书，指定的私钥为已加密，私钥密码由 `-passin` 参数指定。

```bash
openssl req -new -x509 -key certificate.private.pem -passin pass:"password" -days 3650 -out certificate.der -outform DER -subj "/C=CN/ST=Province|State/L=City/O=Organization/OU=Branch|Department/CN=Common Name/emailAddress=certificate@zongsoft.com"
```

### 合并为 PKCS#12 文件

- 使用 [`openssl pkcs12`](https://docs.openssl.org/master/man1/openssl-pkcs12) 命令合并无密码的证书文件。
	> 注：如果私钥为加密格式，则使用 `-passin` 参数指定该私钥密码。

```bash
openssl pkcs12 -inkey certificate.private.pem -in certificate.der -export -out certificate.pfx -passout pass:"" -name "FriendlyName"
```

```bash
openssl pkcs12 -inkey certificate.private.pem -passin pass:"password" -in certificate.der -export -out certificate.pfx -passout pass:"" -name "FriendlyName"
```

- 使用 [`openssl pkcs12`](https://docs.openssl.org/master/man1/openssl-pkcs12) 命令合并含密码的证书文件，证书文件密码由 `-passout` 参数指定。
	> 注：如果私钥为加密格式，则使用 `-passin` 参数指定该私钥密码。

```bash
openssl pkcs12 -inkey certificate.private.pem -in certificate.der -export -out certificate.pfx -passout pass:"password" -name "FriendlyName"
```

```bash
openssl pkcs12 -inkey certificate.private.pem -passin pass:"password" -in certificate.der -export -out certificate.pfx -passout pass:"password" -name "FriendlyName"
```

## OPC UA 服务端

需要以插件方式启动 OPC UA 服务时，部署 `site=daemon` 对应的可选 daemon 插件和 option。主插件只提供 OPC 客户端与服务器类型，不会自行启动监听。

daemon 配置中的 `Server.Urls` 决定实际服务监听地址，支持以逗号分隔多个显式 `opc.tcp` URL；`Server.Discovery` 是注册目标，单独控制服务器公告，不会作为监听地址。binding 从相同 `Urls` 生成，保留 URL 路径。启用条件由 `Server.Enabled` 同时控制启动与声明。

绑定只声明 OPC UA TCP 服务。证书、信任目录、安全策略及对外 Endpoint 公告仍由 OPC UA 运行配置管理；此声明不代表 HTTP 健康检查、代理或 NAT 映射。

## 插件化接入

优先通过宿主组合本能力；包引用用于编译，而插件加载还需要部署清单和运行产物。完整流程见[插件化入门](../../Zongsoft.Plugins/README.zh-Hans.md)。

插件使 OPC 适配器及 SDK 可用，应用仍需配置端点、证书信任及会话/订阅生命周期；复制清单不代表已经建立 OPC 连接。

| 运行产物 | 源码依据 |
| --- | --- |
| `Zongsoft.Externals.Opc` | [Zongsoft.Externals.Opc.plugin](src/Zongsoft.Externals.Opc.plugin) |
| 文件复制及依赖 | [Zongsoft.Externals.Opc.deploy](src/Zongsoft.Externals.Opc.deploy) |

在已有宿主的 `.deploy` 中加入以下片段（保留宿主原有 Main 等基础清单，不要用片段覆盖整份文件）：

```ini
[plugins zongsoft externals opc]
nuget:Zongsoft.Externals.Opc
```

按入门指南在测试部署目录执行 `dotnet deploy`，指定匹配宿主的 `framework`、`platform`、`architecture`，并按需指定 `site`。实际部署应固定兼容版本；片段没有列出的数据库、缓存、商业运行时等应用依赖仍需另外准备。

清单列出的附属产物包括：`Zongsoft.Externals.Opc.plugin`。同时保留程序集、依赖与附属资源目录。部署后重启宿主，先检查插件加载与服务/驱动注册，再验证前文的使用流程；不要把“文件已复制”当作“功能已启用”。
