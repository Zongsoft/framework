/* 日志表 */
IF OBJECT_ID(N'dbo.[Log]', N'U') IS NULL
BEGIN
	CREATE TABLE dbo.[Log]
	(
		[LogId]       BIGINT IDENTITY(1, 1) NOT NULL,
		[UserId]      INT                   NOT NULL,
		[TenantId]    INT                   NOT NULL,
		[BranchId]    INT                   NOT NULL,
		[Domain]      VARCHAR(50)           NOT NULL DEFAULT '_',
		[Target]      VARCHAR(100)          NULL,
		[Action]      VARCHAR(100)          NULL,
		[Caption]     NVARCHAR(200)         NULL,
		[Content]     NVARCHAR(MAX)         NULL,
		[Severity]    TINYINT               NOT NULL DEFAULT 0,
		[Timestamp]   DATETIME              NOT NULL DEFAULT CURRENT_TIMESTAMP,
		[Description] NVARCHAR(500)         NULL,
		PRIMARY KEY ([LogId])
	);
END;
GO

IF NOT EXISTS (SELECT * FROM sys.indexes WHERE [object_id]=OBJECT_ID(N'dbo.[Log]') AND [name]=N'IX_User')
	CREATE INDEX [IX_User] ON dbo.[Log] ([UserId], [Domain], [Timestamp], [Severity]);
GO

IF NOT EXISTS (SELECT * FROM sys.indexes WHERE [object_id]=OBJECT_ID(N'dbo.[Log]') AND [name]=N'IX_Severity')
	CREATE INDEX [IX_Severity] ON dbo.[Log] ([TenantId], [BranchId], [Domain], [Timestamp], [Severity]);
GO

IF NOT EXISTS (SELECT * FROM sys.indexes WHERE [object_id]=OBJECT_ID(N'dbo.[Log]') AND [name]=N'IX_Target')
	CREATE INDEX [IX_Target] ON dbo.[Log] ([TenantId], [BranchId], [Domain], [Timestamp], [Target], [Action]);
GO

