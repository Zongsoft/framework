CREATE TABLE IF NOT EXISTS "Log"
(
    "LogId"       BIGSERIAL     NOT NULL PRIMARY KEY,
    "UserId"      INTEGER       NOT NULL,
    "TenantId"    INTEGER       NOT NULL,
    "BranchId"    INTEGER       NOT NULL,
    "Domain"      VARCHAR(50)   NOT NULL DEFAULT '_',
    "Target"      VARCHAR(100),
    "Action"      VARCHAR(100),
    "Caption"     VARCHAR(200)  COLLATE "C.utf8",
    "Content"     TEXT          COLLATE "C.utf8",
    "Severity"    SMALLINT      NOT NULL DEFAULT 0,
    "Timestamp"   TIMESTAMP     NOT NULL DEFAULT CURRENT_TIMESTAMP,
    "Description" VARCHAR(500)  COLLATE "C.utf8"
);

CREATE INDEX IF NOT EXISTS "IX_User" ON "Log" ("UserId", "Domain", "Timestamp", "Severity");
CREATE INDEX IF NOT EXISTS "IX_Severity" ON "Log" ("TenantId", "BranchId", "Domain", "Timestamp", "Severity");
CREATE INDEX IF NOT EXISTS "IX_Target" ON "Log" ("TenantId", "BranchId", "Domain", "Timestamp", "Target", "Action");

COMMENT ON TABLE "Log" IS '日志表';
COMMENT ON COLUMN "Log"."LogId"       IS '主键，日志编号';
COMMENT ON COLUMN "Log"."UserId"      IS '用户编号';
COMMENT ON COLUMN "Log"."TenantId"    IS '所属租户编号';
COMMENT ON COLUMN "Log"."BranchId"    IS '分支机构编号';
COMMENT ON COLUMN "Log"."Domain"      IS '领域标识';
COMMENT ON COLUMN "Log"."Target"      IS '操作目标';
COMMENT ON COLUMN "Log"."Action"      IS '操作行为';
COMMENT ON COLUMN "Log"."Caption"     IS '日志标题';
COMMENT ON COLUMN "Log"."Content"     IS '日志内容';
COMMENT ON COLUMN "Log"."Severity"    IS '严重级别';
COMMENT ON COLUMN "Log"."Timestamp"   IS '日志时间';
COMMENT ON COLUMN "Log"."Description" IS '备注说明';


CREATE TABLE IF NOT EXISTS "Tenant"
(
    "TenantId"                          INTEGER      NOT NULL,
    "TenantNo"                          VARCHAR(50)  NOT NULL COLLATE "C",
    "Name"                              VARCHAR(50)  NOT NULL COLLATE "C.utf8",
    "Abbr"                              VARCHAR(50)  COLLATE "C.utf8",
    "Acronym"                           VARCHAR(50)  COLLATE "C",
    "LogoPath"                          VARCHAR(200) COLLATE "C",
    "Country"                           INTEGER      NOT NULL DEFAULT 0,
    "Language"                          CHAR(2)      NOT NULL DEFAULT 'zh',
    "AddressId"                         INTEGER      NOT NULL DEFAULT 0,
    "AddressDetail"                     VARCHAR(100) COLLATE "C.utf8",
    "Longitude"                         DOUBLE PRECISION,
    "Latitude"                          DOUBLE PRECISION,
    "TenantTypeId"                      INTEGER,
    "TenantSubtypeId"                   SMALLINT,
    "BusinessLicenseNo"                 VARCHAR(50)  COLLATE "C",
    "BusinessLicenseKind"               SMALLINT     NOT NULL DEFAULT 0,
    "BusinessLicenseAuthority"          VARCHAR(50)  COLLATE "C.utf8",
    "BusinessLicensePhotoPath"          VARCHAR(200) COLLATE "C",
    "BusinessLicenseIssueDate"          DATE,
    "BusinessLicenseExpiryDate"         DATE,
    "BusinessLicenseDescription"        VARCHAR(500) COLLATE "C.utf8",
    "RegisteredCapital"                 INTEGER,
    "RegisteredAddress"                 VARCHAR(100) COLLATE "C.utf8",
    "StaffScale"                        SMALLINT     NOT NULL DEFAULT 0,
    "AdministratorEmail"                VARCHAR(50),
    "AdministratorPhone"                VARCHAR(50),
    "AdministratorPassword"             BYTEA,
    "LegalRepresentativeName"           VARCHAR(50)  COLLATE "C.utf8",
    "LegalRepresentativeGender"         BOOLEAN,
    "LegalRepresentativeEmail"          VARCHAR(50)  COLLATE "C",
    "LegalRepresentativeIdentityId"     VARCHAR(50)  COLLATE "C",
    "LegalRepresentativeIdentityKind"   SMALLINT     NOT NULL DEFAULT 0,
    "LegalRepresentativeIdentityIssued" DATE,
    "LegalRepresentativeIdentityExpiry" DATE,
    "LegalRepresentativeMobilePhone"    VARCHAR(50)  COLLATE "C",
    "LegalRepresentativeIdentityPath1"  VARCHAR(200) COLLATE "C",
    "LegalRepresentativeIdentityPath2"  VARCHAR(200) COLLATE "C",
    "BankCode"                          VARCHAR(50)  COLLATE "C",
    "BankName"                          VARCHAR(50)  COLLATE "C.utf8",
    "BankAccountCode"                   VARCHAR(50)  COLLATE "C",
    "BankAccountSetting"                VARCHAR(500) COLLATE "C.utf8",
    "PhoneNumber"                       VARCHAR(50)  COLLATE "C",
    "WebUrl"                            VARCHAR(100) COLLATE "C",
    "ContactName"                       VARCHAR(50)  COLLATE "C.utf8",
    "ContactGender"                     BOOLEAN,
    "ContactEmail"                      VARCHAR(50)  COLLATE "C",
    "ContactMobilePhone"                VARCHAR(50)  COLLATE "C",
    "ContactOfficePhone"                VARCHAR(50)  COLLATE "C",
    "ContactIdentityId"                 VARCHAR(50)  COLLATE "C",
    "ContactIdentityKind"               SMALLINT     NOT NULL DEFAULT 0,
    "ContactIdentityIssued"             DATE,
    "ContactIdentityExpiry"             DATE,
    "ContactIdentityPath1"              VARCHAR(200) COLLATE "C",
    "ContactIdentityPath2"              VARCHAR(200) COLLATE "C",
    "Flags"                             SMALLINT     NOT NULL DEFAULT 0,
    "Grade"                             SMALLINT     NOT NULL DEFAULT 0,
    "Status"                            SMALLINT     NOT NULL DEFAULT 0,
    "StatusTimestamp"                   TIMESTAMP,
    "StatusDescription"                 VARCHAR(100) COLLATE "C.utf8",
    "CreatorId"                         INTEGER      NOT NULL,
    "CreatedTime"                       TIMESTAMP    NOT NULL DEFAULT CURRENT_TIMESTAMP,
    "ModifierId"                        INTEGER,
    "ModifiedTime"                      TIMESTAMP,
    "Remark"                            VARCHAR(500) COLLATE "C.utf8",
    PRIMARY KEY ("TenantId")
);

