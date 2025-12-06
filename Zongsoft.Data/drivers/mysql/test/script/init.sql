CREATE TABLE IF NOT EXISTS `Log`
(
	`LogId`       bigint unsigned  NOT NULL AUTO_INCREMENT COMMENT '主键，日志编号',
	`UserId`      int unsigned     NOT NULL COMMENT '用户编号',
	`TenantId`    int unsigned     NOT NULL COMMENT '所属租户编号',
	`BranchId`    int unsigned     NOT NULL COMMENT '分支机构编号',
	`Domain`      varchar(50)      NOT NULL COMMENT '领域标识' COLLATE 'ascii_general_ci' DEFAULT '_',
	`Target`      varchar(100)     NULL     COMMENT '操作目标' COLLATE 'ascii_general_ci',
	`Action`      varchar(100)     NULL     COMMENT '操作行为' COLLATE 'ascii_general_ci',
	`Caption`     varchar(200)     NULL     COMMENT '日志标题' COLLATE 'utf8mb4_0900_ai_ci',
	`Content`     text             NULL     COMMENT '日志内容' COLLATE 'utf8mb4_0900_ai_ci',
	`Severity`    tinyint unsigned NOT NULL COMMENT '严重级别' DEFAULT 0,
	`Timestamp`   datetime         NOT NULL COMMENT '日志时间' DEFAULT CURRENT_TIMESTAMP,
	`Description` varchar(500)     NULL     COMMENT '备注说明' COLLATE 'utf8mb4_0900_ai_ci',
	PRIMARY KEY (`LogId`),
	INDEX `IX_User` (`UserId`, `Domain`, `Timestamp`, `Severity`),
	INDEX `IX_Severity` (`TenantId`, `BranchId`, `Domain`, `Timestamp`, `Severity`),
	INDEX `IX_Target` (`TenantId`, `BranchId`, `Domain`, `Timestamp`, `Target`, `Action`)
) ENGINE=InnoDB COMMENT='日志表';

