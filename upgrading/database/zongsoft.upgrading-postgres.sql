CREATE TABLE IF NOT EXISTS "Upgrading_Application" (
  "ApplicationId" int          NOT NULL,
  "Name"          varchar(200) NOT NULL COLLATE "C",
  "Title"         varchar(200) NULL     COLLATE "C.utf8",
  "Description"   varchar(500) NULL     COLLATE "C.utf8",
  "Enabled"       boolean      NOT NULL DEFAULT true,
  "Creation"      timestamp    NOT NULL DEFAULT CURRENT_TIMESTAMP,
  "Modification"  timestamp    NULL,
  PRIMARY KEY ("ApplicationId")
);

CREATE UNIQUE INDEX IF NOT EXISTS "UX_Upgrading_Application_Name" ON "Upgrading_Application" USING btree
  ("Name");

COMMENT ON TABLE "Upgrading_Application" IS '应用表';
COMMENT ON COLUMN "Upgrading_Application"."ApplicationId" IS '主键，应用编号';
COMMENT ON COLUMN "Upgrading_Application"."Name"          IS '应用名称，具有唯一性';
COMMENT ON COLUMN "Upgrading_Application"."Title"         IS '应用标题';
COMMENT ON COLUMN "Upgrading_Application"."Description"   IS '描述信息';
COMMENT ON COLUMN "Upgrading_Application"."Enabled"       IS '是否启用';
COMMENT ON COLUMN "Upgrading_Application"."Creation"      IS '创建时间';
COMMENT ON COLUMN "Upgrading_Application"."Modification"  IS '修改时间';

CREATE TABLE IF NOT EXISTS "Upgrading_ApplicationEdition" (
  "ApplicationId" int          NOT NULL,
  "Name"          varchar(100) NOT NULL COLLATE "C",
  "Title"         varchar(200) NULL     COLLATE "C.utf8",
  "Description"   varchar(500) NULL     COLLATE "C.utf8",
  "Licenced"      boolean      NOT NULL DEFAULT false,
  "Enabled"       boolean      NOT NULL DEFAULT true,
  "Creation"      timestamp    NOT NULL DEFAULT CURRENT_TIMESTAMP,
  "Modification"  timestamp    NULL,
  PRIMARY KEY ("ApplicationId", "Name")
);

COMMENT ON TABLE "Upgrading_ApplicationEdition" IS '应用版本表';
COMMENT ON COLUMN "Upgrading_ApplicationEdition"."ApplicationId" IS '主键，应用编号';
COMMENT ON COLUMN "Upgrading_ApplicationEdition"."Name"          IS '主键，版本名';
COMMENT ON COLUMN "Upgrading_ApplicationEdition"."Title"         IS '标题';
COMMENT ON COLUMN "Upgrading_ApplicationEdition"."Description"   IS '描述信息';
COMMENT ON COLUMN "Upgrading_ApplicationEdition"."Licenced"      IS '是否需要 License 授权';
COMMENT ON COLUMN "Upgrading_ApplicationEdition"."Enabled"       IS '是否启用';
COMMENT ON COLUMN "Upgrading_ApplicationEdition"."Creation"      IS '创建时间';
COMMENT ON COLUMN "Upgrading_ApplicationEdition"."Modification"  IS '修改时间';

CREATE TABLE IF NOT EXISTS "Upgrading_Release" (
  "ReleaseId"     int          NOT NULL,
  "ApplicationId" int          NOT NULL,
  "Name"          varchar(200) NOT NULL COLLATE "C",
  "Edition"       varchar(100) NOT NULL COLLATE "C" DEFAULT '',
  "Version"       varchar(50)  NOT NULL COLLATE "C",
  "Kind"          smallint     NOT NULL DEFAULT 0,
  "Mode"          smallint     NOT NULL DEFAULT 0,
  "Platform"      varchar(20)  NOT NULL COLLATE "C",
  "Architecture"  varchar(20)  NOT NULL COLLATE "C",
  "Path"          varchar(500) NULL     COLLATE "C.utf8",
  "Size"          bigint       NOT NULL DEFAULT 0,
  "Checksum"      varchar(200) NULL     COLLATE "C",
  "Title"         varchar(200) NULL     COLLATE "C.utf8",
  "Summary"       text         NULL     COLLATE "C.utf8",
  "Description"   text         NULL     COLLATE "C.utf8",
  "Tags"          varchar(500) NULL     COLLATE "C.utf8",
  "FilterName"    varchar(100) NULL     COLLATE "C.utf8",
  "FilterData"    text         NULL     COLLATE "C.utf8",
  "FilterSetting" text         NULL     COLLATE "C.utf8",
  "Deprecated"    boolean      NOT NULL DEFAULT false,
  "Published"     boolean      NOT NULL DEFAULT false,
  "Visible"       boolean      NOT NULL DEFAULT true,
  "Creation"      timestamp    NOT NULL DEFAULT CURRENT_TIMESTAMP,
  "Modification"  timestamp    NULL,
  PRIMARY KEY ("ReleaseId")
);

