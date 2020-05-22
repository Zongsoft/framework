# Zongsoft.Security 安全管理 API 接口手册

[TOC]

## 获取验证码
```url
[GET] /Security/Authentication/Secret/{phone}
[GET] /Security/Authentication/Secret/{namespace}:{phone}
[GET] /Security/Authentication/Secret/{email}
[GET] /Security/Authentication/Secret/{namespace}:{email}
```

### 参数说明
- phone 表示通过手机短信的方式获取验证码，之后即可通过该验证码进行身份验证（登录、注册等）；
- email 表示通过电子邮件的方式获取验证码，之后即可通过该验证码进行身份验证（登录、注册等）；
- namespace 可选参数，表示手机号或邮箱地址所属的命名空间，如果不设置则表示当前用户所属命名空间。

-----

## 登录接口
```url
[POST] /Security/Authentication/Signin/{scenario}
```

### 参数说明

- scenario
  > 应用场景，为了避免相同账号登录导致的互斥而指定的场景标识。譬如：
  > - `web` 网页端；
  > - `mobile` 移动端；
  > - `wechat` 微信端。

### 请求参数

```json
{
	Identity:"<name|phone|email>",
	Password:"******",
	Secret:"******",
	Namespace:"",
	Parameters:{}
}
```

#### 字段说明

- `Identity` 字段：必选项，表示登录账号名称（用户名）或手机号、邮箱地址这三种用户标识。
- `Password` 字段：必选项，表示用户的登录密码，该字段与 `Secret` 字段相斥。
- `Secret` 字段：可选项，表示用户的登录验证码，该字段与 `Password` 字段相斥，需要先调用“验证码API”获得该字段值。
- `Namespace` 字段：可选项，表示用户所属命名空间，不同业务系统对该字段的定义可能存在差异，通常在 SaaS 系统中，该字段表示租户的唯一代码。
- `Parameters` 字段：可选项，表示业务系统中需要传入的额外附加参数，该JSON实体将以键值对的方式保存在凭证中。

### 响应消息
```json
{
    "CredentialId": "1643429050478TWZBK",
    "RenewalToken": "17447225E75367F8SN315",
    "Scenario": "web",
    "Expiration": "04:00:00",
    "Identity": {
        "UserId": 100,
        "Name": "Popeye",
        "FullName": "钟少",
        "Namespace": "zongsoft",
        "Description": "钟峰",
        "Status": "active",
        "Creation": "2020-05-12T23:33:51",
        "Properties": {
            "Roles": [
                "Administrators",
                "Managers",
                "Users"
            ],
            "Authorizations": [
                "ASSET:CREATE,GET,UPDATE",
                "TASK:CREATE,GET,UPDATE"
            ]
        }
    },
    "Identities": []
}
```

-----

## 注销接口
```url
[POST] /Security/Authentication/Signout
```

### 参数说明
- 必须带有 `Authorization` 身份授权头。

-----

## 续约凭证

根据指定的续约票根重新续约。

```url
[POST] /Security/Authentication/Renew/{renewalToken}
```

### 响应消息
如果续约成功则返回预约的新凭证对象，可参考“登录(Signin)”操作的响应消息内容。

-----

## 获取角色信息
```url
[GET] /Security/Roles
[GET] /Security/Roles/{roleId}
[GET] /Security/Roles/{name}
[GET] /Security/Roles/{namespace}:{name}
```

### 参数说明
- `roleId` 指定的角色编号。
- `namespace` 表示角色所属的命名空间，如果未指定则表示当前用户命名空间。
- `name` 表示角色名称，在带 `namespace` 参数时，星号`*`则可以查询指定命名空间中的所有角色。

### 响应消息
根据参数类型，返回单个角色实体或多个角色实体，具体角色实体定义请参考“表结构设计”相关文档。

-----

## 删除角色（单个）
```url
[DELETE] /Security/Roles/{roleId}
```

### 参数说明
- `roleId` 指定的要删除的单个角色编号。

-----

## 删除角色（多个）
```url
[DELETE] /Security/Roles
```

### 内容说明
内容类型为纯文本(`text/plain`)，内容格式为以逗号分隔的编号。

-----

## 新增角色
```url
[POST] /Security/Roles
```