CREATE TABLE IF NOT EXISTS `Tenant`
(
	`TenantId`                          int unsigned      NOT NULL COMMENT '主键，租户编号',
	`TenantNo`                          varchar(50)       NOT NULL COMMENT '租户编码' COLLATE 'ascii_general_ci',
	`Name`                              varchar(50)       NOT NULL COMMENT '租户名称' COLLATE 'utf8mb4_0900_ai_ci',
	`Abbr`                              varchar(50)       NULL     COMMENT '租户简称' COLLATE 'utf8mb4_0900_ai_ci',
	`Acronym`                           varchar(50)       NULL     COMMENT '名称缩写' COLLATE 'ascii_general_ci',
	`LogoPath`                          varchar(200)      NULL     COMMENT '标志图片路径' COLLATE 'ascii_general_ci',
	`Country`                           smallint unsigned NOT NULL COMMENT '国别地区' DEFAULT 0,
	`Language`                          char(2)           NOT NULL COMMENT '语言标识' DEFAULT 'zh',
	`AddressId`                         int unsigned      NOT NULL COMMENT '所在地址代码' DEFAULT 0,
	`AddressDetail`                     varchar(100)      NULL     COMMENT '经营详细地址' COLLATE 'utf8mb4_0900_ai_ci',
	`Longitude`                         double(12,8)      NULL     COMMENT '经度坐标',
	`Latitude`                          double(12,8)      NULL     COMMENT '纬度坐标',
	`TenantTypeId`                      int unsigned      NULL     COMMENT '租户类型编号',
	`TenantSubtypeId`                   tinyint unsigned  NULL     COMMENT '租户子类编号',
	`BusinessLicenseNo`                 varchar(50)       NULL     COMMENT '工商执照号' COLLATE 'ascii_general_ci',
	`BusinessLicenseKind`               tinyint unsigned  NOT NULL COMMENT '工商执照种类' DEFAULT 0,
	`BusinessLicenseAuthority`          varchar(50)       NULL     COMMENT '工商执照发证机关' COLLATE 'utf8mb4_0900_ai_ci',
	`BusinessLicensePhotoPath`          varchar(200)      NULL     COMMENT '工商执照相片路径' COLLATE 'ascii_general_ci',
	`BusinessLicenseIssueDate`          date              NULL     COMMENT '工商执照登记日期',
	`BusinessLicenseExpiryDate`         date              NULL     COMMENT '工商执照过期日期',
	`BusinessLicenseDescription`        varchar(500)      NULL     COMMENT '工商执照经营范围' COLLATE 'utf8mb4_0900_ai_ci',
	`RegisteredCapital`                 smallint unsigned NULL     COMMENT '注册资本(万元)',
	`RegisteredAddress`                 varchar(100)      NULL     COMMENT '注册地址' COLLATE 'utf8mb4_0900_ai_ci',
	`StaffScale`                        tinyint unsigned  NOT NULL COMMENT '人员规模' DEFAULT 0,
	`AdministratorEmail`                varchar(50)       NULL     COMMENT '管理员邮箱',
	`AdministratorPhone`                varchar(50)       NULL     COMMENT '管理员电话',
	`AdministratorPassword`             varbinary(100)    NULL     COMMENT '管理员密码',
	`LegalRepresentativeName`           varchar(50)       NULL     COMMENT '法人姓名' COLLATE 'utf8mb4_0900_ai_ci',
	`LegalRepresentativeGender`         tinyint(1)        NULL     COMMENT '法人性别',
	`LegalRepresentativeEmail`          varchar(50)       NULL     COMMENT '法人邮箱' COLLATE 'ascii_general_ci',
	`LegalRepresentativeIdentityId`     varchar(50)       NULL     COMMENT '法人身份证号码' COLLATE 'ascii_general_ci',
	`LegalRepresentativeIdentityKind`   tinyint unsigned  NOT NULL COMMENT '法人身份证种类' DEFAULT 0,
	`LegalRepresentativeIdentityIssued` date              NULL     COMMENT '法人身份证签发日期',
	`LegalRepresentativeIdentityExpiry` date              NULL     COMMENT '法人身份证过期日期',
	`LegalRepresentativeMobilePhone`    varchar(50)       NULL     COMMENT '法人移动电话' COLLATE 'ascii_general_ci',
	`LegalRepresentativeIdentityPath1`  varchar(200)      NULL     COMMENT '法人证件照片路径1' COLLATE 'ascii_general_ci',
	`LegalRepresentativeIdentityPath2`  varchar(200)      NULL     COMMENT '法人证件照片路径2' COLLATE 'ascii_general_ci',
	`BankCode`                          varchar(50)       NULL     COMMENT '开户银行代号' COLLATE 'ascii_general_ci',
	`BankName`                          varchar(50)       NULL     COMMENT '开户银行名称' COLLATE 'utf8mb4_0900_ai_ci',
	`BankAccountCode`                   varchar(50)       NULL     COMMENT '开户银行账户号码' COLLATE 'ascii_general_ci',
	`BankAccountSetting`                varchar(500)      NULL     COMMENT '开户银行账户设置' COLLATE 'utf8mb4_0900_ai_ci',
	`PhoneNumber`                       varchar(50)       NULL     COMMENT '办公电话' COLLATE 'ascii_general_ci',
	`WebUrl`                            varchar(100)      NULL     COMMENT '网站网址' COLLATE 'ascii_general_ci',
	`ContactName`                       varchar(50)       NULL     COMMENT '联系人姓名' COLLATE 'utf8mb4_0900_ai_ci',
	`ContactGender`                     tinyint unsigned  NULL     COMMENT '联系人性别',
	`ContactEmail`                      varchar(50)       NULL     COMMENT '联系人邮箱' COLLATE 'ascii_general_ci',
	`ContactMobilePhone`                varchar(50)       NULL     COMMENT '联系人移动电话' COLLATE 'ascii_general_ci',
	`ContactOfficePhone`                varchar(50)       NULL     COMMENT '联系人办公电话' COLLATE 'ascii_general_ci',
	`ContactIdentityId`                 varchar(50)       NULL     COMMENT '联系人身份证号' COLLATE 'ascii_general_ci',
	`ContactIdentityKind`               tinyint unsigned  NOT NULL COMMENT '联系人身份证种类' DEFAULT 0,
	`ContactIdentityIssued`             date              NULL     COMMENT '联系人身份证签发日期',
	`ContactIdentityExpiry`             date              NULL     COMMENT '联系人身份证过期日期',
	`ContactIdentityPath1`              varchar(200)      NULL     COMMENT '联系人证件照片路径1' COLLATE 'ascii_general_ci',
	`ContactIdentityPath2`              varchar(200)      NULL     COMMENT '联系人证件照片路径2' COLLATE 'ascii_general_ci',
	`Flags`                             tinyint unsigned  NOT NULL COMMENT '标记' DEFAULT 0,
	`Grade`                             tinyint unsigned  NOT NULL COMMENT '等级' DEFAULT 0,
	`Status`                            tinyint unsigned  NOT NULL COMMENT '租户状态' DEFAULT 0,
	`StatusTimestamp`                   datetime          NULL     COMMENT '状态变更时间',
	`StatusDescription`                 varchar(100)      NULL     COMMENT '状态变更描述' COLLATE 'utf8mb4_0900_ai_ci',
	`CreatorId`                         int unsigned      NOT NULL COMMENT '创建人编号',
	`CreatedTime`                       datetime          NOT NULL COMMENT '创建时间' DEFAULT CURRENT_TIMESTAMP,
	`ModifierId`                        int unsigned      NULL     COMMENT '修改人编号',
	`ModifiedTime`                      datetime          NULL     COMMENT '修改时间',
	`Remark`                            varchar(500)      NULL     COMMENT '备注' COLLATE 'utf8mb4_0900_ai_ci',
	PRIMARY KEY (`TenantId`),
	UNIQUE INDEX `UX_TenantNo` (`TenantNo`),
	UNIQUE INDEX `UX_BusinessLicenseNo` (`BusinessLicenseNo`),
	INDEX `IX_LegalRepresentativeEmail` (`LegalRepresentativeEmail`),
	INDEX `IX_LegalRepresentativeIdentityId` (`LegalRepresentativeIdentityId`),
	INDEX `IX_LegalRepresentativeMobilePhone` (`LegalRepresentativeMobilePhone`),
	INDEX `IX_ContactEmail` (`ContactEmail`),
	INDEX `IX_ContactIdentityId` (`ContactIdentityId`),
	INDEX `IX_ContactMobilePhone` (`ContactMobilePhone`)
) ENGINE=InnoDB COMMENT '租户表';

