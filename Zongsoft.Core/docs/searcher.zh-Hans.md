# 本地文件搜索

[English](searcher.md) | [简体中文](searcher.zh-Hans.md)

`Zongsoft.IO.Searcher` 为 `System.IO.DirectoryInfo` 提供统一的 Search 扩展方法。仅操作本地文件系统，不经过 IFileSystem、IDirectory 或现有正则 Pattern，不增加包依赖。

## 调用和结果

`Searcher.Target` 指定 Files（文件）、Directories（目录）或 Both（两者，默认值）。Both = 0，因此枚举默认值搜索两种类型。调用方负责传入有效的 target，Search 不校验其有效性。默认搜索两种类型并传入取消令牌时，使用 `directory.Search(pattern, cancellation: cancellation)`。

```csharp
var directory = new System.IO.DirectoryInfo(source);
foreach(var match in Zongsoft.IO.Searcher.Search(directory, "plugins/**/*.json", Zongsoft.IO.Searcher.Target.Files, cancellation))
{
	var name = System.IO.Path.GetRelativePath(match.Origin.FullName, match.Path);
	if(match.IsFile(out var file))
		System.Console.WriteLine($"{name}: {file.FullName}");
}
```

Match.Path 是保留链接名称的逻辑绝对路径；ToString() 返回该逻辑路径，默认结构值返回空字符串，不执行 IO；Result 是解析文件及祖先目录链接后的实际目标。Origin 是第一个通配段之前的逻辑固定前缀，精确路径取父目录。Captures 为不可修改的通配段完整匹配集合，按模式顺序排列，** 可以捕获空字符串。IsFile(out FileInfo)、IsDirectory(out DirectoryInfo) 只判断目标类型，不执行 IO；默认结构返回 false，输出 null。

模式 plugins/*/assets/**/*.json 命中 plugins/orders/assets/config/site.json 时，基准为 plugins，捕获依次为 orders、config、site.json。输出名称使用逻辑 Path，读取内容使用 Result。

## 模式和执行

- 模式必须是基于接收目录的非空相对路径；绝对模式、null 参数报参数异常。
- * 和 ? 仅匹配当前段，只有独立段 ** 匹配零层或多层普通目录；ab**cd 不跨目录。
- assets/** 搜索目录时可包含 assets 本身；assets/**/* 只匹配后代。精确目录不隐式展开内容。
- 固定前缀允许 . 和 ..，通配之后的 .. 报错。目录基准不构成访问范围限制。
- Windows 匹配和逻辑路径去重使用 OrdinalIgnoreCase，其它平台使用 Ordinal，不提供大小写选项。Unix 不转换反斜杠。
- 每次枚举有独立遍历状态，不使用静态运行状态或缓存。多个 ** 产生歧义时，靠前的 ** 优先消费较少层级。同一物理目标的不同逻辑名称分别返回。
- 结果先收集，再按 / 规范化的逻辑相对路径执行 Ordinal 排序，因此并非无缓冲流式遍历。调用者维护多模式之间的参数顺序。
- 调用阶段验证参数并固定路径，枚举阶段执行 IO；遍历及返回结果时检查取消，所有退出路径释放枚举器。
- 普通不存在项不产生结果；选中链接悬空、循环、无法访问及其它 IO 错误明确失败。搜索不提供输入快照，也不保证搜索后文件未变化。

## 链接规则

以链接名称匹配。选中文件链接时返回目标，逻辑路径保留链接名称。目录链接本身可作为结果返回，但搜索不会穿过模式中的目录链接继续匹配后续段；固定中间段也不能通过提前提取前缀绕过。显式传入的 DirectoryInfo 搜索起点可以是链接。

末尾 ** 可以选中目录链接本身，但不会进入它。不相关或被跳过的目录链接无需解析。硬链接视为普通文件，不按物理身份去重。

Packager 和 Deployer 在输出中保留逻辑名称，生成普通文件/目录。当目录本身成为载荷根时展开其目标；内部普通目录递归，嵌套目录链接跳过，选中的文件链接按原名称读取目标内容。不创建符号链接。链接 INI 和 .deploy 的相对引用始终以逻辑配置文件目录为基准。

## 验证

见[实施任务清单](../LOCAL-SEARCHER-TASKS.md)。Windows 测试不能替代 Linux/macOS 原生权限、链接和大小写验证；没有可用共享时不宣称已完成 UNC 实际访问测试。

内部 Enumerate 方法以 directory 表示调用方指定的遍历起点，以 origin 表示 Match.Origin 对应的固定前缀。二者分别保留，避免从固定前缀开始遍历时绕过模式中目录链接的限制。