/* 租户表 */
IF OBJECT_ID(N'dbo.[Tenant]', N'U') IS NULL
BEGIN
	CREATE TABLE dbo.[Tenant]
	(
		[TenantId]                          INT            NOT NULL,
		[TenantNo]                          VARCHAR(50)    NOT NULL,
		[Name]                              NVARCHAR(50)   NOT NULL,
		[Abbr]                              NVARCHAR(50)   NULL,
		[Acronym]                           VARCHAR(50)    NULL,
		[LogoPath]                          VARCHAR(200)   NULL,
		[Country]                           SMALLINT       NOT NULL DEFAULT 0,
		[Language]                          CHAR(2)        NOT NULL DEFAULT 'zh',
		[AddressId]                         INT            NOT NULL DEFAULT 0,
		[AddressDetail]                     NVARCHAR(100)  NULL,
		[Longitude]                         FLOAT          NULL,
		[Latitude]                          FLOAT          NULL,
		[TenantTypeId]                      INT            NULL,
		[TenantSubtypeId]                   TINYINT        NULL,
		[BusinessLicenseNo]                 VARCHAR(50)    NULL,
		[BusinessLicenseKind]               TINYINT        NOT NULL DEFAULT 0,
		[BusinessLicenseAuthority]          NVARCHAR(50)   NULL,
		[BusinessLicensePhotoPath]          VARCHAR(200)   NULL,
		[BusinessLicenseIssueDate]          DATE           NULL,
		[BusinessLicenseExpiryDate]         DATE           NULL,
		[BusinessLicenseDescription]        NVARCHAR(500)  NULL,
		[RegisteredCapital]                 SMALLINT       NULL,
		[RegisteredAddress]                 NVARCHAR(100)  NULL,
		[StaffScale]                        TINYINT        NOT NULL DEFAULT 0,
		[AdministratorEmail]                NVARCHAR(50)   NULL,
		[AdministratorPhone]                NVARCHAR(50)   NULL,
		[AdministratorPassword]             VARBINARY(100) NULL,
		[LegalRepresentativeName]           NVARCHAR(50)   NULL,
		[LegalRepresentativeGender]         BIT            NULL,
		[LegalRepresentativeEmail]          VARCHAR(50)    NULL,
		[LegalRepresentativeIdentityId]     VARCHAR(50)    NULL,
		[LegalRepresentativeIdentityKind]   TINYINT        NOT NULL DEFAULT 0,
		[LegalRepresentativeIdentityIssued] DATE           NULL,
		[LegalRepresentativeIdentityExpiry] DATE           NULL,
		[LegalRepresentativeMobilePhone]    VARCHAR(50)    NULL,
		[LegalRepresentativeIdentityPath1]  VARCHAR(200)   NULL,
		[LegalRepresentativeIdentityPath2]  VARCHAR(200)   NULL,
		[BankCode]                          VARCHAR(50)    NULL,
		[BankName]                          NVARCHAR(50)   NULL,
		[BankAccountCode]                   VARCHAR(50)    NULL,
		[BankAccountSetting]                NVARCHAR(500)  NULL,
		[PhoneNumber]                       VARCHAR(50)    NULL,
		[WebUrl]                            VARCHAR(100)   NULL,
		[ContactName]                       NVARCHAR(50)   NULL,
		[ContactGender]                     TINYINT        NULL,
		[ContactEmail]                      VARCHAR(50)    NULL,
		[ContactMobilePhone]                VARCHAR(50)    NULL,
		[ContactOfficePhone]                VARCHAR(50)    NULL,
		[ContactIdentityId]                 VARCHAR(50)    NULL,
		[ContactIdentityKind]               TINYINT        NOT NULL DEFAULT 0,
		[ContactIdentityIssued]             DATE           NULL,
		[ContactIdentityExpiry]             DATE           NULL,
		[ContactIdentityPath1]              VARCHAR(200)   NULL,
		[ContactIdentityPath2]              VARCHAR(200)   NULL,
		[Flags]                             TINYINT        NOT NULL DEFAULT 0,
		[Grade]                             TINYINT        NOT NULL DEFAULT 0,
		[Status]                            TINYINT        NOT NULL DEFAULT 0,
		[StatusTimestamp]                   DATETIME       NULL,
		[StatusDescription]                 NVARCHAR(100)  NULL,
		[CreatorId]                         INT            NOT NULL,
		[CreatedTime]                       DATETIME       NOT NULL DEFAULT CURRENT_TIMESTAMP,
		[ModifierId]                        INT            NULL,
		[ModifiedTime]                      DATETIME       NULL,
		[Remark]                            NVARCHAR(500)  NULL,
		PRIMARY KEY ([TenantId])
	);
END;
GO

IF NOT EXISTS (SELECT * FROM sys.indexes WHERE [object_id]=OBJECT_ID(N'dbo.[Tenant]') AND [name]=N'UX_TenantNo')
	CREATE UNIQUE INDEX [UX_TenantNo] ON dbo.[Tenant] ([TenantNo]);
GO

IF NOT EXISTS (SELECT * FROM sys.indexes WHERE [object_id]=OBJECT_ID(N'dbo.[Tenant]') AND [name]=N'UX_BusinessLicenseNo')
	CREATE UNIQUE INDEX [UX_BusinessLicenseNo] ON dbo.[Tenant] ([BusinessLicenseNo]) WHERE [BusinessLicenseNo] IS NOT NULL;
GO

IF NOT EXISTS (SELECT * FROM sys.indexes WHERE [object_id]=OBJECT_ID(N'dbo.[Tenant]') AND [name]=N'IX_LegalRepresentativeEmail')
	CREATE INDEX [IX_LegalRepresentativeEmail] ON dbo.[Tenant] ([LegalRepresentativeEmail]);