CREATE TABLE IF NOT EXISTS `Branch`
(
	`TenantId`                          int unsigned      NOT NULL COMMENT '主键，租户编号',
	`BranchId`                          int unsigned      NOT NULL COMMENT '主键，分支编号',
	`BranchNo`                          varchar(50)       NOT NULL COMMENT '分支机构编码' COLLATE 'ascii_general_ci',
	`Name`                              varchar(50)       NOT NULL COMMENT '分支机构名称' COLLATE 'utf8mb4_0900_ai_ci',
	`Abbr`                              varchar(50)       NULL     COMMENT '分支机构简称' COLLATE 'utf8mb4_0900_ai_ci',
	`Acronym`                           varchar(50)       NULL     COMMENT '名称缩写' COLLATE 'ascii_general_ci',
	`LogoPath`                          varchar(200)      NULL     COMMENT '标志图片路径' COLLATE 'ascii_general_ci',
	`Ordinal`                           smallint          NOT NULL COMMENT '排列顺序' DEFAULT 0,
	`Country`                           smallint unsigned NOT NULL COMMENT '国别地区' DEFAULT 0,
	`Language`                          char(2)           NOT NULL COMMENT '语言标识' DEFAULT 'zh',
	`AddressId`                         int unsigned      NOT NULL COMMENT '地址编号' DEFAULT 0,
	`AddressDetail`                     varchar(100)      NULL     COMMENT '详细地址' COLLATE 'utf8mb4_0900_ai_ci',
	`Longitude`                         double(12,8)      NULL     COMMENT '经度坐标',
	`Latitude`                          double(12,8)      NULL     COMMENT '纬度坐标',
	`BusinessLicenseNo`                 varchar(50)       NULL     COMMENT '工商执照号' COLLATE 'ascii_general_ci',
	`BusinessLicenseKind`               tinyint unsigned  NOT NULL COMMENT '工商执照种类' DEFAULT 0,
	`BusinessLicenseAuthority`          varchar(50)       NULL     COMMENT '工商执照发证机关' COLLATE 'utf8mb4_0900_ai_ci',
	`BusinessLicensePhotoPath`          varchar(200)      NULL     COMMENT '工商执照相片路径' COLLATE 'ascii_general_ci',
	`BusinessLicenseIssueDate`          date              NULL     COMMENT '工商执照登记日期',
	`BusinessLicenseExpiryDate`         date              NULL     COMMENT '工商执照过期日期',
	`BusinessLicenseDescription`        varchar(500)      NULL     COMMENT '工商执照经营范围' COLLATE 'utf8mb4_0900_ai_ci',
	`RegisteredCapital`                 smallint unsigned NULL     COMMENT '注册资本(万元)',
	`RegisteredAddress`                 varchar(100)      NULL     COMMENT '注册地址' COLLATE 'utf8mb4_0900_ai_ci',
	`StaffScale`                        tinyint unsigned  NOT NULL COMMENT '人员规模' DEFAULT 0,
	`LegalRepresentativeName`           varchar(50)       NULL     COMMENT '法人姓名' COLLATE 'utf8mb4_0900_ai_ci',
	`LegalRepresentativeGender`         tinyint(1)        NULL     COMMENT '法人性别',
	`LegalRepresentativeEmail`          varchar(50)       NULL     COMMENT '法人邮箱' COLLATE 'ascii_general_ci',
	`LegalRepresentativeIdentityId`     varchar(50)       NULL     COMMENT '法人身份证号码' COLLATE 'ascii_general_ci',
	`LegalRepresentativeIdentityKind`   tinyint unsigned  NOT NULL COMMENT '法人身份证种类' DEFAULT 0,
	`LegalRepresentativeIdentityIssued` date              NULL     COMMENT '法人身份证签发日期',
	`LegalRepresentativeIdentityExpiry` date              NULL     COMMENT '法人身份证过期日期',
	`LegalRepresentativeMobilePhone`    varchar(50)       NULL     COMMENT '法人移动电话' COLLATE 'ascii_general_ci',
	`LegalRepresentativeIdentityPath1`  varchar(200)      NULL     COMMENT '法人证件照片路径1' COLLATE 'ascii_general_ci',
	`LegalRepresentativeIdentityPath2`  varchar(200)      NULL     COMMENT '法人证件照片路径2' COLLATE 'ascii_general_ci',
	`BankCode`                          varchar(50)       NULL     COMMENT '开户银行代号' COLLATE 'ascii_general_ci',
	`BankName`                          varchar(50)       NULL     COMMENT '开户银行名称' COLLATE 'utf8mb4_0900_ai_ci',
	`BankAccountCode`                   varchar(50)       NULL     COMMENT '开户银行账户号码' COLLATE 'ascii_general_ci',
	`BankAccountSetting`                varchar(500)      NULL     COMMENT '开户银行账户设置' COLLATE 'utf8mb4_0900_ai_ci',
	`PhoneNumber`                       varchar(50)       NULL     COMMENT '电话号码' COLLATE 'ascii_general_ci',
	`PrincipalId`                       int unsigned      NULL     COMMENT '负责人编号',
	`ContactName`                       varchar(50)       NULL     COMMENT '联系人姓名' COLLATE 'utf8mb4_0900_ai_ci',
	`ContactGender`                     tinyint unsigned  NULL     COMMENT '联系人性别',
	`ContactEmail`                      varchar(50)       NULL     COMMENT '联系人邮箱' COLLATE 'ascii_general_ci',
	`ContactMobilePhone`                varchar(50)       NULL     COMMENT '联系人移动电话' COLLATE 'ascii_general_ci',
	`ContactOfficePhone`                varchar(50)       NULL     COMMENT '联系人办公电话' COLLATE 'ascii_general_ci',
	`ContactIdentityId`                 varchar(50)       NULL     COMMENT '联系人身份证号' COLLATE 'ascii_general_ci',
	`ContactIdentityKind`               tinyint unsigned  NOT NULL COMMENT '联系人身份证种类' DEFAULT 0,
	`ContactIdentityIssued`             date              NULL     COMMENT '联系人身份证签发日期',
	`ContactIdentityExpiry`             date              NULL     COMMENT '联系人身份证过期日期',
	`ContactIdentityPath1`              varchar(200)      NULL     COMMENT '联系人证件照片路径1' COLLATE 'ascii_general_ci',
	`ContactIdentityPath2`              varchar(200)      NULL     COMMENT '联系人证件照片路径2' COLLATE 'ascii_general_ci',
	`Status`                            tinyint unsigned  NOT NULL COMMENT '机构状态' DEFAULT 0,
	`StatusTimestamp`                   datetime          NULL     COMMENT '状态变更时间',
	`StatusDescription`                 varchar(100)      NULL     COMMENT '状态变更描述' COLLATE 'utf8mb4_0900_ai_ci',
	`CreatorId`                         int unsigned      NOT NULL COMMENT '创建人编号',
	`CreatedTime`                       datetime          NOT NULL COMMENT '创建时间' DEFAULT CURRENT_TIMESTAMP,
	`ModifierId`                        int unsigned      NULL     COMMENT '修改人编号',
	`ModifiedTime`                      datetime          NULL     COMMENT '修改时间',
	`Remark`                            varchar(500)      NULL     COMMENT '备注' COLLATE 'utf8mb4_0900_ai_ci',
	PRIMARY KEY (`TenantId`, `BranchId`),
	UNIQUE INDEX `UX_BranchNo` (`TenantId`, `BranchNo`),
	UNIQUE INDEX `UX_BusinessLicenseNo` (`TenantId`, `BusinessLicenseNo`),
	INDEX `IX_Ordinal` (`TenantId`, `Ordinal`)
) ENGINE=InnoDB COMMENT '分支机构表';

