IF OBJECT_ID(N'dbo.Security_Role', N'U') IS NULL
CREATE TABLE [dbo].[Security_Role] (
  [RoleId]      INT           NOT NULL,
  [Namespace]   VARCHAR(50)   NULL,
  [Name]        VARCHAR(50)   NOT NULL,
  [Nickname]    NVARCHAR(50)  NULL,
  [Avatar]      NVARCHAR(100) NULL,
  [Enabled]     BIT           NOT NULL DEFAULT 1,
  [Description] NVARCHAR(500) NULL,
  CONSTRAINT [PK_Security_Role] PRIMARY KEY CLUSTERED ([RoleId]),
  CONSTRAINT [UX_Security_Role_Name] UNIQUE NONCLUSTERED ([Namespace], [Name])
)
GO

IF OBJECT_ID(N'dbo.Security_User', N'U') IS NULL
CREATE TABLE [dbo].[Security_User] (
  [UserId]            INT           NOT NULL,
  [Namespace]         VARCHAR(50)   NULL,
  [Name]              VARCHAR(50)   NOT NULL,
  [Nickname]          NVARCHAR(50)  NULL,
  [Avatar]            NVARCHAR(100) NULL,
  [Password]          VARBINARY(64) NULL,
  [PasswordSalt]      BIGINT        NULL,
  [Email]             VARCHAR(50)   NULL,
  [Phone]             VARCHAR(50)   NULL,
  [Gender]            BIT           NULL,
  [Enabled]           BIT           NOT NULL DEFAULT 1,
  [PasswordQuestion1] NVARCHAR(50)  NULL,
  [PasswordAnswer1]   VARBINARY(64) NULL,
  [PasswordQuestion2] NVARCHAR(50)  NULL,
  [PasswordAnswer2]   VARBINARY(64) NULL,
  [PasswordQuestion3] NVARCHAR(50)  NULL,
  [PasswordAnswer3]   VARBINARY(64) NULL,
  [Creation]          DATETIME      NOT NULL DEFAULT getdate(),
  [Modification]      DATETIME      NULL,
  [Description]       NVARCHAR(500) NULL,
  CONSTRAINT [PK_Security_User] PRIMARY KEY CLUSTERED ([UserId]),
  CONSTRAINT [UX_Security_User_Name] UNIQUE NONCLUSTERED ([Namespace], [Name])
)
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE object_id = OBJECT_ID(N'dbo.Security_User') AND name = N'UX_Security_User_Phone')
CREATE UNIQUE NONCLUSTERED INDEX [UX_Security_User_Phone]
    ON [dbo].[Security_User]([Namespace] ASC, [Phone] ASC) WHERE ([Phone] IS NOT NULL);
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE object_id = OBJECT_ID(N'dbo.Security_User') AND name = N'UX_Security_User_Email')
CREATE UNIQUE NONCLUSTERED INDEX [UX_Security_User_Email]
    ON [dbo].[Security_User]([Namespace] ASC, [Email] ASC) WHERE ([Email] IS NOT NULL);
GO

IF OBJECT_ID(N'dbo.Security_Member', N'U') IS NULL
CREATE TABLE [dbo].[Security_Member] (
  [RoleId]     INT     NOT NULL,
  [MemberId]   INT     NOT NULL,
  [MemberType] TINYINT NOT NULL,
  CONSTRAINT [PK_Security_Member] PRIMARY KEY CLUSTERED ([RoleId], [MemberId], [MemberType])
)
GO

IF OBJECT_ID(N'dbo.Security_Privilege', N'U') IS NULL
CREATE TABLE [dbo].[Security_Privilege] (
  [MemberId]      INT         NOT NULL,
  [MemberType]    TINYINT     NOT NULL,
  [PrivilegeName] VARCHAR(50) NOT NULL,
  [PrivilegeMode] TINYINT     NOT NULL,
  CONSTRAINT [PK_Security_Privilege] PRIMARY KEY CLUSTERED ([MemberId], [MemberType], [PrivilegeName])
)
GO