### 请求消息
```json
{
	Name:"",
	FullName:"",
	Namespace:"",
	Description:""
}
```

#### 字段说明
除了 `Name` 字段（属性）以外都是可选字段。

### 响应消息
返回新增成功的角色实体，具体角色实体定义请参考“表结构设计”相关文档。

-----

## 修改角色特定属性值
```url
[PATCH] /Security/Roles/{roleId}/Name
[PATCH] /Security/Roles/{roleId}/FullName
[PATCH] /Security/Roles/{roleId}/Namespace
[PATCH] /Security/Roles/{roleId}/Description
```

### 参数说明
- `roleId` 表示要修改的角色编号；

### 内容说明
内容类型为纯文本(`text/plain`)。


-----

## 判断角色是否存在

```url
[GET] /Security/Roles/{roleId}/Exists
[GET] /Security/Roles/Exits/{name}
[GET] /Security/Roles/Exits/{namespace}:{name}
```

### 参数说明
- `roleId` 指定的角色编号，通过角色编号进行查询必须带有 `Authorization` 身份授权头。
- `namespace` 表示角色所属的命名空间，如果未指定则表示当前用户命名空间。
- `name` 表示角色名称。

### 响应消息
- `204` No Content
  > 查找成功。
- `404` Not Found
  > 查找失败。

-----

## 获取角色的父角色集

获取指定角色所属的父级角色集。

```url
[GET] /Security/Roles/{roleId}/Roles
```

### 参数说明
`roleId` 必须项，要获取的角色编号。

### 响应消息
返回多个角色实体，具体角色实体定义请参考“表结构设计”相关文档。

-----

## 获取角色的成员集

获取指定角色的成员集。

```url
[GET] /Security/Roles/{roleId}/Members
```

### 参数说明
`roleId` 必须项，要获取的角色编号。

### 请求头
```json
x-data-schema: *, MemberUser{*}, MemberRole{*}
```

> 可以通过指定 HTTP 的 x-data-schema 头来定义返回的角色成员包含的导航属性内容，更多内容请参考数据引擎的 schema 定义。

### 响应消息
```json
[
	{
	    "RoleId": 10,
	    "MemberId": 110,
	    "MemberType": 0,
	    "MemberUser": null,
	    "MemberRole": null
	}
]
```

-----

## 设置单个角色成员
```url
[PUT] /Security/Roles/{roleId}/Member/user:{memberId}
[PUT] /Security/Roles/{roleId}/Member/role:{memberId}
```

### 参数说明
- `roleId` 表示要设置的角色编号；
- `memberId` 表示要设置的成员编号，通过成员编号前面的成员类型来标定其对应的成员。

-----

## 设置多个角色成员
```url
[PUT] /Security/Roles/{roleId}/Members
```

### 参数说明
- `roleId` 表示要设置的角色编号。

### 请求消息内容
```json
[
	{
		"MemberId":100,
		"MemberType":"User"
	},
	{
		"MemberId":200,
		"MemberType":"Role"
	}
]
```

-----

## 删除单个角色成员
```url
[DELETE] /Security/Roles/{roleId}/Member/user:{memberId}
[DELETE] /Security/Roles/{roleId}/Member/role:{memberId}
```

### 参数说明
- `roleId` 表示要删除的角色编号；
- `memberId` 表示要删除的成员编号，通过成员编号前面的成员类型来标定其对应的成员。

-----

## 清空指定角色下的成员集
```url
[DELETE] /Security/Roles/{roleId}/Members
```

### 参数说明
- `roleId` 表示要清空的角色编号。

-----

## 获取角色授权状态集

获取指定角色具有授权的状态清单。

```url
[GET] /Security/Roles/{roleId}/Authorizes
```

### 参数说明
`roleId` 必须项，要获取的角色编号。

### 响应消息
```json
[
	"schemaId:actionId,actionId,...",
	"Asset:Get,Create,Delete,Update"
]
```

-----

## 获取角色权限设置集

获取指定角色的权限设置集。

```url
[GET] /Security/Roles/{roleId}/Permissions
[GET] /Security/Roles/{roleId}/Permissions/{schema}
```

### 参数说明
`roleId` 必须项，要获取的角色编号。
`schema` 可选项，要获取的目标标识，如果未指定则获取所有目标授权对象。

