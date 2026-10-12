# 普通设置（Settings）与连接设置（ConnectionSettings）

[English](settings.md) | [简体中文](settings.zh-Hans.md)

## 字典契约

`ISettings` 继承 `IDictionary<string, string>` 和 `IEquatable<ISettings>`。自身只声明 `Name`、`Value`、`IsEmpty` 和 `HasValue`，不重复声明继承的字典成员。`Settings` 类同时实现 `IReadOnlyDictionary<string, string>`。

普通设置（`Settings`）表示分号分隔的键值字符串。名称和键忽略大小写，值区分大小写。`Add`、`Remove`、`Clear`、集合操作及字典键值视图采用显式接口实现：

```csharp
ISettings settings = new Settings("mail", "Host=mail.example.test;Port=2525");
settings.Add("Sender", "sender@example.test");
settings["Port"] = "2526";
settings.Remove("Sender");
var text = settings.Value;
```

原始索引器在键缺失时返回 `null`，赋 `null` 或空字符串会删除主参数。`Add` 拒绝重复键。`ICollection<KeyValuePair<string, string>>.Remove` 要求键和值同时匹配。

`Value` 包含所有显式设置。修改使文本缓存失效，下次读取时序列化当前状态，并转义引号文本和分隔符。给 `Value` 赋值会全量替换状态；赋 `null` 或空文本会清空。文本中的重复键按原文顺序后值覆盖前值。驱动别名参与同一顺序：当 `Host` 是 `Server` 的别名时，`Server=a;Host=b;Server=c` 的结果为 `Server=c`。输出顺序、键名大小写和末尾分号不属于格式保证。

## 文本语法

设置项使用 `key=value`，以分号分隔。省略赋值符或赋值内容为空时，键仍存在且值为 null；`key=''` 显式提供空字符串。单引号或双引号可以保护分号、首尾空白及值开头的等号。引号内通过反斜杠转义引号和反斜杠，`\t`、`\r`、`\n` 表示控制字符，不使用重复引号转义。例如 `Password='=abc=='` 保留 `=abc==`，`Text='a\'b;=c\\'` 保留 `a'b;=c\`。

解析检查全部输入，包括最后一个字符。引号未闭合、引号内转义未完成或末尾含非法字符均失败。`TryParse` 返回 false 且结果为 null；`Parse`、构造函数及 `Value` 赋值抛出 `ArgumentException`。`Value` 赋值失败保留原有文本与条目。序列化显式空值或带首尾空白的值时使用字典 `Add` 方法；索引器会删除空值并裁剪非空值。

## 连接设置（ConnectionSettings）

连接设置接口（`IConnectionSettings`）继承 `ISettings`，提供驱动、`Properties`、`HasProperties`、`GetValue<T>` 和 `TryGetValue<T>`。连接设置基类（`ConnectionSettingsBase`）继承 `Settings`，共用解析器与主字典。扩展属性集（`Properties`）仅属于连接设置（`ConnectionSettings`）。

| 访问入口 | 内容 |
| --- | --- |
| 索引器、非泛型 `TryGetValue`、`ContainsKey`、`Count`、`IsEmpty`、`HasValue`、枚举 | 显式主参数，不补默认值 |
| 类型化属性、`GetValue<T>`、`GetOptions()` | 转换后的主参数及描述器默认值 |
| 扩展属性集（`Properties`） | 显式扩展参数 |
| `Value` | 显式主参数及扩展参数 |

类型化 `TryGetValue<T>` 在显式值可转换或描述器声明了默认值时成功。缺失且未声明默认值的 `bool`、`int`，类型化 `TryGetValue<T>` 返回 `false`，`GetValue<T>` 返回 CLR 默认值；显式 `false`、`0` 在类型化 Try 读取中返回成功。非法显式值保留在原始字典中：类型化 getter 和选项转换抛出 `InvalidOperationException`，类型化 `TryGetValue<T>` 返回 `false`。

不带 `TOptions` 时允许任意主参数键。带 `TOptions` 时由 `Driver.Descriptors` 决定合法名称、别名和复合路径，包括标记为 `Ignored` 的业务参数。文本中的未知项进入扩展属性集（`Properties`）；向主字典写未知键抛出 `ArgumentException`。

对于带约束的连接设置（`ConnectionSettings`），通过扩展属性集（`Properties`）的索引器或 `Add` 写已声明名称、别名或有效复合路径，同样抛出 `ArgumentException`。写入失败保持原状态。查询和删除仅作用于所访问的字典；清空主字典保留扩展参数。

以原文 `server=example.test;port=3306;CircuitBreaker.Duration=60s` 创建的 MySQL 连接设置（`MySqlConnectionSettings`）为例：

```csharp
settings.Properties["ABC"] = "Value";
settings["database"] = "zongsoft";
settings["port"] = "3307";
// settings.Properties["Port"] = "3308"; // 抛出 ArgumentException。
```

此时 `Value` 除更新后的主参数外，还保留 `CircuitBreaker.Duration=60s` 和 `ABC=Value`。`GetOptions()` 将驱动参数转换为原生选项。完整框架设置文本（`Value`）不保证能直接作为第三方连接字符串；传给提供程序时使用驱动的原生选项。

普通设置（`Settings`）比较名称及主字典内容；连接设置（`ConnectionSettings`）还比较驱动名称及扩展属性。条目顺序不影响结果，各接口及基类入口的相等性一致。普通设置（`Settings`）与连接设置（`ConnectionSettings`）不相等。作为哈希键使用时不要修改内容。

## 配置绑定与驱动选项

一组标量值使用单个普通设置（`Settings`）。字典键是设置名称，字典值直接是字符串。例如，邮件配置在同一字典中保存 `Sender`、`Host`、`Port` 和 `AuthorityCode`。

将普通设置（`Settings`）或协议包装设置转为具体驱动时显式重建：

```csharp
var options = driver.GetSettings(settings.Name, settings.Value).GetOptions();
```

`ConnectionSettingsUtility.GetOptions<TOptions>()` 不会通过驱动自动重建另一种设置对象。连接元数据通过驱动实例的 `Descriptors` 属性获取。

配置绑定先应用 `Value`，再应用独立属性，`Driver` 按连接元数据处理。以 `ISettings` 为配置目标时创建 `Settings` 实例。

## 派生扩展

`GetKey(string key, bool validate = false)` 是主参数键名归一化的可重写入口。查询和删除使用 `validate: false`，索引器赋值和添加使用 `validate: true`。连接设置（`ConnectionSettings`）在同一次调用中解析别名、复合路径，并仅在受选项类型约束且校验写入时拒绝未知名称。基类实现原样保留键名，不修剪首尾空白。

`OnEntryChanged(string key, string value)` 处理从设置文本解析得到的条目。初始化或替换 `Value` 时，`OnValueChanged` 按文本顺序调用它，派生类可将条目分派到主字典或扩展属性。索引器赋值、添加、删除和清空操作不调用此方法。
