/* 日志表 */
CREATE TABLE IF NOT EXISTS "Log"
(
	"LogId"       INTEGER PRIMARY KEY AUTOINCREMENT,
	"UserId"      INTEGER       NOT NULL,
	"TenantId"    INTEGER       NOT NULL,
	"BranchId"    INTEGER       NOT NULL,
	"Domain"      VARCHAR(50)   NOT NULL DEFAULT '_',
	"Target"      VARCHAR(100),
	"Action"      VARCHAR(100),
	"Caption"     VARCHAR(200)  COLLATE NOCASE,
	"Content"     TEXT          COLLATE NOCASE,
	"Severity"    TINYINT       NOT NULL DEFAULT 0,
	"Timestamp"   DATETIME      NOT NULL DEFAULT CURRENT_TIMESTAMP,
	"Description" VARCHAR(500)  COLLATE NOCASE
);

CREATE INDEX IF NOT EXISTS "IX_User" ON "Log" ("UserId", "Domain", "Timestamp", "Severity");
CREATE INDEX IF NOT EXISTS "IX_Severity" ON "Log" ("TenantId", "BranchId", "Domain", "Timestamp", "Severity");
CREATE INDEX IF NOT EXISTS "IX_Target" ON "Log" ("TenantId", "BranchId", "Domain", "Timestamp", "Target", "Action");

/* 租户表 */
CREATE TABLE IF NOT EXISTS "Tenant"
(
	"TenantId"                          INTEGER      NOT NULL,
	"TenantNo"                          VARCHAR(50)  NOT NULL COLLATE NOCASE,
	"Name"                              VARCHAR(50)  NOT NULL COLLATE NOCASE,
	"Abbr"                              VARCHAR(50)  COLLATE NOCASE,
	"Acronym"                           VARCHAR(50)  COLLATE NOCASE,
	"LogoPath"                          VARCHAR(200) COLLATE NOCASE,
	"Country"                           INTEGER      NOT NULL DEFAULT 0,
	"Language"                          CHAR(2)      NOT NULL DEFAULT 'zh',
	"AddressId"                         INTEGER      NOT NULL DEFAULT 0,
	"AddressDetail"                     VARCHAR(100) COLLATE NOCASE,
	"Longitude"                         DOUBLE PRECISION,
	"Latitude"                          DOUBLE PRECISION,
	"TenantTypeId"                      INTEGER,
	"TenantSubtypeId"                   SMALLINT,
	"BusinessLicenseNo"                 VARCHAR(50)  COLLATE NOCASE,
	"BusinessLicenseKind"               TINYINT      NOT NULL DEFAULT 0,
	"BusinessLicenseAuthority"          VARCHAR(50)  COLLATE NOCASE,
	"BusinessLicensePhotoPath"          VARCHAR(200) COLLATE NOCASE,
	"BusinessLicenseIssueDate"          DATE,
	"BusinessLicenseExpiryDate"         DATE,
	"BusinessLicenseDescription"        VARCHAR(500) COLLATE NOCASE,
	"RegisteredCapital"                 INTEGER,
	"RegisteredAddress"                 VARCHAR(100) COLLATE NOCASE,
	"StaffScale"                        TINYINT      NOT NULL DEFAULT 0,
	"AdministratorEmail"                VARCHAR(50),
	"AdministratorPhone"                VARCHAR(50),
	"AdministratorPassword"             BLOB,
	"LegalRepresentativeName"           VARCHAR(50)  COLLATE NOCASE,
	"LegalRepresentativeGender"         BOOLEAN,
	"LegalRepresentativeEmail"          VARCHAR(50)  COLLATE NOCASE,
	"LegalRepresentativeIdentityId"     VARCHAR(50)  COLLATE NOCASE,
	"LegalRepresentativeIdentityKind"   TINYINT      NOT NULL DEFAULT 0,
	"LegalRepresentativeIdentityIssued" DATE,
	"LegalRepresentativeIdentityExpiry" DATE,
	"LegalRepresentativeMobilePhone"    VARCHAR(50)  COLLATE NOCASE,
	"LegalRepresentativeIdentityPath1"  VARCHAR(200) COLLATE NOCASE,
	"LegalRepresentativeIdentityPath2"  VARCHAR(200) COLLATE NOCASE,
	"BankCode"                          VARCHAR(50)  COLLATE NOCASE,
	"BankName"                          VARCHAR(50)  COLLATE NOCASE,
	"BankAccountCode"                   VARCHAR(50)  COLLATE NOCASE,
	"BankAccountSetting"                VARCHAR(500) COLLATE NOCASE,
	"PhoneNumber"                       VARCHAR(50)  COLLATE NOCASE,
	"WebUrl"                            VARCHAR(100) COLLATE NOCASE,
	"ContactName"                       VARCHAR(50)  COLLATE NOCASE,
	"ContactGender"                     BOOLEAN,
	"ContactEmail"                      VARCHAR(50)  COLLATE NOCASE,
	"ContactMobilePhone"                VARCHAR(50)  COLLATE NOCASE,
	"ContactOfficePhone"                VARCHAR(50)  COLLATE NOCASE,
	"ContactIdentityId"                 VARCHAR(50)  COLLATE NOCASE,
	"ContactIdentityKind"               TINYINT      NOT NULL DEFAULT 0,
	"ContactIdentityIssued"             DATE,
	"ContactIdentityExpiry"             DATE,
	"ContactIdentityPath1"              VARCHAR(200) COLLATE NOCASE,
	"ContactIdentityPath2"              VARCHAR(200) COLLATE NOCASE,
	"Flags"                             TINYINT      NOT NULL DEFAULT 0,
	"Grade"                             TINYINT      NOT NULL DEFAULT 0,
	"Status"                            TINYINT      NOT NULL DEFAULT 0,
	"StatusTimestamp"                   DATETIME,
	"StatusDescription"                 VARCHAR(100) COLLATE NOCASE,
	"CreatorId"                         INTEGER      NOT NULL,
	"CreatedTime"                       DATETIME     NOT NULL DEFAULT CURRENT_TIMESTAMP,
	"ModifierId"                        INTEGER,
	"ModifiedTime"                      DATETIME,
	"Remark"                            VARCHAR(500) COLLATE NOCASE,
	PRIMARY KEY ("TenantId")
);

