# Operating Instruction

[English](README.md) |
[简体中文](README.zh-Hans.md)

-----

## Test Scenarios

### Test the Supervised Object Lifecycle

* After starting the application, run the `info S5` command once per second. After about `10` seconds, confirm that the supervised object named `S5` is removed from the supervisor because it becomes inactive _(`Inactived`)_.
	> This command calls the supervisor indexer to get the supervised object by name. Running it confirms that the indexer does not extend the lifecycle of the supervised object.

* Next, run the `info` command once per second. After about `20` seconds, confirm that the supervised objects named `S3` and `S4` are removed from the supervisor because they become inactive _(`Inactived`)_.
	> This command enumerates the supervisor to get all supervised objects. Running it confirms that enumeration does not extend the lifecycle of supervised objects.
	> Continue this operation. After about `30` seconds, confirm that `S1` and `S2` are removed from the supervisor because they become inactive _(`Inactived`)_.

* Run `create key --lifecycle:5s`, wait `5` seconds, and confirm that the supervised object named `key` is removed from the supervisor because it becomes inactive _(`Inactived`)_.
* Run `create key --lifecycle:5s | open key`, then wait more than `5` seconds and confirm that the supervised object named `key` is not removed from the supervisor.
	> Run the `info` command repeatedly to observe that the supervised object named `key` remains in the `Running | Observed` state and that its timestamp is updated at least every `2` seconds.

### Test Supervised Object Error Handling

#### Fail-Allowed

1. Run `create key --lifecycle:5s | open key` and confirm that the supervised object named `key` is supervised.
2. Run `error key --round:5` and confirm that the supervised object named `key` is removed from the supervisor because it fails _(`Failed`)_.

#### Fail-Disallowed

1. Run `create key --lifecycle:1h --errors:-1 | open key` and confirm that the supervised object named `key` is supervised.
2. Run `error key --round:10 | info key` and confirm that the supervised object named `key` is not removed from the supervisor because of failure _(`Failed`)_.

### Test Manual Unsupervision

1. Run `create key --lifecycle:1h --errors:-1 | open key` and confirm that the supervised object named `key` is supervised.
2. Run `close key` and confirm that the supervised object named `key` is removed from the supervisor.

## Prerequisites and Run

This is a .NET 10 interactive sample; no server or database is needed. From the framework root:

```shell
dotnet run --project Zongsoft.Core/samples/superviser/Zongsoft.Samples.Superviser.csproj
```

[Program.cs](Program.cs) registers terminal commands; [MySupervisable.cs](MySupervisable.cs) emits observations on a timer and reports errors/completion to its observer.

## Activity Is Not Inspection

Looking up an object or enumerating the superviser does not report activity. `open` starts periodic observations; `pause` stops them, `resume` restarts them, and `close` reports completion. This distinction is the reason for the tests above.

Expiry times are approximate due to scanning and scheduling. Read the current Program.cs options for initial S1–S5 configuration rather than treating the original elapsed-time narrative as a hard deadline.

## Cleanup

Use `reset` to clear sample supervision or `exit` to dispose the superviser. No files or external resources are created. Avoid huge `error --round` values because output can overwhelm the interactive terminal.