GO

IF NOT EXISTS (SELECT * FROM sys.indexes WHERE [object_id]=OBJECT_ID(N'dbo.[Tenant]') AND [name]=N'IX_LegalRepresentativeIdentityId')
	CREATE INDEX [IX_LegalRepresentativeIdentityId] ON dbo.[Tenant] ([LegalRepresentativeIdentityId]);
GO

IF NOT EXISTS (SELECT * FROM sys.indexes WHERE [object_id]=OBJECT_ID(N'dbo.[Tenant]') AND [name]=N'IX_LegalRepresentativeMobilePhone')
	CREATE INDEX [IX_LegalRepresentativeMobilePhone] ON dbo.[Tenant] ([LegalRepresentativeMobilePhone]);
GO

IF NOT EXISTS (SELECT * FROM sys.indexes WHERE [object_id]=OBJECT_ID(N'dbo.[Tenant]') AND [name]=N'IX_ContactEmail')
	CREATE INDEX [IX_ContactEmail] ON dbo.[Tenant] ([ContactEmail]);
GO

IF NOT EXISTS (SELECT * FROM sys.indexes WHERE [object_id]=OBJECT_ID(N'dbo.[Tenant]') AND [name]=N'IX_ContactIdentityId')
	CREATE INDEX [IX_ContactIdentityId] ON dbo.[Tenant] ([ContactIdentityId]);
GO

IF NOT EXISTS (SELECT * FROM sys.indexes WHERE [object_id]=OBJECT_ID(N'dbo.[Tenant]') AND [name]=N'IX_ContactMobilePhone')
	CREATE INDEX [IX_ContactMobilePhone] ON dbo.[Tenant] ([ContactMobilePhone]);
GO

/* 分支机构表 */
IF OBJECT_ID(N'dbo.[Branch]', N'U') IS NULL
BEGIN
	CREATE TABLE dbo.[Branch]
	(
		[TenantId]                          INT            NOT NULL,
		[BranchId]                          INT            NOT NULL,
		[BranchNo]                          VARCHAR(50)    NOT NULL,
		[Name]                              NVARCHAR(50)   NOT NULL,
		[Abbr]                              NVARCHAR(50)   NULL,
		[Acronym]                           VARCHAR(50)    NULL,
		[LogoPath]                          VARCHAR(200)   NULL,
		[Ordinal]                           SMALLINT       NOT NULL DEFAULT 0,
		[Country]                           SMALLINT       NOT NULL DEFAULT 0,
		[Language]                          CHAR(2)        NOT NULL DEFAULT 'zh',
		[AddressId]                         INT            NOT NULL DEFAULT 0,
		[AddressDetail]                     NVARCHAR(100)  NULL,
		[Longitude]                         FLOAT          NULL,
		[Latitude]                          FLOAT          NULL,
		[BusinessLicenseNo]                 VARCHAR(50)    NULL,
		[BusinessLicenseKind]               TINYINT        NOT NULL DEFAULT 0,
		[BusinessLicenseAuthority]          NVARCHAR(50)   NULL,
		[BusinessLicensePhotoPath]          VARCHAR(200)   NULL,
		[BusinessLicenseIssueDate]          DATE           NULL,
		[BusinessLicenseExpiryDate]         DATE           NULL,
		[BusinessLicenseDescription]        NVARCHAR(500)  NULL,
		[RegisteredCapital]                 SMALLINT       NULL,
		[RegisteredAddress]                 NVARCHAR(100)  NULL,
		[StaffScale]                        TINYINT        NOT NULL DEFAULT 0,
		[LegalRepresentativeName]           NVARCHAR(50)   NULL,
		[LegalRepresentativeGender]         BIT            NULL,
		[LegalRepresentativeEmail]          VARCHAR(50)    NULL,
		[LegalRepresentativeIdentityId]     VARCHAR(50)    NULL,
		[LegalRepresentativeIdentityKind]   TINYINT        NOT NULL DEFAULT 0,
		[LegalRepresentativeIdentityIssued] DATE           NULL,
		[LegalRepresentativeIdentityExpiry] DATE           NULL,
		[LegalRepresentativeMobilePhone]    VARCHAR(50)    NULL,
		[LegalRepresentativeIdentityPath1]  VARCHAR(200)   NULL,
		[LegalRepresentativeIdentityPath2]  VARCHAR(200)   NULL,
		[BankCode]                          VARCHAR(50)    NULL,
		[BankName]                          NVARCHAR(50)   NULL,
		[BankAccountCode]                   VARCHAR(50)    NULL,
		[BankAccountSetting]                NVARCHAR(500)  NULL,
		[PhoneNumber]                       VARCHAR(50)    NULL,
		[PrincipalId]                       INT            NULL,
		[ContactName]                       NVARCHAR(50)   NULL,
		[ContactGender]                     TINYINT        NULL,
		[ContactEmail]                      VARCHAR(50)    NULL,
		[ContactMobilePhone]                VARCHAR(50)    NULL,
		[ContactOfficePhone]                VARCHAR(50)    NULL,
		[ContactIdentityId]                 VARCHAR(50)    NULL,
		[ContactIdentityKind]               TINYINT        NOT NULL DEFAULT 0,
		[ContactIdentityIssued]             DATE           NULL,
		[ContactIdentityExpiry]             DATE           NULL,
		[ContactIdentityPath1]              VARCHAR(200)   NULL,
		[ContactIdentityPath2]              VARCHAR(200)   NULL,
		[Status]                            TINYINT        NOT NULL DEFAULT 0,
		[StatusTimestamp]                   DATETIME       NULL,
		[StatusDescription]                 NVARCHAR(100)  NULL,
		[CreatorId]                         INT            NOT NULL,
		[CreatedTime]                       DATETIME       NOT NULL DEFAULT CURRENT_TIMESTAMP,
		[ModifierId]                        INT            NULL,
		[ModifiedTime]                      DATETIME       NULL,
		[Remark]                            NVARCHAR(500)  NULL,
		PRIMARY KEY ([TenantId], [BranchId])
	);
