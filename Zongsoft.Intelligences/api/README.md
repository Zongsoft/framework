# Zongsoft.Intelligences.Web Plugin Library

![License](https://img.shields.io/github/license/Zongsoft/framework)
![NuGet Version](https://img.shields.io/nuget/v/Zongsoft.Intelligences.Web)
![NuGet Downloads](https://img.shields.io/nuget/dt/Zongsoft.Intelligences.Web)
![GitHub Stars](https://img.shields.io/github/stars/Zongsoft/framework?style=social)

[English](README.md) |
[简体中文](README.zh-Hans.md)

-----

## Overview

[**Z**ongsoft.**I**ntelligences.**W**eb](https://github.com/Zongsoft/framework/tree/main/Zongsoft.Intelligences/api) is the _**AI**_ **W**eb plugin library for the [_**Z**ongsoft_](https://github.com/Zongsoft/framework) open-source framework. It provides _**W**eb **API**_ plugin support for _**AI**_ features.

It exposes assistants, models, chat sessions, history, and streaming chat from [Zongsoft.Intelligences](../README.md) as framework MVC endpoints. It does not add an AI provider: at least one assistant and its driver/connection must be configured in the parent plugin.

## Installation and Deployment

```shell
dotnet add package Zongsoft.Intelligences.Web
```

Deploy `Zongsoft.Intelligences.Web.plugin` in a Zongsoft web host. Its manifest depends on `Zongsoft.Intelligences`, and routes use the `AI` area.

## API Shape

| Resource | Operations |
| --- | --- |
| assistants | list assistants or inspect one by name |
| models | list/get provider models and activate a model |
| chats | list/open/abandon sessions and submit a message |
| history | read or clear a session's role/text entries |

A streaming chat response is written by `WebUtility.EnumerableAsync` using `System.Net.ServerSentEvents`. Clients should process [server-sent events](https://html.spec.whatwg.org/multipage/server-sent-events.html) incrementally and close the response to cancel generation.

```http
POST /AI/ollama/Chats/my-session/Chat?role=user
Content-Type: text/plain; charset=utf-8

Explain bounded channels in one paragraph.
```

The route above follows the explicit controller templates; replace `ollama` and the session id with configured values. Create or look up the session before relying on history.

> 💡 Generate OpenAPI from the running host for its complete routes and response shapes. AI model payloads are provider-defined and can change independently of this transport layer.

## Safety and Operations

🚨 Prompts, history, model identifiers, and generated text may contain sensitive or untrusted content. Authenticate and authorize endpoints, isolate tenants, bound request/history/output size and duration, apply rate limits, and never interpret model output as trusted HTML or commands.

Cancellation is propagated to assistant operations. Session state is owned by the configured assistant implementation; do not assume it is durable or shared across host instances unless that implementation guarantees it.

## Related Resources

- [AI provider and configuration guide](../README.md)
- [Zongsoft.Web conventions](../../Zongsoft.Web/README.md)
- [Server-sent events specification](https://html.spec.whatwg.org/multipage/server-sent-events.html)

## Plugin-Based Integration

Compose this feature through the host; a package reference supplies compile-time APIs, while plugin loading also requires deployed manifests and runtime assets. See [the complete plugin workflow](../../Zongsoft.Plugins/README.md).

Use a Plugins.Web host and deploy the corresponding main adapter listed below. Service/controller discovery runs during host initialization; do not put business logic into Program.cs. Verify the configured endpoint and authorization before accepting requests.

| Runtime artifact | Source of truth |
| --- | --- |
| `Zongsoft.Intelligences.Web` | [Zongsoft.Intelligences.Web.plugin](Zongsoft.Intelligences.Web.plugin) |
| File copying and dependencies | [Zongsoft.Intelligences.Web.deploy](Zongsoft.Intelligences.Web.deploy) |

Add this fragment to an existing host `.deploy` (retain Main and the host’s other base manifests; do not replace the whole file):

```ini
[plugins zongsoft intelligences]
nuget:Zongsoft.Intelligences

[plugins zongsoft intelligences web]
nuget:Zongsoft.Intelligences.Web
```

Run `dotnet deploy` against a test deployment as explained in the workflow, with the host's `framework`, `platform`, `architecture` and, where needed, `site`. Pin compatible versions in real deployments; application dependencies such as databases, caches or commercial runtimes are still separate prerequisites.

Additional artifacts listed by the deployment manifest include `Zongsoft.Intelligences.Web.plugin`. Retain assemblies, dependencies and satellite resource directories as well. Restart the host after deployment, check plugin loading and service/driver registration, then verify the workflow above; copied files alone do not prove that the feature is active.