IF OBJECT_ID(N'dbo.Security_PrivilegeFiltering', N'U') IS NULL
CREATE TABLE [dbo].[Security_PrivilegeFiltering] (
  [MemberId]        INT          NOT NULL,
  [MemberType]      TINYINT      NOT NULL,
  [PrivilegeName]   VARCHAR(50)  NOT NULL,
  [PrivilegeFilter] VARCHAR(500) NOT NULL,
  CONSTRAINT [PK_Security_PrivilegeFiltering] PRIMARY KEY CLUSTERED ([MemberId], [MemberType], [PrivilegeName])
)
GO

IF NOT EXISTS (SELECT 1 FROM sys.extended_properties WHERE class = 1 AND major_id = OBJECT_ID(N'dbo.Security_Role') AND minor_id = 0 AND name = N'MS_Description')
	EXEC sys.sp_addextendedproperty @name=N'MS_Description', @value=N'角色表', @level0type=N'SCHEMA',@level0name=N'dbo', @level1type=N'TABLE',@level1name=N'Security_Role'
IF NOT EXISTS (SELECT 1 FROM sys.extended_properties WHERE class = 1 AND major_id = OBJECT_ID(N'dbo.Security_User') AND minor_id = 0 AND name = N'MS_Description')
	EXEC sys.sp_addextendedproperty @name=N'MS_Description', @value=N'用户表', @level0type=N'SCHEMA',@level0name=N'dbo', @level1type=N'TABLE',@level1name=N'Security_User'
IF NOT EXISTS (SELECT 1 FROM sys.extended_properties WHERE class = 1 AND major_id = OBJECT_ID(N'dbo.Security_Member') AND minor_id = 0 AND name = N'MS_Description')
	EXEC sys.sp_addextendedproperty @name=N'MS_Description', @value=N'角色成员表', @level0type=N'SCHEMA',@level0name=N'dbo', @level1type=N'TABLE',@level1name=N'Security_Member'
IF NOT EXISTS (SELECT 1 FROM sys.extended_properties WHERE class = 1 AND major_id = OBJECT_ID(N'dbo.Security_Privilege') AND minor_id = 0 AND name = N'MS_Description')
	EXEC sys.sp_addextendedproperty @name=N'MS_Description', @value=N'权限表', @level0type=N'SCHEMA',@level0name=N'dbo', @level1type=N'TABLE',@level1name=N'Security_Privilege'
IF NOT EXISTS (SELECT 1 FROM sys.extended_properties WHERE class = 1 AND major_id = OBJECT_ID(N'dbo.Security_PrivilegeFiltering') AND minor_id = 0 AND name = N'MS_Description')
	EXEC sys.sp_addextendedproperty @name=N'MS_Description', @value=N'权限过滤表', @level0type=N'SCHEMA',@level0name=N'dbo', @level1type=N'TABLE',@level1name=N'Security_PrivilegeFiltering'
GO

IF NOT EXISTS (SELECT 1 FROM sys.extended_properties WHERE class = 1 AND major_id = OBJECT_ID(N'dbo.Security_Role') AND minor_id = COLUMNPROPERTY(OBJECT_ID(N'dbo.Security_Role'), N'RoleId', 'ColumnId') AND name = N'MS_Description')
	EXEC sys.sp_addextendedproperty @name=N'MS_Description', @value=N'主键，角色编号', @level0type=N'SCHEMA',@level0name=N'dbo', @level1type=N'TABLE',@level1name=N'Security_Role', @level2type=N'COLUMN',@level2name=N'RoleId'
IF NOT EXISTS (SELECT 1 FROM sys.extended_properties WHERE class = 1 AND major_id = OBJECT_ID(N'dbo.Security_Role') AND minor_id = COLUMNPROPERTY(OBJECT_ID(N'dbo.Security_Role'), N'Namespace', 'ColumnId') AND name = N'MS_Description')
	EXEC sys.sp_addextendedproperty @name=N'MS_Description', @value=N'命名空间，表示应用或组织机构的标识', @level0type=N'SCHEMA',@level0name=N'dbo', @level1type=N'TABLE',@level1name=N'Security_Role', @level2type=N'COLUMN',@level2name=N'Namespace'
