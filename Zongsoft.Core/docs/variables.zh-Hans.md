# 变量

[English](variables.md) | [简体中文](variables.zh-Hans.md)

`Zongsoft.Common` 提供独立于表达式和模板语法的变量契约与实现：`IVariables` 负责按名称和可选命名空间查询原始值，`Variables` 提供内存字典、字典包装和环境变量访问。命令选项与 Profile 也可以作为变量来源。

## 开始使用

```csharp
using Zongsoft.Common;

var variables = new Variables
{
	["name"] = "Zongsoft",
	["app.runtime:workers"] = 4,
	["optional"] = null,
};

IVariables source = variables;
source.TryGetValue("NAME", out var name); // Zongsoft
source.TryGetValue("APP.RUNTIME", "WORKERS", out var workers); // 4
var found = source.TryGetValue("optional", out var value); // found 为 true，value 为 null
```

## 变量契约

变量来源实现以下接口：

```csharp
public interface IVariables
{
	bool TryGetValue(string name, out object value);
	bool TryGetValue(string name, bool fallback, out object value);
	bool TryGetValue(string @namespace, string name, out object value);
	bool TryGetValue(string @namespace, string name, bool fallback, out object value);
}
```

`TryGetValue(name, out value)` 只查询默认命名空间，语义等价于 `TryGetValue(null, name, out value)` 和 `TryGetValue(string.Empty, name, out value)`。显式重载先传命名空间，再传变量名称，两者由独立参数提供，不将名称再次拆分为命名空间与名称。