CREATE UNIQUE INDEX IF NOT EXISTS "UX_Upgrading_Release_Key" ON "Upgrading_Release" USING btree
  ("Name", "Edition", "Version", "Platform", "Architecture");
CREATE INDEX IF NOT EXISTS "IX_Upgrading_Release_Application" ON "Upgrading_Release" USING btree
  ("ApplicationId");

COMMENT ON TABLE "Upgrading_Release" IS '发布表';
COMMENT ON COLUMN "Upgrading_Release"."ReleaseId"     IS '主键，发布编号';
COMMENT ON COLUMN "Upgrading_Release"."ApplicationId" IS '应用编号';
COMMENT ON COLUMN "Upgrading_Release"."Name"          IS '应用名称';
COMMENT ON COLUMN "Upgrading_Release"."Edition"       IS '版本名';
COMMENT ON COLUMN "Upgrading_Release"."Version"       IS '版本号';
COMMENT ON COLUMN "Upgrading_Release"."Kind"          IS '发布类型(0:完整发布; 1:增量发布)';
COMMENT ON COLUMN "Upgrading_Release"."Mode"          IS '升级部署模式(0:默认; 1:尽快执行)';
COMMENT ON COLUMN "Upgrading_Release"."Platform"      IS '平台';
COMMENT ON COLUMN "Upgrading_Release"."Architecture"  IS '架构';
COMMENT ON COLUMN "Upgrading_Release"."Path"          IS 'Zongsoft.IO 虚拟文件系统完整文件路径';
COMMENT ON COLUMN "Upgrading_Release"."Size"          IS '包大小';
COMMENT ON COLUMN "Upgrading_Release"."Checksum"      IS '校验码';
COMMENT ON COLUMN "Upgrading_Release"."Title"         IS '标题';
COMMENT ON COLUMN "Upgrading_Release"."Summary"       IS '摘要';
COMMENT ON COLUMN "Upgrading_Release"."Description"   IS '描述信息';
COMMENT ON COLUMN "Upgrading_Release"."Tags"          IS '标签集';
COMMENT ON COLUMN "Upgrading_Release"."FilterName"    IS '过滤器名称';
COMMENT ON COLUMN "Upgrading_Release"."FilterData"    IS '过滤器数据';
COMMENT ON COLUMN "Upgrading_Release"."FilterSetting" IS '过滤器设置';
COMMENT ON COLUMN "Upgrading_Release"."Deprecated"    IS '是否废弃';
COMMENT ON COLUMN "Upgrading_Release"."Published"     IS '是否已发布';
COMMENT ON COLUMN "Upgrading_Release"."Visible"       IS '是否可见';
COMMENT ON COLUMN "Upgrading_Release"."Creation"      IS '创建时间';
COMMENT ON COLUMN "Upgrading_Release"."Modification"  IS '修改时间';

CREATE TABLE IF NOT EXISTS "Upgrading_ReleaseProperty" (
  "ReleaseId" int          NOT NULL,
  "Name"      varchar(100) NOT NULL COLLATE "C",
  "Type"      varchar(100) NULL     COLLATE "C",
  "Value"     text         NULL     COLLATE "C.utf8",
  PRIMARY KEY ("ReleaseId", "Name")
);

COMMENT ON TABLE "Upgrading_ReleaseProperty" IS '发布属性表';
COMMENT ON COLUMN "Upgrading_ReleaseProperty"."ReleaseId" IS '主键，发布编号';
COMMENT ON COLUMN "Upgrading_ReleaseProperty"."Name"      IS '主键，属性名';
COMMENT ON COLUMN "Upgrading_ReleaseProperty"."Type"      IS '属性类型';
COMMENT ON COLUMN "Upgrading_ReleaseProperty"."Value"     IS '属性值';

