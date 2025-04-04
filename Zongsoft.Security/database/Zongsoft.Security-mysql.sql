SET NAMES utf8mb4;
SET TIME_ZONE='+08:00';

CREATE TABLE IF NOT EXISTS `Security_Role` (
  `RoleId`      int unsigned NOT NULL COMMENT '主键，角色编号',
  `Namespace`   varchar(50)  NULL     COMMENT '命名空间，表示应用或组织机构的标识' COLLATE 'ascii_general_ci',
  `Name`        varchar(50)  NOT NULL COMMENT '角色名称，所属命名空间内具有唯一性' COLLATE 'utf8mb4_0900_ai_ci',
  `Avatar`      varchar(100  NULL     COMMENT '角色头像' COLLATE 'utf8mb4_0900_ai_ci',
  `Enabled`     tinyint(1)   NOT NULL COMMENT '是否启用' DEFAULT 1,
  `Nickname`    varchar(50)  NULL     COMMENT '角色昵称' COLLATE 'utf8mb4_0900_ai_ci',
  `Description` varchar(500) NULL     COMMENT '描述信息' COLLATE 'utf8mb4_0900_ai_ci',
  PRIMARY KEY (`RoleId`),
  UNIQUE INDEX `UX_Security_Role_Name` (`Namespace`, `Name`)
) ENGINE = InnoDB COMMENT='角色表';

CREATE TABLE IF NOT EXISTS `Security_User` (
  `UserId`            int unsigned     NOT NULL COMMENT '主键，用户编号',
  `Namespace`         varchar(50)      NULL     COMMENT '命名空间，表示应用或组织机构的标识' COLLATE 'ascii_general_ci',
  `Name`              varchar(50)      NOT NULL COMMENT '用户名称，所属命名空间内具有唯一性' COLLATE 'utf8mb4_0900_ai_ci',
  `Avatar`            varchar(100)     NULL     COMMENT '用户头像' COLLATE 'utf8mb4_0900_ai_ci',
  `Nickname`          varchar(50)      NULL     COMMENT '用户昵称' COLLATE 'utf8mb4_0900_ai_ci',
  `Password`          varbinary(64)    NULL     COMMENT '用户的登录口令',
  `PasswordSalt`      bigint unsigned  NULL     COMMENT '口令加密向量(随机数)',
  `Email`             varchar(50)      NULL     COMMENT '用户的电子邮箱，该邮箱地址在所属命名空间内具有唯一性' COLLATE 'ascii_general_ci',
  `Phone`             varchar(50)      NULL     COMMENT '用户的手机号码，该手机号码在所属命名空间内具有唯一性' COLLATE 'ascii_general_ci',
  `Gender`            tinyint(1)       NULL     COMMENT '用户性别',
  `Enabled`           tinyint(1)       NOT NULL COMMENT '是否启用' DEFAULT 1,
  `PasswordQuestion1` varchar(50)      NULL     COMMENT '用户的密码问答的题面(1)' COLLATE 'utf8mb4_0900_ai_ci',
  `PasswordAnswer1`   varbinary(64)    NULL     COMMENT '用户的密码问答的答案(1)',
  `PasswordQuestion2` varchar(50)      NULL     COMMENT '用户的密码问答的题面(2)' COLLATE 'utf8mb4_0900_ai_ci',
  `PasswordAnswer2`   varbinary(64)    NULL     COMMENT '用户的密码问答的答案(2)',
  `PasswordQuestion3` varchar(50)      NULL     COMMENT '用户的密码问答的题面(3)' COLLATE 'utf8mb4_0900_ai_ci',
  `PasswordAnswer3`   varbinary(64)    NULL     COMMENT '用户的密码问答的答案(3)',
  `Creation`          datetime         NOT NULL DEFAULT CURRENT_TIMESTAMP COMMENT '创建时间',
  `Modification`      datetime         NULL     COMMENT '最后修改时间',
  `Description`       varchar(500)     NULL     COMMENT '描述信息' COLLATE 'utf8mb4_0900_ai_ci',
  PRIMARY KEY (`UserId`),
  UNIQUE INDEX `UX_Security_User_Name` (`Namespace`, `Name`),
  UNIQUE INDEX `UX_Security_User_Email` (`Namespace`, `Email`),
  UNIQUE INDEX `UX_Security_User_Phone` (`Namespace`, `Phone`)
) ENGINE = InnoDB COMMENT='用户表';

CREATE TABLE IF NOT EXISTS `Security_Member` (
  `RoleId`     int unsigned     NOT NULL COMMENT '主键，角色编号',
  `MemberId`   int unsigned     NOT NULL COMMENT '主键，成员编号',
  `MemberType` tinyint unsigned NOT NULL COMMENT '主键，成员类型',
  PRIMARY KEY (`RoleId`, `MemberId`, `MemberType`)
) ENGINE = InnoDB COMMENT='角色成员表';

CREATE TABLE IF NOT EXISTS `Security_Privilege` (
  `MemberId`      int unsigned     NOT NULL COMMENT '主键，成员编号',
  `MemberType`    tinyint unsigned NOT NULL COMMENT '主键，成员类型',
  `PrivilegeName` varchar(50)      NOT NULL COMMENT '主键，权限标识' COLLATE 'ascii_general_ci',
  `PrivilegeMode` tinyint unsigned NOT NULL COMMENT '授权方式(0: 表示拒绝; 1: 表示授予)',
  PRIMARY KEY (`MemberId`, `MemberType`, `PrivilegeName`)
) ENGINE = InnoDB COMMENT='权限表';

CREATE TABLE IF NOT EXISTS `Security_PrivilegeFiltering` (
  `MemberId`        int unsigned     NOT NULL COMMENT '主键，成员编号',
  `MemberType`      tinyint unsigned NOT NULL COMMENT '主键，成员类型',
  `PrivilegeName`   varchar(50)      NOT NULL COMMENT '主键，权限标识' COLLATE 'ascii_general_ci',
  `PrivilegeFilter` varchar(500)     NOT NULL COMMENT '授权过滤表达式' COLLATE 'ascii_general_ci',
  PRIMARY KEY (`MemberId`, `MemberType`, `PrivilegeName`)
) ENGINE = InnoDB COMMENT='权限过滤表';


/* 添加系统内置角色 */
INSERT INTO Security_Role (RoleId, Name, Nickname, Description) VALUES (1, 'Administrators', '系统管理', '系统管理角色(系统内置角色)');
INSERT INTO Security_Role (RoleId, Name, Nickname, Description) VALUES (2, 'Security', '安全管理', '安全管理角色(系统内置角色)');

/* 添加系统内置用户 */
INSERT INTO Security_User (UserId, Name, Nickname, Description, Status) VALUES (1, 'Administrator', '系统管理员', '系统管理员(系统内置帐号)', 0);
INSERT INTO Security_User (UserId, Name, Nickname, Description, Status) VALUES (2, 'Guest', '来宾', '来宾', 1);
