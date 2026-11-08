# 标识

[English](identifiers.md) | [简体中文](identifiers.zh-Hans.md)

`Zongsoft.Components.Identifier` 通过逻辑名称 `Name` 限定键的身份。`Identifier<T>` 及二至七个泛型参数的版本提供类型化键；复合键使用 `ValueTuple`。转换为非泛型标识时保留键类型和元数据。

通用标识和以元组作为键的 `Identifier<T>` 支持二至七个分量的完整文本、JSON、类型转换及按序号读取和替换。独立泛型版本与简写登记同样最多提供七个分量。元组分量必须是标量键，不支持嵌套元组。

```csharp
var machine = new Identifier("Machine", 17u, "售货机");
var user = new Identifier(Zongsoft.Security.Privileges.Users.Identifiers.User, 17u);
var branch = new Identifier<uint, uint>("Branch", 1, 1001, (null, "杭运"), "杭州");
Identifier untyped = branch;
Identifier<(uint, uint)> tuple = branch;
```

二至七个泛型参数的标识支持解构为类型化键分量，局部变量名称表达各分量的业务含义。默认标识解构后得到各分量的默认值。

```csharp
var (tenantId, branchId) = branch;
```

泛型标识提供 `Validate(name, predicate)`。各类型化键分量分别作为 Lambda 参数，只有现有类别验证通过后才执行一次条件判断。条件返回 false 表示验证失败；条件抛出的异常直接向调用方传播，空条件委托抛出 `ArgumentNullException`。相等运算符仍比较完整身份。

```csharp
var valid = branch.Validate("Branch", static (tenantId, _) => tenantId != 0);
var specific = branch.Validate("Branch", static (tenantId, branchId) => tenantId != 0 && branchId != 0);
```

## 读取与替换键值

TryGetValue 获取完整键值，可同时验证逻辑 Name。带序号的重载读取从零开始的键分量，单键仅支持序号 0。分量不存在、标识为空、类别不符或转换失败时返回 false，输出默认值；转换沿用标识的既有键转换规则，零值本身不表示读取失败。按分量读取时显式指定输出类型，运行时序号不能使编译器从 out var 推断泛型类型。

```csharp
branch.TryGetValue("Branch", out var key);
branch.TryGetValue("Branch", 0, out uint tenantId);
branch = branch.With(1, 1002);
```

With(ordinal, value) 返回同一 Identifier 版本的新值，替换值按原分量类型转换。其余分量和所有元数据保持原值，Properties 共享原有字典。空标识抛出 InvalidOperationException，序号越界抛出 ArgumentOutOfRangeException，无法转换的值抛出 InvalidCastException。模型在属性 setter 中调用 With 前，应先初始化类别及键值。Validate 保留类别与条件判断职责。

泛型标识的 Value、Code、Label、Alias、Description、Properties 支持对象初始化器和原生 `with` 表达式；Name 类别由构造函数确定。Value 赋值使用完整类型化键，需要运行时分量序号时使用 With(ordinal, value)。原生 with 保留其他字段并共享 Properties；初始化新的 Properties 字典时进行一次浅复制。这些 init 访问器不改变非泛型 Identifier 的只读字段。

```csharp
var branch = new Identifier<uint, uint>("Branch", 1, 1001)
{
	Code = "HZ001",
	Label = "杭州",
	Alias = "杭运",
};
branch = branch with { Value = (1, 1002), Label = "宁波" };
```

## 身份与元数据

泛型参数使用实际的非空主键类型；身份缺失使用默认标识表示。Name 验证拒绝空标识。零键值仍表示有值，是否允许由业务条件判断。

Name 仅包含 ASCII 字母、数字、下划线和点，首字符不能为点。Name 忽略大小写；Name 和实际键类型属于身份。例如 `Machine:<uint>17`、`Machine:<int>17` 与 `Machine:17` 是三个不同标识，字符串键区分大小写。

`Code`、`Label`、`Alias`、`Description`、`Properties` 不参与相等与哈希。Code 是可为空的业务编号，与 Value 中的身份键独立，保留原文，不从 Value 或 Alias 回退生成；仅有 Code 而 Value 为空时仍为空标识。Alias 是可选展示字段。非泛型元数据字段只读，泛型版本提供 init 访问器。构造函数提供三组元数据参数，非泛型和所有泛型版本采用相同规则：

- `label、description、properties`：只提供展示名称、说明或扩展属性。
- `(code, alias)、label、description、properties`：将业务编号和别名组成 extra 元组，展示名称单独提供。
- `(code, alias, label)、description、properties`：将业务编号、别名和展示名称合并为 extra 元组。

extra 紧跟键值，元组中未提供的字段使用 null，其余尾部参数均可省略。泛型复合键支持逐个分量或完整键元组；键元组与元数据元组按参数位置区分。构造参数分组不改变文本协议的字段顺序。

```csharp
var order = new Identifier("Order", 1001UL, ("SO001", null), "采购订单");
var employee = new Identifier<uint>("Employee", 17u, ("E001", "小张", "张三"), "采购部");
```

