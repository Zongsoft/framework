CREATE TABLE IF NOT EXISTS "Security_Role" (
  "RoleId"      int          NOT NULL,
  "Namespace"   varchar(50)  NULL     COLLATE "C",
  "Name"        varchar(50)  NOT NULL COLLATE "C.utf8",
  "Avatar"      varchar(100) NULL     COLLATE "C.utf8",
  "Enabled"     boolean      NOT NULL DEFAULT true,
  "Nickname"    varchar(50)  NULL     COLLATE "C.utf8",
  "Description" varchar(500) NULL     COLLATE "C.utf8",
  PRIMARY KEY ("RoleId")
);

CREATE UNIQUE INDEX IF NOT EXISTS "UX_Security_Role_Name" ON "Security_Role" USING btree
  ("Namespace", "Name");

COMMENT ON TABLE "Security_Role" IS '角色表';
COMMENT ON COLUMN "Security_Role"."RoleId"      IS '主键，角色编号';
COMMENT ON COLUMN "Security_Role"."Namespace"   IS '命名空间，表示应用或组织机构的标识';
COMMENT ON COLUMN "Security_Role"."Name"        IS '角色名称，所属命名空间内具有唯一性';
COMMENT ON COLUMN "Security_Role"."Avatar"      IS '角色头像';
COMMENT ON COLUMN "Security_Role"."Enabled"     IS '是否启用';
COMMENT ON COLUMN "Security_Role"."Nickname"    IS '角色昵称';
COMMENT ON COLUMN "Security_Role"."Description" IS '描述信息';


CREATE TABLE IF NOT EXISTS "Security_User" (
  "UserId"            int          NOT NULL,
  "Namespace"         varchar(50)  NULL     COLLATE "C",
  "Name"              varchar(50)  NOT NULL COLLATE "C.utf8",
  "Avatar"            varchar(100) NULL     COLLATE "C.utf8",
  "Nickname"          varchar(50)  NULL     COLLATE "C.utf8",
  "Password"          bytea        NULL,
  "Email"             varchar(50)  NULL     COLLATE "C",
  "Phone"             varchar(50)  NULL     COLLATE "C",
  "Gender"            boolean      NULL,
  "Enabled"           boolean      NOT NULL DEFAULT true,
  "PasswordQuestion"  varchar(200) NULL     COLLATE "C.utf8",
  "PasswordAnswer"    bytea        NULL,
  "Creation"          timestamp    NOT NULL DEFAULT CURRENT_TIMESTAMP,
  "Modification"      timestamp    NULL,
  "Description"       varchar(500) NULL     COLLATE "C.utf8",
  PRIMARY KEY ("UserId")
);

CREATE UNIQUE INDEX IF NOT EXISTS "UX_Security_User_Name" ON "Security_User" USING btree
  ("Namespace", "Name");
CREATE UNIQUE INDEX IF NOT EXISTS "UX_Security_User_Email" ON "Security_User" USING btree
  ("Namespace", "Email");
CREATE UNIQUE INDEX IF NOT EXISTS "UX_Security_User_Phone" ON "Security_User" USING btree
  ("Namespace", "Phone");

COMMENT ON TABLE "Security_User" IS '用户表';
COMMENT ON COLUMN "Security_User"."UserId"           IS '主键，用户编号';
COMMENT ON COLUMN "Security_User"."Namespace"        IS '命名空间，表示应用或组织机构的标识';
COMMENT ON COLUMN "Security_User"."Name"             IS '用户名称，所属命名空间内具有唯一性';
COMMENT ON COLUMN "Security_User"."Avatar"           IS '用户头像';
COMMENT ON COLUMN "Security_User"."Nickname"         IS '用户昵称';
COMMENT ON COLUMN "Security_User"."Password"         IS '用户的登录密码';
COMMENT ON COLUMN "Security_User"."Email"            IS '用户的电子邮箱，该邮箱地址在所属命名空间内具有唯一性';
COMMENT ON COLUMN "Security_User"."Phone"            IS '用户的手机号码，该手机号码在所属命名空间内具有唯一性';
COMMENT ON COLUMN "Security_User"."Gender"           IS '用户性别';
COMMENT ON COLUMN "Security_User"."Enabled"          IS '是否启用';
COMMENT ON COLUMN "Security_User"."PasswordQuestion" IS '用户的密码问答的题面';
COMMENT ON COLUMN "Security_User"."PasswordAnswer"   IS '用户的密码问答的答案';
COMMENT ON COLUMN "Security_User"."Creation"         IS '创建时间';
COMMENT ON COLUMN "Security_User"."Modification"     IS '修改时间';
COMMENT ON COLUMN "Security_User"."Description"      IS '描述信息';

CREATE TABLE IF NOT EXISTS "Security_Member" (
  "RoleId"     int      NOT NULL,
  "MemberId"   int      NOT NULL,
  "MemberType" smallint NOT NULL,
  PRIMARY KEY ("RoleId", "MemberId", "MemberType")
);

COMMENT ON TABLE "Security_Member" IS '角色成员表';
COMMENT ON COLUMN "Security_Member"."RoleId"     IS '主键，角色编号';
COMMENT ON COLUMN "Security_Member"."MemberId"   IS '主键，成员编号';
COMMENT ON COLUMN "Security_Member"."MemberType" IS '主键，成员类型';

/* 添加系统内置角色 */
INSERT INTO "Security_Role" ("RoleId", "Name", "Nickname", "Description") VALUES
  (1, 'Administrators', '系统管理', '系统管理角色(系统内置角色)'),
  (2, 'Security', '安全管理', '安全管理角色(系统内置角色)');

/* 添加系统内置用户 */
INSERT INTO "Security_User" ("UserId", "Name", "Nickname", "Description") VALUES
  (1, 'Administrator', '系统管理员', '系统管理员(系统内置帐号)'),
  (2, 'Guest', '来宾', '来宾');
