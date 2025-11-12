# 版本变更记录

## 2025年11月12号

主要因为数据访问相关进行升级，增加了 PostgreSQL 数据驱动及因此带来的变动。

### Zongsoft.Core
> 7.21.0 ⇢ 7.22.0

- 增加 `Zongsoft.Data.DataType` 类型；
- 调整原来对 `DbType` 的依赖改为 `DataType` 类型：
	- `DataCommandParameter`, `IDataCommandParameter`
	- `DataEntitySimplexProperty`, `IDataEntitySimplexProperty`
	- `DataEntityPropertyCollection`
	- `Operand`

### Zongsoft.Data
> 7.7.0 ⇢ 7.8.0

- 重构对 `DbType` 的依赖改为 `DataType` 类型；
- 重构组装(_**P**opulators_)相关的接口和类；
- 重构模型成员访问动态生成器 `ModelMemberEmitter` 类；
- 为增删改查语句增加 `With` 子句，以支持 _**CTE**_ 表达式；
- 重构语句的构建器和访问器；
- 增加 `IDataRecordGetter` 接口和 `DataRecordGetter` 类；
- 增加 `IDataParameterSetter` 接口。

### Zongsoft.Data.PostgreSql
> 0.1.0

新增 _**P**ostgre**SQL**_ 数据驱动。

### Zongsoft.Data.MySql
> 7.3.0 ⇢ 7.4.0

兼容性升级。

### Zongsoft.Data.MsSql
> 7.2.0 ⇢ 7.3.0

兼容性升级。

### Zongsoft.Data.SQLite
> 2.2.0 ⇢ 2.3.0

兼容性升级。

### Zongsoft.Data.Influx
> 0.2.0 ⇢ 0.3.0

兼容性升级。

### Zongsoft.Data.ClickHouse
> 2.2.0 ⇢ 2.3.0

兼容性升级。

### Zongsoft.Data.TDengine
> 2.1.0 ⇢ 2.2.0

兼容性升级。
