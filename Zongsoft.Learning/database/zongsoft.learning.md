# Zongsoft.Learning 数据库表结构定义

## 数据集表 `Dataset`

字段名称 | 数据类型 | 长度 | 可空 | 备注
------- |:-------:|:---:|:---:| ----
DatasetId    | int      | 4   | ✗ | 主键，数据编号
Name         | varchar  | 50  | ✗ | 数据集名
Enabled      | bool     | -   | ✗ | 是否启用
Provider     | varchar  | 50  | ✗ | 提供程序
Settings     | varchar  | 500 | ✓ | 设置文本
Creation     | datetime | -   | ✗ | 创建时间
Modification | datetime | -   | ✓ | 修改时间
Description  | nvarchar | 500 | ✓ | 描述信息


## 数据字段表 `DatasetField`

字段名称 | 数据类型 | 长度 | 可空 | 备注
------- |:-------:|:---:|:---:| ----
DatasetId | int      | 4  | ✗ | 主键，数据编号
FieldId   | smallint | 2  | ✗ | 主键，字段编号
Name      | varchar  | 50 | ✗ | 字段名称
Type      | byte     | 1  | ✗ | 字段类型
Kind      | byte     | 1  | ✗ | 字段种类
Index     | int      | 4  | ✗ | 索引序号
Alias     | varchar  | 50 | ✓ | 字段别名


## 评估器表 `Estimator`

字段名称 | 数据类型 | 长度 | 可空 | 备注
------- |:-------:|:---:|:---:| ----
EstimatorId  | int      | 4   | ✗ | 主键，评估器编号
Name         | varchar  | 50  | ✗ | 评估器名称
Enabled      | bool     | -   | ✗ | 是否启用
Provider     | varchar  | 50  | ✗ | 提供程序
Settings     | varchar  | 500 | ✓ | 设置文本
Creation     | datetime | -   | ✗ | 创建时间
Modification | datetime | -   | ✓ | 修改时间
Description  | nvarchar | 500 | ✓ | 描述信息


## 管线表 `Pipeline`

字段名称 | 数据类型 | 长度 | 可空 | 备注
------- |:-------:|:---:|:---:| ----
PipelineId        | int      | 4   | ✗ | 主键，管线编号
Name              | varchar  | 50  | ✗ | 管线名称
Settings          | varchar  | 500 | ✓ | 设置文本
Enabled           | bool     | -   | ✗ | 是否可用
Visible           | bool     | -   | ✗ | 是否可见
DatasetId         | int      | 4   | ✓ | 数据源模板编号
DatasetProvider   | varchar  | 50  | ✗ | 数据源提供程序
DatasetSettings   | varchar  | 500 | ✗ | 数据源配置文本
Creation          | datetime | -   | ✗ | 创建时间
Modification      | datetime | -   | ✓ | 修改时间
Description       | nvarchar | 500 | ✓ | 描述信息


## 管线数据字段表 `PipelineField`

字段名称 | 数据类型 | 长度 | 可空 | 备注
------- |:-------:|:---:|:---:| ----
PipelineId | int      | 4  | ✗ | 主键，发布编号
FieldId    | smallint | 2  | ✗ | 主键，字段编号
Name       | varchar  | 50 | ✗ | 字段名称
Type       | byte     | 1  | ✗ | 字段类型
Kind       | byte     | 1  | ✗ | 字段种类
Index      | int      | 4  | ✗ | 索引序号
Alias      | varchar  | 50 | ✓ | 字段别名


## 管线评估器表 `PipelineEstimator`

字段名称 | 数据类型 | 长度 | 可空 | 备注
------- |:-------:|:---:|:---:| ----
PipelineId   | int      | 4   | ✗ | 主键，发布编号
SerialId     | smallint | 2   | ✗ | 主键，管线序号
EstimatorId  | int      | 4   | ✗ | 评估器编号
Name         | varchar  | 50  | ✗ | 评估器名称
Enabled      | bool     | -   | ✗ | 是否启用
Provider     | varchar  | 50  | ✗ | 提供程序
Settings     | varchar  | 500 | ✓ | 设置文本
Creation     | datetime | -   | ✗ | 创建时间
Modification | datetime | -   | ✓ | 修改时间
Description  | nvarchar | 500 | ✓ | 描述信息


## 训练表 `Training`

字段名称 | 数据类型 | 长度 | 可空 | 备注
------- |:-------:|:---:|:---:| ----
TrainingId  | int      | 4   | ✗ | 主键，训练编号
PipelineId  | int      | 4   | ✗ | 管线编号
ModelId     | int      | 4   | ✓ | 模型编号
Settings    | varchar  | 500 | ✓ | 设置信息
StartTime   | datetime | -   | ✓ | 开始时间
FinalTime   | datetime | -   | ✓ | 结束时间
Creation    | datetime | -   | ✗ | 创建时间
Description | nvarchar | 500 | ✓ | 描述信息


## 模型表 `Model`

字段名称 | 数据类型 | 长度 | 可空 | 备注
------- |:-------:|:---:|:---:| ----
ModelId        | int      | 4   | ✗ | 主键，模型编号
Name           | varchar  | 50  | ✗ | 模型名称
Size           | bigint   | 8   | ✗ | 模型大小
Type           | varchar  | 50  | ✗ | 模型类型
Path           | varchar  | 200 | ✓ | 文件路径
Version        | bigint   | 8   | ✗ | 版本号码
Enabled        | bool     | -   | ✗ | 是否启用
Creation       | datetime | -   | ✗ | 创建时间
Modification   | datetime | -   | ✓ | 修改时间
Description    | nvarchar | 500 | ✓ | 描述信息
