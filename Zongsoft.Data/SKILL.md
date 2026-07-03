---
name: zongsoft-data-mapping
description: 创建、编辑或审查 Zongsoft.Data `.mapping` XML 数据映射文件。当 Codex 需要定义 Zongsoft 数据实体、属性、组合键、导航/复合属性、命令脚本，或根据 `Zongsoft.Data.xsd` 以及相关项目中的约定校验 `.mapping` 文件时使用。
---

# Zongsoft 数据映射

## 工作流程

以 `Zongsoft.Data.xsd` 作为 schema 权威。项目中的 `.mapping` 文件只用于推断命名和建模约定；不要复制不符合 schema 的历史误写属性。

1. 找到最近的 `Zongsoft.Data.xsd` 和相关的同级 `.mapping` 文件。
2. 根据项目或模块确定容器命名空间。使用 `container name="Security"`、`name="Things"` 等；只有在既有项目已经这样做时才使用空名称。
3. 先建模实体：主键、标量属性，然后是复合/导航属性。
4. 只有普通实体 CRUD 无法表达的自定义 SQL 或存储过程操作才添加命令。
5. 在可用时使用 XML 校验器按 XSD 校验结构；否则也要按 XSD 规则进行人工核对。

## 文件结构

从以下带命名空间的结构开始：

```xml
<?xml version="1.0" encoding="utf-8" ?>

<schema xmlns="http://schemas.zongsoft.com/data">
	<container name="ModuleName">
		<entity name="EntityName" table="Module_TableName">
			<key>
				<member name="EntityId" />
			</key>

			<property name="EntityId" type="uint" nullable="false" sequence="#" />
			<property name="Name" type="varchar" length="100" nullable="false" />
			<property name="Creation" type="datetime" nullable="false" default="now()" />
		</entity>
	</container>
</schema>
```

顶层允许的内容是 `schema/container/entity` 和 `schema/container/command`。同一容器内的实体名和命令名必须唯一。

## 实体

为每个数据聚合或表模型使用一个 `entity`。

- `name` 必填，是代码中使用的实体名。
- `table` 可选；当数据库表名与实体名一致时省略。对于 `Security_Role` 这类带前缀的表使用它。
- `driver` 可选；只用于驱动特定实体，例如 `driver="MySQL"`、`driver="TDengine"`。
- `immutable="true"` 表示实体支持读取和新增，但不支持更新和删除。
- `inherits` 由 XSD 支持，用于指定父实体名；如果父实体在其他容器或命名空间中，使用完整限定名。

在属性之前定义 `<key>`，该元素表示对应实体的主键；主键成员字段通过 `<member>` 来指向对应的字段名。

```xml
<key>
	<member name="TenantId" />
	<member name="BranchId" />
	<member name="Key" />
</key>
```

## 标量属性

使用 `property` 定义标量列。必填属性是 `name` 和 `type`；项目文件中几乎总是显式指定 `nullable`，所以要有意识地设置它。

- 使用常见字段类型：`uint`、`ulong`、`ushort`、`byte`、`short`、`int`、`bool`、`varchar`、`nvarchar`、`string`、`datetime`、`timestamp`、`date`、`decimal`、`double`、`binary`。
- 字符串或二进制字段使用 `length`。`decimal`、`money`、`currency` 使用 `precision` 和 `scale`。
- 只有数据库列名与属性名不同时才使用 `field`。
- 使用 `default` 表示默认值，例如 `true`、`false`、`_`、`0`、`now()`、`today()`。
- 对只在新增时设置的值使用 `immutable="true"`，例如所有者 ID 和创建元数据。
- 只有字段确实要参与排序或查询排序时才使用 `sortable="true"`。
- `hint` 只用于驱动特定行为，例如时序映射使用 `hint="tag"` 和 `hint="timestamp"`。

序号约定：

- `sequence="*"` 使用数据库内置序号器。
- `sequence="#"` 为该属性使用外部序号器。
- `sequence="#(TenantId,BranchId)"` 根据列出的属性创建有作用域的子级序号。
- `sequence="Security.Role:RoleId"` 引用另一个实体/属性的序号。