COMMENT ON TABLE "Tenant" IS '租户表';
COMMENT ON COLUMN "Tenant"."TenantId"                   IS '主键，租户编号';
COMMENT ON COLUMN "Tenant"."TenantNo"                   IS '租户编码';
COMMENT ON COLUMN "Tenant"."Name"                       IS '租户名称';
COMMENT ON COLUMN "Tenant"."Abbr"                       IS '租户简称';
COMMENT ON COLUMN "Tenant"."Acronym"                    IS '名称缩写';
COMMENT ON COLUMN "Tenant"."LogoPath"                   IS '标志图片路径';
COMMENT ON COLUMN "Tenant"."Country"                    IS '国别地区';
COMMENT ON COLUMN "Tenant"."Language"                   IS '语言标识';
COMMENT ON COLUMN "Tenant"."AddressId"                  IS '所在地址代码';
COMMENT ON COLUMN "Tenant"."AddressDetail"              IS '经营详细地址';
COMMENT ON COLUMN "Tenant"."Longitude"                  IS '经度坐标';
COMMENT ON COLUMN "Tenant"."Latitude"                   IS '纬度坐标';
COMMENT ON COLUMN "Tenant"."TenantTypeId"               IS '租户类型编号';
COMMENT ON COLUMN "Tenant"."TenantSubtypeId"            IS '租户子类编号';
COMMENT ON COLUMN "Tenant"."BusinessLicenseNo"          IS '工商执照号';
COMMENT ON COLUMN "Tenant"."BusinessLicenseKind"        IS '工商执照种类';
COMMENT ON COLUMN "Tenant"."BusinessLicenseAuthority"   IS '工商执照发证机关';
COMMENT ON COLUMN "Tenant"."BusinessLicensePhotoPath"   IS '工商执照相片路径';
COMMENT ON COLUMN "Tenant"."BusinessLicenseIssueDate"   IS '工商执照登记日期';
COMMENT ON COLUMN "Tenant"."BusinessLicenseExpiryDate"  IS '工商执照过期日期';
COMMENT ON COLUMN "Tenant"."BusinessLicenseDescription" IS '工商执照经营范围';
COMMENT ON COLUMN "Tenant"."RegisteredCapital"          IS '注册资本(万元)';
COMMENT ON COLUMN "Tenant"."RegisteredAddress"          IS '注册地址';
COMMENT ON COLUMN "Tenant"."StaffScale"                 IS '人员规模';
COMMENT ON COLUMN "Tenant"."AdministratorEmail"         IS '管理员邮箱';
COMMENT ON COLUMN "Tenant"."AdministratorPhone"         IS '管理员电话';
COMMENT ON COLUMN "Tenant"."AdministratorPassword"      IS '管理员密码';
COMMENT ON COLUMN "Tenant"."LegalRepresentativeName"    IS '法人姓名';
COMMENT ON COLUMN "Tenant"."LegalRepresentativeGender"  IS '法人性别';
COMMENT ON COLUMN "Tenant"."LegalRepresentativeEmail"   IS '法人邮箱';
COMMENT ON COLUMN "Tenant"."LegalRepresentativeIdentityId"     IS '法人身份证号码';
COMMENT ON COLUMN "Tenant"."LegalRepresentativeIdentityKind"   IS '法人身份证种类';
COMMENT ON COLUMN "Tenant"."LegalRepresentativeIdentityIssued" IS '法人身份证签发日期';
COMMENT ON COLUMN "Tenant"."LegalRepresentativeIdentityExpiry" IS '法人身份证过期日期';
COMMENT ON COLUMN "Tenant"."LegalRepresentativeMobilePhone"    IS '法人移动电话';
COMMENT ON COLUMN "Tenant"."LegalRepresentativeIdentityPath1"  IS '法人证件照片路径1';
COMMENT ON COLUMN "Tenant"."LegalRepresentativeIdentityPath2"  IS '法人证件照片路径2';
COMMENT ON COLUMN "Tenant"."BankCode"              IS '开户银行代号';
COMMENT ON COLUMN "Tenant"."BankName"              IS '开户银行名称';
COMMENT ON COLUMN "Tenant"."BankAccountCode"       IS '开户银行账户号码';
COMMENT ON COLUMN "Tenant"."BankAccountSetting"    IS '开户银行账户设置';
COMMENT ON COLUMN "Tenant"."PhoneNumber"           IS '办公电话';
COMMENT ON COLUMN "Tenant"."WebUrl"                IS '网站网址';
COMMENT ON COLUMN "Tenant"."ContactName"           IS '联系人姓名';
COMMENT ON COLUMN "Tenant"."ContactGender"         IS '联系人性别';
COMMENT ON COLUMN "Tenant"."ContactEmail"          IS '联系人邮箱';
COMMENT ON COLUMN "Tenant"."ContactMobilePhone"    IS '联系人移动电话';
COMMENT ON COLUMN "Tenant"."ContactOfficePhone"    IS '联系人办公电话';
COMMENT ON COLUMN "Tenant"."ContactIdentityId"     IS '联系人身份证号';
COMMENT ON COLUMN "Tenant"."ContactIdentityKind"   IS '联系人身份证种类';
COMMENT ON COLUMN "Tenant"."ContactIdentityIssued" IS '联系人身份证签发日期';
COMMENT ON COLUMN "Tenant"."ContactIdentityExpiry" IS '联系人身份证过期日期';
COMMENT ON COLUMN "Tenant"."ContactIdentityPath1"  IS '联系人证件照片路径1';
COMMENT ON COLUMN "Tenant"."ContactIdentityPath2"  IS '联系人证件照片路径2';
COMMENT ON COLUMN "Tenant"."Flags"                 IS '标记';
COMMENT ON COLUMN "Tenant"."Grade"                 IS '等级';
COMMENT ON COLUMN "Tenant"."Status"                IS '租户状态';
COMMENT ON COLUMN "Tenant"."StatusTimestamp"       IS '状态变更时间';
COMMENT ON COLUMN "Tenant"."StatusDescription"     IS '状态变更描述';
COMMENT ON COLUMN "Tenant"."CreatorId"             IS '创建人编号';
COMMENT ON COLUMN "Tenant"."CreatedTime"           IS '创建时间';
COMMENT ON COLUMN "Tenant"."ModifierId"            IS '修改人编号';
COMMENT ON COLUMN "Tenant"."ModifiedTime"          IS '修改时间';
COMMENT ON COLUMN "Tenant"."Remark"                IS '备注说明';

