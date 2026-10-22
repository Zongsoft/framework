# Zongsoft.Externals.Wechat Extension Plugin Library

![License](https://img.shields.io/github/license/Zongsoft/framework)
![NuGet Version](https://img.shields.io/nuget/v/Zongsoft.Externals.Wechat)
![NuGet Downloads](https://img.shields.io/nuget/dt/Zongsoft.Externals.Wechat)
![GitHub Stars](https://img.shields.io/github/stars/Zongsoft/framework?style=social)

[English](README.md) |
[简体中文](README.zh-Hans.md)

-----

## Current capabilities and limitations

This library provides selected REST wrappers for mini program login and phone-number exchange, official account OAuth and template messages, payments, and refunds. Payment implementations include both APIv3 and APIv2/XML paths; this is not a complete SDK for all WeChat products.

The payment HTTP client signs outgoing requests but does not verify response signatures. The default callback gateway dispatches requests and the payment callback handler decrypts resources. This pipeline does not validate notification signatures, timestamp windows, or business idempotency. The host must verify notifications before they enter the pipeline and validate order ownership and amounts during business processing.

The Web API login and credential endpoints include WeChat session or access credentials and must not be exposed directly as login endpoints for untrusted clients. Hosts must restrict these endpoints and transform their responses. Mini program `session_key` values must remain on the server.

The third-party platform authorization flow below is a platform reference. The library does not provide complete services for receiving verification tickets or managing component and authorizer tokens.

## WeChat Third-Party Platform

### Concepts

- Third-party platform application `component_appid`
- Third-party platform secret `component_appsecret`
- Encryption and decryption keys `symmetric_key`, `AESEncodingKey`

- Platform access token `component_access_token`
	> - The credential used to call third-party platform APIs. The token is valid for `2` hours. Refresh it before it expires, for example at about `1` hour and `50` minutes.
	> - Reference: https://developers.weixin.qq.com/doc/oplatform/openApi/ticket-token/api_getcomponentaccesstoken.html

- Platform verify ticket `component_verify_ticket`
	> - After a third-party platform is created and approved, the WeChat server sends the verify ticket to its authorization event receiving URL by `POST` every 10 minutes. The third-party platform must verify the message signature, decrypt and store the ticket, then return the string `success`.
	> - Reference: https://developers.weixin.qq.com/doc/oplatform/Third-party_Platforms/2.0/Before_Develop/component_verify_ticket.html

### Authorization Flow

1. Wait for the `component_verify_ticket` callback pushed by WeChat.
2. Get or asynchronously refresh the platform access token `component_access_token`.
3. Use the platform access token to get the pre-authorization code `pre_auth_code`.
4. Generate two authorization page URLs from the pre-authorization code.
	1. Scan page: https://mp.weixin.qq.com/cgi-bin/componentloginpage?component_appid=xxxx&pre_auth_code=xxxxx&redirect_uri=xxxx&auth_type=xxx
	2. WeChat share: https://mp.weixin.qq.com/safe/bindcomponent?action=bindcomponent&no_scan=1&component_appid=xxxx&pre_auth_code=xxxxx&redirect_uri=xxxx&auth_type=xxx&biz_appid=xxxx#wechat_redirect
5. After the user authorizes, WeChat pushes a callback. Read the authorization binding information in that callback.
	> Use https://developers.weixin.qq.com/doc/oplatform/openApi/ticket-token/api_getauthorizerrefreshtoken.html to obtain `authorizer_appid`, `authorizer_access_token`, and `authorizer_refresh_token`.
6. Periodically refresh the authorizer access credential.

### References

- Regular merchant documentation center
	> https://pay.weixin.qq.com/doc/v3/merchant/4012081606

- Service provider documentation center
	> https://pay.weixin.qq.com/doc/v3/partner/4012081673

- Official WeChat Pay SDKs and tools
	> https://github.com/wechatpay-apiv3

- API certificate
	> https://kf.qq.com/faq/161222NneAJf161222U7fARv.html

### Tools

- WeChat Pay API verification script for Postman
	> https://github.com/wechatpay-apiv3/wechatpay-postman-script

- WeChat Pay signing, signature verification, encryption, and decryption tools
	> https://pay.weixin.qq.com/wiki/doc/apiv3/wechatpay/download/Product_5.zip

- WeChat Pay certificate tool
	> https://wx.gtimg.com/mch/files/WXCertUtil.exe

## Plugin-Based Integration

Compose this feature through the host; a package reference supplies compile-time APIs, while plugin loading also requires deployed manifests and runtime assets. See [the complete plugin workflow](../../Zongsoft.Plugins/README.md).

The manifest exposes the Wechat components and services. Keep account, certificate and payment settings under application-owned options. API and callback gateway plugins are optional, separate host contributions.

| Runtime artifact | Source of truth |
| --- | --- |
| `Zongsoft.Externals.Wechat` | [Zongsoft.Externals.Wechat.plugin](src/Zongsoft.Externals.Wechat.plugin) |
| File copying and dependencies | [Zongsoft.Externals.Wechat.deploy](src/Zongsoft.Externals.Wechat.deploy) |

Add this fragment to an existing host `.deploy` (retain Main and the host’s other base manifests; do not replace the whole file):

```ini
[plugins zongsoft externals wechat]
nuget:Zongsoft.Externals.Wechat
```

Run `dotnet deploy` against a test deployment as explained in the workflow, with the host's `framework`, `platform`, `architecture` and, where needed, `site`. Pin compatible versions in real deployments; application dependencies such as databases, caches or commercial runtimes are still separate prerequisites.

Additional artifacts listed by the deployment manifest include `Zongsoft.Externals.Wechat.plugin`, `Zongsoft.Externals.Wechat.option`. Retain assemblies, dependencies and satellite resource directories as well. Restart the host after deployment, check plugin loading and service/driver registration, then verify the workflow above; copied files alone do not prove that the feature is active.

## Publishing

Set `NUGET_API_KEY` and use the Release `pack` task. The `--project` option selects a project or solution. Publish the main package first, wait until it is available from NuGet, and then publish the API and gateway packages that reference it:

```powershell
dotnet cake build.cake --target=pack --edition=Release --project=src/Zongsoft.Externals.Wechat.csproj
dotnet cake build.cake --target=pack --edition=Release --project=api/Zongsoft.Externals.Wechat.Web.csproj
dotnet cake build.cake --target=pack --edition=Release --project=gateway/Zongsoft.Externals.Wechat.Gateway.csproj
```

Without `--project`, the task builds the complete solution.

## Account lookup

Implement `IAccountProvider.GetAccountAsync` to supply dynamic accounts. Use `await AppletManager.GetAppletAsync(code, cancellation)` or `await ChannelManager.GetChannelAsync(code, cancellation)` to obtain an applet or channel. Use `FindAppletAsync` or `FindChannelAsync` and check for null when absence is allowed. Lookups support cancellation and reuse existing instances.