IF NOT EXISTS (SELECT 1 FROM sys.extended_properties WHERE class = 1 AND major_id = OBJECT_ID(N'dbo.Security_Role') AND minor_id = COLUMNPROPERTY(OBJECT_ID(N'dbo.Security_Role'), N'Name', 'ColumnId') AND name = N'MS_Description')
	EXEC sys.sp_addextendedproperty @name=N'MS_Description', @value=N'角色名称，所属命名空间内具有唯一性', @level0type=N'SCHEMA',@level0name=N'dbo', @level1type=N'TABLE',@level1name=N'Security_Role', @level2type=N'COLUMN',@level2name=N'Name'
IF NOT EXISTS (SELECT 1 FROM sys.extended_properties WHERE class = 1 AND major_id = OBJECT_ID(N'dbo.Security_Role') AND minor_id = COLUMNPROPERTY(OBJECT_ID(N'dbo.Security_Role'), N'Nickname', 'ColumnId') AND name = N'MS_Description')
	EXEC sys.sp_addextendedproperty @name=N'MS_Description', @value=N'角色昵称', @level0type=N'SCHEMA',@level0name=N'dbo', @level1type=N'TABLE',@level1name=N'Security_Role', @level2type=N'COLUMN',@level2name=N'Nickname'
IF NOT EXISTS (SELECT 1 FROM sys.extended_properties WHERE class = 1 AND major_id = OBJECT_ID(N'dbo.Security_Role') AND minor_id = COLUMNPROPERTY(OBJECT_ID(N'dbo.Security_Role'), N'Avatar', 'ColumnId') AND name = N'MS_Description')
	EXEC sys.sp_addextendedproperty @name=N'MS_Description', @value=N'角色头像', @level0type=N'SCHEMA',@level0name=N'dbo', @level1type=N'TABLE',@level1name=N'Security_Role', @level2type=N'COLUMN',@level2name=N'Avatar'
IF NOT EXISTS (SELECT 1 FROM sys.extended_properties WHERE class = 1 AND major_id = OBJECT_ID(N'dbo.Security_Role') AND minor_id = COLUMNPROPERTY(OBJECT_ID(N'dbo.Security_Role'), N'Enabled', 'ColumnId') AND name = N'MS_Description')
	EXEC sys.sp_addextendedproperty @name=N'MS_Description', @value=N'是否可用', @level0type=N'SCHEMA',@level0name=N'dbo', @level1type=N'TABLE',@level1name=N'Security_Role', @level2type=N'COLUMN',@level2name=N'Enabled'
IF NOT EXISTS (SELECT 1 FROM sys.extended_properties WHERE class = 1 AND major_id = OBJECT_ID(N'dbo.Security_Role') AND minor_id = COLUMNPROPERTY(OBJECT_ID(N'dbo.Security_Role'), N'Description', 'ColumnId') AND name = N'MS_Description')
	EXEC sys.sp_addextendedproperty @name=N'MS_Description', @value=N'描述信息', @level0type=N'SCHEMA',@level0name=N'dbo', @level1type=N'TABLE',@level1name=N'Security_Role', @level2type=N'COLUMN',@level2name=N'Description'
GO

IF NOT EXISTS (SELECT 1 FROM sys.extended_properties WHERE class = 1 AND major_id = OBJECT_ID(N'dbo.Security_User') AND minor_id = COLUMNPROPERTY(OBJECT_ID(N'dbo.Security_User'), N'UserId', 'ColumnId') AND name = N'MS_Description')
	EXEC sys.sp_addextendedproperty @name=N'MS_Description', @value=N'主键，用户编号', @level0type=N'SCHEMA',@level0name=N'dbo', @level1type=N'TABLE',@level1name=N'Security_User', @level2type=N'COLUMN',@level2name=N'UserId'
IF NOT EXISTS (SELECT 1 FROM sys.extended_properties WHERE class = 1 AND major_id = OBJECT_ID(N'dbo.Security_User') AND minor_id = COLUMNPROPERTY(OBJECT_ID(N'dbo.Security_User'), N'Namespace', 'ColumnId') AND name = N'MS_Description')
	EXEC sys.sp_addextendedproperty @name=N'MS_Description', @value=N'命名空间，表示应用或组织机构的标识', @level0type=N'SCHEMA',@level0name=N'dbo', @level1type=N'TABLE',@level1name=N'Security_User', @level2type=N'COLUMN',@level2name=N'Namespace'