### 响应消息
```json
[
	"schemaId":["actionId:true", "actionId:false"],
	"Asset":["Get:true", "Delete:false", "Create:true", "Update:true"]
]
```

-----

## 设置角色权限设置集

设置指定角色的权限设置集。

```url
[PUT] /Security/Roles/{roleId}/Permissions
[PUT] /Security/Roles/{roleId}/Permissions/{schema}
```

### 参数说明
`roleId` 必须项，要设置的角色编号。
`schema` 可选项，要设置的目标标识，如果不为空则请求实体中的 `SchemaId` 将会被强制更新为该参数值。

### 请求消息
```json
[
	{
		"SchemaId":"",
		"ActionId":"",
		"Granted":true
	},
	{
		"SchemaId":"",
		"ActionId":"",
		"Granted":false
	}
]
```

-----

## 删除单条角色权限

```url
[DELETE] /Security/Roles/{roleId}/Permission/{schemaId}:{actionId}
```

### 参数说明
`roleId` 必须项，要设置的角色编号。
`schemaId` 必须项，要设置的目标标识。
`actionId` 必须项，要设置的操作标识。

## 删除多条角色权限

```url
[DELETE] /Security/Roles/{roleId}/Permissions
```

### 参数说明
`roleId` 必须项，要设置的角色编号。

### 内容说明
内容类型为纯文本(`text/plain`)，内容格式为以逗号分隔的编号。

-----


## 获取用户信息
```url
[GET] /Security/Users
[GET] /Security/Users/{userId}
[GET] /Security/Users/{identity}
[GET] /Security/Users/{namespace}:{identity}
```

### 参数说明
- `userId` 指定的用户编号。
- `namespace` 表示用户所属的命名空间，如果未指定则表示当前用户命名空间。
- `identity` 表示用户标识，可以是用户名、绑定的手机号、Email邮箱地址。在带 `namespace` 参数时，星号`*`则可以查询指定命名空间中的所有用户。

### 响应消息
根据参数类型，返回单个用户实体或多个用户实体，具体用户实体定义请参考“表结构设计”相关文档。

-----

## 删除用户（单个）
```url
[DELETE] /Security/Users/{userId}
```

### 参数说明
- `userId` 指定的要删除的单个用户编号。

-----

## 删除用户（多个）
```url
[DELETE] /Security/Users
```

### 内容说明
内容类型为纯文本(`text/plain`)，内容格式为以逗号分隔的编号。

-----

## 新增用户
```url
[POST] /Security/Users
```

### 参数说明
可以通过名为 `x-password` 的 HTTP 头来定义新增用户的密码，如果未指定该扩展头，则由系统生成特定密码或空密码。

### 请求消息
```json
{
	"Name":"",
	"FullName":"",
	"Namespace":"",
	"Email":"",
	"Phone":"",
	"Description":""
}
```

#### 字段说明
除了 `Name` 字段（属性）以外都是可选字段。

### 响应消息
返回新增成功的用户实体，具体用户实体定义请参考“表结构设计”相关文档。

-----

## 修改用户特定属性值
```url
[PATCH] /Security/Users/{userId}/Name
[PATCH] /Security/Users/{userId}/FullName
[PATCH] /Security/Users/{userId}/Namespace
[PATCH] /Security/Users/{userId}/Email
[PATCH] /Security/Users/{userId}/Phone
[PATCH] /Security/Users/{userId}/Status/{value}
[PATCH] /Security/Users/{userId}/Description
```

### 参数说明
- `userId` 表示要修改的用户编号；
- `value` 表示要修改的用户的新属性值。

### 内容说明
内容类型为纯文本(`text/plain`)。

-----

## 判断用户是否存在

```url
[GET] /Security/Users/{userId}/Exists
[GET] /Security/Users/Exits/{identity}
[GET] /Security/Users/Exits/{namespace}:{identity}
```

### 参数说明
- `userId` 指定的用户编号，通过用户编号进行查询必须带有 `Authorization` 身份授权头。
- `namespace` 表示用户所属的命名空间，如果未指定则表示当前用户命名空间。
- `identity` 表示用户标识，可以是用户名、绑定的手机号、Email邮箱地址。

