---
name: zongsoft-etcd
description: Implement, review, debug, test, or refactor the Zongsoft.Externals.Etcd plugin, including connection lifecycle, KV commands, CAS sequences, lease-based distributed locks, Podman integration, and samples.
---

# Zongsoft Etcd

Use this skill for work under `externals/etcd` and its Podman manifest in `D:\Zongsoft\hosting`.

## Architecture

- Keep `EtcdService` as the single public facade. It owns one lazily created `EtcdClient`, freezes `Namespace` after activation, and disposes the client exactly once.
- Resolve configured instances through `EtcdServiceProvider`. It must expose `IServiceProvider<ISequence>`, `IServiceProvider<ISequenceBase>`, and `IServiceProvider<IDistributedLockManager>`.
- Prefix physical keys with `<Namespace>:`. Public find results must remove only that prefix.
- Keep README files user-oriented. Put algorithms, invariants, risks, and maintainer instructions here.

## Sequence algorithm

Implement sequence changes as a retrying etcd transaction:

1. Read the value, lease ID, and modification revision.
2. Parse numbers with `InvariantCulture`; format doubles with `R`.
3. Return the current value without writing when interval is zero.
4. Compare `CreateRevision == 0` for a missing key or compare the captured `ModRevision` for an existing key.
5. Put `current + interval` only on compare success; otherwise revoke any unused lease and retry.

The first result is `seed + interval`. An expiry applies when a missing sequence is created. Preserve an existing lease during increments. Reset creates or replaces the value and replaces/removes its lease.

## Distributed-lock algorithm

Do not use etcd's blocking Lock service because the Zongsoft contract requires non-blocking acquisition that returns a reusable unheld lock object.

- Grant a lease whose TTL is `ceil(expiry.TotalSeconds)`, with a minimum of one second.
- Acquire with a transaction comparing `CreateRevision == 0`, then put the ownership token with the lease.
- Use the successful transaction header revision as the fencing token. A failed acquisition has fencing token zero.
- Release only with a transaction comparing the stored value to the ownership token, then delete and revoke the old lease.
- Renew by reading value/revision/lease, granting a replacement lease, and conditionally putting the same token with the replacement lease. Revoke the replacement on failure and the old lease after success.
- Treat any failed renewal as loss of ownership. Automatic renewal starts only when `DistributedLockOptions.RenewalInterval` is positive and less than Expiry.

Never let a stale owner delete or renew a successor's lock. Do not weaken the token and revision comparisons.

## Basic KV and commands

- Store strings as UTF-8 and attach a lease only for a positive expiry.
- Use prefix range requests for `Find` and count-only range requests for `Count`.
- Keep plugin command declarations synchronized with concrete command types.
- Do not claim `IDistributedCache`; the Etcd service intentionally provides basic KV, sequences, and coordination primitives.

## Tests

- Follow `.testagent/research.md` and `.testagent/plan.md`.
- Keep validation tests network-free. Gate integration tests with `ZONGSOFT_ETCD_TESTS=1` and probe `127.0.0.1:2379` before running.
- Use unique namespaces and disable integration-test parallelism.
- Cover concurrent sequence increments, lease expiry, ownership mismatch, manual/automatic renewal, contention, cancellation, and strictly increasing fencing tokens.
- Build all inherited target frameworks and run the net10.0 integration suite against the Podman manifest.

## Podman

The local manifest is transient by design: `D:\Zongsoft\hosting\zongsoft.pod-etcd.yaml` exposes client port 2379 without a persistent volume. Keep its `etcd` choices synchronized in `zongsoft.pod(start).cmd` and `zongsoft.pod(stop).cmd`.
