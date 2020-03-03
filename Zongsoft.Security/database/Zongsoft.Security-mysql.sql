SET NAMES utf8;
SET TIME_ZONE='+08:00';

CREATE TABLE IF NOT EXISTS `Security_Role` (
  `RoleId` int unsigned NOT NULL COMMENT '主键，角色编号',
  `Namespace` VARCHAR(50) NULL COMMENT '角色所属的命名空间，该字段表示应用或组织机构的标识',
  `Name` VARCHAR(50) NOT NULL COMMENT '角色名称，该名称在所属命名空间内具有唯一性',
  `FullName` VARCHAR(50) NULL COMMENT '角色全称',
  `Description` VARCHAR(500) NULL COMMENT '描述信息',
  PRIMARY KEY (`RoleId`),
  UNIQUE INDEX `UX_Security_Role_Name` (`Namespace`, `Name`)
) ENGINE = InnoDB DEFAULT CHARSET=utf8 COMMENT='角色表';

CREATE TABLE IF NOT EXISTS `Security_User` (
  `UserId` int unsigned NOT NULL COMMENT '主键，用户编号',
  `Namespace` VARCHAR(50) NULL COMMENT '用户所属的命名空间，该字段表示应用或组织机构的标识',
  `Name` VARCHAR(50) NOT NULL COMMENT '用户名称，该名称在所属命名空间内具有唯一性',
  `FullName` VARCHAR(50) NULL COMMENT '用户全称',
  `Password` BINARY(64) NULL COMMENT '用户的登录口令',
  `PasswordSalt` bigint unsigned NULL COMMENT '口令加密向量(随机数)',
  `Email` VARCHAR(50) NULL COMMENT '用户的电子邮箱，该邮箱地址在所属命名空间内具有唯一性',
  `Phone` VARCHAR(50) NULL COMMENT '用户的手机号码，该手机号码在所属命名空间内具有唯一性',
  `Status` TINYINT unsigned NOT NULL DEFAULT 1 COMMENT '用户状态（0:正常; 1:待批准; 2:已停用）',
  `StatusTimestamp` DATETIME NULL COMMENT '状态更新时间',
  `PasswordQuestion1` VARCHAR(50) NULL COMMENT '用户的密码问答的题面(1)',
  `PasswordAnswer1` VARBINARY(64) NULL COMMENT '用户的密码问答的答案(1)',
  `PasswordQuestion2` VARCHAR(50) NULL COMMENT '用户的密码问答的题面(2)',
  `PasswordAnswer2` VARBINARY(64) NULL COMMENT '用户的密码问答的答案(2)',
  `PasswordQuestion3` VARCHAR(50) NULL COMMENT '用户的密码问答的题面(3)',
  `PasswordAnswer3` VARBINARY(64) NULL COMMENT '用户的密码问答的答案(3)',
  `Creation` DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP COMMENT '创建时间',
  `Modification` DATETIME NULL COMMENT '最后修改时间',
  `Description` VARCHAR(500) NULL COMMENT '描述信息',
  PRIMARY KEY (`UserId`),
  UNIQUE INDEX `UX_Security_User_Name` (`Namespace`, `Name`),
  UNIQUE INDEX `UX_Security_User_Email` (`Namespace`, `Email`),
  UNIQUE INDEX `UX_Security_User_Phone` (`Namespace`, `Phone`)
) ENGINE = InnoDB DEFAULT CHARSET=utf8 COMMENT='用户表';

CREATE TABLE IF NOT EXISTS `Security_Member` (
  `RoleId` int unsigned NOT NULL COMMENT '主键，角色编号',
  `MemberId` int unsigned NOT NULL COMMENT '主键，成员编号',
  `MemberType` TINYINT unsigned NOT NULL COMMENT '主键，成员类型',
  PRIMARY KEY (`RoleId`, `MemberId`, `MemberType`)
) ENGINE = InnoDB DEFAULT CHARSET=utf8 COMMENT='角色成员表';

CREATE TABLE IF NOT EXISTS `Security_Permission` (
  `MemberId` int unsigned NOT NULL COMMENT '主键，成员编号',
  `MemberType` TINYINT unsigned NOT NULL COMMENT '主键，成员类型',
  `SchemaId` VARCHAR(50) NOT NULL COMMENT '主键，授权目标的标识',
  `ActionId` VARCHAR(50) NOT NULL COMMENT '主键，授权行为的标识',
  `Granted` TINYINT(1) NOT NULL COMMENT '是否授权(0: 表示拒绝; 1: 表示授予)',
  PRIMARY KEY (`MemberId`, `MemberType`, `SchemaId`, `ActionId`)
) ENGINE = InnoDB DEFAULT CHARSET=utf8 COMMENT='权限表';

CREATE TABLE IF NOT EXISTS `Security_PermissionFilter` (
  `MemberId` int unsigned NOT NULL COMMENT '主键，成员编号',
  `MemberType` TINYINT unsigned NOT NULL COMMENT '主键，成员类型',
  `SchemaId` VARCHAR(50) NOT NULL COMMENT '主键，授权目标的标识',
  `ActionId` VARCHAR(50) NOT NULL COMMENT '主键，授权行为的标识',
  `Filter` VARCHAR(4000) NOT NULL COMMENT '拒绝授权的过滤表达式',
  PRIMARY KEY (`MemberId`, `MemberType`, `SchemaId`, `ActionId`)
) ENGINE = InnoDB DEFAULT CHARSET=utf8 COMMENT='权限过滤表';

CREATE TABLE IF NOT EXISTS `Security_Censorship` (
  `Name` VARCHAR(50) NOT NULL COMMENT '主键，审查类名',
  `Word` VARCHAR(50) NOT NULL COMMENT '主键，阻止词汇',
  PRIMARY KEY (`Name`, `Word`)
) ENGINE = InnoDB DEFAULT CHARSET=utf8 COMMENT='词汇审查表';


/* 添加系统内置角色 */
INSERT INTO Security_Role (RoleId, Name, FullName, Description) VALUES (1, 'Administrators', '系统管理', '系统管理角色(系统内置角色)');
INSERT INTO Security_Role (RoleId, Name, FullName, Description) VALUES (2, 'Security', '安全管理', '安全管理角色(系统内置角色)');

/* 添加系统内置用户 */
INSERT INTO Security_User (UserId, Name, FullName, Description, Status) VALUES (1, 'Administrator', '系统管理员', '系统管理员(系统内置帐号)', 0);
INSERT INTO Security_User (UserId, Name, FullName, Description, Status) VALUES (2, 'Guest', '来宾', '来宾', 1);

/* 添加系统内置保留名字 */
INSERT INTO Security_Censorship (Name, Word) VALUES ('Names', 'Zongsoft');

/* 添加非法关键字 */
INSERT INTO Security_Censorship (Name, Word) VALUES ('Sensitives', 'fuck');
INSERT INTO Security_Censorship (Name, Word) VALUES ('Sensitives', 'bitch');