CREATE TABLE IF NOT EXISTS `BranchMember`
(
	`TenantId` int unsigned NOT NULL COMMENT '主键，租户编号',
	`BranchId` int unsigned NOT NULL COMMENT '主键，机构编号',
	`UserId`   int unsigned NOT NULL COMMENT '主键，用户编号',
	PRIMARY KEY (`TenantId`, `BranchId`, `UserId`),
	INDEX `IX_User` (`TenantId`, `UserId`, `BranchId`)
) ENGINE=InnoDB COMMENT '分支机构成员表';

CREATE TABLE IF NOT EXISTS `Department`
(
	`TenantId`     int unsigned       NOT NULL COMMENT '主键，租户编号',
	`BranchId`     int unsigned       NOT NULL COMMENT '主键，分支机构编号',
	`DepartmentId` smallint unsigned  NOT NULL COMMENT '主键，部门编号',
	`ParentId`     smallint unsigned  NOT NULL COMMENT '上级部门编号',
	`DepartmentNo` varchar(50)        NOT NULL COMMENT '部门代号' COLLATE 'ascii_general_ci',
	`Name`         varchar(50)        NOT NULL COMMENT '部门名称' COLLATE 'utf8mb4_0900_ai_ci',
	`Acronym`      varchar(50)        NULL     COMMENT '名称缩写' COLLATE 'ascii_general_ci',
	`Icon`         varchar(100)       NULL     COMMENT '图标标识' COLLATE 'ascii_general_ci',
	`PrincipalId`  int unsigned       NULL     COMMENT '负责人编号',
	`PhoneNumber`  varchar(50)        NULL     COMMENT '部门电话' COLLATE 'ascii_general_ci',
	`Address`      varchar(100)       NULL     COMMENT '部门办公地址' COLLATE 'utf8mb4_0900_ai_ci',
	`Ordinal`      smallint           NOT NULL COMMENT '排列顺序' DEFAULT 0,
	`Remark`       varchar(500)       NULL     COMMENT '备注' COLLATE 'utf8mb4_0900_ai_ci',
	PRIMARY KEY (`TenantId`, `BranchId`, `DepartmentId`),
	UNIQUE INDEX `UX_DepartmentNo` (`TenantId`, `BranchId`, `DepartmentNo`),
	INDEX `IX_Ordinal` (`TenantId`, `BranchId`, `Ordinal`)
) ENGINE=InnoDB COMMENT '部门表';