IF NOT EXISTS (SELECT 1 FROM sys.extended_properties WHERE class = 1 AND major_id = OBJECT_ID(N'dbo.Security_User') AND minor_id = COLUMNPROPERTY(OBJECT_ID(N'dbo.Security_User'), N'Name', 'ColumnId') AND name = N'MS_Description')
	EXEC sys.sp_addextendedproperty @name=N'MS_Description', @value=N'用户名称，所属命名空间内具有唯一性', @level0type=N'SCHEMA',@level0name=N'dbo', @level1type=N'TABLE',@level1name=N'Security_User', @level2type=N'COLUMN',@level2name=N'Name'
IF NOT EXISTS (SELECT 1 FROM sys.extended_properties WHERE class = 1 AND major_id = OBJECT_ID(N'dbo.Security_User') AND minor_id = COLUMNPROPERTY(OBJECT_ID(N'dbo.Security_User'), N'Password', 'ColumnId') AND name = N'MS_Description')
	EXEC sys.sp_addextendedproperty @name=N'MS_Description', @value=N'用户的登录口令', @level0type=N'SCHEMA',@level0name=N'dbo', @level1type=N'TABLE',@level1name=N'Security_User', @level2type=N'COLUMN',@level2name=N'Password'
IF NOT EXISTS (SELECT 1 FROM sys.extended_properties WHERE class = 1 AND major_id = OBJECT_ID(N'dbo.Security_User') AND minor_id = COLUMNPROPERTY(OBJECT_ID(N'dbo.Security_User'), N'PasswordSalt', 'ColumnId') AND name = N'MS_Description')
	EXEC sys.sp_addextendedproperty @name=N'MS_Description', @value=N'口令加密向量(随机数)', @level0type=N'SCHEMA',@level0name=N'dbo', @level1type=N'TABLE',@level1name=N'Security_User', @level2type=N'COLUMN',@level2name=N'PasswordSalt'
IF NOT EXISTS (SELECT 1 FROM sys.extended_properties WHERE class = 1 AND major_id = OBJECT_ID(N'dbo.Security_User') AND minor_id = COLUMNPROPERTY(OBJECT_ID(N'dbo.Security_User'), N'Nickname', 'ColumnId') AND name = N'MS_Description')
	EXEC sys.sp_addextendedproperty @name=N'MS_Description', @value=N'用户昵称', @level0type=N'SCHEMA',@level0name=N'dbo', @level1type=N'TABLE',@level1name=N'Security_User', @level2type=N'COLUMN',@level2name=N'Nickname'
IF NOT EXISTS (SELECT 1 FROM sys.extended_properties WHERE class = 1 AND major_id = OBJECT_ID(N'dbo.Security_User') AND minor_id = COLUMNPROPERTY(OBJECT_ID(N'dbo.Security_User'), N'Avatar', 'ColumnId') AND name = N'MS_Description')
	EXEC sys.sp_addextendedproperty @name=N'MS_Description', @value=N'用户头像', @level0type=N'SCHEMA',@level0name=N'dbo', @level1type=N'TABLE',@level1name=N'Security_User', @level2type=N'COLUMN',@level2name=N'Avatar'
IF NOT EXISTS (SELECT 1 FROM sys.extended_properties WHERE class = 1 AND major_id = OBJECT_ID(N'dbo.Security_User') AND minor_id = COLUMNPROPERTY(OBJECT_ID(N'dbo.Security_User'), N'Email', 'ColumnId') AND name = N'MS_Description')
	EXEC sys.sp_addextendedproperty @name=N'MS_Description', @value=N'用户的电子邮箱，该邮箱地址在所属命名空间内具有唯一性', @level0type=N'SCHEMA',@level0name=N'dbo', @level1type=N'TABLE',@level1name=N'Security_User', @level2type=N'COLUMN',@level2name=N'Email'