END;
GO

IF NOT EXISTS (SELECT * FROM sys.indexes WHERE [object_id]=OBJECT_ID(N'dbo.[Branch]') AND [name]=N'UX_BranchNo')
	CREATE UNIQUE INDEX [UX_BranchNo] ON dbo.[Branch] ([TenantId], [BranchNo]);
GO

IF NOT EXISTS (SELECT * FROM sys.indexes WHERE [object_id]=OBJECT_ID(N'dbo.[Branch]') AND [name]=N'UX_BusinessLicenseNo')
	CREATE UNIQUE INDEX [UX_BusinessLicenseNo] ON dbo.[Branch] ([TenantId], [BusinessLicenseNo]) WHERE [BusinessLicenseNo] IS NOT NULL;
GO

IF NOT EXISTS (SELECT * FROM sys.indexes WHERE [object_id]=OBJECT_ID(N'dbo.[Branch]') AND [name]=N'IX_Ordinal')
	CREATE INDEX [IX_Ordinal] ON dbo.[Branch] ([TenantId], [Ordinal]);
GO

/* 分支机构成员表 */
IF OBJECT_ID(N'dbo.[BranchMember]', N'U') IS NULL
BEGIN
	CREATE TABLE dbo.[BranchMember]
	(
		[TenantId] INT NOT NULL,
		[BranchId] INT NOT NULL,
		[UserId]   INT NOT NULL,
		PRIMARY KEY ([TenantId], [BranchId], [UserId])
	);
END;
GO

IF NOT EXISTS (SELECT * FROM sys.indexes WHERE [object_id]=OBJECT_ID(N'dbo.[BranchMember]') AND [name]=N'IX_User')
	CREATE INDEX [IX_User] ON dbo.[BranchMember] ([TenantId], [UserId], [BranchId]);