CREATE UNIQUE INDEX "UX_Tenant_TenantNo" ON "Tenant" ("TenantNo");
CREATE UNIQUE INDEX "UX_Tenant_BusinessLicenseNo" ON "Tenant" ("BusinessLicenseNo");
CREATE INDEX "IX_Tenant_LegalRepresentativeEmail" ON "Tenant" ("LegalRepresentativeEmail");
CREATE INDEX "IX_Tenant_LegalRepresentativeIdentityId" ON "Tenant" ("LegalRepresentativeIdentityId");
CREATE INDEX "IX_Tenant_LegalRepresentativeMobilePhone" ON "Tenant" ("LegalRepresentativeMobilePhone");
CREATE INDEX "IX_Tenant_ContactEmail" ON "Tenant" ("ContactEmail");
CREATE INDEX "IX_Tenant_ContactIdentityId" ON "Tenant" ("ContactIdentityId");
CREATE INDEX "IX_Tenant_ContactMobilePhone" ON "Tenant" ("ContactMobilePhone");


CREATE TABLE IF NOT EXISTS "Branch"
(
    "TenantId"                          INTEGER      NOT NULL,
    "BranchId"                          INTEGER      NOT NULL,
    "BranchNo"                          VARCHAR(50)  NOT NULL COLLATE "C",
    "Name"                              VARCHAR(50)  NOT NULL COLLATE "C.utf8",
    "Abbr"                              VARCHAR(50)  COLLATE "C.utf8",
    "Acronym"                           VARCHAR(50)  COLLATE "C",
    "LogoPath"                          VARCHAR(200) COLLATE "C",
    "Ordinal"                           SMALLINT     NOT NULL DEFAULT 0,
    "Country"                           SMALLINT     NOT NULL DEFAULT 0,
    "Language"                          CHAR(2)      NOT NULL DEFAULT 'zh',
    "AddressId"                         INTEGER      NOT NULL DEFAULT 0,
    "AddressDetail"                     VARCHAR(100) COLLATE "C.utf8",
    "Longitude"                         DOUBLE PRECISION,
    "Latitude"                          DOUBLE PRECISION,
    "BusinessLicenseNo"                 VARCHAR(50)  COLLATE "C",
    "BusinessLicenseKind"               SMALLINT     NOT NULL DEFAULT 0,
    "BusinessLicenseAuthority"          VARCHAR(50)  COLLATE "C.utf8",
    "BusinessLicensePhotoPath"          VARCHAR(200) COLLATE "C",
    "BusinessLicenseIssueDate"          DATE,
    "BusinessLicenseExpiryDate"         DATE,
    "BusinessLicenseDescription"        VARCHAR(500) COLLATE "C.utf8",
    "RegisteredCapital"                 SMALLINT,
    "RegisteredAddress"                 VARCHAR(100) COLLATE "C.utf8",
    "StaffScale"                        SMALLINT     NOT NULL DEFAULT 0,
    "LegalRepresentativeName"           VARCHAR(50)  COLLATE "C.utf8",
    "LegalRepresentativeGender"         BOOLEAN,
    "LegalRepresentativeEmail"          VARCHAR(50)  COLLATE "C",
    "LegalRepresentativeIdentityId"     VARCHAR(50)  COLLATE "C",
    "LegalRepresentativeIdentityKind"   SMALLINT     NOT NULL DEFAULT 0,
    "LegalRepresentativeIdentityIssued" DATE,
    "LegalRepresentativeIdentityExpiry" DATE,
    "LegalRepresentativeMobilePhone"    VARCHAR(50)  COLLATE "C",
    "LegalRepresentativeIdentityPath1"  VARCHAR(200) COLLATE "C",
    "LegalRepresentativeIdentityPath2"  VARCHAR(200) COLLATE "C",
    "BankCode"                          VARCHAR(50)  COLLATE "C",
    "BankName"                          VARCHAR(50)  COLLATE "C.utf8",
    "BankAccountCode"                   VARCHAR(50)  COLLATE "C",
    "BankAccountSetting"                VARCHAR(500) COLLATE "C.utf8",
    "PhoneNumber"                       VARCHAR(50)  COLLATE "C",
    "PrincipalId"                       INTEGER,
    "ContactName"                       VARCHAR(50)  COLLATE "C.utf8",
    "ContactGender"                     BOOLEAN,
    "ContactEmail"                      VARCHAR(50)  COLLATE "C",
    "ContactMobilePhone"                VARCHAR(50)  COLLATE "C",
    "ContactOfficePhone"                VARCHAR(50)  COLLATE "C",
    "ContactIdentityId"                 VARCHAR(50)  COLLATE "C",
    "ContactIdentityKind"               SMALLINT     NOT NULL DEFAULT 0,
    "ContactIdentityIssued"             DATE,
    "ContactIdentityExpiry"             DATE,
    "ContactIdentityPath1"              VARCHAR(200) COLLATE "C",
    "ContactIdentityPath2"              VARCHAR(200) COLLATE "C",
    "Status"                            SMALLINT     NOT NULL DEFAULT 0,
    "StatusTimestamp"                   TIMESTAMP,
    "StatusDescription"                 VARCHAR(100) COLLATE "C.utf8",
    "CreatorId"                         INTEGER      NOT NULL,
    "CreatedTime"                       TIMESTAMP    NOT NULL DEFAULT CURRENT_TIMESTAMP,
    "ModifierId"                        INTEGER,
    "ModifiedTime"                      TIMESTAMP,
    "Remark"                            VARCHAR(500) COLLATE "C.utf8",
    PRIMARY KEY ("TenantId", "BranchId")
);