名称和命名空间按 `OrdinalIgnoreCase` 比较；[环境变量视图](#环境变量视图) 遵循平台规则。null 和空字符串均表示全局命名空间。没有布尔参数的重载等价于 `fallback=false`；`fallback=true` 时依次查询指定命名空间、父级、全局及来源自身已声明的默认值。例如 A.B.C → A.B → A → 全局。命名空间只限定变量，没有保留的查询模式名称。

查询是否成功由布尔返回值决定，存在且值为 null 仍返回 true；变量不存在返回 false，不因此抛出异常。读取来源时发生的其它异常可以传播。

提供器返回原始对象，不负责解析变量引用、成员导航、类型转换、格式化或模板展开。缓存、实时性及并发同步由各实现决定，接口不保证多次查询得到相同值。

## 多来源查询

`VariablesExtension.TryGetValue` 为 `IEnumerable<IVariables>` 提供与接口对应的四个重载：

1. 在当前命名空间按来源顺序查询，均传入 false。
2. 全部未找到且允许回退时，逐级进入父命名空间直至全局，每一级都先查询全部来源。
3. 全局普通查询仍全部失败，才按来源顺序调用 `TryGetValue(null, name, true, out value)`，允许来源提供自身已声明的默认值。
4. 任何查询命中 null、空字符串、false 或 0 即结束。不带布尔参数或传入 false 时，仅执行第一步。

以下片段假定已有命令上下文 context 和配置 profile：

```csharp
IVariables[] sources = [context.Options, profile.ToVariables(), Variables.Environments()];
sources.TryGetValue("compilation", true, out var raw);
var evaluator = new TemplateEvaluator(new() { Fallback = true })
{
	Providers = { context.Options, profile.ToVariables(), Variables.Environments() },
};
var text = evaluator.Evaluate("${compilation}");
```

描述符默认值为 Release、配置为 Debug 时，省略选项得到 Debug；显式传入 `--compilation:Custom` 得到 Custom；选项、配置和环境均缺失才使用 Release。命令选项只注册一次，无需复制或拆成两层。

更具体的命名空间优先，同一命名空间内来源顺序优先。例如客户来源只有全局 `key=customer`，随后公共来源提供 `A.B:key=shared`，查询 `A.B.C:key` 并允许回退时得到 shared。

集合须可重复枚举且查询期间稳定；空集合返回 false，忽略 null 来源，集合或名称为 null 抛 ArgumentNullException。模板评估器独立拒绝 Providers 中的 null 元素。查询不缓存、转换、展开或吞掉异常，不保证跨来源原子快照。Profile 的 [default] 是普通章节，`${default:compilation}` 查询该命名空间。

## 内存变量

Zongsoft.Common.Variables 继承 Dictionary<string, object> 并实现 IVariables，固定使用 StringComparer.OrdinalIgnoreCase。索引器、集合初始化器、Add、TryAdd、Remove、Clear 和枚举均沿用字典行为。

| 操作 | 示例 |
| --- | --- |
| 写入默认变量 | `variables["name"] = "Zongsoft";` |
| 写入具名命名空间的变量 | `variables["app.runtime:name"] = "worker";` |
| 查询默认命名空间 | `variables.TryGetValue("NAME", out var value);` |
| 查询指定命名空间 | `variables.TryGetValue("APP.RUNTIME", "NAME", out var value);` |
| 删除具名命名空间的变量 | `variables.Remove("app.runtime:name");` |

字典操作使用完整的键。提供器重载分别接收命名空间和变量名称并组合成键，不裁剪或拆分参数。冒号专用于分隔命名空间与变量名称，两部分本身不应包含冒号。null 和空字符串均忽略命名空间限定，直接查询 `name`；非空命名空间查询 `namespace:name`，仅含空白字符的命名空间不会被忽略。变量名称为 null 时抛出 ArgumentNullException。

可通过 `new Variables()`、`new Variables(capacity)` 或 `new Variables(entries)` 创建字典。集合构造函数复制 IEnumerable<KeyValuePair<string, object>> 中的条目；后续增删和替换不影响源集合，变量值仍引用原始对象。所有构造函数使用相同的比较器；复制时存在按忽略大小写规则比较后重复的键会抛出异常。

变量值是原始对象，可以为 null。Variables 不进行模板求值、成员导航或类型转换。字典修改会反映在后续查询中。它与 Dictionary 一样不提供并发读写同步，需要时由调用方负责同步。

## 字典变量视图

使用 `Variables.Wrap(dictionary)` 或 `dictionary.ToVariables()`（位于 `Zongsoft.Common`）可将 `IDictionary`、`IDictionary<string, object>` 或 `IDictionary<object, object>` 包装为实时 `IVariables` 视图：

```csharp
using System.Collections.Generic;
using Zongsoft.Common;
using Zongsoft.Text.Templating;

IDictionary<string, object> dictionary = new Dictionary<string, object>
{
	["Name"] = "Zongsoft",
	["App:Version"] = "1.0",
};

IVariables variables = Variables.Wrap(dictionary);
var evaluator = new TemplateEvaluator { Providers = { variables } };
dictionary["Name"] = "Updated"; // 后续查询立即看到修改。

// 反复包装同一个字典时，显式开启复用。
IVariables shared = dictionary.ToVariables(reuse: true);
IVariables same = Variables.Wrap(dictionary, reuse: true);
```

`Wrap` 提供三个带字典类型约束的重载，`ToVariables` 提供对应的扩展方法：

```csharp
public static IVariables Wrap<TDictionary>(TDictionary dictionary, bool reuse = false)
	where TDictionary : IDictionary;
public static IVariables Wrap(IDictionary<string, object> dictionary, bool reuse = false);
public static IVariables Wrap(IDictionary<object, object> dictionary, bool reuse = false);
```

实现非泛型 `IDictionary` 的具体字典（包括 `Dictionary<string, object>`、`Dictionary<object, object>` 和 `Hashtable`）优先匹配受约束的泛型方法，因此直接调用不会产生这些接口之间的重载歧义。只实现其中一种泛型字典接口的类型使用对应的接口重载。任意 `object` 或只实现 `IVariables` 的非字典对象不能直接传入；自定义类型若同时实现两种泛型接口而未实现非泛型 `IDictionary`，须显式转换为其中一种接口。裸 `null` 也须指定字典类型。

适配器内部依次优先使用字符串键泛型接口、对象键泛型接口和非泛型接口，保证同一字典通过不同重载得到一致的视图行为。

两个入口均接受可选参数 `bool reuse = false`，默认每次新建适配器，不读取、登记或移除缓存条目，视图仍读取同一个源字典。指定 `reuse: true` 时，同一个字典实例通过两个入口及不同静态接口类型得到同一个视图。字典已经实现 `IVariables` 时，不论 `reuse` 为何值均直接返回原对象。

复用的适配器使用 `ConditionalWeakTable` 按引用身份缓存，重写相等比较不会使不同字典共享视图。字典存活期间缓存视图保持可用，持有任何视图也会保留其来源；两者均无其它可达引用时，缓存不会阻止回收。启用复用的并发调用共享最终关联的视图，但首次并发访问时工厂可能创建额外适配器。复用可减少重复包装的分配；默认行为适合只包装一次并自行持有视图的场景，省去缓存查询和登记。

视图读取当前条目，立即反映新增、替换、删除和空值；不复制条目、不缓存查询结果、不展开模板或转换类型。需要独立复制字符串键/对象值条目时，可将 `IEnumerable<KeyValuePair<string, object>>` 传入 `new Variables(entries)`。缓存自身支持并发访问，源字典的访问仍遵循其同步要求。

只有字符串键提供变量，其它类型的键被忽略，不调用 `ToString()` 转换。键遵循 `Variables` 的约定：默认命名空间使用 `name`，具名命名空间使用 `namespace:name`。null 和空字符串命名空间均查询默认命名空间，非空命名空间仅在 fallback=true 时逐级回退到父级和全局。查询参数不裁剪、不拆分、不归一化。字典或查询名称为 null 时抛出 `ArgumentNullException`。

查询始终使用 `OrdinalIgnoreCase` 比较键。采用 `StringComparer.OrdinalIgnoreCase` 的标准 `Dictionary<string, object>` 和 `ConcurrentDictionary<string, object>` 实例直接查询；其它实现、派生类型和比较器按当前条目的枚举顺序匹配，找到第一个匹配项就立即返回，包括值为 null 的情况。允许忽略大小写后的重名键，精确大小写命中没有额外优先级；枚举查询最坏为 O(n)。存在且值为 null 仍表示查询成功，阻止后续提供器回退。

## 环境变量视图

使用 `Variables.Environments(EnvironmentVariableTarget target = EnvironmentVariableTarget.Process)` 获取环境变量实时视图，显式加入模板提供器集合：

```csharp
using Zongsoft.Common;
using Zongsoft.Text.Templating;

var evaluator = new TemplateEvaluator
{
	Providers = { Variables.Environments() },
};

var text = evaluator.Evaluate("${PATH}");
```

视图按来源共享实例，但不缓存变量值；每次查询调用 `System.Environment.GetEnvironmentVariable(name, target)`，因此视图创建后的进程环境变量增删和修改会反映到后续查询中。返回原始字符串，不进行类型转换或模板展开；存在的空字符串仍表示查询成功，缺失返回 false。访问异常向调用方传播，经模板求值时包装为 `ProviderFailed`。多次查询之间不保证环境保持一致。

`Variables.Environments()` 返回的视图是 `IVariables` 忽略大小写契约的明确特例；接口原有约定及字典、Profile 视图的行为保持不变。环境变量名称比较遵循平台：Windows 忽略大小写，Unix/Linux 区分大小写。Unix/Linux 调用方应使用准确的环境变量名，仅大小写不同的名称分别查询。环境变量名保持原样，不把 `__`、下划线或其它字符转换成命名空间；通过模板引用时仍须符合模板的标识符语法。

环境变量只属于默认命名空间。`TryGetValue(name, out value)`、null 命名空间和空字符串命名空间等价；其它命名空间仅在 fallback=true 时回退到全局；本目标没有额外默认值。查询参数不裁剪；名称为 null 时抛出 `ArgumentNullException`。

`Process` 读取当前进程；`User`、`Machine` 遵循 .NET 的平台支持范围，Unix/Linux 上查询不到变量。不合并或回退到其它来源；无效枚举值在创建视图时抛出 `ArgumentOutOfRangeException`（参数名为 `target`）。

`Variables.Wrap(Environment.GetEnvironmentVariables())` 包装调用时取得的字典快照，并使用字典视图的忽略大小写规则。需要环境变量的实时值和平台名称规则时使用 `Variables.Environments()`。

## 命令选项变量

`CommandLine.CmdletOptionCollection` 直接实现 `Zongsoft.Common.IVariables`，可以将 `CommandContext.Options` 注册为模板提供器：

```csharp
using Zongsoft.Common;
using Zongsoft.Text.Templating;

IVariables variables = context.Options;
variables.TryGetValue("install_path", out var path);

var evaluator = new TemplateEvaluator { Providers = { context.Options } };
var text = evaluator.Evaluate("${install_path}");
```

变量名称将选项名称中的 `.`、`-` 替换成 `_`：`install.path` 和 `install-path` 都对应 `install_path`。转换结果必须符合 `[A-Za-z_][A-Za-z0-9_]*`，否则该选项及其短名称均不参与变量查询；合法短名称也可以作为变量名。查询名称必须已经是合法标识符，不进行裁剪、规范化或命名空间拆分。普通选项查询使用原始名称，例如 `context.Options.GetValue("install-path")`。

变量查询忽略大小写。命令选项只提供全局变量：

| 调用 | 行为 |
| --- | --- |
| `TryGetValue(null, name, false, out value)` | 仅查显式选项。 |
| `TryGetValue(null, name, true, out value)` | 显式选项优先，没有才查询已声明的默认值。 |
| `TryGetValue("app", name, false, out value)` | 返回 false。 |
| `TryGetValue("app", name, true, out value)` | 回退到全局，显式选项优先，其次已声明的默认值。 |

无布尔参数的重载不回退。显式值保留转换后的类型；null、空字符串、false 和 0 均停止回退。显式值和描述引用独立保留，映射冲突各按首项处理。普通 GetValue/TryGetValue 使用原始选项名并允许已声明的默认值；没有显式值或默认值时，GetValue 抛选项未找到异常，TryGetValue 返回 false。可选读取使用 TryGetValue 或带调用方默认值的 GetValue。

`CommandOptionDescriptor.HasDefaultValue` 和 `CommandOptionAttribute.HasDefaultValue` 区别未声明与显式 null。无默认值构造函数保持 false；传入默认值或设置 DefaultValue 后为 true，包括显式 null；Describe 保留声明状态。DefaultValue 不根据类型合成 false 或 0。直接读取默认值时使用描述符的这两个属性。

显式传入 `--compilation:Debug`、声明默认值 Release 时，两种设置都得到 Debug。省略选项时 false 查询失败，true 得到 Release。没有声明默认值则两者均失败。

规范化名称索引在首次有效变量查询时惰性创建，之后直接查询字典。名称映射在此时固定，后续增删或替换描述集合中的选项不会重建索引；默认值每次从原描述对象读取，因此修改这些默认值仍立即生效。索引安全发布后可并发读取，描述对象的并发修改由调用方协调。

## Profile 变量视图

[Profile](profiles.zh-Hans.md#变量视图) 通过 ProfileExtension.ToVariables() 将整个配置、指定章节子树或单个条目适配为 IVariables 实时视图，可用 evaluator.Providers.Add(profile.ToVariables()) 显式注册。章节层级以点连接为命名空间，章节名称内的点原样保留；条目名称中的点和连字符替换为下划线后验证标识符。启用回退时逐级查询父命名空间，但不扩展所选视图范围：章节只提供其子树，条目只提供自身。查询返回原始值，多个条目映射到同一变量时，仅该变量的查询抛出 ProfileException；通过模板查询时包装为 ProviderFailed，并保留原始异常。

## 模板集成

上述变量来源均可显式加入 `TemplateEvaluator.Providers`；模板通过 `VariablesExtension.TryGetValue` 查询；`TemplateEvaluatorOptions.Fallback` 默认 false，显式设为 true 才启用上述回退，递归模板沿用相同设置。命中 null 也终止查询。完整模板语法、成员导航、格式化、事件及错误处理见[文本模板文档](expressions.zh-Hans.md)。

## 实现入口

- [IVariables](../src/Common/IVariables.cs)：变量查询契约。
- [Variables](../src/Common/Variables.cs)、[VariablesExtension](../src/Common/VariablesExtension.cs)：内存字典、字典包装和扩展方法。
- [环境变量视图](../src/Common/Variables.Environments.cs)：平台环境变量访问。
- [命令选项集合](../src/Components/CommandLine.Options.cs)：命令选项的变量索引与查询。
- [ProfileExtension](../src/Configuration/Profiles/ProfileExtension.cs)：配置变量适配。
- [内存变量测试](../test/Common/VariablesTest.cs)、[字典包装测试](../test/Common/VariablesWrappingTest.cs)、[对象键字典测试](../test/Common/VariablesObjectWrappingTest.cs)、[环境变量测试](../test/Common/VariablesEnvironmentsTest.cs)。
- [命令选项变量测试](../test/Components/CommandLineVariablesTest.cs)、[Profile 变量测试](../test/Configuration/Profiles/ProfileVariablesTest.cs)。