CREATE UNIQUE INDEX IF NOT EXISTS "UX_Tenant_TenantNo" ON "Tenant" ("TenantNo");
CREATE UNIQUE INDEX IF NOT EXISTS "UX_Tenant_BusinessLicenseNo" ON "Tenant" ("BusinessLicenseNo");
CREATE INDEX IF NOT EXISTS "IX_Tenant_LegalRepresentativeEmail" ON "Tenant" ("LegalRepresentativeEmail");
CREATE INDEX IF NOT EXISTS "IX_Tenant_LegalRepresentativeIdentityId" ON "Tenant" ("LegalRepresentativeIdentityId");
CREATE INDEX IF NOT EXISTS "IX_Tenant_LegalRepresentativeMobilePhone" ON "Tenant" ("LegalRepresentativeMobilePhone");
CREATE INDEX IF NOT EXISTS "IX_Tenant_ContactEmail" ON "Tenant" ("ContactEmail");
CREATE INDEX IF NOT EXISTS "IX_Tenant_ContactIdentityId" ON "Tenant" ("ContactIdentityId");
CREATE INDEX IF NOT EXISTS "IX_Tenant_ContactMobilePhone" ON "Tenant" ("ContactMobilePhone");

/* 分支机构表 */
CREATE TABLE IF NOT EXISTS "Branch"
(
	"TenantId"                          INTEGER      NOT NULL,
	"BranchId"                          INTEGER      NOT NULL,
	"BranchNo"                          VARCHAR(50)  NOT NULL COLLATE NOCASE,
	"Name"                              VARCHAR(50)  NOT NULL COLLATE NOCASE,
	"Abbr"                              VARCHAR(50)  COLLATE NOCASE,
	"Acronym"                           VARCHAR(50)  COLLATE NOCASE,
	"LogoPath"                          VARCHAR(200) COLLATE NOCASE,
	"Ordinal"                           SMALLINT     NOT NULL DEFAULT 0,
	"Country"                           SMALLINT     NOT NULL DEFAULT 0,
	"Language"                          CHAR(2)      NOT NULL DEFAULT 'zh',
	"AddressId"                         INTEGER      NOT NULL DEFAULT 0,
	"AddressDetail"                     VARCHAR(100) COLLATE NOCASE,
	"Longitude"                         DOUBLE PRECISION,
	"Latitude"                          DOUBLE PRECISION,
	"BusinessLicenseNo"                 VARCHAR(50)  COLLATE NOCASE,
	"BusinessLicenseKind"               TINYINT      NOT NULL DEFAULT 0,
	"BusinessLicenseAuthority"          VARCHAR(50)  COLLATE NOCASE,
	"BusinessLicensePhotoPath"          VARCHAR(200) COLLATE NOCASE,
	"BusinessLicenseIssueDate"          DATE,
	"BusinessLicenseExpiryDate"         DATE,
	"BusinessLicenseDescription"        VARCHAR(500) COLLATE NOCASE,
	"RegisteredCapital"                 TINYINT ,
	"RegisteredAddress"                 VARCHAR(100) COLLATE NOCASE,
	"StaffScale"                        TINYINT      NOT NULL DEFAULT 0,
	"LegalRepresentativeName"           VARCHAR(50)  COLLATE NOCASE,
	"LegalRepresentativeGender"         BOOLEAN,
	"LegalRepresentativeEmail"          VARCHAR(50)  COLLATE NOCASE,
	"LegalRepresentativeIdentityId"     VARCHAR(50)  COLLATE NOCASE,
	"LegalRepresentativeIdentityKind"   TINYINT      NOT NULL DEFAULT 0,
	"LegalRepresentativeIdentityIssued" DATE,
	"LegalRepresentativeIdentityExpiry" DATE,
	"LegalRepresentativeMobilePhone"    VARCHAR(50)  COLLATE NOCASE,
	"LegalRepresentativeIdentityPath1"  VARCHAR(200) COLLATE NOCASE,
	"LegalRepresentativeIdentityPath2"  VARCHAR(200) COLLATE NOCASE,
	"BankCode"                          VARCHAR(50)  COLLATE NOCASE,
	"BankName"                          VARCHAR(50)  COLLATE NOCASE,
	"BankAccountCode"                   VARCHAR(50)  COLLATE NOCASE,
	"BankAccountSetting"                VARCHAR(500) COLLATE NOCASE,
	"PhoneNumber"                       VARCHAR(50)  COLLATE NOCASE,
	"PrincipalId"                       INTEGER,
	"ContactName"                       VARCHAR(50)  COLLATE NOCASE,
	"ContactGender"                     BOOLEAN,
	"ContactEmail"                      VARCHAR(50)  COLLATE NOCASE,
	"ContactMobilePhone"                VARCHAR(50)  COLLATE NOCASE,
	"ContactOfficePhone"                VARCHAR(50)  COLLATE NOCASE,
	"ContactIdentityId"                 VARCHAR(50)  COLLATE NOCASE,
	"ContactIdentityKind"               TINYINT      NOT NULL DEFAULT 0,
	"ContactIdentityIssued"             DATE,
	"ContactIdentityExpiry"             DATE,
	"ContactIdentityPath1"              VARCHAR(200) COLLATE NOCASE,
	"ContactIdentityPath2"              VARCHAR(200) COLLATE NOCASE,
	"Status"                            TINYINT      NOT NULL DEFAULT 0,
	"StatusTimestamp"                   DATETIME,
	"StatusDescription"                 VARCHAR(100) COLLATE NOCASE,
	"CreatorId"                         INTEGER      NOT NULL,
	"CreatedTime"                       DATETIME     NOT NULL DEFAULT CURRENT_TIMESTAMP,
	"ModifierId"                        INTEGER,
	"ModifiedTime"                      DATETIME,
	"Remark"                            VARCHAR(500) COLLATE NOCASE,
	PRIMARY KEY ("TenantId", "BranchId")
);

