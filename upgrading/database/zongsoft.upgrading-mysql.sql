SET NAMES utf8mb4;
SET TIME_ZONE='+08:00';

CREATE TABLE IF NOT EXISTS `Upgrading_Application` (
  `ApplicationId` int unsigned NOT NULL COMMENT '主键，应用编号',
  `Name`          varchar(200) NOT NULL COMMENT '应用名称，具有唯一性' COLLATE 'ascii_general_ci',
  `Title`         varchar(200) NULL     COMMENT '应用标题' COLLATE 'utf8mb4_0900_ai_ci',
  `Description`   varchar(500) NULL     COMMENT '描述信息' COLLATE 'utf8mb4_0900_ai_ci',
  `Enabled`       tinyint(1)   NOT NULL COMMENT '是否启用' DEFAULT 1,
  `Creation`      datetime     NOT NULL COMMENT '创建时间' DEFAULT CURRENT_TIMESTAMP,
  `Modification`  datetime     NULL     COMMENT '修改时间',
  PRIMARY KEY (`ApplicationId`),
  UNIQUE INDEX `UX_Upgrading_Application_Name` (`Name`)
) ENGINE = InnoDB COMMENT='应用表';

CREATE TABLE IF NOT EXISTS `Upgrading_ApplicationEdition` (
  `ApplicationId` int unsigned NOT NULL COMMENT '主键，应用编号',
  `Name`          varchar(100) NOT NULL COMMENT '主键，版本名' COLLATE 'ascii_general_ci',
  `Title`         varchar(200) NULL     COMMENT '标题' COLLATE 'utf8mb4_0900_ai_ci',
  `Description`   varchar(500) NULL     COMMENT '描述信息' COLLATE 'utf8mb4_0900_ai_ci',
  `Licenced`      tinyint(1)   NOT NULL COMMENT '是否需要 License 授权' DEFAULT 0,
  `Enabled`       tinyint(1)   NOT NULL COMMENT '是否启用' DEFAULT 1,
  `Creation`      datetime     NOT NULL COMMENT '创建时间' DEFAULT CURRENT_TIMESTAMP,
  `Modification`  datetime     NULL     COMMENT '修改时间',
  PRIMARY KEY (`ApplicationId`, `Name`)
) ENGINE = InnoDB COMMENT='应用版本表';

CREATE TABLE IF NOT EXISTS `Upgrading_Release` (
  `ReleaseId`     int unsigned    NOT NULL COMMENT '主键，发布编号',
  `ApplicationId` int unsigned    NOT NULL COMMENT '应用编号',
  `Name`          varchar(200)    NOT NULL COMMENT '应用名称' COLLATE 'ascii_general_ci',
  `Edition`       varchar(100)    NOT NULL COMMENT '版本名' COLLATE 'ascii_general_ci' DEFAULT '',
  `Version`       varchar(50)     NOT NULL COMMENT '版本号' COLLATE 'ascii_general_ci',
  `Kind`          tinyint unsigned NOT NULL COMMENT '发布类型(0:完整发布; 1:增量发布)' DEFAULT 0,
  `Mode`          tinyint unsigned NOT NULL COMMENT '升级部署模式(0:默认; 1:尽快执行)' DEFAULT 0,
  `Platform`      varchar(20)     NOT NULL COMMENT '平台' COLLATE 'ascii_general_ci',
  `Architecture`  varchar(20)     NOT NULL COMMENT '架构' COLLATE 'ascii_general_ci',
  `Path`          varchar(500)    NULL     COMMENT 'Zongsoft.IO 虚拟文件系统完整文件路径' COLLATE 'utf8mb4_0900_ai_ci',
  `Size`          bigint unsigned NOT NULL COMMENT '包大小' DEFAULT 0,
  `Checksum`      varchar(200)    NULL     COMMENT '校验码' COLLATE 'ascii_general_ci',
  `Title`         varchar(200)    NULL     COMMENT '标题' COLLATE 'utf8mb4_0900_ai_ci',
  `Summary`       text            NULL     COMMENT '摘要' COLLATE 'utf8mb4_0900_ai_ci',
  `Description`   text            NULL     COMMENT '描述信息' COLLATE 'utf8mb4_0900_ai_ci',
  `Tags`          varchar(500)    NULL     COMMENT '标签集' COLLATE 'utf8mb4_0900_ai_ci',
  `FilterName`    varchar(100)    NULL     COMMENT '过滤器名称' COLLATE 'utf8mb4_0900_ai_ci',
  `FilterData`    text            NULL     COMMENT '过滤器数据' COLLATE 'utf8mb4_0900_ai_ci',
  `FilterSetting` text            NULL     COMMENT '过滤器设置' COLLATE 'utf8mb4_0900_ai_ci',
  `Deprecated`    tinyint(1)      NOT NULL COMMENT '是否废弃' DEFAULT 0,
  `Published`     tinyint(1)      NOT NULL COMMENT '是否已发布' DEFAULT 0,
  `Visible`       tinyint(1)      NOT NULL COMMENT '是否可见' DEFAULT 1,
  `Creation`      datetime        NOT NULL COMMENT '创建时间' DEFAULT CURRENT_TIMESTAMP,
  `Modification`  datetime        NULL     COMMENT '修改时间',
  PRIMARY KEY (`ReleaseId`),
  UNIQUE INDEX `UX_Upgrading_Release_Key` (`Name`, `Edition`, `Version`, `Platform`, `Architecture`),
  INDEX `IX_Upgrading_Release_Application` (`ApplicationId`)
) ENGINE = InnoDB COMMENT='发布表';

