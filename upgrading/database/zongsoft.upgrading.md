# Zongsoft.Upgrading 数据库表结构定义

## 应用表 `Application`

字段名称 | 数据类型 | 长度 | 可空 | 备注
------- |:-------:|:---:|:---:| ----
ApplicationId | int      | 4   | ✗ | 主键，应用编号
Name          | varchar  | 50  | ✗ | 应用名称 _(具有唯一性)_
Title         | nvarchar | 100 | ✓ | 应用标题
Enabled       | bool     | -   | ✗ | 是否启用
Creation      | datetime | -   | ✗ | 创建时间
Modification  | datetime | -   | ✓ | 修改时间
Description   | nvarchar | 500 | ✓ | 描述信息


## 应用版本表 `ApplicationEdition`

字段名称 | 数据类型 | 长度 | 可空 | 备注
------- |:-------:|:---:|:---:| ----
ApplicationId | int      | 4   | ✗ | 主键，应用编号
Name          | varchar  | 50  | ✗ | 主键，版本标识
Title         | nvarchar | 100 | ✓ | 版本标题
Enabled       | bool     | -   | ✗ | 是否启用 _(默认真)_
Licensed      | bool     | -   | ✗ | 是否授权 _(默认假)_
Creation      | datetime | -   | ✗ | 创建时间
Modification  | datetime | -   | ✓ | 修改时间
Description   | nvarchar | 500 | ✓ | 描述信息


## 实例表 `Instance`

字段名称 | 数据类型 | 长度 | 可空 | 备注
------- |:-------:|:---:|:---:| ----
InstanceId   | int      | 4   | ✗ | 主键，实例编号
InstanceCode | varchar  | 50  | ✗ | 实例代号 _(具有唯一性)_
Name         | nvarchar | 100 | ✓ | 实例名称
Tags         | nvarchar | 500 | ✓ | 标签集
Profile      | ntext    | -   | ✓ | 配置信息，包含硬件和操作系统等
Creation     | datetime | -   | ✗ | 创建时间
Modification | datetime | -   | ✓ | 修改时间
Description  | nvarchar | 500 | ✓ | 描述说明


## 发布表 `Release`

字段名称 | 数据类型 | 长度 | 可空 | 备注
------- |:-------:|:---:|:---:| ----
ReleaseId     | int      | 4    | ✗ | 主键，发布编号
ApplicationId | int      | 4    | ✗ | 应用编号
Name          | varchar  | 50   | ✗ | 应用名称
Edition       | varchar  | 50   | ✗ | 版本名
Version       | varchar  | 50   | ✗ | 版本号
Kind          | byte     | 1    | ✗ | 发布类型 _(`0`:Fully; `1`:Delta)_
Mode          | byte     | 1    | ✗ | 升级部署模式 _(`0`:默认; `1`:尽快执行)_
Platform      | byte     | 1    | ✗ | 平台
Architecture  | byte     | 1    | ✗ | 架构
Path          | varchar  | 200  | ✓ | 文件路径
Size          | long     | 8    | ✗ | 包大小
Checksum      | varchar  | 100  | ✓ | 校验码
Tags          | nvarchar | 500  | ✓ | 标签集
Deprecated    | bool     | -    | ✗ | 是否废弃
Published     | bool     | -    | ✗ | 是否已发布
Visible       | bool     | -    | ✗ | 是否可见
Title         | nvarchar | 100  | ✓ | 标题
Summary       | ntext    | -    | ✓ | 摘要
FilterName    | nvarchar | 50   | ✓ | 过滤器名称
FilterData    | nvarchar | 500  | ✓ | 过滤器数据
FilterSetting | nvarchar | 500  | ✓ | 过滤器设置
Creation      | datetime | -    | ✗ | 创建时间
Modification  | datetime | -    | ✓ | 修改时间
Description   | nvarchar | 500  | ✓ | 描述信息


## 发布属性表 `ReleaseProperty`

字段名称 | 数据类型 | 长度 | 可空 | 备注
------- |:-------:|:---:|:---:| ----
ReleaseId | int      | 4   | ✗ | 主键，发布编号
Name      | varchar  | 50  | ✗ | 主键，属性名称
Type      | varchar  | 100 | ✓ | 属性类型
Value     | nvarchar | 500 | ✓ | 属性值


## 发布执行器表 `ReleaseExecutor`

字段名称 | 数据类型 | 长度 | 可空 | 备注
------- |:-------:|:---:|:---:| ----
ReleaseId | int      | 4   | ✗ | 主键，发布编号
SerialId  | byte     | 1   | ✗ | 主键，执行序号
Event     | varchar  | 50  | ✗ | 执行事件
Command   | nvarchar | 500 | ✗ | 执行命令


## 发布实例状态表 `ReleasePublishing`

字段名称 | 数据类型 | 长度 | 可空 | 备注
------- |:-------:|:---:|:---:| ----
ReleaseId   | int      | 4   | ✗ | 主键，发布编号
InstanceId  | int      | 4   | ✗ | 主键，实例编号
Status      | byte     | 1   | ✗ | 发布状态 _(`Fetch`, `Downloading`, `Downloaded`, `Upgrading`, `Upgraded`, `Completed`)_
Message     | nvarchar | 500 | ✓ | 失败消息
Timestamp   | datetime | -   | ✗ | 更新时间
Description | nvarchar | 500 | ✓ | 更新描述