GO

/* 部门表 */
IF OBJECT_ID(N'dbo.[Department]', N'U') IS NULL
BEGIN
	CREATE TABLE dbo.[Department]
	(
		[TenantId]     INT           NOT NULL,
		[BranchId]     INT           NOT NULL,
		[DepartmentId] SMALLINT      NOT NULL,
		[ParentId]     SMALLINT      NOT NULL,
		[DepartmentNo] VARCHAR(50)   NOT NULL,
		[Name]         NVARCHAR(50)  NOT NULL,
		[Acronym]      VARCHAR(50)   NULL,
		[Icon]         VARCHAR(100)  NULL,
		[PrincipalId]  INT           NULL,
		[PhoneNumber]  VARCHAR(50)   NULL,
		[Address]      NVARCHAR(100) NULL,
		[Ordinal]      SMALLINT      NOT NULL DEFAULT 0,
		[Remark]       NVARCHAR(500) NULL,
		PRIMARY KEY ([TenantId], [BranchId], [DepartmentId])
	);
END;
GO

IF NOT EXISTS (SELECT * FROM sys.indexes WHERE [object_id]=OBJECT_ID(N'dbo.[Department]') AND [name]=N'UX_DepartmentNo')
	CREATE UNIQUE INDEX [UX_DepartmentNo] ON dbo.[Department] ([TenantId], [BranchId], [DepartmentNo]);
GO

IF NOT EXISTS (SELECT * FROM sys.indexes WHERE [object_id]=OBJECT_ID(N'dbo.[Department]') AND [name]=N'IX_Ordinal')
	CREATE INDEX [IX_Ordinal] ON dbo.[Department] ([TenantId], [BranchId], [Ordinal]);
GO

/* 部门成员表 */
IF OBJECT_ID(N'dbo.[DepartmentMember]', N'U') IS NULL
BEGIN
	CREATE TABLE dbo.[DepartmentMember]
	(
		[TenantId]     INT      NOT NULL,
		[BranchId]     INT      NOT NULL,
		[DepartmentId] SMALLINT NOT NULL,
		[UserId]       INT      NOT NULL,
		PRIMARY KEY ([TenantId], [BranchId], [DepartmentId], [UserId])
	);
END;
GO

IF NOT EXISTS (SELECT * FROM sys.indexes WHERE [object_id]=OBJECT_ID(N'dbo.[DepartmentMember]') AND [name]=N'IX_UserId')
	CREATE INDEX [IX_UserId] ON dbo.[DepartmentMember] ([TenantId], [BranchId], [UserId]);
GO

/* 班组表 */
IF OBJECT_ID(N'dbo.[Team]', N'U') IS NULL
BEGIN
	CREATE TABLE dbo.[Team]
	(
		[TenantId]     INT           NOT NULL,
		[BranchId]     INT           NOT NULL,
		[TeamId]       SMALLINT      NOT NULL,
		[TeamNo]       VARCHAR(50)   NOT NULL,
		[Name]         NVARCHAR(50)  NOT NULL,
		[Acronym]      VARCHAR(50)   NULL,
		[Icon]         VARCHAR(100)  NULL,
		[LeaderId]     INT           NULL,
		[DepartmentId] SMALLINT      NULL,
		[Visible]      BIT           NOT NULL DEFAULT 1,
		[Ordinal]      SMALLINT      NOT NULL DEFAULT 0,
		[Remark]       NVARCHAR(500) NULL,
		PRIMARY KEY ([TenantId], [BranchId], [TeamId])
	);
END;
GO

IF NOT EXISTS (SELECT * FROM sys.indexes WHERE [object_id]=OBJECT_ID(N'dbo.[Team]') AND [name]=N'UX_TeamNo')
	CREATE UNIQUE INDEX [UX_TeamNo] ON dbo.[Team] ([TenantId], [BranchId], [TeamNo]);
