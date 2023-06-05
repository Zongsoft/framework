# Zongsoft.Security 数据库表结构定义

## 用户表 `User`

字段名称 | 数据类型 | 长度 | 可空 | 备注
------- |:-------:|:---:|:---:| ----
UserId            | int       | 4   | ✗ | 主键，用户编号
Namespace         | varchar   | 50  | ✓ | 命名空间(_表示对应的组织机构标识_)
Name              | varchar   | 50  | ✗ | 用户名称(_在所属命名空间内具有唯一性_)
FullName          | nvarchar  | 50  | ✓ | 用户全称
Password          | varbinary | 64  | ✓ | 登录密码
PasswordSalt      | bigint    | 8   | ✓ | 密码盐(_随机数_)
Email             | varchar   | 50  | ✓ | 绑定的电子邮箱(_在所属命名空间内具有唯一性_)
Phone             | varchar   | 50  | ✓ | 绑定的手机号码(_在所属命名空间内具有唯一性_)
Status            | byte      | 1   | ✗ | 用户状态(_0:正常; 1:待批准; 2:已禁用_)
StatusTimestamp   | datetime  | -   | ✓ | 状态更改时间
PasswordQuestion1 | nvarchar  | 50  | ✓ | 用户的密码问答的题面[1]
PasswordAnswer1   | varbinary | 64  | ✓ | 用户的密码问答的答案[1] (_哈希值_)
PasswordQuestion2 | nvarchar  | 50  | ✓ | 用户的密码问答的题面[2]
PasswordAnswer2   | varbinary | 64  | ✓ | 用户的密码问答的答案[2] (_哈希值_)
PasswordQuestion3 | nvarchar  | 50  | ✓ | 用户的密码问答的题面[3]
PasswordAnswer3   | varbinary | 64  | ✓ | 用户的密码问答的答案[3] (_哈希值_)
Creation          | datetime  | -   | ✗ | 创建时间
Modification      | datetime  | -   | ✓ | 最后修改时间
Description       | nvarchar  | 500 | ✓ | 描述信息


## 角色表 `Role`

字段名称 | 数据类型 | 长度 | 可空 | 备注
------- |:-------:|:---:|:---:| ----
RoleId      | int      | 4   | ✗ | 主键，角色编号
Namespace   | varchar  | 50  | ✓ | 命名空间(_表示对应的组织机构标识_)
Module      | varchar  | 50  | ✗ | 模块标识(_表示对应的业务模块标识_)
Name        | varchar  | 50  | ✗ | 角色名称(_在所属命名空间和模块内具有唯一性_)
FullName    | nvarchar | 50  | ✓ | 角色全称
Description | nvarchar | 500 | ✓ | 描述信息


## 成员表 `Member`

字段名称 | 数据类型 | 长度 | 可空 | 备注
------- |:-------:|:---:|:---:| ----
RoleId     | int  | 4 | ✗ | 主键，角色编号
MemberId   | int  | 4 | ✗ | 主键，用户或角色的编号
MemberType | byte | 1 | ✗ | 主键，成员类型(_0:用户; 1:角色_)


## 权限表 `Permission`

字段名称 | 数据类型 | 长度 | 可空 | 备注
------- |:-------:|:---:|:---:| ----
MemberId   | int     | 4  | ✗ | 主键，用户或角色编号
MemberType | byte    | 1  | ✗ | 主键，成员类型(_0:用户; 1:角色_)
SchemaId   | varchar | 50 | ✗ | 主键，授权目标的标识
ActionId   | varchar | 50 | ✗ | 主键，授权行为的标识
Granted    | bool    | -  | ✗ | 授权标记

> 注：授权标记(_`Granted`_)字段表示成员(_用户或角色_)对操作目标(_`Schema`_)是否具有指定操作(`Action`)的授权，定义如下：
> - **真**：表示用户或角色对指定目标拥有指定操作的授权；
> - **假**：表示用户或角色对指定目标没有指定操作的权限。


## 权限过滤表 `PermissionFilter`

字段名称 | 数据类型 | 长度 | 可空 | 备注
------- |:-------:|:---:|:---:| ----
MemberId   | int     | 4   | ✗ | 主键，用户或角色编号
MemberType | byte    | 1   | ✗ | 主键，成员类型(_0:用户; 1:角色_)
SchemaId   | varchar | 50  | ✗ | 主键，授权目标的标识
ActionId   | varchar | 50  | ✗ | 主键，授权行为的标识
Filter     | varchar | 500 | ✗ | 过滤表达式

-----

## 其它说明

### 权限计算

权限计算准则：**拒绝优先、就近优先**。以下为相关表中的范例数据：