```xml
<property name="OrderId" type="ulong" nullable="false" sequence="#" />
<property name="AttachmentId" type="ulong" nullable="false" sequence="#(OrderId)" />
<property name="TenantId" type="uint" nullable="false" immutable="true" sortable="true" />
<property name="Amount" type="decimal" precision="18" scale="2" nullable="false" default="0" />
```

## 复合属性

使用 `complexProperty` 定义导航属性。必填属性是 `name` 和 `port`。

- `port` 通常命名目标实体。
- `port="JoinEntity:TargetNavigation"` 表示通过中间实体路由到该实体上的导航/属性。
- `multiplicity` 默认为 `?`（零或一）。使用 `!` 表示恰好一个，使用 `*` 表示集合。
- 复合属性的 `immutable` 默认为 `true`。当集合或导航属性需要可变时设置 `immutable="false"`。
- `behaviors="principal"` 标记目标为主控端；明细或子类型记录指向其所有者时常见。

链接把目标端口连接到当前实体锚点：

- `<link port="TenantId" />` 表示目标端口和当前锚点使用同名属性。
- `<link port="MemberId" anchor="UserId" />` 表示把目标 `MemberId` 映射到当前 `UserId`。
- 组合关系使用多个 `link`。

```xml
<complexProperty name="Branch" port="Branch">
	<link port="TenantId" />
	<link port="BranchId" />
</complexProperty>

<complexProperty name="Roles" port="Member:Role" multiplicity="*">
	<link port="MemberId" anchor="UserId" />

	<constraints>
		<constraint name="MemberType" value="0" />
	</constraints>
</complexProperty>
```

使用 `constraints` 为关系添加常量过滤条件。`actor` 可选；出现时，使用 `Principal` 表示当前/主控端，使用 `Foreign` 表示外链/目标端。

```xml
<constraints>
	<constraint name="Target" actor="Foreign" value="Device" />
</constraints>
```

## 命令

使用 `command` 定义命名 SQL 或存储过程操作。命令应放在其操作实体所在的同一容器内。

- `name` 必填。
- `alias` 可用于指定存储过程、函数或视图名称。
- `type` 可以是 `text` 或 `procedure`；样例项目中省略的命令都是 SQL 文本。
- `mutability` 默认为 `none`；只有命令会改变数据且调用方需要该元数据时，才设置为 `insert`、`update`、`delete` 或 `upsert`。
- 添加 `parameter` 子节点，必填 `name` 和 `type`；除非确实需要输出、双向或返回参数，否则使用 `direction="input"`。
- 添加一个或多个 `script` 子节点，必填 `driver`；为提升可读性并避免转义问题，将 SQL 包在 CDATA 中。

```xml
<command name="SetDeviceMetricMappedCode" mutability="update">
	<parameter name="Prefix" type="string" direction="input" />
	<parameter name="DeviceId" type="uint" direction="input" />
	<script driver="MySql">
		<![CDATA[
		UPDATE Things_Metric m
		INNER JOIN Things_DeviceMetric dm ON dm.MetricId = m.MetricId
		SET m.MappedCode = CONCAT(@Prefix, m.MetricCode)
		WHERE dm.DeviceId = @DeviceId;
		]]>
	</script>
</command>
```

## 审查清单

完成 `.mapping` 修改前：

- 保持 XML 命名空间精确为 `http://schemas.zongsoft.com/data`。
- 编辑项目映射文件或 XML 示例时使用 Tab 缩进。
- 确认每个主键成员都有对应的标量属性。
- 对主键、必填字段和使用序号的属性设置 `nullable="false"`。
- 文本字段添加 `length`，小数字段添加 `precision` 和 `scale`。
- 优先沿用周边模块已有的 `varchar`/`nvarchar` 约定。
- 确保每个 `complexProperty` 至少有一个 `link`，并且 anchor/port 都指向真实属性。
- 集合导航使用 `multiplicity="*"`；只有必需的单值导航才使用 `!`。
- 驱动名称与附近文件保持一致（历史上同时出现过 `MySql`、`MySQL` 等，按本地项目写法匹配）。
- 不要引入 `Zongsoft.Data.xsd` 之外的属性，即使附近旧文件中出现过。
