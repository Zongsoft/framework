# Zongsoft.Externals.Wechat.Gateway

[English](README.md) | [简体中文](README.zh-Hans.md)

## Features and Configuration

This plugin receives WeChat Pay APIv3 payment, refund and offline face agreement notifications. The [main adapter](../README.md) supplies merchant configuration, signature verification and resource decryption. Application management endpoints belong to the [Web API package](../api/README.md).

| POST route | Supported events |
| --- | --- |
| `/Externals/Wechat/Fallback/payment/{merchant}` | `TRANSACTION.SUCCESS` |
| `/Externals/Wechat/Fallback/refundment/{merchant}` | `REFUND.SUCCESS`, `REFUND.ABNORMAL`, `REFUND.CLOSED` |
| `/Externals/Wechat/Fallback/offlinefaceagreement/{merchant}` | `FACEPAY.USER_STATE_CHANGE` |

`merchant` is the configured merchant or service provider number, digits only, without role suffixes. Query parameters are rejected. Requests require JSON content and are limited to 2 MiB. Public account messages, third-party authorization events and GET URL verification are outside these routes.

Configure `ApiV3Key`, an explicit verification mode and permitted applications as described in [merchant credentials](../docs/implementation.md#merchant-credentials). The adapter verifies the original bytes and a five-minute timestamp window before authenticated decryption and event/merchant checks. The application must validate business ownership, amounts, currency and durable deduplication.

## Business Handlers

Derive from `PaymentHandlerBase`, `RefundmentHandlerBase` or the relevant `FallbackHandlerBase<TRequest>`. Override `OnReceiveAsync` when using durable acceptance. Register `IFallbackHandler` objects at `/Workbench/Externals/Wechat/Fallback/Handlers` under `Payment`, `Refundment` or `OfflineFaceAgreement`, matching their `Kind`. Registration happens during startup, before serving requests.

`Completed`, `Accepted` and `Duplicate` produce an empty HTTP 204 response. `Accepted` means the application durably owns the notification and subsequent processing/recovery. Rejection, processing failure or the four-second acceptance deadline produces 503; malformed input produces 400, server body limits may produce 413, and unknown routes produce 404. Handler failure responses contain JSON `code=FAIL` without exception details.

The typed `HandleAsync` method is for trusted local calls and does not verify network notifications. External requests must use `ReceiveAsync`. Use TLS and exclude keys and complete payment/user payloads from logs. See the [notification contract](../docs/implementation.md#payment-notifications) for details.

## Plugin-Based Integration

Compose this feature through the host; a package reference supplies compile-time APIs, while plugin loading also requires deployed manifests and runtime assets. See [the complete plugin workflow](../../../Zongsoft.Plugins/README.md).

Use a Plugins.Web host and deploy the corresponding main adapter listed below. Service/controller discovery runs during host initialization; do not put business logic into Program.cs. Verify the configured endpoint and authorization before accepting requests.

| Runtime artifact | Source of truth |
| --- | --- |
| `Zongsoft.Externals.Wechat.Gateway` | [Zongsoft.Externals.Wechat.Gateway.plugin](src/Zongsoft.Externals.Wechat.Gateway.plugin) |
| File copying and dependencies | [Zongsoft.Externals.Wechat.Gateway.deploy](src/Zongsoft.Externals.Wechat.Gateway.deploy) |

Add this fragment to an existing host `.deploy` (retain Main and the host’s other base manifests; do not replace the whole file):

```ini
[plugins zongsoft externals wechat]
nuget:Zongsoft.Externals.Wechat@7.10.0

[plugins zongsoft externals wechat gateway]
nuget:Zongsoft.Externals.Wechat.Gateway@7.10.0
```

Run `dotnet deploy` against a test deployment as explained in the workflow, with the host's `framework`, `platform`, `architecture` and, where needed, `site`. Pin compatible versions in real deployments; application dependencies such as databases, caches or commercial runtimes are still separate prerequisites.

Additional artifacts listed by the deployment manifest include `Zongsoft.Externals.Wechat.Gateway.plugin`. Retain assemblies, dependencies and satellite resource directories as well. Restart the host after deployment, check plugin loading and service/driver registration, then verify the workflow above; copied files alone do not prove that the feature is active.

## Third-party platform authorization events

`POST /Externals/Wechat/Components/{componentAppId}/Events` accepts encrypted XML for verification tickets and authorization changes. Configure the component separately and register a durable `IComponentStore`. Verified, reliably stored events return `success`; storage failures return 503. See the [component contract](../docs/implementation.md#component-platform) for size limits, signature rules, idempotency and host responsibilities. This route does not receive authorizer messages or create tenant associations automatically.