COMMENT ON TABLE "Branch" IS '分支机构表';
COMMENT ON COLUMN "Branch"."TenantId"                   IS '主键，租户编号';
COMMENT ON COLUMN "Branch"."BranchId"                   IS '主键，分支编号';
COMMENT ON COLUMN "Branch"."BranchNo"                   IS '分支机构编码';
COMMENT ON COLUMN "Branch"."Name"                       IS '分支机构名称';
COMMENT ON COLUMN "Branch"."Abbr"                       IS '分支机构简称';
COMMENT ON COLUMN "Branch"."Acronym"                    IS '名称缩写';
COMMENT ON COLUMN "Branch"."LogoPath"                   IS '标志图片路径';
COMMENT ON COLUMN "Branch"."Ordinal"                    IS '排列顺序';
COMMENT ON COLUMN "Branch"."Country"                    IS '国别地区';
COMMENT ON COLUMN "Branch"."Language"                   IS '语言标识';
COMMENT ON COLUMN "Branch"."AddressId"                  IS '地址编号';
COMMENT ON COLUMN "Branch"."AddressDetail"              IS '详细地址';
COMMENT ON COLUMN "Branch"."Longitude"                  IS '经度坐标';
COMMENT ON COLUMN "Branch"."Latitude"                   IS '纬度坐标';
COMMENT ON COLUMN "Branch"."BusinessLicenseNo"          IS '工商执照号';
COMMENT ON COLUMN "Branch"."BusinessLicenseKind"        IS '工商执照种类';
COMMENT ON COLUMN "Branch"."BusinessLicenseAuthority"   IS '工商执照发证机关';
COMMENT ON COLUMN "Branch"."BusinessLicensePhotoPath"   IS '工商执照相片路径';
COMMENT ON COLUMN "Branch"."BusinessLicenseIssueDate"   IS '工商执照登记日期';
COMMENT ON COLUMN "Branch"."BusinessLicenseExpiryDate"  IS '工商执照过期日期';
COMMENT ON COLUMN "Branch"."BusinessLicenseDescription" IS '工商执照经营范围';
COMMENT ON COLUMN "Branch"."RegisteredCapital"          IS '注册资本(万元)';
COMMENT ON COLUMN "Branch"."RegisteredAddress"          IS '注册地址';
COMMENT ON COLUMN "Branch"."StaffScale"                 IS '人员规模';
COMMENT ON COLUMN "Branch"."LegalRepresentativeName"           IS '法人姓名';
COMMENT ON COLUMN "Branch"."LegalRepresentativeGender"         IS '法人性别';
COMMENT ON COLUMN "Branch"."LegalRepresentativeEmail"          IS '法人邮箱';
COMMENT ON COLUMN "Branch"."LegalRepresentativeIdentityId"     IS '法人身份证号码';
COMMENT ON COLUMN "Branch"."LegalRepresentativeIdentityKind"   IS '法人身份证种类';
COMMENT ON COLUMN "Branch"."LegalRepresentativeIdentityIssued" IS '法人身份证签发日期';
COMMENT ON COLUMN "Branch"."LegalRepresentativeIdentityExpiry" IS '法人身份证过期日期';
COMMENT ON COLUMN "Branch"."LegalRepresentativeMobilePhone"    IS '法人移动电话';
COMMENT ON COLUMN "Branch"."LegalRepresentativeIdentityPath1"  IS '法人证件照片路径1';
COMMENT ON COLUMN "Branch"."LegalRepresentativeIdentityPath2"  IS '法人证件照片路径2';
COMMENT ON COLUMN "Branch"."BankCode"              IS '开户银行代号';
COMMENT ON COLUMN "Branch"."BankName"              IS '开户银行名称';
COMMENT ON COLUMN "Branch"."BankAccountCode"       IS '开户银行账户号码';
COMMENT ON COLUMN "Branch"."BankAccountSetting"    IS '开户银行账户设置';
COMMENT ON COLUMN "Branch"."PhoneNumber"           IS '电话号码';
COMMENT ON COLUMN "Branch"."PrincipalId"           IS '负责人编号';
COMMENT ON COLUMN "Branch"."ContactName"           IS '联系人姓名';
COMMENT ON COLUMN "Branch"."ContactGender"         IS '联系人性别';
COMMENT ON COLUMN "Branch"."ContactEmail"          IS '联系人邮箱';
COMMENT ON COLUMN "Branch"."ContactMobilePhone"    IS '联系人移动电话';
COMMENT ON COLUMN "Branch"."ContactOfficePhone"    IS '联系人办公电话';
COMMENT ON COLUMN "Branch"."ContactIdentityId"     IS '联系人身份证号';
COMMENT ON COLUMN "Branch"."ContactIdentityKind"   IS '联系人身份证种类';
COMMENT ON COLUMN "Branch"."ContactIdentityIssued" IS '联系人身份证签发日期';
COMMENT ON COLUMN "Branch"."ContactIdentityExpiry" IS '联系人身份证过期日期';
COMMENT ON COLUMN "Branch"."ContactIdentityPath1"  IS '联系人证件照片路径1';
COMMENT ON COLUMN "Branch"."ContactIdentityPath2"  IS '联系人证件照片路径2';
COMMENT ON COLUMN "Branch"."Status"                IS '机构状态';
COMMENT ON COLUMN "Branch"."StatusTimestamp"       IS '状态变更时间';
COMMENT ON COLUMN "Branch"."StatusDescription"     IS '状态变更描述';
COMMENT ON COLUMN "Branch"."CreatorId"             IS '创建人编号';
COMMENT ON COLUMN "Branch"."CreatedTime"           IS '创建时间';
COMMENT ON COLUMN "Branch"."ModifierId"            IS '修改人编号';
COMMENT ON COLUMN "Branch"."ModifiedTime"          IS '修改时间';
COMMENT ON COLUMN "Branch"."Remark"                IS '备注说明';