CREATE TABLE IF NOT EXISTS "Upgrading_ReleaseExecutor" (
  "ReleaseId" int         NOT NULL,
  "SerialId"  int         NOT NULL,
  "Event"     varchar(50) NOT NULL COLLATE "C",
  "Command"   text        NOT NULL COLLATE "C.utf8",
  PRIMARY KEY ("ReleaseId", "SerialId")
);

COMMENT ON TABLE "Upgrading_ReleaseExecutor" IS '发布执行器表';
COMMENT ON COLUMN "Upgrading_ReleaseExecutor"."ReleaseId" IS '主键，发布编号';
COMMENT ON COLUMN "Upgrading_ReleaseExecutor"."SerialId"  IS '主键，执行器序号';
COMMENT ON COLUMN "Upgrading_ReleaseExecutor"."Event"     IS '执行事件';
COMMENT ON COLUMN "Upgrading_ReleaseExecutor"."Command"   IS '执行命令';

CREATE TABLE IF NOT EXISTS "Upgrading_Instance" (
  "InstanceId"   int          NOT NULL,
  "InstanceCode" varchar(100) NOT NULL COLLATE "C",
  "Name"         varchar(200) NULL     COLLATE "C.utf8",
  "Tags"         varchar(500) NULL     COLLATE "C.utf8",
  "Profile"      text         NULL     COLLATE "C.utf8",
  "Creation"     timestamp    NOT NULL DEFAULT CURRENT_TIMESTAMP,
  "Modification" timestamp    NULL,
  "Description"  varchar(500) NULL     COLLATE "C.utf8",
  PRIMARY KEY ("InstanceId")
);

CREATE UNIQUE INDEX IF NOT EXISTS "UX_Upgrading_Instance_Code" ON "Upgrading_Instance" USING btree
  ("InstanceCode");

COMMENT ON TABLE "Upgrading_Instance" IS '实例表';
COMMENT ON COLUMN "Upgrading_Instance"."InstanceId"   IS '主键，实例编号';
COMMENT ON COLUMN "Upgrading_Instance"."InstanceCode" IS '客户端机器唯一编号，具有唯一性';
COMMENT ON COLUMN "Upgrading_Instance"."Name"         IS '实例名称';
COMMENT ON COLUMN "Upgrading_Instance"."Tags"         IS '标签集';
COMMENT ON COLUMN "Upgrading_Instance"."Profile"      IS '配置信息，包含硬件和操作系统等';
COMMENT ON COLUMN "Upgrading_Instance"."Creation"     IS '创建时间';
COMMENT ON COLUMN "Upgrading_Instance"."Modification" IS '修改时间';
COMMENT ON COLUMN "Upgrading_Instance"."Description"  IS '描述说明';

CREATE TABLE IF NOT EXISTS "Upgrading_ReleasePublishing" (
  "ReleaseId"   int          NOT NULL,
  "InstanceId"  int          NOT NULL,
  "Status"      varchar(20)  NOT NULL COLLATE "C",
  "Message"     varchar(500) NULL     COLLATE "C.utf8",
  "Timestamp"   timestamp    NOT NULL DEFAULT CURRENT_TIMESTAMP,
  "Description" varchar(500) NULL     COLLATE "C.utf8",
  PRIMARY KEY ("ReleaseId", "InstanceId")
);

COMMENT ON TABLE "Upgrading_ReleasePublishing" IS '发布实例状态表';
COMMENT ON COLUMN "Upgrading_ReleasePublishing"."ReleaseId"   IS '主键，发布编号';
COMMENT ON COLUMN "Upgrading_ReleasePublishing"."InstanceId"  IS '主键，实例编号';
COMMENT ON COLUMN "Upgrading_ReleasePublishing"."Status"      IS '发布状态';
COMMENT ON COLUMN "Upgrading_ReleasePublishing"."Message"     IS '失败消息';
COMMENT ON COLUMN "Upgrading_ReleasePublishing"."Timestamp"   IS '更新时间';
COMMENT ON COLUMN "Upgrading_ReleasePublishing"."Description" IS '更新描述';

