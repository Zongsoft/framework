CREATE TABLE [dbo].[Security_Role] (
  [RoleId]      INT           NOT NULL,
  [Namespace]   VARCHAR(50)   NULL,
  [Name]        VARCHAR(50)   NOT NULL,
  [Nickname]    NVARCHAR(50)  NULL,
  [Description] NVARCHAR(500) NULL,
  CONSTRAINT [PK_Security_Role] PRIMARY KEY CLUSTERED ([RoleId]),
  CONSTRAINT [UX_Security_Role_Name] UNIQUE NONCLUSTERED ([Namespace], [Module], [Name])
)
GO

CREATE TABLE [dbo].[Security_User] (
  [UserId]            INT           NOT NULL,
  [Namespace]         VARCHAR(50)   NULL,
  [Name]              VARCHAR(50)   NOT NULL,
  [Nickname]          NVARCHAR(50)  NULL,
  [Password]          VARBINARY(64) NULL,
  [PasswordSalt]      BIGINT        NULL,
  [Email]             VARCHAR(50)   NULL,
  [Phone]             VARCHAR(50)   NULL,
  [Status]            TINYINT       NOT NULL DEFAULT 0,
  [StatusTimestamp]   DATETIME      NULL,
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

CREATE UNIQUE NONCLUSTERED INDEX [UX_Security_User_Phone]
    ON [dbo].[Security_User]([Namespace] ASC, [Phone] ASC) WHERE ([Phone] IS NOT NULL);
CREATE UNIQUE NONCLUSTERED INDEX [UX_Security_User_Email]
    ON [dbo].[Security_User]([Namespace] ASC, [Email] ASC) WHERE ([Email] IS NOT NULL);
GO

CREATE TABLE [dbo].[Security_Member] (
  [RoleId]     INT     NOT NULL,
  [MemberId]   INT     NOT NULL,
  [MemberType] TINYINT NOT NULL,
  CONSTRAINT [PK_Security_Member] PRIMARY KEY CLUSTERED ([RoleId], [MemberId], [MemberType])
)
GO

CREATE TABLE [dbo].[Security_Permission] (
  [MemberId]   INT         NOT NULL,
  [MemberType] TINYINT     NOT NULL,
  [Target]     VARCHAR(50) NOT NULL,
  [Action]     VARCHAR(50) NOT NULL,
  [Granted]    BIT         NOT NULL,
  CONSTRAINT [PK_Security_Permission] PRIMARY KEY CLUSTERED ([MemberId], [MemberType], [Target], [Action])
)
GO

CREATE TABLE [dbo].[Security_PermissionFilter] (
  [MemberId]   INT           NOT NULL,
  [MemberType] TINYINT       NOT NULL,
  [Target]     VARCHAR(50)   NOT NULL,
  [Action]     VARCHAR(50)   NOT NULL,
  [Filter]     VARCHAR(1000) NOT NULL,
  CONSTRAINT [PK_Security_PermissionFilter] PRIMARY KEY CLUSTERED ([MemberId], [MemberType], [Target], [Action])
)
GO

CREATE TABLE [dbo].[Security_Censorship] (
  [Name] VARCHAR(50)  NOT NULL,
  [Word] NVARCHAR(50) NOT NULL,
  CONSTRAINT [PK_Security_Censorship] PRIMARY KEY CLUSTERED ([Name], [Word])
)
GO


EXEC sys.sp_addextendedproperty @name=N'MS_Description', @value=N'角色表', @level0type=N'SCHEMA',@level0name=N'dbo', @level1type=N'TABLE',@level1name=N'Security_Role'
EXEC sys.sp_addextendedproperty @name=N'MS_Description', @value=N'用户表', @level0type=N'SCHEMA',@level0name=N'dbo', @level1type=N'TABLE',@level1name=N'Security_User'
EXEC sys.sp_addextendedproperty @name=N'MS_Description', @value=N'角色成员表', @level0type=N'SCHEMA',@level0name=N'dbo', @level1type=N'TABLE',@level1name=N'Security_Member'
EXEC sys.sp_addextendedproperty @name=N'MS_Description', @value=N'权限表', @level0type=N'SCHEMA',@level0name=N'dbo', @level1type=N'TABLE',@level1name=N'Security_Permission'
EXEC sys.sp_addextendedproperty @name=N'MS_Description', @value=N'权限过滤表', @level0type=N'SCHEMA',@level0name=N'dbo', @level1type=N'TABLE',@level1name=N'Security_PermissionFilter'
EXEC sys.sp_addextendedproperty @name=N'MS_Description', @value=N'词汇审查表', @level0type=N'SCHEMA',@level0name=N'dbo', @level1type=N'TABLE',@level1name=N'Security_Censorship'
GO

EXEC sys.sp_addextendedproperty @name=N'MS_Description', @value=N'主键，角色编号', @level0type=N'SCHEMA',@level0name=N'dbo', @level1type=N'TABLE',@level1name=N'Security_Role', @level2type=N'COLUMN',@level2name=N'RoleId'
EXEC sys.sp_addextendedproperty @name=N'MS_Description', @value=N'命名空间，表示应用或组织机构的标识', @level0type=N'SCHEMA',@level0name=N'dbo', @level1type=N'TABLE',@level1name=N'Security_Role', @level2type=N'COLUMN',@level2name=N'Namespace'
EXEC sys.sp_addextendedproperty @name=N'MS_Description', @value=N'角色名称，所属命名空间内具有唯一性', @level0type=N'SCHEMA',@level0name=N'dbo', @level1type=N'TABLE',@level1name=N'Security_Role', @level2type=N'COLUMN',@level2name=N'Name'
EXEC sys.sp_addextendedproperty @name=N'MS_Description', @value=N'角色昵称', @level0type=N'SCHEMA',@level0name=N'dbo', @level1type=N'TABLE',@level1name=N'Security_Role', @level2type=N'COLUMN',@level2name=N'Nickname'
EXEC sys.sp_addextendedproperty @name=N'MS_Description', @value=N'描述信息', @level0type=N'SCHEMA',@level0name=N'dbo', @level1type=N'TABLE',@level1name=N'Security_Role', @level2type=N'COLUMN',@level2name=N'Description'
GO

EXEC sys.sp_addextendedproperty @name=N'MS_Description', @value=N'主键，用户编号', @level0type=N'SCHEMA',@level0name=N'dbo', @level1type=N'TABLE',@level1name=N'Security_User', @level2type=N'COLUMN',@level2name=N'UserId'
EXEC sys.sp_addextendedproperty @name=N'MS_Description', @value=N'命名空间，表示应用或组织机构的标识', @level0type=N'SCHEMA',@level0name=N'dbo', @level1type=N'TABLE',@level1name=N'Security_User', @level2type=N'COLUMN',@level2name=N'Namespace'
EXEC sys.sp_addextendedproperty @name=N'MS_Description', @value=N'用户名称，所属命名空间内具有唯一性', @level0type=N'SCHEMA',@level0name=N'dbo', @level1type=N'TABLE',@level1name=N'Security_User', @level2type=N'COLUMN',@level2name=N'Name'
EXEC sys.sp_addextendedproperty @name=N'MS_Description', @value=N'用户的登录口令', @level0type=N'SCHEMA',@level0name=N'dbo', @level1type=N'TABLE',@level1name=N'Security_User', @level2type=N'COLUMN',@level2name=N'Password'
EXEC sys.sp_addextendedproperty @name=N'MS_Description', @value=N'口令加密向量(随机数)', @level0type=N'SCHEMA',@level0name=N'dbo', @level1type=N'TABLE',@level1name=N'Security_User', @level2type=N'COLUMN',@level2name=N'PasswordSalt'
EXEC sys.sp_addextendedproperty @name=N'MS_Description', @value=N'用户昵称', @level0type=N'SCHEMA',@level0name=N'dbo', @level1type=N'TABLE',@level1name=N'Security_User', @level2type=N'COLUMN',@level2name=N'Nickname'
EXEC sys.sp_addextendedproperty @name=N'MS_Description', @value=N'用户的电子邮箱，该邮箱地址在所属命名空间内具有唯一性', @level0type=N'SCHEMA',@level0name=N'dbo', @level1type=N'TABLE',@level1name=N'Security_User', @level2type=N'COLUMN',@level2name=N'Email'
EXEC sys.sp_addextendedproperty @name=N'MS_Description', @value=N'用户的手机号码，该手机号码在所属命名空间内具有唯一性', @level0type=N'SCHEMA',@level0name=N'dbo', @level1type=N'TABLE',@level1name=N'Security_User', @level2type=N'COLUMN',@level2name=N'Phone'
EXEC sys.sp_addextendedproperty @name=N'MS_Description', @value=N'用户状态（0:正常; 1:待批准; 2:已停用）', @level0type=N'SCHEMA',@level0name=N'dbo', @level1type=N'TABLE',@level1name=N'Security_User', @level2type=N'COLUMN',@level2name=N'Status'
EXEC sys.sp_addextendedproperty @name=N'MS_Description', @value=N'状态更新时间', @level0type=N'SCHEMA',@level0name=N'dbo', @level1type=N'TABLE',@level1name=N'Security_User', @level2type=N'COLUMN',@level2name=N'StatusTimestamp'
EXEC sys.sp_addextendedproperty @name=N'MS_Description', @value=N'用户的密码问答的题面(1)', @level0type=N'SCHEMA',@level0name=N'dbo', @level1type=N'TABLE',@level1name=N'Security_User', @level2type=N'COLUMN',@level2name=N'PasswordQuestion1'
EXEC sys.sp_addextendedproperty @name=N'MS_Description', @value=N'用户的密码问答的答案(1)', @level0type=N'SCHEMA',@level0name=N'dbo', @level1type=N'TABLE',@level1name=N'Security_User', @level2type=N'COLUMN',@level2name=N'PasswordAnswer1'
EXEC sys.sp_addextendedproperty @name=N'MS_Description', @value=N'用户的密码问答的题面(2)', @level0type=N'SCHEMA',@level0name=N'dbo', @level1type=N'TABLE',@level1name=N'Security_User', @level2type=N'COLUMN',@level2name=N'PasswordQuestion2'
EXEC sys.sp_addextendedproperty @name=N'MS_Description', @value=N'用户的密码问答的答案(2)', @level0type=N'SCHEMA',@level0name=N'dbo', @level1type=N'TABLE',@level1name=N'Security_User', @level2type=N'COLUMN',@level2name=N'PasswordAnswer2'
EXEC sys.sp_addextendedproperty @name=N'MS_Description', @value=N'用户的密码问答的题面(3)', @level0type=N'SCHEMA',@level0name=N'dbo', @level1type=N'TABLE',@level1name=N'Security_User', @level2type=N'COLUMN',@level2name=N'PasswordQuestion3'
EXEC sys.sp_addextendedproperty @name=N'MS_Description', @value=N'用户的密码问答的答案(3)', @level0type=N'SCHEMA',@level0name=N'dbo', @level1type=N'TABLE',@level1name=N'Security_User', @level2type=N'COLUMN',@level2name=N'PasswordAnswer3'
EXEC sys.sp_addextendedproperty @name=N'MS_Description', @value=N'创建时间', @level0type=N'SCHEMA',@level0name=N'dbo', @level1type=N'TABLE',@level1name=N'Security_User', @level2type=N'COLUMN',@level2name=N'Creation'
EXEC sys.sp_addextendedproperty @name=N'MS_Description', @value=N'最后修改时间', @level0type=N'SCHEMA',@level0name=N'dbo', @level1type=N'TABLE',@level1name=N'Security_User', @level2type=N'COLUMN',@level2name=N'Modification'
EXEC sys.sp_addextendedproperty @name=N'MS_Description', @value=N'描述信息', @level0type=N'SCHEMA',@level0name=N'dbo', @level1type=N'TABLE',@level1name=N'Security_User', @level2type=N'COLUMN',@level2name=N'Description'
GO

EXEC sys.sp_addextendedproperty @name=N'MS_Description', @value=N'主键，角色编号', @level0type=N'SCHEMA',@level0name=N'dbo', @level1type=N'TABLE',@level1name=N'Security_Member', @level2type=N'COLUMN',@level2name=N'RoleId'
EXEC sys.sp_addextendedproperty @name=N'MS_Description', @value=N'主键，成员编号', @level0type=N'SCHEMA',@level0name=N'dbo', @level1type=N'TABLE',@level1name=N'Security_Member', @level2type=N'COLUMN',@level2name=N'MemberId'
EXEC sys.sp_addextendedproperty @name=N'MS_Description', @value=N'主键，成员类型', @level0type=N'SCHEMA',@level0name=N'dbo', @level1type=N'TABLE',@level1name=N'Security_Member', @level2type=N'COLUMN',@level2name=N'MemberType'
GO

EXEC sys.sp_addextendedproperty @name=N'MS_Description', @value=N'主键，成员编号', @level0type=N'SCHEMA',@level0name=N'dbo', @level1type=N'TABLE',@level1name=N'Security_Permission', @level2type=N'COLUMN',@level2name=N'MemberId'
EXEC sys.sp_addextendedproperty @name=N'MS_Description', @value=N'主键，成员类型', @level0type=N'SCHEMA',@level0name=N'dbo', @level1type=N'TABLE',@level1name=N'Security_Permission', @level2type=N'COLUMN',@level2name=N'MemberType'
EXEC sys.sp_addextendedproperty @name=N'MS_Description', @value=N'主键，授权目标的标识', @level0type=N'SCHEMA',@level0name=N'dbo', @level1type=N'TABLE',@level1name=N'Security_Permission', @level2type=N'COLUMN',@level2name=N'Target'
EXEC sys.sp_addextendedproperty @name=N'MS_Description', @value=N'主键，授权行为的标识', @level0type=N'SCHEMA',@level0name=N'dbo', @level1type=N'TABLE',@level1name=N'Security_Permission', @level2type=N'COLUMN',@level2name=N'Action'
EXEC sys.sp_addextendedproperty @name=N'MS_Description', @value=N'是否授权(0: 表示拒绝; 1: 表示授予)', @level0type=N'SCHEMA',@level0name=N'dbo', @level1type=N'TABLE',@level1name=N'Security_Permission', @level2type=N'COLUMN',@level2name=N'Granted'
GO

EXEC sys.sp_addextendedproperty @name=N'MS_Description', @value=N'主键，成员编号', @level0type=N'SCHEMA',@level0name=N'dbo', @level1type=N'TABLE',@level1name=N'Security_PermissionFilter', @level2type=N'COLUMN',@level2name=N'MemberId'
EXEC sys.sp_addextendedproperty @name=N'MS_Description', @value=N'主键，成员类型', @level0type=N'SCHEMA',@level0name=N'dbo', @level1type=N'TABLE',@level1name=N'Security_PermissionFilter', @level2type=N'COLUMN',@level2name=N'MemberType'
EXEC sys.sp_addextendedproperty @name=N'MS_Description', @value=N'主键，授权目标的标识', @level0type=N'SCHEMA',@level0name=N'dbo', @level1type=N'TABLE',@level1name=N'Security_PermissionFilter', @level2type=N'COLUMN',@level2name=N'Target'
EXEC sys.sp_addextendedproperty @name=N'MS_Description', @value=N'主键，授权行为的标识', @level0type=N'SCHEMA',@level0name=N'dbo', @level1type=N'TABLE',@level1name=N'Security_PermissionFilter', @level2type=N'COLUMN',@level2name=N'Action'
EXEC sys.sp_addextendedproperty @name=N'MS_Description', @value=N'拒绝授权的过滤表达式', @level0type=N'SCHEMA',@level0name=N'dbo', @level1type=N'TABLE',@level1name=N'Security_PermissionFilter', @level2type=N'COLUMN',@level2name=N'Filter'
GO

EXEC sys.sp_addextendedproperty @name=N'MS_Description', @value=N'主键，审查类名', @level0type=N'SCHEMA',@level0name=N'dbo', @level1type=N'TABLE',@level1name=N'Security_Censorship', @level2type=N'COLUMN',@level2name=N'Name'
EXEC sys.sp_addextendedproperty @name=N'MS_Description', @value=N'主键，阻止词汇', @level0type=N'SCHEMA',@level0name=N'dbo', @level1type=N'TABLE',@level1name=N'Security_Censorship', @level2type=N'COLUMN',@level2name=N'Word'
GO


/* 添加系统内置角色 */
INSERT INTO Security_Role (RoleId, Name, Nickname, Description) VALUES (1, 'Administrators', N'系统管理', N'系统管理角色(系统内置角色)');
INSERT INTO Security_Role (RoleId, Name, Nickname, Description) VALUES (2, 'Security', N'安全管理', N'安全管理角色(系统内置角色)');

/* 添加系统内置用户 */
INSERT INTO Security_User (UserId, Name, Nickname, Description, Status) VALUES (1, 'Administrator', N'系统管理员', N'系统管理员(系统内置帐号)', 0);
INSERT INTO Security_User (UserId, Name, Nickname, Description, Status) VALUES (2, 'Guest', N'来宾', N'来宾', 1);

/* 添加系统内置保留名字 */
INSERT INTO Security_Censorship (Name, Word) VALUES ('Names', 'Zongsoft');

/* 添加非法关键字 */
INSERT INTO Security_Censorship (Name, Word) VALUES ('Sensitives', 'fuck');
INSERT INTO Security_Censorship (Name, Word) VALUES ('Sensitives', 'bitch');