CREATE TABLE IF NOT EXISTS `Upgrading_ReleaseProperty` (
  `ReleaseId` int unsigned NOT NULL COMMENT '主键，发布编号',
  `Name`      varchar(100) NOT NULL COMMENT '主键，属性名' COLLATE 'ascii_general_ci',
  `Type`      varchar(100) NULL     COMMENT '属性类型' COLLATE 'ascii_general_ci',
  `Value`     text         NULL     COMMENT '属性值' COLLATE 'utf8mb4_0900_ai_ci',
  PRIMARY KEY (`ReleaseId`, `Name`)
) ENGINE = InnoDB COMMENT='发布属性表';

CREATE TABLE IF NOT EXISTS `Upgrading_ReleaseExecutor` (
  `ReleaseId` int unsigned     NOT NULL COMMENT '主键，发布编号',
  `SerialId`  int unsigned     NOT NULL COMMENT '主键，执行器序号',
  `Event`     varchar(50)      NOT NULL COMMENT '执行事件' COLLATE 'ascii_general_ci',
  `Command`   text             NOT NULL COMMENT '执行命令' COLLATE 'utf8mb4_0900_ai_ci',
  PRIMARY KEY (`ReleaseId`, `SerialId`)
) ENGINE = InnoDB COMMENT='发布执行器表';

CREATE TABLE IF NOT EXISTS `Upgrading_Instance` (
  `InstanceId`   int unsigned NOT NULL COMMENT '主键，实例编号',
  `InstanceCode` varchar(100) NOT NULL COMMENT '客户端机器唯一编号，具有唯一性' COLLATE 'ascii_general_ci',
  `Name`         varchar(200) NULL     COMMENT '实例名称' COLLATE 'utf8mb4_0900_ai_ci',
  `Tags`         varchar(500) NULL     COMMENT '标签集' COLLATE 'utf8mb4_0900_ai_ci',
  `Profile`      text         NULL     COMMENT '配置信息，包含硬件和操作系统等' COLLATE 'utf8mb4_0900_ai_ci',
  `Creation`     datetime     NOT NULL COMMENT '创建时间' DEFAULT CURRENT_TIMESTAMP,
  `Modification` datetime     NULL     COMMENT '修改时间',
  `Description`  varchar(500) NULL     COMMENT '描述说明' COLLATE 'utf8mb4_0900_ai_ci',
  PRIMARY KEY (`InstanceId`),
  UNIQUE INDEX `UX_Upgrading_Instance_Code` (`InstanceCode`)
) ENGINE = InnoDB COMMENT='实例表';

CREATE TABLE IF NOT EXISTS `Upgrading_ReleasePublishing` (
  `ReleaseId`   int unsigned NOT NULL COMMENT '主键，发布编号',
  `InstanceId`  int unsigned NOT NULL COMMENT '主键，实例编号',
  `Status`      varchar(20)  NOT NULL COMMENT '发布状态' COLLATE 'ascii_general_ci',
  `Message`     varchar(500) NULL     COMMENT '失败消息' COLLATE 'utf8mb4_0900_ai_ci',
  `Timestamp`   datetime     NOT NULL COMMENT '更新时间' DEFAULT CURRENT_TIMESTAMP,
  `Description` varchar(500) NULL     COMMENT '更新描述' COLLATE 'utf8mb4_0900_ai_ci',
  PRIMARY KEY (`ReleaseId`, `InstanceId`)
) ENGINE = InnoDB COMMENT='发布实例状态表';