CREATE UNIQUE INDEX IF NOT EXISTS "UX_Branch_BranchNo" ON "Branch" ("TenantId", "BranchNo");
CREATE UNIQUE INDEX IF NOT EXISTS "UX_Branch_BusinessLicenseNo" ON "Branch" ("TenantId", "BusinessLicenseNo");
CREATE INDEX IF NOT EXISTS "IX_Branch_Ordinal" ON "Branch" ("TenantId", "Ordinal");

/* 分支机构成员表 */
CREATE TABLE IF NOT EXISTS "BranchMember"
(
	"TenantId" INTEGER NOT NULL,
	"BranchId" INTEGER NOT NULL,
	"UserId"   INTEGER NOT NULL,
	PRIMARY KEY ("TenantId", "BranchId", "UserId")
);

CREATE INDEX IF NOT EXISTS "IX_BranchMember_User" ON "BranchMember" ("TenantId", "UserId", "BranchId");

/* 部门表 */
CREATE TABLE IF NOT EXISTS "Department"
(
	"TenantId"     INTEGER      NOT NULL,
	"BranchId"     INTEGER      NOT NULL,
	"DepartmentId" SMALLINT     NOT NULL,
	"ParentId"     SMALLINT     NOT NULL,
	"DepartmentNo" VARCHAR(50)  NOT NULL COLLATE NOCASE,
	"Name"         VARCHAR(50)  NOT NULL COLLATE NOCASE,
	"Acronym"      VARCHAR(50)  COLLATE NOCASE,
	"Icon"         VARCHAR(100) COLLATE NOCASE,
	"PrincipalId"  INTEGER,
	"PhoneNumber"  VARCHAR(50)  COLLATE NOCASE,
	"Address"      VARCHAR(100) COLLATE NOCASE,
	"Ordinal"      SMALLINT     NOT NULL DEFAULT 0,
	"Remark"       VARCHAR(500) COLLATE NOCASE,
	PRIMARY KEY ("TenantId", "BranchId", "DepartmentId"),
	CONSTRAINT "UX_DepartmentNo" UNIQUE ("TenantId", "BranchId", "DepartmentNo")
);