IF NOT EXISTS (SELECT 1 FROM sys.extended_properties WHERE class = 1 AND major_id = OBJECT_ID(N'dbo.Security_User') AND minor_id = COLUMNPROPERTY(OBJECT_ID(N'dbo.Security_User'), N'Phone', 'ColumnId') AND name = N'MS_Description')
	EXEC sys.sp_addextendedproperty @name=N'MS_Description', @value=N'用户的手机号码，该手机号码在所属命名空间内具有唯一性', @level0type=N'SCHEMA',@level0name=N'dbo', @level1type=N'TABLE',@level1name=N'Security_User', @level2type=N'COLUMN',@level2name=N'Phone'
IF NOT EXISTS (SELECT 1 FROM sys.extended_properties WHERE class = 1 AND major_id = OBJECT_ID(N'dbo.Security_User') AND minor_id = COLUMNPROPERTY(OBJECT_ID(N'dbo.Security_User'), N'Gender', 'ColumnId') AND name = N'MS_Description')
	EXEC sys.sp_addextendedproperty @name=N'MS_Description', @value=N'用户性别（0:女; 1:男）', @level0type=N'SCHEMA',@level0name=N'dbo', @level1type=N'TABLE',@level1name=N'Security_User', @level2type=N'COLUMN',@level2name=N'Gender'
IF NOT EXISTS (SELECT 1 FROM sys.extended_properties WHERE class = 1 AND major_id = OBJECT_ID(N'dbo.Security_User') AND minor_id = COLUMNPROPERTY(OBJECT_ID(N'dbo.Security_User'), N'Enabled', 'ColumnId') AND name = N'MS_Description')
	EXEC sys.sp_addextendedproperty @name=N'MS_Description', @value=N'是否可用', @level0type=N'SCHEMA',@level0name=N'dbo', @level1type=N'TABLE',@level1name=N'Security_User', @level2type=N'COLUMN',@level2name=N'Enabled'
IF NOT EXISTS (SELECT 1 FROM sys.extended_properties WHERE class = 1 AND major_id = OBJECT_ID(N'dbo.Security_User') AND minor_id = COLUMNPROPERTY(OBJECT_ID(N'dbo.Security_User'), N'PasswordQuestion1', 'ColumnId') AND name = N'MS_Description')
	EXEC sys.sp_addextendedproperty @name=N'MS_Description', @value=N'用户的密码问答的题面(1)', @level0type=N'SCHEMA',@level0name=N'dbo', @level1type=N'TABLE',@level1name=N'Security_User', @level2type=N'COLUMN',@level2name=N'PasswordQuestion1'
IF NOT EXISTS (SELECT 1 FROM sys.extended_properties WHERE class = 1 AND major_id = OBJECT_ID(N'dbo.Security_User') AND minor_id = COLUMNPROPERTY(OBJECT_ID(N'dbo.Security_User'), N'PasswordAnswer1', 'ColumnId') AND name = N'MS_Description')
	EXEC sys.sp_addextendedproperty @name=N'MS_Description', @value=N'用户的密码问答的答案(1)', @level0type=N'SCHEMA',@level0name=N'dbo', @level1type=N'TABLE',@level1name=N'Security_User', @level2type=N'COLUMN',@level2name=N'PasswordAnswer1'
IF NOT EXISTS (SELECT 1 FROM sys.extended_properties WHERE class = 1 AND major_id = OBJECT_ID(N'dbo.Security_User') AND minor_id = COLUMNPROPERTY(OBJECT_ID(N'dbo.Security_User'), N'PasswordQuestion2', 'ColumnId') AND name = N'MS_Description')
	EXEC sys.sp_addextendedproperty @name=N'MS_Description', @value=N'用户的密码问答的题面(2)', @level0type=N'SCHEMA',@level0name=N'dbo', @level1type=N'TABLE',@level1name=N'Security_User', @level2type=N'COLUMN',@level2name=N'PasswordQuestion2'
IF NOT EXISTS (SELECT 1 FROM sys.extended_properties WHERE class = 1 AND major_id = OBJECT_ID(N'dbo.Security_User') AND minor_id = COLUMNPROPERTY(OBJECT_ID(N'dbo.Security_User'), N'PasswordAnswer2', 'ColumnId') AND name = N'MS_Description')
	EXEC sys.sp_addextendedproperty @name=N'MS_Description', @value=N'用户的密码问答的答案(2)', @level0type=N'SCHEMA',@level0name=N'dbo', @level1type=N'TABLE',@level1name=N'Security_User', @level2type=N'COLUMN',@level2name=N'PasswordAnswer2'