GO

IF NOT EXISTS (SELECT * FROM sys.indexes WHERE [object_id]=OBJECT_ID(N'dbo.[Team]') AND [name]=N'IX_Ordinal')
	CREATE INDEX [IX_Ordinal] ON dbo.[Team] ([TenantId], [BranchId], [Ordinal]);
GO

/* 班组成员表 */
IF OBJECT_ID(N'dbo.[TeamMember]', N'U') IS NULL
BEGIN
	CREATE TABLE dbo.[TeamMember]
	(
		[TenantId] INT      NOT NULL,
		[BranchId] INT      NOT NULL,
		[TeamId]   SMALLINT NOT NULL,
		[UserId]   INT      NOT NULL,
		PRIMARY KEY ([TenantId], [BranchId], [TeamId], [UserId])
	);
END;
GO

IF NOT EXISTS (SELECT * FROM sys.indexes WHERE [object_id]=OBJECT_ID(N'dbo.[TeamMember]') AND [name]=N'IX_UserId')
	CREATE INDEX [IX_UserId] ON dbo.[TeamMember] ([TenantId], [BranchId], [UserId]);
GO

/* 员工表 */
IF OBJECT_ID(N'dbo.[Employee]', N'U') IS NULL
BEGIN
	CREATE TABLE dbo.[Employee]
	(
		[TenantId]            INT           NOT NULL,
		[UserId]              INT           NOT NULL,
		[BranchId]            INT           NOT NULL,
		[EmployeeNo]          VARCHAR(50)   NULL,
		[EmployeeCode]        VARCHAR(50)   NULL,
		[EmployeeKind]        TINYINT       NOT NULL DEFAULT 0,
		[FullName]            NVARCHAR(50)  NULL,
		[Acronym]             VARCHAR(50)   NULL,
		[Summary]             NVARCHAR(500) NULL,
		[JobTitle]            NVARCHAR(50)  NULL,
		[JobStatus]           TINYINT       NOT NULL DEFAULT 0,
		[Hiredate]            DATE          NULL,
		[Leavedate]           DATE          NULL,
		[BankName]            NVARCHAR(50)  NULL,
		[BankCode]            VARCHAR(50)   NULL,
		[Birthdate]           DATE          NULL,
		[PhotoPath]           VARCHAR(200)  NULL,
		[IdentityId]          VARCHAR(50)   NULL,
		[IdentityKind]        TINYINT       NOT NULL DEFAULT 0,
		[IdentityIssued]      DATE          NULL,
		[IdentityExpiry]      DATE          NULL,
		[IdentityPath1]       VARCHAR(200)  NULL,
		[IdentityPath2]       VARCHAR(200)  NULL,
		[MaritalStatus]       TINYINT       NOT NULL DEFAULT 0,
		[EducationDegree]     TINYINT       NOT NULL DEFAULT 0,
		[NativePlace]         NVARCHAR(50)  NULL,
		[MobilePhone]         VARCHAR(50)   NULL,
		[HomePhone]           VARCHAR(50)   NULL,
		[HomeCountry]         SMALLINT      NOT NULL DEFAULT 0,
		[HomeAddressId]       INT           NOT NULL DEFAULT 0,
		[HomeAddressDetail]   NVARCHAR(100) NULL,
		[OfficePhone]         VARCHAR(50)   NULL,
		[OfficeTitle]         NVARCHAR(100) NULL,
		[OfficeCountry]       SMALLINT      NOT NULL DEFAULT 0,
		[OfficeAddressId]     INT           NOT NULL DEFAULT 0,
		[OfficeAddressDetail] NVARCHAR(100) NULL,
		[CreatorId]           INT           NOT NULL,
		[CreatedTime]         DATETIME      NOT NULL DEFAULT CURRENT_TIMESTAMP,
		[ModifierId]          INT           NULL,
		[ModifiedTime]        DATETIME      NULL,
		[Remark]              NVARCHAR(500) NULL,
		PRIMARY KEY ([TenantId], [UserId])
	);