CREATE INDEX IF NOT EXISTS "IX_Department_Ordinal" ON "Department" ("TenantId", "BranchId", "Ordinal");

/* 部门成员表 */
CREATE TABLE IF NOT EXISTS "DepartmentMember"
(
	"TenantId"     INTEGER  NOT NULL,
	"BranchId"     INTEGER  NOT NULL,
	"DepartmentId" SMALLINT NOT NULL,
	"UserId"       INTEGER  NOT NULL,
	PRIMARY KEY ("TenantId", "BranchId", "DepartmentId", "UserId")
);

CREATE INDEX IF NOT EXISTS "IX_DepartmentMember_UserId" ON "DepartmentMember" ("TenantId", "BranchId", "UserId");

/* 班组表 */
CREATE TABLE IF NOT EXISTS "Team"
(
	"TenantId"     INTEGER      NOT NULL,
	"BranchId"     INTEGER      NOT NULL,
	"TeamId"       SMALLINT     NOT NULL,
	"TeamNo"       VARCHAR(50)  COLLATE NOCASE NOT NULL,
	"Name"         VARCHAR(50)  COLLATE NOCASE NOT NULL,
	"Acronym"      VARCHAR(50)  COLLATE NOCASE NULL,
	"Icon"         VARCHAR(100) COLLATE NOCASE NULL,
	"LeaderId"     INTEGER      NULL,
	"DepartmentId" SMALLINT     NULL,
	"Visible"      BOOLEAN      NOT NULL DEFAULT true,
	"Ordinal"      SMALLINT     NOT NULL DEFAULT 0,
	"Remark"       VARCHAR(500) COLLATE NOCASE NULL,
	PRIMARY KEY ("TenantId", "BranchId", "TeamId"),
	CONSTRAINT "UX_TeamNo" UNIQUE ("TenantId", "BranchId", "TeamNo")
);

CREATE INDEX IF NOT EXISTS "IX_Team_Ordinal" ON "Team" ("TenantId", "BranchId", "Ordinal");

