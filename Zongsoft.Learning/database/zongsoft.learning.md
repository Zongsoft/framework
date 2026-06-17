# Zongsoft.Learning 数据库表结构定义

## 数据集表 `Dataset`

字段名称 | 数据类型 | 长度 | 可空 | 备注
------- |:-------:|:---:|:---:| ----
DatasetId    | int      | 4   | ✗ | 主键，数据编号
Name         | varchar  | 50  | ✗ | 数据集名
Enabled      | bool     | -   | ✗ | 是否启用
Provider     | varchar  | 50  | ✗ | 提供程序
Settings     | varchar  | 200 | ✓ | 设置文本
Creation     | datetime | -   | ✗ | 创建时间
Modification | datetime | -   | ✓ | 修改时间
Description  | nvarchar | 500 | ✓ | 描述信息


## 数据字段表 `DatasetColumn`

字段名称 | 数据类型 | 长度 | 可空 | 备注
------- |:-------:|:---:|:---:| ----
DatasetId | int      | 4  | ✗ | 主键，数据编号
ColumnId  | smallint | 2  | ✗ | 主键，字段编号
Name      | varchar  | 50 | ✗ | 字段名称
Type      | byte     | 1  | ✗ | 字段类型
Index     | int      | 4  | ✗ | 索引序号


## 算法表 `Algorithm`

字段名称 | 数据类型 | 长度 | 可空 | 备注
------- |:-------:|:---:|:---:| ----
AlgorithmId  | int      | 4   | ✗ | 主键，算法编号
Name         | varchar  | 50  | ✗ | 算法名称
Enabled      | bool     | -   | ✗ | 是否启用
Provider     | varchar  | 50  | ✗ | 提供程序
Settings     | varchar  | 200 | ✓ | 设置文本
Creation     | datetime | -   | ✗ | 创建时间
Modification | datetime | -   | ✓ | 修改时间
Description  | nvarchar | 500 | ✓ | 描述信息


## 管线表 `Pipeline`

字段名称 | 数据类型 | 长度 | 可空 | 备注
------- |:-------:|:---:|:---:| ----
PipelineId       | int      | 4    | ✗ | 主键，管线编号
Name             | varchar  | 50   | ✗ | 管线名称
Settings         | varchar  | 200  | ✓ | 设置文本
Enabled          | bool     | -    | ✗ | 是否可用
Visible          | bool     | -    | ✗ | 是否可见
Creation         | datetime | -    | ✗ | 创建时间
Modification     | datetime | -    | ✓ | 修改时间
Description      | nvarchar | 500  | ✓ | 描述信息


## 管线数据表 `PipelineDataset`

字段名称 | 数据类型 | 长度 | 可空 | 备注
------- |:-------:|:---:|:---:| ----
PipelineId   | int      | 4   | ✗ | 主键，发布编号
DatasetId    | int      | 4   | ✗ | 主键，数据编号
Name         | varchar  | 50  | ✗ | 数据集名
Enabled      | bool     | -   | ✗ | 是否启用
Provider     | varchar  | 50  | ✗ | 提供程序
Settings     | varchar  | 200 | ✓ | 设置文本
Creation     | datetime | -   | ✗ | 创建时间
Modification | datetime | -   | ✓ | 修改时间
Description  | nvarchar | 500 | ✓ | 描述信息


## 管线数据字段表 `PipelineDatasetColumn`

字段名称 | 数据类型 | 长度 | 可空 | 备注
------- |:-------:|:---:|:---:| ----
PipelineId | int      | 4  | ✗ | 主键，发布编号
DatasetId  | int      | 4  | ✗ | 主键，数据编号
ColumnId   | smallint | 2  | ✗ | 主键，字段编号
Name       | varchar  | 50 | ✗ | 字段名称
Type       | byte     | 1  | ✗ | 字段类型
Index      | int      | 4  | ✗ | 索引序号


## 管线算法表 `PipelineAlgorithm`

字段名称 | 数据类型 | 长度 | 可空 | 备注
------- |:-------:|:---:|:---:| ----
PipelineId   | int      | 4   | ✗ | 主键，发布编号
AlgorithmId  | int      | 4   | ✗ | 主键，算法编号
Name         | varchar  | 50  | ✗ | 算法名称
Enabled      | bool     | -   | ✗ | 是否启用
Provider     | varchar  | 50  | ✗ | 提供程序
Settings     | varchar  | 200 | ✓ | 设置文本
Creation     | datetime | -   | ✗ | 创建时间
Modification | datetime | -   | ✓ | 修改时间
Description  | nvarchar | 500 | ✓ | 描述信息


## 训练表 `Training`

字段名称 | 数据类型 | 长度 | 可空 | 备注
------- |:-------:|:---:|:---:| ----
TrainingId  | int      | 4   | ✗ | 主键，训练编号
PipelineId  | int      | 4   | ✗ | 主键，管线编号
ModelId     | int      | 4   | ✓ | 模型编号
Settings    | varchar  | 200 | ✓ | 设置信息
StartTime   | datetime | -   | ✓ | 开始时间
FinalTime   | datetime | -   | ✓ | 结束时间
Description | nvarchar | 500 | ✓ | 描述信息


## 模型表 `Model`

字段名称 | 数据类型 | 长度 | 可空 | 备注
------- |:-------:|:---:|:---:| ----
ModelId      | int      | 4   | ✗ | 主键，模型编号
Name         | varchar  | 50  | ✗ | 模型名称
Size         | bigint   | 8   | ✗ | 模型大小
Type         | varchar  | 50  | ✗ | 模型类型
Path         | varchar  | 200 | ✓ | 文件路径
Enabled      | bool     | -   | ✗ | 是否启用
Creation     | datetime | -   | ✗ | 创建时间
Modification | datetime | -   | ✓ | 修改时间
Description  | nvarchar | 500 | ✓ | 描述信息