END;
GO

IF NOT EXISTS (SELECT * FROM sys.indexes WHERE [object_id]=OBJECT_ID(N'dbo.[Employee]') AND [name]=N'UX_EmployeeNo')
	CREATE UNIQUE INDEX [UX_EmployeeNo] ON dbo.[Employee] ([TenantId], [EmployeeNo]) WHERE [EmployeeNo] IS NOT NULL;
GO

IF NOT EXISTS (SELECT * FROM sys.indexes WHERE [object_id]=OBJECT_ID(N'dbo.[Employee]') AND [name]=N'UX_IdentityId')
	CREATE UNIQUE INDEX [UX_IdentityId] ON dbo.[Employee] ([TenantId], [IdentityId]) WHERE [IdentityId] IS NOT NULL;
GO

IF NOT EXISTS (SELECT * FROM sys.indexes WHERE [object_id]=OBJECT_ID(N'dbo.[Employee]') AND [name]=N'IX_Birthdate')
	CREATE INDEX [IX_Birthdate] ON dbo.[Employee] ([TenantId], [Birthdate]);
GO

IF NOT EXISTS (SELECT * FROM sys.indexes WHERE [object_id]=OBJECT_ID(N'dbo.[Employee]') AND [name]=N'IX_EmployeeCode')
	CREATE INDEX [IX_EmployeeCode] ON dbo.[Employee] ([TenantId], [EmployeeCode]);
GO

IF NOT EXISTS (SELECT * FROM sys.indexes WHERE [object_id]=OBJECT_ID(N'dbo.[Employee]') AND [name]=N'IX_BranchId')
	CREATE INDEX [IX_BranchId] ON dbo.[Employee] ([UserId], [TenantId], [BranchId]);
GO

IF NOT EXISTS (SELECT * FROM sys.indexes WHERE [object_id]=OBJECT_ID(N'dbo.[Employee]') AND [name]=N'IX_FullName')
	CREATE INDEX [IX_FullName] ON dbo.[Employee] ([TenantId], [FullName]);
GO

/* 角色表 */
IF OBJECT_ID(N'dbo.[Security_Role]', N'U') IS NULL
BEGIN
	CREATE TABLE dbo.[Security_Role]
	(
		[RoleId]      INT           NOT NULL,
		[Namespace]   VARCHAR(50)   NULL,
		[Name]        NVARCHAR(50)  NOT NULL,
		[Avatar]      NVARCHAR(100) NULL,
		[Enabled]     BIT           NOT NULL DEFAULT 1,
		[Nickname]    NVARCHAR(50)  NULL,
		[Description] NVARCHAR(500) NULL,
		PRIMARY KEY ([RoleId])
	);
END;
GO

IF NOT EXISTS (SELECT * FROM sys.indexes WHERE [object_id]=OBJECT_ID(N'dbo.[Security_Role]') AND [name]=N'UX_Security_Role_Name')
	CREATE UNIQUE INDEX [UX_Security_Role_Name] ON dbo.[Security_Role] ([Namespace], [Name]) WHERE [Namespace] IS NOT NULL;
GO

/* 用户表 */
IF OBJECT_ID(N'dbo.[Security_User]', N'U') IS NULL
BEGIN
	CREATE TABLE dbo.[Security_User]
	(
		[UserId]           INT            NOT NULL,
		[Namespace]        VARCHAR(50)    NULL,
		[Name]             NVARCHAR(50)   NOT NULL,
		[Avatar]           NVARCHAR(100)  NULL,
		[Nickname]         NVARCHAR(50)   NULL,
		[Password]         VARBINARY(64)  NULL,
		[Email]            VARCHAR(50)    NULL,
		[Phone]            VARCHAR(50)    NULL,
		[Gender]           BIT            NULL,
		[Enabled]          BIT            NOT NULL DEFAULT 1,
		[PasswordQuestion] NVARCHAR(200)  NULL,
		[PasswordAnswer]   VARBINARY(200) NULL,
		[Creation]         DATETIME       NOT NULL DEFAULT CURRENT_TIMESTAMP,
		[Modification]     DATETIME       NULL,
		[Description]      NVARCHAR(500)  NULL,
		PRIMARY KEY ([UserId])
	);