CREATE UNIQUE INDEX "UX_Branch_BranchNo" ON "Branch" ("TenantId", "BranchNo");
CREATE UNIQUE INDEX "UX_Branch_BusinessLicenseNo" ON "Branch" ("TenantId", "BusinessLicenseNo");
CREATE INDEX "IX_Branch_Ordinal" ON "Branch" ("TenantId", "Ordinal");


CREATE TABLE IF NOT EXISTS "BranchMember"
(
    "TenantId" integer NOT NULL,
    "BranchId" integer NOT NULL,
    "UserId"   integer NOT NULL,
    PRIMARY KEY ("TenantId", "BranchId", "UserId")
);

COMMENT ON TABLE "BranchMember" IS '分支机构成员表';
COMMENT ON COLUMN "BranchMember"."TenantId" IS '主键，租户编号';
COMMENT ON COLUMN "BranchMember"."BranchId" IS '主键，机构编号';
COMMENT ON COLUMN "BranchMember"."UserId"   IS '主键，用户编号';

CREATE INDEX IF NOT EXISTS "IX_BranchMember_User" ON "BranchMember" ("TenantId", "UserId", "BranchId");


CREATE TABLE IF NOT EXISTS "Department"
(
    "TenantId" integer         NOT NULL,
    "BranchId" integer         NOT NULL,
    "DepartmentId" smallint    NOT NULL,
    "ParentId" smallint        NOT NULL,
    "DepartmentNo" varchar(50) NOT NULL COLLATE "C",
    "Name" varchar(50)         NOT NULL COLLATE "C.utf8",
    "Acronym" varchar(50)      COLLATE "C",
    "Icon" varchar(100)        COLLATE "C.utf8",
    "PrincipalId" integer,
    "PhoneNumber" varchar(50)  COLLATE "C",
    "Address" varchar(100)     COLLATE "C.utf8",
    "Ordinal" smallint         NOT NULL DEFAULT 0,
    "Remark" varchar(500)      COLLATE "C.utf8",
    PRIMARY KEY ("TenantId", "BranchId", "DepartmentId"),
    CONSTRAINT "UX_DepartmentNo" UNIQUE ("TenantId", "BranchId", "DepartmentNo")
);

