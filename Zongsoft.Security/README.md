# Zongsoft.Security Security Plugin Library

![License](https://img.shields.io/github/license/Zongsoft/framework)
![NuGet Version](https://img.shields.io/nuget/v/Zongsoft.Security)
![NuGet Downloads](https://img.shields.io/nuget/dt/Zongsoft.Security)
![GitHub Stars](https://img.shields.io/github/stars/Zongsoft/framework?style=social)

[English](README.md) |
[简体中文](README.zh-Hans.md)

-----

## Overview

[**Z**ongsoft.**S**ecurity](https://github.com/Zongsoft/framework/tree/main/Zongsoft.Security) is the security plugin library for the [_**Z**ongsoft_](https://github.com/Zongsoft/framework) open-source framework. It provides foundational security features such as user roles, authentication, and authorization management.

The package supplies the default persistent user, role, membership, privilege, authentication, and authorization services. It builds on the security contracts in `Zongsoft.Core` and persists its models through `Zongsoft.Data`; add [Zongsoft.Security.Web](api/README.md) only when HTTP endpoints are required.

## Security Model

- **Authentication** proves an identity by an authenticator scheme such as `Identity` or `Secretor` and issues a credential with a bounded lifetime.
- **Authorization** evaluates whether that principal may perform an operation. Roles, direct privileges, inherited memberships, and filtering privileges contribute to the decision.
- **Credential** is the issued proof used by subsequent requests. It must be protected like a password until it expires or is revoked.
- **Challenge/secret** is an out-of-band verification step used by sign-in, password recovery, and contact changes; it is not itself a long-lived credential.

## Installation and Data Setup

```shell
dotnet add package Zongsoft.Security
```

Deploy `Zongsoft.Security.plugin`, `Zongsoft.Security.option`, and `Zongsoft.Security.mapping`. Initialize a supported database with the matching script under [database](database/) before starting the services; the mapping and schema are one contract and must evolve together.

The plugin depends on `Zongsoft.Data` and mounts the Security module, authenticators, authorizer, and role/user/member/privilege services into the workbench.

## Configuration

```xml
<option path="/Security">
	<identity verification="none" passwordLength="0" passwordStrength="None" />
	<authentication period="8:0:0">
		<attempter limit="5" window="00:01:00" period="00:05:00" />
		<expiration>
			<scenario scenario.name="api" period="1.00:00:00" />
		</expiration>
	</authentication>
	<authorization roles="security,securities" />
</option>
```

`identity` controls identity verification and password policy. `authentication.period` controls credential lifetime; the attempter throttles repeated failures, and scenario expiration bounds verification workflows. `authorization.roles` identifies administrative roles.

🚨 The shipped values are framework defaults, not a production security baseline. Select password and identity policy deliberately, use TLS, protect signing/encryption material, and keep credentials and verification secrets out of logs.

## Application Workflow

Applications normally resolve the Core authentication/authorization contracts rather than instantiate `CredentialProvider`, `UserService`, or `PrivilegeService`. The plugin wires concrete services, raises authentication events through `Module.Events`, and exposes module diagnostics. Use the service APIs for user/role lifecycle so membership and privilege invariants remain intact.

Authorization filtering is data-sensitive: preserve cancellation and caller context, and never replace an empty authorized set with an unrestricted query.

## Related Resources

- [HTTP API plugin](api/README.md)
- [CAPTCHA provider](captcha/README.md)
- [Database table notes](database/Zongsoft.Security-tables.md)
- [API reference](docs/Zongsoft.Security-api.md)
- [Implementation guidance](SKILL.md)

## Plugin-Based Integration

Compose this feature through the host; a package reference supplies compile-time APIs, while plugin loading also requires deployed manifests and runtime assets. See [the complete plugin workflow](../Zongsoft.Plugins/README.md).

Also deploy one Data driver and prepare the corresponding security tables. The manifest mounts the Security module, authentication and authorization services; deploy the mapping with the same plugin. Configure cache/credential dependencies before accepting sign-in requests.

| Runtime artifact | Source of truth |
| --- | --- |
| `Zongsoft.Security` | [Zongsoft.Security.plugin](src/Zongsoft.Security.plugin) |
| File copying and dependencies | [Zongsoft.Security.deploy](src/Zongsoft.Security.deploy) |

Add this fragment to an existing host `.deploy` (retain Main and the host’s other base manifests; do not replace the whole file):

```ini
[plugins zongsoft data]
nuget:Zongsoft.Data

[plugins zongsoft security]
nuget:Zongsoft.Security
```

Run `dotnet deploy` against a test deployment as explained in the workflow, with the host's `framework`, `platform`, `architecture` and, where needed, `site`. Pin compatible versions in real deployments; application dependencies such as databases, caches or commercial runtimes are still separate prerequisites.

Additional artifacts listed by the deployment manifest include `Zongsoft.Security.plugin`, `Zongsoft.Security.option`, `Zongsoft.Security.mapping`. Retain assemblies, dependencies and satellite resource directories as well. Restart the host after deployment, check plugin loading and service/driver registration, then verify the workflow above; copied files alone do not prove that the feature is active.