#### 用户记录 `User`

UserId | Name | FullName
:-----:|:----:|---------
1001   | Jack | 杰克·马
1002   | Pony | 波尼·马

#### 角色记录 `Role`

RoleId | Name | FullName
:-----:|:----:|---------
101    | Users    | 普通用户
201    | Sales    | 销售人员
202    | Services | 客服人员

#### 成员关系 `Member`

RoleId | MemberId | MemberType
:-----:|:--------:|-----------
101    | 201      | `1`(_Role_) _表示：“销售人员”继承自“普通用户”_
101    | 202      | `1`(_Role_) _表示：“客服人员”继承自“普通用户”_
201    | 1001     | `0`(_User_) _表示：“Jack”属于“销售人员”_
201    | 1002     | `0`(_User_) _表示：“Pony”属于“销售人员”_
202    | 1002     | `0`(_User_) _表示：“Pony”属于“客服人员”_

#### 权限记录 `Permission`

MemberId | MemberType | SchemaId | ActionId | Granted
:-------:|:----------:|:--------:|:--------:|:------:
101      | `1`(_Role_) | Product   | Select  | ✓ (_True_)
101      | `1`(_Role_) | Feedback  | Select  | ✓ (_True_)
101      | `1`(_Role_) | SaleOrder | Select  | ✓ (_True_)
201      | `1`(_Role_) | Feedback  | Select  | ✗ (_False_)
201      | `1`(_Role_) | SaleOrder | Update  | ✓ (_True_)
202      | `1`(_Role_) | Feedback  | Update  | ✓ (_True_)
202      | `1`(_Role_) | SaleOrder | Select  | ✗ (_False_)
1001     | `0`(_User_) | Feedback  | Select  | ✓ (_True_)

-----

由以上数据计算所得用户 **J**ack(_`1001`_) 的最终权限集结构如下所示：

> - Product
> 	- Select：继承自“普通用户(`101`)”角色。
> - Feedback
> 	- Select：应用“**就近优先**”原则，虽然所属的“销售人员(`201`)”角色显式拒绝了该项操作，但是自己（最近）又显式授予了该项操作。
> - SaleOrder
> 	- Select：继承自“销售人员(`201`)”角色。
> 	- Update：继承自“销售人员(`201`)”角色。

下面是该用户的最终权限集数据的 JSON 表示

```json
[
	{
		"Schema":"Product",
		"Actions":["Select"]
	},
	{
		"Schema":"Feedback",
		"Actions":["Select"]
	},
	{
		"Schema":"SaleOrder",
		"Actions":["Select", "Update"]
	}
]
```

-----

由以上数据计算所得用户 **P**ony(_`1002`_) 的最终权限集结构如下所示：

> - Product
> 	- Select：继承自“普通用户(`101`)”角色。
> - Feedback
> 	- Select：继承自“客服人员(`202`)”角色。
> 	- ~~Update~~：虽然继承的“客服人员(`202`)”角色声明了该项授权，但是同层级的“销售人员(`201`)”角色却拒绝了该项授权，所以根据“**拒绝优先**”原则，该用户无该项操作的权限。
> - SaleOrder
> 	- Select：继承自“销售人员(`201`)”角色。
> 	- ~~Update~~：虽然继承的“销售人员(`201`)”角色声明了该项授权，但是同层级的“客服人员(`201`)”角色却拒绝了该项授权，所以根据“**拒绝优先**”原则，该用户无该项操作的权限。

下面是该用户的最终权限集数据的 JSON 表示

```json
[
	{
		"Schema":"Product",
		"Actions":["Select"]
	},
	{
		"Schema":"Feedback",
		"Actions":["Select"]
	},
	{
		"Schema":"SaleOrder",
		"Actions":["Select"]
	}
]
```


### 权限过滤

权限过滤是指对具有特定操作权限的目标资源进行字段过滤。承接上面的数据，并有如下“授权过滤”数据：

#### 权限过滤 `PermissionFilter`

MemberId | MemberType | SchemaId | ActionId | Filter
:-------:|:----------:|:--------:|:--------:|:------:
201      | `1`(_Role_) | SaleOrder | Select  | `!Amount,!Details.Price,!Details.Discount,!Details.Quantity`


综上所示，“销售人员(`201`)”角色虽然拥有对“销售订单(`SaleOrder`)”及其子资源的具有“查看(`Select`)”权限，但是却不包含对这些资源中的如下字段：

- `SaleOrder.Amount` 销售订单总金额
- `SaleOrderDetail.Price` 单项单价
- `SaleOrderDetail.Discount` 单项折扣
- `SaleOrderDetail.Quantity` 单项数量