IF NOT EXISTS (SELECT 1 FROM sys.extended_properties WHERE class = 1 AND major_id = OBJECT_ID(N'dbo.Security_User') AND minor_id = COLUMNPROPERTY(OBJECT_ID(N'dbo.Security_User'), N'PasswordQuestion3', 'ColumnId') AND name = N'MS_Description')
	EXEC sys.sp_addextendedproperty @name=N'MS_Description', @value=N'用户的密码问答的题面(3)', @level0type=N'SCHEMA',@level0name=N'dbo', @level1type=N'TABLE',@level1name=N'Security_User', @level2type=N'COLUMN',@level2name=N'PasswordQuestion3'
IF NOT EXISTS (SELECT 1 FROM sys.extended_properties WHERE class = 1 AND major_id = OBJECT_ID(N'dbo.Security_User') AND minor_id = COLUMNPROPERTY(OBJECT_ID(N'dbo.Security_User'), N'PasswordAnswer3', 'ColumnId') AND name = N'MS_Description')
	EXEC sys.sp_addextendedproperty @name=N'MS_Description', @value=N'用户的密码问答的答案(3)', @level0type=N'SCHEMA',@level0name=N'dbo', @level1type=N'TABLE',@level1name=N'Security_User', @level2type=N'COLUMN',@level2name=N'PasswordAnswer3'
IF NOT EXISTS (SELECT 1 FROM sys.extended_properties WHERE class = 1 AND major_id = OBJECT_ID(N'dbo.Security_User') AND minor_id = COLUMNPROPERTY(OBJECT_ID(N'dbo.Security_User'), N'Creation', 'ColumnId') AND name = N'MS_Description')
	EXEC sys.sp_addextendedproperty @name=N'MS_Description', @value=N'创建时间', @level0type=N'SCHEMA',@level0name=N'dbo', @level1type=N'TABLE',@level1name=N'Security_User', @level2type=N'COLUMN',@level2name=N'Creation'
IF NOT EXISTS (SELECT 1 FROM sys.extended_properties WHERE class = 1 AND major_id = OBJECT_ID(N'dbo.Security_User') AND minor_id = COLUMNPROPERTY(OBJECT_ID(N'dbo.Security_User'), N'Modification', 'ColumnId') AND name = N'MS_Description')
	EXEC sys.sp_addextendedproperty @name=N'MS_Description', @value=N'最后修改时间', @level0type=N'SCHEMA',@level0name=N'dbo', @level1type=N'TABLE',@level1name=N'Security_User', @level2type=N'COLUMN',@level2name=N'Modification'
IF NOT EXISTS (SELECT 1 FROM sys.extended_properties WHERE class = 1 AND major_id = OBJECT_ID(N'dbo.Security_User') AND minor_id = COLUMNPROPERTY(OBJECT_ID(N'dbo.Security_User'), N'Description', 'ColumnId') AND name = N'MS_Description')
	EXEC sys.sp_addextendedproperty @name=N'MS_Description', @value=N'描述信息', @level0type=N'SCHEMA',@level0name=N'dbo', @level1type=N'TABLE',@level1name=N'Security_User', @level2type=N'COLUMN',@level2name=N'Description'
GO

IF NOT EXISTS (SELECT 1 FROM sys.extended_properties WHERE class = 1 AND major_id = OBJECT_ID(N'dbo.Security_Member') AND minor_id = COLUMNPROPERTY(OBJECT_ID(N'dbo.Security_Member'), N'RoleId', 'ColumnId') AND name = N'MS_Description')
	EXEC sys.sp_addextendedproperty @name=N'MS_Description', @value=N'主键，角色编号', @level0type=N'SCHEMA',@level0name=N'dbo', @level1type=N'TABLE',@level1name=N'Security_Member', @level2type=N'COLUMN',@level2name=N'RoleId'
