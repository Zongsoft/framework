# Zongsoft.Security 数据库表结构定义

## 用户表 `User`

字段名称 | 数据类型 | 长度 | 可空 | 备注
------- |:-------:|:---:|:---:| ----
UserId            | int       | 4   | ✘ | 主键，用户编号
Namespace         | varchar   | 50  | ✔ | 用户所属的命名空间，该字段表示应用或组织机构的标识
Name              | varchar   | 50  | ✘ | 用户的名称，该名称在所属命名空间内具有唯一性
FullName          | nvarchar  | 50  | ✔ | 用户的全称
Password          | varbinary | 64  | ✔ | 用户登录口令
PasswordSalt      | bigint    | 8   | ✔ | 口令随机数
Email             | varchar   | 50  | ✔ | 用户的电子邮箱，该邮箱地址在所属命名空间内具有唯一性
Phone             | varchar   | 50  | ✔ | 用户的手机号码，该手机号码在所属命名空间内具有唯一性
Status            | byte      | 1   | ✘ | 用户状态 _(0:正常; 1:待批准; 2:已禁用)_
StatusTimestamp   | datetime  | -   | ✔ | 状态更改时间
PasswordQuestion1 | nvarchar  | 50  | ✔ | 用户的密码问答的题面[1]
PasswordAnswer1   | varbinary | 64  | ✔ | 用户的密码问答的答案[1] _(哈希值)_
PasswordQuestion2 | nvarchar  | 50  | ✔ | 用户的密码问答的题面[2]
PasswordAnswer2   | varbinary | 64  | ✔ | 用户的密码问答的答案[2] _(哈希值)_
PasswordQuestion3 | nvarchar  | 50  | ✔ | 用户的密码问答的题面[3]
PasswordAnswer3   | varbinary | 64  | ✔ | 用户的密码问答的答案[3] _(哈希值)_
Creation          | datetime  | -   | ✘ | 创建时间
Modification      | datetime  | -   | ✔ | 最后的修改时间
Description       | nvarchar  | 500 | ✔ | 描述信息


## 角色表 `Role`

字段名称 | 数据类型 | 长度 | 可空 | 备注
------- |:-------:|:---:|:---:| ----
RoleId      | int      | 4   | ✘ | 主键，角色编号
Namespace   | varchar  | 50  | ✔ | 角色所属的命名空间，该字段表示应用或组织机构的标识
Name        | varchar  | 50  | ✘ | 角色的名称，该名称在所属命名空间内具有唯一性
FullName    | nvarchar | 50  | ✔ | 角色的全称
Description | nvarchar | 500 | ✔ | 描述信息


## 成员表 `Member`

字段名称 | 数据类型 | 长度 | 可空 | 备注
------- |:-------:|:---:|:---:| ----
RoleId     | int  | 4 | ✘ | 主键，角色的编号
MemberId   | int  | 4 | ✘ | 主键，用户或角色的编号
MemberType | byte | 1 | ✘ | 主键，成员类型 _(0:用户; 1:角色)_


## 权限表 `Permission`

字段名称 | 数据类型 | 长度 | 可空 | 备注
------- |:-------:|:---:|:---:| ----
MemberId   | int     | 4  | ✘ | 主键，用户或角色编号
MemberType | byte    | 1  | ✘ | 主键，目标类型 _(0:用户; 1:角色)_
SchemaId   | varchar | 50 | ✘ | 主键，授权目标的标识
ActionId   | varchar | 50 | ✘ | 主键，授权行为的标识
Granted    | bool    | -  | ✘ | 是否授权

> 注：“`Granted`”字段表示该用户对指定的系统对象对应的权限字的保护类型，定义如下：
> - **真**：表示用户或角色对指定的系统对象拥有该权限；
> - **假**：表示用户或角色对指定的系统对象拒绝该权限。


## 权限过滤表 `PermissionFilter`

字段名称 | 数据类型 | 长度 | 可空 | 备注
------- |:-------:|:---:|:---:| ----
MemberId   | int     | 4   | ✘ | 主键，用户或角色编号
MemberType | byte    | 1   | ✘ | 主键，目标类型 _(0:用户; 1:角色)_
SchemaId   | varchar | 50  | ✘ | 主键，授权目标的标识
ActionId   | varchar | 50  | ✘ | 主键，授权行为的标识
Filter     | varchar | 500 | ✘ | 数据模式表达式

-----

## 其它说明

假设有角色 **M**arket 和用户 **J**ack，其中 **J**ack 是 **M**arket 角色的成员之一，它们的权限定义如下：

1. **M**arket 角色在 `Permission` 授权表中对应的记录有：
```json
{
	"MemberId":100, /* 即 Market 角色的编号 */
	"MemberType":1, /* 角色 */
	"SchemaId": "SaleOrder",
	"ActionId": "Select",
	"Granted": true
}
```

2. **J**ack 用户在 `PermissionFilter` 授权过滤表中对应的记录有：
```json
{
	"MemberId":1001, /* 即 Jack 用户的编号 */
	"MemberType":0,  /* 用户 */
	"SchemaId": "SaleOrder",
	"ActionId": "Select",
	"Filter": "!Amount,!Details.Price,!Details.Discount,!Details.Quantity"
}
```

以上两个表中的数据指示出 **J**ack 默认拥有 `Select`_（读取）_`SaleOrder`_（销售订单）_ 资源的权限 _（继承自 **M**arket 角色的授权）_，但是他却被拒绝 `Select`_（读取）_`SalesOrder`_（销售订单）_ 资源中如下字段：

- `SaleOrder.Amount` 销售订单总金额
- `SaleOrderDetail.Price` 单项单价
- `SaleOrderDetail.Discount` 单项折扣
- `SaleOrderDetail.Quantity` 单项数量