CREATE TABLE IF NOT EXISTS `DepartmentMember`
(
	`TenantId`     int unsigned       NOT NULL COMMENT '主键，租户编号',
	`BranchId`     int unsigned       NOT NULL COMMENT '主键，分支机构编号',
	`DepartmentId` smallint unsigned  NOT NULL COMMENT '主键，部门编号',
	`UserId`       int unsigned       NOT NULL COMMENT '主键，用户编号',
	PRIMARY KEY (`TenantId`, `BranchId`, `DepartmentId`, `UserId`),
	INDEX `IX_UserId` (`TenantId`, `BranchId`, `UserId`)
) ENGINE=InnoDB COMMENT '部门成员表';

CREATE TABLE IF NOT EXISTS `Team`
(
	`TenantId`     int unsigned         NOT NULL COMMENT '主键，租户编号',
	`BranchId`     int unsigned         NOT NULL COMMENT '主键，分支机构编号',
	`TeamId`       smallint unsigned    NOT NULL COMMENT '主键，班组编号',
	`TeamNo`       varchar(50)          NOT NULL COMMENT '班组代号' COLLATE 'ascii_general_ci',
	`Name`         varchar(50)          NOT NULL COMMENT '班组名称' COLLATE 'utf8mb4_0900_ai_ci',
	`Acronym`      varchar(50)          NULL     COMMENT '名称缩写' COLLATE 'ascii_general_ci',
	`Icon`         varchar(100)         NULL     COMMENT '图标标识' COLLATE 'ascii_general_ci',
	`LeaderId`     int unsigned         NULL     COMMENT '组长编号',
	`DepartmentId` smallint             NULL     COMMENT '所属部门编号',
	`Visible`      tinyint(1)           NOT NULL COMMENT '是否可用' DEFAULT 1,
	`Ordinal`      smallint             NOT NULL COMMENT '排列顺序' DEFAULT 0,
	`Remark`       varchar(500)         NULL     COMMENT '备注' COLLATE 'utf8mb4_0900_ai_ci',
	PRIMARY KEY (`TenantId`, `BranchId`, `TeamId`),
	UNIQUE INDEX `UX_TeamNo` (`TenantId`, `BranchId`, `TeamNo`),
	INDEX `IX_Ordinal` (`TenantId`, `BranchId`, `Ordinal`)
) ENGINE=InnoDB COMMENT '班组表';