END;
GO

IF NOT EXISTS (SELECT * FROM sys.indexes WHERE [object_id]=OBJECT_ID(N'dbo.[Security_User]') AND [name]=N'UX_Security_User_Name')
	CREATE UNIQUE INDEX [UX_Security_User_Name] ON dbo.[Security_User] ([Namespace], [Name]) WHERE [Namespace] IS NOT NULL;
GO

IF NOT EXISTS (SELECT * FROM sys.indexes WHERE [object_id]=OBJECT_ID(N'dbo.[Security_User]') AND [name]=N'UX_Security_User_Email')
	CREATE UNIQUE INDEX [UX_Security_User_Email] ON dbo.[Security_User] ([Namespace], [Email]) WHERE [Namespace] IS NOT NULL AND [Email] IS NOT NULL;
GO

IF NOT EXISTS (SELECT * FROM sys.indexes WHERE [object_id]=OBJECT_ID(N'dbo.[Security_User]') AND [name]=N'UX_Security_User_Phone')
	CREATE UNIQUE INDEX [UX_Security_User_Phone] ON dbo.[Security_User] ([Namespace], [Phone]) WHERE [Namespace] IS NOT NULL AND [Phone] IS NOT NULL;
GO

/* 角色成员表 */
IF OBJECT_ID(N'dbo.[Security_Member]', N'U') IS NULL
BEGIN
	CREATE TABLE dbo.[Security_Member]
	(
		[RoleId]     INT     NOT NULL,
		[MemberId]   INT     NOT NULL,
		[MemberType] TINYINT NOT NULL,
		PRIMARY KEY ([RoleId], [MemberId], [MemberType])
	);
END;
GO

/* 初始化“租户”数据 */
IF NOT EXISTS (SELECT * FROM dbo.[Tenant] WHERE [TenantId]=1)
	INSERT INTO dbo.[Tenant]
		([TenantId], [TenantNo], [Abbr], [Name], [Acronym], [WebUrl], [CreatorId], [CreatedTime])
	VALUES
		(1, 'Zongsoft', N'Zongsoft', N'Zongsoft Studio', 'ZS', 'http://zongsoft.com', 1, '2025-12-05');
GO

/* 添加系统内置角色 */
IF NOT EXISTS (SELECT * FROM dbo.[Security_Role] WHERE [RoleId]=1)
	INSERT INTO dbo.[Security_Role] ([RoleId], [Name], [Nickname], [Description])
	VALUES (1, N'Administrators', N'系统管理', N'系统管理角色(系统内置角色)');
GO

IF NOT EXISTS (SELECT * FROM dbo.[Security_Role] WHERE [RoleId]=2)
	INSERT INTO dbo.[Security_Role] ([RoleId], [Name], [Nickname], [Description])
	VALUES (2, N'Security', N'安全管理', N'安全管理角色(系统内置角色)');
GO

/* 添加系统内置用户 */
IF NOT EXISTS (SELECT * FROM dbo.[Security_User] WHERE [UserId]=1)
	INSERT INTO dbo.[Security_User] ([UserId], [Name], [Nickname], [Description])
	VALUES (1, N'Administrator', N'系统管理员', N'系统管理员(系统内置帐号)');
GO

IF NOT EXISTS (SELECT * FROM dbo.[Security_User] WHERE [UserId]=2)
	INSERT INTO dbo.[Security_User] ([UserId], [Name], [Nickname], [Description])
	VALUES (2, N'Guest', N'来宾', N'来宾');
GO