/* 班组成员表 */
CREATE TABLE IF NOT EXISTS "TeamMember"
(
	"TenantId" INTEGER  NOT NULL,
	"BranchId" INTEGER  NOT NULL,
	"TeamId"   SMALLINT NOT NULL,
	"UserId"   INTEGER  NOT NULL,
	PRIMARY KEY ("TenantId", "BranchId", "TeamId", "UserId")
);

CREATE INDEX IF NOT EXISTS "IX_TeamMember_UserId" ON "TeamMember" ("TenantId", "BranchId", "UserId");

/* 员工表 */
CREATE TABLE IF NOT EXISTS "Employee"
(
	"TenantId"            INTEGER      NOT NULL,
	"UserId"              INTEGER      NOT NULL,
	"BranchId"            INTEGER      NOT NULL,
	"EmployeeNo"          VARCHAR(50)  NULL     COLLATE NOCASE,
	"EmployeeCode"        VARCHAR(50)  NULL     COLLATE NOCASE,
	"EmployeeKind"        TINYINT      NOT NULL DEFAULT 0,
	"FullName"            VARCHAR(50)  NULL     COLLATE NOCASE,
	"Acronym"             VARCHAR(50)  NULL     COLLATE NOCASE,
	"Summary"             VARCHAR(500) NULL     COLLATE NOCASE,
	"JobTitle"            VARCHAR(50)  NULL     COLLATE NOCASE,
	"JobStatus"           TINYINT      NOT NULL DEFAULT 0,
	"Hiredate"            DATE         NULL,
	"Leavedate"           DATE         NULL,
	"BankName"            VARCHAR(50)  NULL     COLLATE NOCASE,
	"BankCode"            VARCHAR(50)  NULL     COLLATE NOCASE,
	"Birthdate"           DATE         NULL,
	"PhotoPath"           VARCHAR(200) NULL     COLLATE NOCASE,
	"IdentityId"          VARCHAR(50)  NULL     COLLATE NOCASE,
	"IdentityKind"        TINYINT      NOT NULL DEFAULT 0,
	"IdentityIssued"      DATE         NULL,
	"IdentityExpiry"      DATE         NULL,
	"IdentityPath1"       VARCHAR(200) NULL     COLLATE NOCASE,
	"IdentityPath2"       VARCHAR(200) NULL     COLLATE NOCASE,
	"MaritalStatus"       TINYINT      NOT NULL DEFAULT 0,
	"EducationDegree"     TINYINT      NOT NULL DEFAULT 0,
	"NativePlace"         VARCHAR(50)  NULL     COLLATE NOCASE,
	"MobilePhone"         VARCHAR(50)  NULL     COLLATE NOCASE,
	"HomePhone"           VARCHAR(50)  NULL     COLLATE NOCASE,
	"HomeCountry"         SMALLINT     NOT NULL DEFAULT 0,
	"HomeAddressId"       INTEGER      NOT NULL DEFAULT 0,
	"HomeAddressDetail"   VARCHAR(100) NULL     COLLATE NOCASE,
	"OfficePhone"         VARCHAR(50)  NULL     COLLATE NOCASE,
	"OfficeTitle"         VARCHAR(100) NULL     COLLATE NOCASE,
	"OfficeCountry"       SMALLINT     NOT NULL DEFAULT 0,
	"OfficeAddressId"     INTEGER      NOT NULL DEFAULT 0,
	"OfficeAddressDetail" VARCHAR(100) NULL     COLLATE NOCASE,
	"CreatorId"           INTEGER      NOT NULL,
	"CreatedTime"         DATETIME     NOT NULL DEFAULT CURRENT_TIMESTAMP,
	"ModifierId"          INTEGER      NULL,
	"ModifiedTime"        DATETIME     NULL,
	"Remark"              VARCHAR(500) NULL     COLLATE NOCASE,
	PRIMARY KEY ("TenantId", "UserId")
);

