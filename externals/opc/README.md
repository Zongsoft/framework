# Zongsoft.Externals.Opc Extension Plugin Library

![License](https://img.shields.io/github/license/Zongsoft/framework)
![NuGet Version](https://img.shields.io/nuget/v/Zongsoft.Externals.Opc)
![NuGet Downloads](https://img.shields.io/nuget/dt/Zongsoft.Externals.Opc)
![GitHub Stars](https://img.shields.io/github/stars/Zongsoft/framework?style=social)

[English](README.md) |
[简体中文](README.zh-Hans.md)

-----

## Certificate Generation

If X509 certificates are used for authentication, use [OpenSSL](https://docs.openssl.org) to generate the X509 certificate files.

> 💡 **Note:** adjust the command parameter values as needed.

### Generate a Private Key

- Use the [`openssl genpkey`](https://docs.openssl.org/master/man1/openssl-genpkey) command to generate an unencrypted private key file.

```bash
openssl genpkey -algorithm RSA -pkeyopt rsa_keygen_bits:2048 -out certificate.private.pem -outform PEM
```

- Use the [`openssl genpkey`](https://docs.openssl.org/master/man1/openssl-genpkey) command to generate an encrypted private key file. Specify the password with the `-pass` parameter.

```bash
openssl genpkey -algorithm RSA -aes256 -pkeyopt rsa_keygen_bits:2048 -out certificate.private.pem -outform PEM -pass pass:"password"
```

### Create a Self-Signed Certificate

The `-subj` parameter contains these fields:
> - CN = Common Name
> - C  = Country or region
> - ST = State or province
> - L  = Locality or city
> - O  = Organization
> - OU = Organizational unit or department

- Use the [`openssl req`](https://docs.openssl.org/master/man1/openssl-req) command to create a self-signed certificate with an unencrypted private key.

```bash
openssl req -new -x509 -key certificate.private.pem -days 3650 -out certificate.der -outform DER -subj "/C=CN/ST=Province|State/L=City/O=Organization/OU=Branch|Department/CN=Common Name/emailAddress=certificate@zongsoft.com"
```

- Use the [`openssl req`](https://docs.openssl.org/master/man1/openssl-req) command to create a self-signed certificate with an encrypted private key. Specify the private-key password with the `-passin` parameter.

```bash
openssl req -new -x509 -key certificate.private.pem -passin pass:"password" -days 3650 -out certificate.der -outform DER -subj "/C=CN/ST=Province|State/L=City/O=Organization/OU=Branch|Department/CN=Common Name/emailAddress=certificate@zongsoft.com"
```

### Merge into a PKCS#12 File

- Use the [`openssl pkcs12`](https://docs.openssl.org/master/man1/openssl-pkcs12) command to merge a certificate file without a password.
	> Note: if the private key is encrypted, specify its password with the `-passin` parameter.

```bash
openssl pkcs12 -inkey certificate.private.pem -in certificate.der -export -out certificate.pfx -passout pass:"" -name "FriendlyName"
```

```bash
openssl pkcs12 -inkey certificate.private.pem -passin pass:"password" -in certificate.der -export -out certificate.pfx -passout pass:"" -name "FriendlyName"
```

- Use the [`openssl pkcs12`](https://docs.openssl.org/master/man1/openssl-pkcs12) command to merge a certificate file with a password. Specify the certificate-file password with the `-passout` parameter.
	> Note: if the private key is encrypted, specify its password with the `-passin` parameter.

```bash
openssl pkcs12 -inkey certificate.private.pem -in certificate.der -export -out certificate.pfx -passout pass:"password" -name "FriendlyName"
```

```bash
openssl pkcs12 -inkey certificate.private.pem -passin pass:"password" -in certificate.der -export -out certificate.pfx -passout pass:"password" -name "FriendlyName"
```
