# Zongsoft.Externals.Opc 扩展插件库

![License](https://img.shields.io/github/license/Zongsoft/framework)
![NuGet Version](https://img.shields.io/nuget/v/Zongsoft.Externals.Opc)
![NuGet Downloads](https://img.shields.io/nuget/dt/Zongsoft.Externals.Opc)
![GitHub Stars](https://img.shields.io/github/stars/Zongsoft/framework?style=social)

[English](README.md) |
[简体中文](README.zh-Hans.md)

-----

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