CREATE TABLE IF NOT EXISTS `TeamMember`
(
	`TenantId` int unsigned      NOT NULL COMMENT '主键，租户编号',
	`BranchId` int unsigned      NOT NULL COMMENT '主键，分支机构编号',
	`TeamId`   smallint unsigned NOT NULL COMMENT '主键，小组编号',
	`UserId`   int unsigned      NOT NULL COMMENT '主键，用户编号',
	PRIMARY KEY (`TenantId`, `BranchId`, `TeamId`, `UserId`),
	INDEX `IX_UserId` (`TenantId`, `BranchId`, `UserId`)
) ENGINE=InnoDB COMMENT '班组成员表';

CREATE TABLE IF NOT EXISTS `Employee`
(
	`TenantId`            int unsigned      NOT NULL COMMENT '主键，租户编号',
	`UserId`              int unsigned      NOT NULL COMMENT '主键，用户编号',
	`BranchId`            int unsigned      NOT NULL COMMENT '分支机构编号',
	`EmployeeNo`          varchar(50)       NULL     COMMENT '员工代号' COLLATE 'ascii_general_ci',
	`EmployeeCode`        varchar(50)       NULL     COMMENT '内部代号' COLLATE 'ascii_general_ci',
	`EmployeeKind`        tinyint unsigned  NOT NULL COMMENT '员工种类' DEFAULT 0,
	`FullName`            varchar(50)       NULL     COMMENT '员工全称' COLLATE 'utf8mb4_0900_ai_ci',
	`Acronym`             varchar(50)       NULL     COMMENT '名称缩写' COLLATE 'ascii_general_ci',
	`Summary`             varchar(500)      NULL     COMMENT '个人简介' COLLATE 'utf8mb4_0900_ai_ci',
	`JobTitle`            varchar(50)       NULL     COMMENT '员工职称' COLLATE 'utf8mb4_0900_ai_ci',
	`JobStatus`           tinyint unsigned  NOT NULL COMMENT '就职状态' DEFAULT 0,
	`Hiredate`            date              NULL     COMMENT '入职日期',
	`Leavedate`           date              NULL     COMMENT '离职日期',
	`BankName`            varchar(50)       NULL     COMMENT '开户银行' COLLATE 'utf8mb4_0900_ai_ci',
	`BankCode`            varchar(50)       NULL     COMMENT '银行账号' COLLATE 'ascii_general_ci',
	`Birthdate`           date              NULL     COMMENT '出生日期',
	`PhotoPath`           varchar(200)      NULL     COMMENT '相片路径' COLLATE 'ascii_general_ci',
	`IdentityId`          varchar(50)       NULL     COMMENT '身份证号' COLLATE 'ascii_general_ci',
	`IdentityKind`        tinyint unsigned  NOT NULL COMMENT '身份证种类' DEFAULT 0,
	`IdentityIssued`      date              NULL     COMMENT '身份证签发日期',
	`IdentityExpiry`      date              NULL     COMMENT '身份证过期日期',
	`IdentityPath1`       varchar(200)      NULL     COMMENT '证件照片路径1' COLLATE 'ascii_general_ci',
	`IdentityPath2`       varchar(200)      NULL     COMMENT '证件照片路径2' COLLATE 'ascii_general_ci',
	`MaritalStatus`       tinyint unsigned  NOT NULL COMMENT '婚姻状况' DEFAULT 0,
	`EducationDegree`     tinyint unsigned  NOT NULL COMMENT '教育程度' DEFAULT 0,
	`NativePlace`         varchar(50)       NULL     COMMENT '籍贯' COLLATE 'utf8mb4_0900_ai_ci',
	`MobilePhone`         varchar(50)       NULL     COMMENT '移动电话' COLLATE 'ascii_general_ci',
	`HomePhone`           varchar(50)       NULL     COMMENT '家庭电话' COLLATE 'ascii_general_ci',
	`HomeCountry`         smallint unsigned NOT NULL COMMENT '家庭国别地区' DEFAULT 0,
	`HomeAddressId`       int unsigned      NOT NULL COMMENT '家庭地址编号' DEFAULT 0,
	`HomeAddressDetail`   varchar(100)      NULL     COMMENT '家庭住址' COLLATE 'utf8mb4_0900_ai_ci',
	`OfficePhone`         varchar(50)       NULL     COMMENT '办公电话' COLLATE 'ascii_general_ci',
	`OfficeTitle`         varchar(100)      NULL     COMMENT '办公单位' COLLATE 'utf8mb4_0900_ai_ci',
	`OfficeCountry`       smallint unsigned NOT NULL COMMENT '办公国别地区' DEFAULT 0,
	`OfficeAddressId`     int unsigned      NOT NULL COMMENT '办公地址编号' DEFAULT 0,
	`OfficeAddressDetail` varchar(100)      NULL     COMMENT '办公详细地址' COLLATE 'utf8mb4_0900_ai_ci',
	`CreatorId`           int unsigned      NOT NULL COMMENT '创建人编号',
	`CreatedTime`         datetime          NOT NULL COMMENT '创建时间' DEFAULT CURRENT_TIMESTAMP,
	`ModifierId`          int unsigned      NULL     COMMENT '修改人编号',
	`ModifiedTime`        datetime          NULL     COMMENT '修改时间',
	`Remark`              varchar(500)      NULL     COMMENT '备注说明' COLLATE 'utf8mb4_0900_ai_ci',
	PRIMARY KEY (`TenantId`, `UserId`),
	UNIQUE INDEX `UX_EmployeeNo` (`TenantId`, `EmployeeNo`),
	UNIQUE INDEX `UX_IdentityId` (`TenantId`, `IdentityId`),
	INDEX `IX_Birthdate` (`TenantId`, `Birthdate`),
	INDEX `IX_EmployeeCode` (`TenantId`, `EmployeeCode`),
	INDEX `IX_BranchId` (`UserId`, `TenantId`, `BranchId`),
	INDEX `IX_FullName` (`TenantId`, `FullName`)
) ENGINE=InnoDB COMMENT '员工表';

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

/* 初始化“租户”数据 */
INSERT IGNORE INTO Tenant 
	(`TenantId`, `TenantNo`, `Abbr`, `Name`, `Acronym`, `WebUrl`, `CreatorId`, `CreatedTime`) VALUES
	(1, 'Zongsoft', 'Zongsoft', 'Zongsoft Studio', 'ZS', 'http://zongsoft.com', 1, '2025-12-05');

/* 添加系统内置角色 */
INSERT INTO Security_Role (`RoleId`, `Name`, `Nickname`, `Description`) VALUES
	(1, 'Administrators', '系统管理', '系统管理角色(系统内置角色)'),
	(2, 'Security', '安全管理', '安全管理角色(系统内置角色)');

/* 添加系统内置用户 */
INSERT INTO Security_User (`UserId`, `Name`, `Nickname`, `Description`) VALUES
	(1, 'Administrator', '系统管理员', '系统管理员(系统内置帐号)'),
	(2, 'Guest', '来宾', '来宾');
