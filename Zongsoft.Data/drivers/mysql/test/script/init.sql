CREATE TABLE IF NOT EXISTS `Security_Role` (
  `RoleId`      int unsigned NOT NULL COMMENT '主键，角色编号',
  `Namespace`   varchar(50)  NULL     COMMENT '命名空间，表示应用或组织机构的标识' COLLATE 'ascii_general_ci',
  `Name`        varchar(50)  NOT NULL COMMENT '角色名称，所属命名空间内具有唯一性' COLLATE 'utf8mb4_0900_ai_ci',
  `Avatar`      varchar(100) NULL     COMMENT '角色头像' COLLATE 'utf8mb4_0900_ai_ci',
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
  `Password`          varbinary(64)    NULL     COMMENT '用户的登录密码',
  `Email`             varchar(50)      NULL     COMMENT '用户的电子邮箱，该邮箱地址在所属命名空间内具有唯一性' COLLATE 'ascii_general_ci',
  `Phone`             varchar(50)      NULL     COMMENT '用户的手机号码，该手机号码在所属命名空间内具有唯一性' COLLATE 'ascii_general_ci',
  `Gender`            tinyint(1)       NULL     COMMENT '用户性别',
  `Enabled`           tinyint(1)       NOT NULL COMMENT '是否启用' DEFAULT 1,
  `PasswordQuestion`  varchar(200)     NULL     COMMENT '用户的密码问答的题面' COLLATE 'utf8mb4_0900_ai_ci',
  `PasswordAnswer`    varbinary(200)   NULL     COMMENT '用户的密码问答的答案',
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

/* 添加系统内置角色 */
INSERT INTO Security_Role (`RoleId`, `Name`, `Nickname`, `Description`) VALUES
  (1, 'Administrators', '系统管理', '系统管理角色(系统内置角色)'),
  (2, 'Security', '安全管理', '安全管理角色(系统内置角色)');

/* 添加系统内置用户 */
INSERT INTO Security_User (`UserId`, `Name`, `Nickname`, `Description`) VALUES
  (1, 'Administrator', '系统管理员', '系统管理员(系统内置帐号)'),
  (2, 'Guest', '来宾', '来宾');