`Properties` 是可为空的可变 `IDictionary<string, string>`。构造或泛型 Properties 初始化时对外部字典进行一次浅复制，结构复制和无损标识转换共享内部字典。扩展键忽略大小写，可以包含 Unicode 字符、空格、核心成员名称或空字符串。键和值使用相同的转义规则，null 值输出为空字符串。格式化按键排序读取当前字典；并发修改由应用同步。

## 文本与 JSON

```text
Name:<keyTypes>keyValues#Code?Label|Alias(Description){key=value;...}
```

单个字符串键可以省略 `<string>`；其他键保留类型声明，或使用匹配的已登记简写。完整格式以逗号分隔复合键。元数据顺序固定为 Code、Label/Alias、Description、Properties。空的可选元数据不输出；仅有 Alias 时使用 `?|Alias`。Description 可以独立使用 `(Description)`，必须闭合括号。键类型位于名称冒号之后。

```text
Machine:17
Machine:<uint>17
Zongsoft.Security.User:<uint>17
Branch:<uint,uint>1,1001?杭州|杭运(区域运营){zone=east}
Store:<uint,ushort>1,2
Order:<ulong>1001#SO001?采购订单(内部)
```

反斜杠转义包括尖括号、圆括号在内的结构分隔符及自身。例如 `Order:<ulong>1001(采购订单\(内部\))` 的描述包含字面圆括号。控制字符使用 `\n`、`\r`、`\t` 或 `\uXXXX`。内容不被裁剪。数字采用固定区域格式，日期和时间使用可往返格式；枚举和自定义标量键必须支持双向文本转换，不能依赖任意对象的 `ToString()`。枚举及自定义键类型通过 TypeAlias 解析，仅接受已知别名和已加载类型。

所有标识版本实现 `ISpanParsable`，提供一致的 string/span Parse、TryParse 入口，格式提供者不改变协议。空文本得到默认值；`Parse(null)` 抛出 `ArgumentNullException`，`TryParse(null)` 返回 false。非法结构、转义、重复扩展键、键数量和键值均解析失败。

`ToString()`、TypeConverter 和 JSON 使用同一文本。JSON 输出包含类别的完整标识字符串，默认值输出 null，不接受原始 JSON 数字；JSON 字典键同样使用标识文本。显式基本类型转换和 IConvertible 提取键值，不属于标识序列化。

## 简写登记

在创建应用或接受输入之前登记应用格式：

```csharp
Identifier.Formatting.Register<uint, uint>("Branch");
Identifier.Formatting.Register<uint, ushort>("Store");

var branch = new Identifier<uint, uint>("Branch", 1, 1001);
var text = branch.ToString();           // Branch:1-1001
var full = branch.ToString("F");        // Branch:<uint,uint>1,1001
var parsed = Identifier.Parse(text);
```

登记以逻辑 Name 为键，支持一至七个分量。默认分隔符为短横线 `-`，也可以显式指定逗号、斜杠或点；分量中的同字符会被转义。相同登记可重复调用，冲突登记抛出异常，不允许替换规则；支持并发读取与登记。

显式类型声明优先于登记规则。同名字符串键输出 `<string>` 避免混淆。已登记简写的数量、类型或范围不符时失败；未登记且未声明类型的非泛型输入按单个字符串键处理。单键泛型解析接受可赋值给 T 的声明或登记键类型，保留实际键类型，不进行数值类型转换。例如 `Identifier<object>` 可以承载 `Scalar:<uint>17`，而 `Identifier<int>` 拒绝该文本。没有类型声明和登记时，能够承载字符串的单键泛型按字符串解析，其余泛型参数提供预期键类型。复合键要求各分量类型匹配。跨应用传输简写要求采用相同登记；`ToString("F")` 不依赖业务简写规则。

## 可标识对象与 Security

`IIdentifiable.Identifier` 保持可读写，`IIdentifiable<T>` 继承它并提供默认桥接，保留元数据并转换键类型。键类型无需实现 `IEquatable<T>`，枚举键可以直接使用该接口。实现仍负责类别和业务约束校验；只读对象的 setter 抛出 `NotSupportedException`。

二至七参数的 `IIdentifiable<T1, …, T7>` 各自暴露对应分量数的 Identifier，继承 `IIdentifiable<(T1, …, T7)>`，并提供默认双向桥接。模型只需实现对应的多参数属性，即可通过完整元组键和非泛型接口访问相同身份、元数据及扩展属性字典。

Security 的用户、角色和权限类别分别由 `Users.Identifiers.User`、`Roles.Identifiers.Role` 和 `Privilege.IdentifierName` 定义，值为 `Zongsoft.Security.User`、`Zongsoft.Security.Role` 和 `Zongsoft.Security.Privilege`。类别使用固定逻辑名称，不依赖模块初始化。实际键类型由模型决定，不登记全局固定键类型。Member 暴露实际用户或角色身份。Claims 保留声明值类型，通过模型的 Identifier setter 回填；非用户模型须通过已有 Identifier.Name 提供类别，键值可以为空。标识不代表授权，服务仍须验证租户范围和操作权限。