IF NOT EXISTS (SELECT 1 FROM sys.extended_properties WHERE class = 1 AND major_id = OBJECT_ID(N'dbo.Security_Member') AND minor_id = COLUMNPROPERTY(OBJECT_ID(N'dbo.Security_Member'), N'MemberId', 'ColumnId') AND name = N'MS_Description')
	EXEC sys.sp_addextendedproperty @name=N'MS_Description', @value=N'主键，成员编号', @level0type=N'SCHEMA',@level0name=N'dbo', @level1type=N'TABLE',@level1name=N'Security_Member', @level2type=N'COLUMN',@level2name=N'MemberId'
IF NOT EXISTS (SELECT 1 FROM sys.extended_properties WHERE class = 1 AND major_id = OBJECT_ID(N'dbo.Security_Member') AND minor_id = COLUMNPROPERTY(OBJECT_ID(N'dbo.Security_Member'), N'MemberType', 'ColumnId') AND name = N'MS_Description')
	EXEC sys.sp_addextendedproperty @name=N'MS_Description', @value=N'主键，成员类型', @level0type=N'SCHEMA',@level0name=N'dbo', @level1type=N'TABLE',@level1name=N'Security_Member', @level2type=N'COLUMN',@level2name=N'MemberType'
GO

IF NOT EXISTS (SELECT 1 FROM sys.extended_properties WHERE class = 1 AND major_id = OBJECT_ID(N'dbo.Security_Privilege') AND minor_id = COLUMNPROPERTY(OBJECT_ID(N'dbo.Security_Privilege'), N'MemberId', 'ColumnId') AND name = N'MS_Description')
	EXEC sys.sp_addextendedproperty @name=N'MS_Description', @value=N'主键，成员编号', @level0type=N'SCHEMA',@level0name=N'dbo', @level1type=N'TABLE',@level1name=N'Security_Privilege', @level2type=N'COLUMN',@level2name=N'MemberId'
IF NOT EXISTS (SELECT 1 FROM sys.extended_properties WHERE class = 1 AND major_id = OBJECT_ID(N'dbo.Security_Privilege') AND minor_id = COLUMNPROPERTY(OBJECT_ID(N'dbo.Security_Privilege'), N'MemberType', 'ColumnId') AND name = N'MS_Description')
	EXEC sys.sp_addextendedproperty @name=N'MS_Description', @value=N'主键，成员类型', @level0type=N'SCHEMA',@level0name=N'dbo', @level1type=N'TABLE',@level1name=N'Security_Privilege', @level2type=N'COLUMN',@level2name=N'MemberType'
IF NOT EXISTS (SELECT 1 FROM sys.extended_properties WHERE class = 1 AND major_id = OBJECT_ID(N'dbo.Security_Privilege') AND minor_id = COLUMNPROPERTY(OBJECT_ID(N'dbo.Security_Privilege'), N'PrivilegeName', 'ColumnId') AND name = N'MS_Description')
	EXEC sys.sp_addextendedproperty @name=N'MS_Description', @value=N'主键，权限标识', @level0type=N'SCHEMA',@level0name=N'dbo', @level1type=N'TABLE',@level1name=N'Security_Privilege', @level2type=N'COLUMN',@level2name=N'PrivilegeName'
IF NOT EXISTS (SELECT 1 FROM sys.extended_properties WHERE class = 1 AND major_id = OBJECT_ID(N'dbo.Security_Privilege') AND minor_id = COLUMNPROPERTY(OBJECT_ID(N'dbo.Security_Privilege'), N'PrivilegeMode', 'ColumnId') AND name = N'MS_Description')
	EXEC sys.sp_addextendedproperty @name=N'MS_Description', @value=N'权限模式(0: 表示拒绝; 1: 表示授予)', @level0type=N'SCHEMA',@level0name=N'dbo', @level1type=N'TABLE',@level1name=N'Security_Privilege', @level2type=N'COLUMN',@level2name=N'PrivilegeMode'
GO