### 响应消息
- `204` No Content
  > 查找成功。
- `404` Not Found
  > 查找失败。

-----

## 校验验证码

**注意：** 该接口支持匿名调用，即不需要提供 `Authorization` 验证头。

```url
[GET] /Security/Users/{userId}/Verify/{type}?secret=xxx
```

### 参数说明
- `userId` 必须项，指定要校验的用户编号。
- `type` 必须项，表示校验的类型，由业务方定义，譬如：`forget-password`、`register` 等。
- `secret` 必须项，表示要校验的秘密值，通常为通过手机短信或电子邮件收到的一个随机数验证码。

### 响应消息
- `204` No Content
  > 校验成功。
- `400` Bad Request
  > 查找失败。

-----

## 判断用户是否有密码

```url
[GET] /Security/Users/{userId}/Password.Has
[GET] /Security/Users/Password.Has/{identity}
[GET] /Security/Users/Password.Has/{namespace}:{identity}
```

### 参数说明
- `userId` 指定的用户编号，通过用户编号进行查询必须带有 `Authorization` 身份授权头。
- `namespace` 表示用户所属的命名空间，如果未指定则表示当前用户命名空间。
- `identity` 表示用户标识，可以是用户名、绑定的手机号、Email邮箱地址。

-----

## 修改用户密码
```url
[PUT] /Security/Users/{userId}/Password.Change
```

### 请求消息
```json
{
	"OldPassword":"***",
	"NewPassword":"***"
}
```

-----

## 忘记用户密码

**注意：** 该接口支持匿名调用，即不需要提供 `Authorization` 验证头。

该方法会根据参数类型，通过相应的通道（手机短信或电子邮件）发送一个验证码到对应的手机或邮箱中。

```url
[POST] /Security/Users/Password.Forget/{phone}
[POST] /Security/Users/Password.Forget/{namespace}:{phone}
[POST] /Security/Users/Password.Forget/{email}
[POST] /Security/Users/Password.Forget/{namespace}:{email}
```

### 参数说明
- `phone` 表示通过手机短信的方式获取验证码，之后即可通过该验证码重置密码；
- `email` 表示通过电子邮件的方式获取验证码，之后即可通过该验证码重置密码；
- `namespace` 可选参数，表示手机号或邮箱地址所属的命名空间，如果不设置则表示当前用户所属命名空间。

### 响应消息
发送成功的用户编号。

-----

## 重置用户密码（验证码）

**注意：** 该接口支持匿名调用，即不需要提供 `Authorization` 验证头。

```url
[POST] /Security/Users/{userId}/Password.Reset
```

### 参数说明
- `userId` 指定要重置的用户编号。

### 请求消息
```json
{
	"Secret":"xxxx",
	"Password":"***"
}
```

#### 字段说明
- `secret` 通过手机短信或电子邮件获取到的验证码；
- `password` 要重置的新密码。


-----

## 重置用户密码（密码问答）

**注意：** 该接口支持匿名调用，即不需要提供 `Authorization` 验证头。

```url
[POST] /Security/Users/{phone}/Password/Reset
[POST] /Security/Users/{namespace}:{phone}/Password/Reset
[POST] /Security/Users/{email}/Password/Reset
[POST] /Security/Users/{namespace}:{email}/Password/Reset
```

### 参数说明
- `phone` 表示要重置密码的用户手机号码；
- `email` 表示要重置密码的用户邮箱地址；
- `namespace` 可选参数，表示手机号或邮箱地址所属的命名空间。

### 请求消息
```json
{
	"Password":"***",
	"PasswordAnswers":["answer1", "answer2", "answer3"]
}
```

#### 字段说明
- `passwordAnswers` 用户信息中密码问答中的三个答案值（必须按设置中的顺序）。
- `password` 要重置的新密码。


-----

## 获取用户密码问题题面

```url
[GET] /Security/Users/{userId}/Password.Questions
[GET] /Security/Users/Password.Questions/{identity}
[GET] /Security/Users/Password.Questions/{namespace}:{identity}
```

### 参数说明
- `userId` 指定的用户编号，通过用户编号进行查询必须带有 `Authorization` 身份授权头。
- `namespace` 表示用户所属的命名空间，如果未指定则表示当前用户命名空间。
- `identity` 表示用户标识，可以是用户名、绑定的手机号、Email邮箱地址。

