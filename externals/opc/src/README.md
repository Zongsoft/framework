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