IF NOT EXISTS (SELECT 1 FROM sys.extended_properties WHERE class = 1 AND major_id = OBJECT_ID(N'dbo.Security_PrivilegeFiltering') AND minor_id = COLUMNPROPERTY(OBJECT_ID(N'dbo.Security_PrivilegeFiltering'), N'MemberId', 'ColumnId') AND name = N'MS_Description')
	EXEC sys.sp_addextendedproperty @name=N'MS_Description', @value=N'主键，成员编号', @level0type=N'SCHEMA',@level0name=N'dbo', @level1type=N'TABLE',@level1name=N'Security_PrivilegeFiltering', @level2type=N'COLUMN',@level2name=N'MemberId'
IF NOT EXISTS (SELECT 1 FROM sys.extended_properties WHERE class = 1 AND major_id = OBJECT_ID(N'dbo.Security_PrivilegeFiltering') AND minor_id = COLUMNPROPERTY(OBJECT_ID(N'dbo.Security_PrivilegeFiltering'), N'MemberType', 'ColumnId') AND name = N'MS_Description')
	EXEC sys.sp_addextendedproperty @name=N'MS_Description', @value=N'主键，成员类型', @level0type=N'SCHEMA',@level0name=N'dbo', @level1type=N'TABLE',@level1name=N'Security_PrivilegeFiltering', @level2type=N'COLUMN',@level2name=N'MemberType'
IF NOT EXISTS (SELECT 1 FROM sys.extended_properties WHERE class = 1 AND major_id = OBJECT_ID(N'dbo.Security_PrivilegeFiltering') AND minor_id = COLUMNPROPERTY(OBJECT_ID(N'dbo.Security_PrivilegeFiltering'), N'PrivilegeName', 'ColumnId') AND name = N'MS_Description')
	EXEC sys.sp_addextendedproperty @name=N'MS_Description', @value=N'主键，权限标识', @level0type=N'SCHEMA',@level0name=N'dbo', @level1type=N'TABLE',@level1name=N'Security_PrivilegeFiltering', @level2type=N'COLUMN',@level2name=N'PrivilegeName'
IF NOT EXISTS (SELECT 1 FROM sys.extended_properties WHERE class = 1 AND major_id = OBJECT_ID(N'dbo.Security_PrivilegeFiltering') AND minor_id = COLUMNPROPERTY(OBJECT_ID(N'dbo.Security_PrivilegeFiltering'), N'PrivilegeFilter', 'ColumnId') AND name = N'MS_Description')
	EXEC sys.sp_addextendedproperty @name=N'MS_Description', @value=N'权限过滤表达式', @level0type=N'SCHEMA',@level0name=N'dbo', @level1type=N'TABLE',@level1name=N'Security_PrivilegeFiltering', @level2type=N'COLUMN',@level2name=N'PrivilegeFilter'
GO


/* 添加系统内置角色 */
INSERT INTO [dbo].[Security_Role] ([RoleId], [Name], [Nickname], [Description])
SELECT seed.[RoleId], seed.[Name], seed.[Nickname], seed.[Description]
FROM (VALUES
  (1, 'Administrators', N'系统管理', N'系统管理角色(系统内置角色)'),
  (2, 'Security', N'安全管理', N'安全管理角色(系统内置角色)')
) AS seed ([RoleId], [Name], [Nickname], [Description])
WHERE NOT EXISTS (
	SELECT 1 FROM [dbo].[Security_Role] WITH (UPDLOCK, HOLDLOCK)
	WHERE [RoleId] = seed.[RoleId] OR ([Namespace] IS NULL AND [Name] = seed.[Name])
);

/* 添加系统内置用户 */
INSERT INTO [dbo].[Security_User] ([UserId], [Name], [Nickname], [Description])
SELECT seed.[UserId], seed.[Name], seed.[Nickname], seed.[Description]
FROM (VALUES
  (1, 'Administrator', N'系统管理员', N'系统管理员(系统内置帐号)'),
  (2, 'Guest', N'来宾', N'来宾')
) AS seed ([UserId], [Name], [Nickname], [Description])
WHERE NOT EXISTS (
	SELECT 1 FROM [dbo].[Security_User] WITH (UPDLOCK, HOLDLOCK)
	WHERE [UserId] = seed.[UserId] OR ([Namespace] IS NULL AND [Name] = seed.[Name])
);