COMMENT ON TABLE "Department" IS '部门表';
COMMENT ON COLUMN "Department"."TenantId"     IS '主键，租户编号';
COMMENT ON COLUMN "Department"."BranchId"     IS '主键，分支机构编号';
COMMENT ON COLUMN "Department"."DepartmentId" IS '主键，部门编号';
COMMENT ON COLUMN "Department"."ParentId"     IS '上级部门编号';
COMMENT ON COLUMN "Department"."DepartmentNo" IS '部门代号';
COMMENT ON COLUMN "Department"."Name"         IS '部门名称';
COMMENT ON COLUMN "Department"."Acronym"      IS '名称缩写';
COMMENT ON COLUMN "Department"."Icon"         IS '图标标识';
COMMENT ON COLUMN "Department"."PrincipalId"  IS '负责人编号';
COMMENT ON COLUMN "Department"."PhoneNumber"  IS '部门电话';
COMMENT ON COLUMN "Department"."Address"      IS '部门办公地址';
COMMENT ON COLUMN "Department"."Ordinal"      IS '排列顺序';
COMMENT ON COLUMN "Department"."Remark"       IS '备注说明';

CREATE INDEX IF NOT EXISTS "IX_Department_Ordinal" ON "Department" ("TenantId", "BranchId", "Ordinal");


CREATE TABLE IF NOT EXISTS "DepartmentMember"
(
    "TenantId" integer      NOT NULL,
    "BranchId" integer      NOT NULL,
    "DepartmentId" smallint NOT NULL,
    "UserId" integer        NOT NULL,
    PRIMARY KEY ("TenantId", "BranchId", "DepartmentId", "UserId")
);

COMMENT ON TABLE "DepartmentMember" IS '部门成员表';
COMMENT ON COLUMN "DepartmentMember"."TenantId"     IS '主键，租户编号';
COMMENT ON COLUMN "DepartmentMember"."BranchId"     IS '主键，分支机构编号';
COMMENT ON COLUMN "DepartmentMember"."DepartmentId" IS '主键，部门编号';
COMMENT ON COLUMN "DepartmentMember"."UserId"       IS '主键，用户编号';

CREATE INDEX IF NOT EXISTS "IX_DepartmentMember_UserId" ON "DepartmentMember" ("TenantId", "BranchId", "UserId");


CREATE TABLE IF NOT EXISTS "Team"
(
    "TenantId" integer      NOT NULL,
    "BranchId" integer      NOT NULL,
    "TeamId" smallint       NOT NULL,
    "TeamNo" varchar(50)    COLLATE "C" NOT NULL,
    "Name" varchar(50)      COLLATE "C.utf8" NOT NULL,
    "Acronym" varchar(50)   COLLATE "C" NULL,
    "Icon" varchar(100)     COLLATE "C.utf8" NULL,
    "LeaderId" integer      NULL,
    "DepartmentId" smallint NULL,
    "Visible" boolean       NOT NULL DEFAULT true,
    "Ordinal" smallint      NOT NULL DEFAULT 0,
    "Remark" varchar(500)   COLLATE "C.utf8" NULL,
    PRIMARY KEY ("TenantId", "BranchId", "TeamId"),
    CONSTRAINT "UX_TeamNo" UNIQUE ("TenantId", "BranchId", "TeamNo")
);