CREATE UNIQUE INDEX IF NOT EXISTS "UX_Employee_EmployeeNo" ON "Employee" ("TenantId", "EmployeeNo");
CREATE UNIQUE INDEX IF NOT EXISTS "UX_Employee_IdentityId" ON "Employee" ("TenantId", "IdentityId");
CREATE INDEX IF NOT EXISTS "IX_Employee_Birthdate" ON "Employee" ("TenantId", "Birthdate");
CREATE INDEX IF NOT EXISTS "IX_Employee_EmployeeCode" ON "Employee" ("TenantId", "EmployeeCode");
CREATE INDEX IF NOT EXISTS "IX_Employee_BranchId" ON "Employee" ("UserId", "TenantId", "BranchId");
CREATE INDEX IF NOT EXISTS "IX_Employee_FullName" ON "Employee" ("TenantId", "FullName");

/* 角色表 */
CREATE TABLE IF NOT EXISTS "Security_Role"
(
	"RoleId"      INTEGER      NOT NULL,
	"Namespace"   VARCHAR(50)  NULL     COLLATE NOCASE,
	"Name"        VARCHAR(50)  NOT NULL COLLATE NOCASE,
	"Avatar"      VARCHAR(100) NULL     COLLATE NOCASE,
	"Enabled"     BOOLEAN      NOT NULL DEFAULT true,
	"Nickname"    VARCHAR(50)  NULL     COLLATE NOCASE,
	"Description" VARCHAR(500) NULL     COLLATE NOCASE,
	PRIMARY KEY ("RoleId")
);

CREATE UNIQUE INDEX IF NOT EXISTS "UX_Security_Role_Name" ON "Security_Role" ("Namespace", "Name");

/* 用户表 */
CREATE TABLE IF NOT EXISTS "Security_User"
(
	"UserId"            INTEGER      NOT NULL,
	"Namespace"         VARCHAR(50)  NULL     COLLATE NOCASE,
	"Name"              VARCHAR(50)  NOT NULL COLLATE NOCASE,
	"Avatar"            VARCHAR(100) NULL     COLLATE NOCASE,
	"Nickname"          VARCHAR(50)  NULL     COLLATE NOCASE,
	"Password"          BLOB         NULL,
	"Email"             VARCHAR(50)  NULL     COLLATE NOCASE,
	"Phone"             VARCHAR(50)  NULL     COLLATE NOCASE,
	"Gender"            BOOLEAN      NULL,
	"Enabled"           BOOLEAN      NOT NULL DEFAULT true,
	"PasswordQuestion"  VARCHAR(200) NULL     COLLATE NOCASE,
	"PasswordAnswer"    BLOB         NULL,
	"Creation"          DATETIME     NOT NULL DEFAULT CURRENT_TIMESTAMP,
	"Modification"      DATETIME     NULL,
	"Description"       VARCHAR(500) NULL     COLLATE NOCASE,
	PRIMARY KEY ("UserId")
);

CREATE UNIQUE INDEX IF NOT EXISTS "UX_Security_User_Name" ON "Security_User" ("Namespace", "Name");
CREATE UNIQUE INDEX IF NOT EXISTS "UX_Security_User_Email" ON "Security_User" ("Namespace", "Email");
CREATE UNIQUE INDEX IF NOT EXISTS "UX_Security_User_Phone" ON "Security_User" ("Namespace", "Phone");

/* 角色成员表 */
CREATE TABLE IF NOT EXISTS "Security_Member"
(
	"RoleId"     INTEGER  NOT NULL,
	"MemberId"   INTEGER  NOT NULL,
	"MemberType" SMALLINT NOT NULL,
	PRIMARY KEY ("RoleId", "MemberId", "MemberType")
);

/* 添加系统内置角色 */
INSERT OR IGNORE INTO "Security_Role" ("RoleId", "Name", "Nickname", "Description") VALUES
	(1, 'Administrators', '系统管理', '系统管理角色(系统内置角色)'),
	(2, 'Security', '安全管理', '安全管理角色(系统内置角色)');

/* 添加系统内置用户 */
INSERT OR IGNORE INTO "Security_User" ("UserId", "Name", "Nickname", "Description") VALUES
	(1, 'Administrator', '系统管理员', '系统管理员(系统内置帐号)'),
	(2, 'Guest', '来宾', '来宾');