### 返回消息
```json
["question1", "question2", "question3"]
```

-----

## 设置用户密码问题

```url
[PUT] /Security/Users/{userId}/Password.Answers
```

### 参数说明
- `userId` 必须项，要设置的用户编号。

### 请求消息
```json
{
	"Password":"***",
	"Questions":["question1", "question2", "question3"],
	"Answers":["answer1", "answer2", "answer3"]
}
```

#### 字段说明
- `password` 指定的用户编号参数对应的用户密码，因为要设置密码问答必须先验证指定用户的密码。
- `questions` 要更新的密码问答的三个题面（注意：与答案顺序一致）。
- `answers` 要更新的密码问答的三个答案（注意：与题面顺序一致）。

-----

## 获取用户的父角色集

获取指定用户所属的父级角色集。

```url
[GET] /Security/Users/{userId}/Roles
```

### 参数说明
`userId` 必须项，要获取的用户编号。

### 响应消息
返回多个角色实体，具体角色实体定义请参考“表结构设计”相关文档。

-----

## 判断指定的用户是否属于指定角色

```url
[GET] /Security/Users/{userId}/In/{roleName}[,...]
```

### 参数说明
`userId` 必须项，要判断的用户编号。
`roleName` 必须项，要判断的角色名称，可以指定多个角色名，名称之间使用逗号分隔。

### 响应消息
- `204` No Content
  > 指定的用户属于指定角色集之一。
- `404` Not Found
  > 指定的用户不属于指定的所有角色。


-----

## 授权判断

判断指定的用户是否对于具有操作指定目标的授权。

```url
[GET] /Security/Users/{userId}/Authorize/{schema}:{action}
```

### 参数说明
`userId` 必须项，要判断的用户编号。
`schema` 必须项，要判断的目标标识。
`action` 必须项，要判断的操作标识。

### 响应消息
如果具有授权则响应状态码为204，否则状态码为400。
- `204` No Content
  > 具有授权。
- `400` Bad Request
  > 没有被授权。

-----

## 获取用户授权状态集

获取指定用户具有授权的状态清单。

```url
[GET] /Security/Users/{userId}/Authorizes
```

### 参数说明
`userId` 必须项，要获取的用户编号。

### 响应消息
```json
[
    "ASSET:CREATE,GET,UPDATE",
    "TASK:CREATE,GET,UPDATE"
]
```

-----

## 获取用户权限设置集

获取指定用户的权限设置集。

```url
[GET] /Security/Users/{userId}/Permissions
[GET] /Security/Users/{userId}/Permissions/{schema}
```

### 参数说明
`userId` 必须项，要获取的用户编号。
`schema` 可选项，要获取的目标标识，如果未指定则获取所有目标授权对象。

### 响应消息
```json
[
	"schemaId":["actionId:true", "actionId:false"],
	"Asset":["Get:true", "Delete:false", "Create:true", "Update:true"]
]
```

-----

## 设置用户权限设置集

设置指定用户的权限设置集。

```url
[PUT] /Security/Users/{userId}/Permissions/
[PUT] /Security/Users/{userId}/Permissions/{schema}
```

### 参数说明
`userId` 必须项，要设置的用户编号。
`schema` 可选项，要设置的目标标识，如果不为空则请求实体中的 `SchemaId` 将会被强制更新为该参数值。

### 请求消息
```json
[
	{
		"SchemaId":"",
		"ActionId":"",
		"Granted":true
	},
	{
		"SchemaId":"",
		"ActionId":"",
		"Granted":false
	}
]
```


## 删除单条用户权限

```url
[DELETE] /Security/Users/{userId}/Permission/{schemaId}:{actionId}
```

### 参数说明
`userId` 必须项，要设置的用户编号。
`schemaId` 必须项，要设置的目标标识。
`actionId` 必须项，要设置的操作标识。

## 删除多条用户权限

```url
[DELETE] /Security/Users/{userId}/Permissions
```

### 参数说明
`userId` 必须项，要设置的用户编号。

### 内容说明
内容类型为纯文本(`text/plain`)，内容格式为以逗号分隔的编号。

-----