COMMENT ON TABLE "Team" IS '班组表';
COMMENT ON COLUMN "Team"."TenantId"     IS '主键，租户编号';
COMMENT ON COLUMN "Team"."BranchId"     IS '主键，分支机构编号';
COMMENT ON COLUMN "Team"."TeamId"       IS '主键，班组编号';
COMMENT ON COLUMN "Team"."TeamNo"       IS '班组代号';
COMMENT ON COLUMN "Team"."Name"         IS '班组名称';
COMMENT ON COLUMN "Team"."Acronym"      IS '名称缩写';
COMMENT ON COLUMN "Team"."Icon"         IS '图标标识';
COMMENT ON COLUMN "Team"."LeaderId"     IS '组长编号';
COMMENT ON COLUMN "Team"."DepartmentId" IS '所属部门编号';
COMMENT ON COLUMN "Team"."Visible"      IS '是否可用';
COMMENT ON COLUMN "Team"."Ordinal"      IS '排列顺序';
COMMENT ON COLUMN "Team"."Remark"       IS '备注说明';

CREATE INDEX IF NOT EXISTS "IX_Team_Ordinal" ON "Team" ("TenantId", "BranchId", "Ordinal");


CREATE TABLE IF NOT EXISTS "TeamMember"
(
    "TenantId" integer  NOT NULL,
    "BranchId" integer  NOT NULL,
    "TeamId"   smallint NOT NULL,
    "UserId"   integer  NOT NULL,
    PRIMARY KEY ("TenantId", "BranchId", "TeamId", "UserId")
);

COMMENT ON TABLE "TeamMember" IS '班组成员表';
COMMENT ON COLUMN "TeamMember"."TenantId" IS '主键，租户编号';
COMMENT ON COLUMN "TeamMember"."BranchId" IS '主键，分支机构编号';
COMMENT ON COLUMN "TeamMember"."TeamId"   IS '主键，小组编号';
COMMENT ON COLUMN "TeamMember"."UserId"   IS '主键，用户编号';

CREATE INDEX IF NOT EXISTS "IX_TeamMember_UserId" ON "TeamMember" ("TenantId", "BranchId", "UserId");


CREATE TABLE IF NOT EXISTS "Employee"
(
	"TenantId"            INTEGER      NOT NULL,
	"UserId"              INTEGER      NOT NULL,
	"BranchId"            INTEGER      NOT NULL,
	"EmployeeNo"          VARCHAR(50)  NULL     COLLATE "C",
	"EmployeeCode"        VARCHAR(50)  NULL     COLLATE "C",
	"EmployeeKind"        SMALLINT     NOT NULL DEFAULT 0,
	"FullName"            VARCHAR(50)  NULL     COLLATE "C.utf8",
	"Acronym"             VARCHAR(50)  NULL     COLLATE "C",
	"Summary"             VARCHAR(500) NULL     COLLATE "C.utf8",
	"JobTitle"            VARCHAR(50)  NULL     COLLATE "C.utf8",
	"JobStatus"           SMALLINT     NOT NULL DEFAULT 0,
	"Hiredate"            DATE         NULL,
	"Leavedate"           DATE         NULL,
	"BankName"            VARCHAR(50)  NULL     COLLATE "C.utf8",
	"BankCode"            VARCHAR(50)  NULL     COLLATE "C",
	"Birthdate"           DATE         NULL,
	"PhotoPath"           VARCHAR(200) NULL     COLLATE "C",
	"IdentityId"          VARCHAR(50)  NULL     COLLATE "C",
	"IdentityKind"        SMALLINT     NOT NULL DEFAULT 0,
	"IdentityIssued"      DATE         NULL,
	"IdentityExpiry"      DATE         NULL,
	"IdentityPath1"       VARCHAR(200) NULL     COLLATE "C",
	"IdentityPath2"       VARCHAR(200) NULL     COLLATE "C",
	"MaritalStatus"       SMALLINT     NOT NULL DEFAULT 0,
	"EducationDegree"     SMALLINT     NOT NULL DEFAULT 0,
	"NativePlace"         VARCHAR(50)  NULL     COLLATE "C.utf8",
	"MobilePhone"         VARCHAR(50)  NULL     COLLATE "C",
	"HomePhone"           VARCHAR(50)  NULL     COLLATE "C",
	"HomeCountry"         SMALLINT     NOT NULL DEFAULT 0,
	"HomeAddressId"       INTEGER      NOT NULL DEFAULT 0,
	"HomeAddressDetail"   VARCHAR(100) NULL     COLLATE "C.utf8",
	"OfficePhone"         VARCHAR(50)  NULL     COLLATE "C",
	"OfficeTitle"         VARCHAR(100) NULL     COLLATE "C.utf8",
	"OfficeCountry"       SMALLINT     NOT NULL DEFAULT 0,
	"OfficeAddressId"     INTEGER      NOT NULL DEFAULT 0,
	"OfficeAddressDetail" VARCHAR(100) NULL     COLLATE "C.utf8",
	"CreatorId"           INTEGER      NOT NULL,
	"CreatedTime"         TIMESTAMP    NOT NULL DEFAULT CURRENT_TIMESTAMP,
	"ModifierId"          INTEGER      NULL,
	"ModifiedTime"        TIMESTAMP    NULL,
	"Remark"              VARCHAR(500) NULL     COLLATE "C.utf8",
	PRIMARY KEY ("TenantId", "UserId")
);

