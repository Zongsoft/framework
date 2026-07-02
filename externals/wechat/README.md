# Zongsoft.Externals.Wechat Extension Plugin Library

![License](https://img.shields.io/github/license/Zongsoft/framework)
![NuGet Version](https://img.shields.io/nuget/v/Zongsoft.Externals.Wechat)
![NuGet Downloads](https://img.shields.io/nuget/dt/Zongsoft.Externals.Wechat)
![GitHub Stars](https://img.shields.io/github/stars/Zongsoft/framework?style=social)

[English](README.md) |
[简体中文](README.zh-Hans.md)

-----

## WeChat Third-Party Platform

### Concepts

- Third-party platform application `component_appid`
- Third-party platform secret `component_appsecret`
- Encryption and decryption keys `symmetric_key`, `AESEncodingKey`

- Platform access token `component_access_token`
	> - The credential used to call third-party platform APIs. The token is valid for `2` hours. Refresh it before it expires, for example at about `1` hour and `50` minutes.
	> - Reference: https://developers.weixin.qq.com/doc/oplatform/Third-party_Platforms/api/component_access_token.html

- Platform verify ticket `component_verify_ticket`
	> - After a third-party platform is created and approved, the WeChat server sends the verify ticket to its authorization event receiving URL by `POST` every 10 minutes. The third-party platform only needs to return the string `success`.
	> - Reference: https://developers.weixin.qq.com/doc/oplatform/Third-party_Platforms/api/component_verify_ticket.html

### Authorization Flow

1. Wait for the `component_verify_ticket` callback pushed by WeChat.
2. Get or asynchronously refresh the platform access token `component_access_token`.
3. Use the platform access token to get the pre-authorization code `pre_auth_code`.
4. Generate two authorization page URLs from the pre-authorization code.
	1. Scan page: https://mp.weixin.qq.com/cgi-bin/componentloginpage?component_appid=xxxx&pre_auth_code=xxxxx&redirect_uri=xxxx&auth_type=xxx
	2. WeChat share: https://mp.weixin.qq.com/safe/bindcomponent?action=bindcomponent&no_scan=1&component_appid=xxxx&pre_auth_code=xxxxx&redirect_uri=xxxx&auth_type=xxx&biz_appid=xxxx#wechat_redirect
5. After the user authorizes, WeChat pushes a callback. Read the authorization binding information in that callback.
	> Use https://developers.weixin.qq.com/doc/oplatform/Third-party_Platforms/api/authorization_info.html to obtain `authorizer_appid`, `authorizer_access_token`, and `authorizer_refresh_token`.
6. Periodically refresh the authorizer access credential.

### References

- Regular merchant documentation center
	> https://pay.weixin.qq.com/wiki/doc/apiv3/wxpay/pages/index.shtml

- Service provider documentation center
	> https://pay.weixin.qq.com/wiki/doc/apiv3_partner/pages/index.shtml

- GitBook
	> https://wechatpay-api.gitbook.io/wechatpay-api-v3

- API certificate
	> https://kf.qq.com/faq/161222NneAJf161222U7fARv.html

### Tools

- WeChat Pay API verification script for Postman
	> https://github.com/wechatpay-apiv3/wechatpay-postman-script

- WeChat Pay signing, signature verification, encryption, and decryption tools
	> https://pay.weixin.qq.com/wiki/doc/apiv3/wechatpay/download/Product_5.zip

- WeChat Pay certificate tool
	> https://wx.gtimg.com/mch/files/WXCertUtil.exe