CREATE UNIQUE INDEX "UX_Employee_EmployeeNo" ON "Employee" ("TenantId", "EmployeeNo");
CREATE UNIQUE INDEX "UX_Employee_IdentityId" ON "Employee" ("TenantId", "IdentityId");
CREATE INDEX "IX_Employee_Birthdate" ON "Employee" ("TenantId", "Birthdate");
CREATE INDEX "IX_Employee_EmployeeCode" ON "Employee" ("TenantId", "EmployeeCode");
CREATE INDEX "IX_Employee_BranchId" ON "Employee" ("UserId", "TenantId", "BranchId");
CREATE INDEX "IX_Employee_FullName" ON "Employee" ("TenantId", "FullName");

COMMENT ON TABLE "Employee" IS '员工表';
COMMENT ON COLUMN "Employee"."TenantId"            IS '主键，租户编号';
COMMENT ON COLUMN "Employee"."UserId"              IS '主键，用户编号';
COMMENT ON COLUMN "Employee"."BranchId"            IS '分支机构编号';
COMMENT ON COLUMN "Employee"."EmployeeNo"          IS '员工代号';
COMMENT ON COLUMN "Employee"."EmployeeCode"        IS '内部代号';
COMMENT ON COLUMN "Employee"."EmployeeKind"        IS '员工种类';
COMMENT ON COLUMN "Employee"."FullName"            IS '员工全称';
COMMENT ON COLUMN "Employee"."Acronym"             IS '名称缩写';
COMMENT ON COLUMN "Employee"."Summary"             IS '个人简介';
COMMENT ON COLUMN "Employee"."JobTitle"            IS '员工职称';
COMMENT ON COLUMN "Employee"."JobStatus"           IS '就职状态';
COMMENT ON COLUMN "Employee"."Hiredate"            IS '入职日期';
COMMENT ON COLUMN "Employee"."Leavedate"           IS '离职日期';
COMMENT ON COLUMN "Employee"."BankName"            IS '开户银行';
COMMENT ON COLUMN "Employee"."BankCode"            IS '银行账号';
COMMENT ON COLUMN "Employee"."Birthdate"           IS '出生日期';
COMMENT ON COLUMN "Employee"."PhotoPath"           IS '相片路径';
COMMENT ON COLUMN "Employee"."IdentityId"          IS '身份证号';
COMMENT ON COLUMN "Employee"."IdentityKind"        IS '身份证种类';
COMMENT ON COLUMN "Employee"."IdentityIssued"      IS '身份证签发日期';
COMMENT ON COLUMN "Employee"."IdentityExpiry"      IS '身份证过期日期';
COMMENT ON COLUMN "Employee"."IdentityPath1"       IS '证件照片路径1';
COMMENT ON COLUMN "Employee"."IdentityPath2"       IS '证件照片路径2';
COMMENT ON COLUMN "Employee"."MaritalStatus"       IS '婚姻状况';
COMMENT ON COLUMN "Employee"."EducationDegree"     IS '教育程度';
COMMENT ON COLUMN "Employee"."NativePlace"         IS '籍贯';
COMMENT ON COLUMN "Employee"."MobilePhone"         IS '移动电话';
COMMENT ON COLUMN "Employee"."HomePhone"           IS '家庭电话';
COMMENT ON COLUMN "Employee"."HomeCountry"         IS '家庭国别地区';
COMMENT ON COLUMN "Employee"."HomeAddressId"       IS '家庭地址编号';
COMMENT ON COLUMN "Employee"."HomeAddressDetail"   IS '家庭住址';
COMMENT ON COLUMN "Employee"."OfficePhone"         IS '办公电话';
COMMENT ON COLUMN "Employee"."OfficeTitle"         IS '办公单位';
COMMENT ON COLUMN "Employee"."OfficeCountry"       IS '办公国别地区';
COMMENT ON COLUMN "Employee"."OfficeAddressId"     IS '办公地址编号';
COMMENT ON COLUMN "Employee"."OfficeAddressDetail" IS '办公详细地址';
COMMENT ON COLUMN "Employee"."CreatorId"           IS '创建人编号';
COMMENT ON COLUMN "Employee"."CreatedTime"         IS '创建时间';
COMMENT ON COLUMN "Employee"."ModifierId"          IS '修改人编号';
COMMENT ON COLUMN "Employee"."ModifiedTime"        IS '修改时间';
COMMENT ON COLUMN "Employee"."Remark"              IS '备注说明';


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

CREATE UNIQUE INDEX IF NOT EXISTS "UX_Security_Role_Name" ON "Security_Role" ("Namespace", "Name");

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

CREATE UNIQUE INDEX IF NOT EXISTS "UX_Security_User_Name" ON "Security_User" ("Namespace", "Name");
CREATE UNIQUE INDEX IF NOT EXISTS "UX_Security_User_Email" ON "Security_User" ("Namespace", "Email");
CREATE UNIQUE INDEX IF NOT EXISTS "UX_Security_User_Phone" ON "Security_User" ("Namespace", "Phone");

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
