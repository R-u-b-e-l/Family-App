IF OBJECT_ID(N'[__EFMigrationsHistory]') IS NULL
BEGIN
    CREATE TABLE [__EFMigrationsHistory] (
        [MigrationId] nvarchar(150) NOT NULL,
        [ProductVersion] nvarchar(32) NOT NULL,
        CONSTRAINT [PK___EFMigrationsHistory] PRIMARY KEY ([MigrationId])
    );
END;
GO

BEGIN TRANSACTION;
IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261011073859_InitialCreate'
)
BEGIN
    CREATE TABLE [AccountingPeriods] (
        [Id] uniqueidentifier NOT NULL,
        [FamilyId] uniqueidentifier NOT NULL,
        [Name] nvarchar(50) NOT NULL,
        [StartDate] date NOT NULL,
        [EndDate] date NOT NULL,
        [Status] int NOT NULL,
        [ClosedBy] uniqueidentifier NULL,
        [ClosedDate] datetime2 NULL,
        [CreatedDate] datetime2 NOT NULL,
        [UpdatedDate] datetime2 NULL,
        [CreatedBy] uniqueidentifier NULL,
        [UpdatedBy] uniqueidentifier NULL,
        CONSTRAINT [PK_AccountingPeriods] PRIMARY KEY ([Id])
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261011073859_InitialCreate'
)
BEGIN
    CREATE TABLE [Accounts] (
        [Id] uniqueidentifier NOT NULL,
        [FamilyId] uniqueidentifier NOT NULL,
        [ParentAccountId] uniqueidentifier NULL,
        [Code] nvarchar(20) NOT NULL,
        [Name] nvarchar(100) NOT NULL,
        [AccountType] int NOT NULL,
        [NormalBalance] int NOT NULL,
        [IsGroup] bit NOT NULL,
        [IsSystem] bit NOT NULL,
        [IsActive] bit NOT NULL,
        [CashBankKind] int NOT NULL,
        [Level] int NOT NULL,
        [Description] nvarchar(500) NULL,
        [CreatedDate] datetime2 NOT NULL,
        [UpdatedDate] datetime2 NULL,
        [CreatedBy] uniqueidentifier NULL,
        [UpdatedBy] uniqueidentifier NULL,
        CONSTRAINT [PK_Accounts] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_Accounts_Accounts_ParentAccountId] FOREIGN KEY ([ParentAccountId]) REFERENCES [Accounts] ([Id]) ON DELETE NO ACTION
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261011073859_InitialCreate'
)
BEGIN
    CREATE TABLE [AspNetRoles] (
        [Id] uniqueidentifier NOT NULL,
        [FamilyId] uniqueidentifier NULL,
        [Description] nvarchar(250) NULL,
        [IsSystem] bit NOT NULL,
        [Name] nvarchar(256) NULL,
        [NormalizedName] nvarchar(256) NULL,
        [ConcurrencyStamp] nvarchar(max) NULL,
        CONSTRAINT [PK_AspNetRoles] PRIMARY KEY ([Id])
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261011073859_InitialCreate'
)
BEGIN
    CREATE TABLE [AuditLogs] (
        [Id] uniqueidentifier NOT NULL,
        [FamilyId] uniqueidentifier NOT NULL,
        [UserId] uniqueidentifier NULL,
        [Action] nvarchar(100) NOT NULL,
        [EntityName] nvarchar(100) NULL,
        [EntityId] nvarchar(50) NULL,
        [OldValueJson] nvarchar(max) NULL,
        [NewValueJson] nvarchar(max) NULL,
        [IpAddress] nvarchar(45) NULL,
        [DeviceInfo] nvarchar(500) NULL,
        [CreatedDate] datetime2 NOT NULL,
        CONSTRAINT [PK_AuditLogs] PRIMARY KEY ([Id])
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261011073859_InitialCreate'
)
BEGIN
    CREATE TABLE [Families] (
        [Id] uniqueidentifier NOT NULL,
        [Name] nvarchar(100) NOT NULL,
        [LogoPath] nvarchar(500) NULL,
        [Status] int NOT NULL,
        [CreatedDate] datetime2 NOT NULL,
        [UpdatedDate] datetime2 NULL,
        CONSTRAINT [PK_Families] PRIMARY KEY ([Id])
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261011073859_InitialCreate'
)
BEGIN
    CREATE TABLE [Notifications] (
        [Id] uniqueidentifier NOT NULL,
        [FamilyId] uniqueidentifier NOT NULL,
        [UserId] uniqueidentifier NOT NULL,
        [Type] int NOT NULL,
        [Title] nvarchar(200) NOT NULL,
        [Body] nvarchar(1000) NULL,
        [IsRead] bit NOT NULL,
        [CreatedDate] datetime2 NOT NULL,
        CONSTRAINT [PK_Notifications] PRIMARY KEY ([Id])
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261011073859_InitialCreate'
)
BEGIN
    CREATE TABLE [Permissions] (
        [Id] int NOT NULL,
        [Code] nvarchar(100) NOT NULL,
        [Group] nvarchar(50) NOT NULL,
        [Description] nvarchar(250) NULL,
        CONSTRAINT [PK_Permissions] PRIMARY KEY ([Id])
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261011073859_InitialCreate'
)
BEGIN
    CREATE TABLE [VoucherSequences] (
        [Id] uniqueidentifier NOT NULL,
        [FamilyId] uniqueidentifier NOT NULL,
        [VoucherType] int NOT NULL,
        [FiscalYear] int NOT NULL,
        [NextNumber] int NOT NULL,
        CONSTRAINT [PK_VoucherSequences] PRIMARY KEY ([Id])
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261011073859_InitialCreate'
)
BEGIN
    CREATE TABLE [Vouchers] (
        [Id] uniqueidentifier NOT NULL,
        [FamilyId] uniqueidentifier NOT NULL,
        [VoucherNumber] nvarchar(30) NOT NULL,
        [VoucherType] int NOT NULL,
        [VoucherDate] date NOT NULL,
        [PeriodId] uniqueidentifier NOT NULL,
        [Narration] nvarchar(500) NULL,
        [TotalDebit] decimal(18,2) NOT NULL,
        [TotalCredit] decimal(18,2) NOT NULL,
        [Status] int NOT NULL,
        [ReversalOfVoucherId] uniqueidentifier NULL,
        [PostedBy] uniqueidentifier NULL,
        [PostedDate] datetime2 NULL,
        [RowVersion] rowversion NOT NULL,
        [CreatedDate] datetime2 NOT NULL,
        [UpdatedDate] datetime2 NULL,
        [CreatedBy] uniqueidentifier NULL,
        [UpdatedBy] uniqueidentifier NULL,
        CONSTRAINT [PK_Vouchers] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_Vouchers_AccountingPeriods_PeriodId] FOREIGN KEY ([PeriodId]) REFERENCES [AccountingPeriods] ([Id]) ON DELETE NO ACTION,
        CONSTRAINT [FK_Vouchers_Vouchers_ReversalOfVoucherId] FOREIGN KEY ([ReversalOfVoucherId]) REFERENCES [Vouchers] ([Id]) ON DELETE NO ACTION
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261011073859_InitialCreate'
)
BEGIN
    CREATE TABLE [AspNetRoleClaims] (
        [Id] int NOT NULL IDENTITY,
        [RoleId] uniqueidentifier NOT NULL,
        [ClaimType] nvarchar(max) NULL,
        [ClaimValue] nvarchar(max) NULL,
        CONSTRAINT [PK_AspNetRoleClaims] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_AspNetRoleClaims_AspNetRoles_RoleId] FOREIGN KEY ([RoleId]) REFERENCES [AspNetRoles] ([Id]) ON DELETE CASCADE
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261011073859_InitialCreate'
)
BEGIN
    CREATE TABLE [AspNetUsers] (
        [Id] uniqueidentifier NOT NULL,
        [FamilyId] uniqueidentifier NOT NULL,
        [FullName] nvarchar(100) NOT NULL,
        [DateOfBirth] date NULL,
        [Gender] int NULL,
        [Address] nvarchar(500) NULL,
        [ProfilePicturePath] nvarchar(500) NULL,
        [Status] int NOT NULL,
        [CreatedDate] datetime2 NOT NULL,
        [UpdatedDate] datetime2 NULL,
        [LastLoginDate] datetime2 NULL,
        [UserName] nvarchar(256) NULL,
        [NormalizedUserName] nvarchar(256) NULL,
        [Email] nvarchar(256) NULL,
        [NormalizedEmail] nvarchar(256) NULL,
        [EmailConfirmed] bit NOT NULL,
        [PasswordHash] nvarchar(max) NULL,
        [SecurityStamp] nvarchar(max) NULL,
        [ConcurrencyStamp] nvarchar(max) NULL,
        [PhoneNumber] nvarchar(450) NULL,
        [PhoneNumberConfirmed] bit NOT NULL,
        [TwoFactorEnabled] bit NOT NULL,
        [LockoutEnd] datetimeoffset NULL,
        [LockoutEnabled] bit NOT NULL,
        [AccessFailedCount] int NOT NULL,
        CONSTRAINT [PK_AspNetUsers] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_AspNetUsers_Families_FamilyId] FOREIGN KEY ([FamilyId]) REFERENCES [Families] ([Id]) ON DELETE NO ACTION
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261011073859_InitialCreate'
)
BEGIN
    CREATE TABLE [FamilySettings] (
        [Id] uniqueidentifier NOT NULL,
        [FamilyId] uniqueidentifier NOT NULL,
        [Currency] nvarchar(3) NOT NULL,
        [FinancialYearStartMonth] int NOT NULL,
        [DateFormat] nvarchar(30) NOT NULL,
        [Timezone] nvarchar(60) NOT NULL,
        [Address] nvarchar(500) NULL,
        [ContactPhone] nvarchar(20) NULL,
        [ContactEmail] nvarchar(256) NULL,
        [CreatedDate] datetime2 NOT NULL,
        [UpdatedDate] datetime2 NULL,
        CONSTRAINT [PK_FamilySettings] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_FamilySettings_Families_FamilyId] FOREIGN KEY ([FamilyId]) REFERENCES [Families] ([Id]) ON DELETE CASCADE
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261011073859_InitialCreate'
)
BEGIN
    CREATE TABLE [RolePermissions] (
        [RoleId] uniqueidentifier NOT NULL,
        [PermissionId] int NOT NULL,
        CONSTRAINT [PK_RolePermissions] PRIMARY KEY ([RoleId], [PermissionId]),
        CONSTRAINT [FK_RolePermissions_AspNetRoles_RoleId] FOREIGN KEY ([RoleId]) REFERENCES [AspNetRoles] ([Id]) ON DELETE CASCADE,
        CONSTRAINT [FK_RolePermissions_Permissions_PermissionId] FOREIGN KEY ([PermissionId]) REFERENCES [Permissions] ([Id]) ON DELETE CASCADE
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261011073859_InitialCreate'
)
BEGIN
    CREATE TABLE [VoucherDetails] (
        [Id] uniqueidentifier NOT NULL,
        [VoucherId] uniqueidentifier NOT NULL,
        [LineNo] int NOT NULL,
        [AccountId] uniqueidentifier NOT NULL,
        [Debit] decimal(18,2) NOT NULL,
        [Credit] decimal(18,2) NOT NULL,
        [Narration] nvarchar(500) NULL,
        CONSTRAINT [PK_VoucherDetails] PRIMARY KEY ([Id]),
        CONSTRAINT [CK_VoucherDetail_NonNegative] CHECK ([Debit] >= 0 AND [Credit] >= 0),
        CONSTRAINT [CK_VoucherDetail_OneSide] CHECK (([Debit] > 0 AND [Credit] = 0) OR ([Credit] > 0 AND [Debit] = 0)),
        CONSTRAINT [FK_VoucherDetails_Accounts_AccountId] FOREIGN KEY ([AccountId]) REFERENCES [Accounts] ([Id]) ON DELETE NO ACTION,
        CONSTRAINT [FK_VoucherDetails_Vouchers_VoucherId] FOREIGN KEY ([VoucherId]) REFERENCES [Vouchers] ([Id]) ON DELETE CASCADE
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261011073859_InitialCreate'
)
BEGIN
    CREATE TABLE [AspNetUserClaims] (
        [Id] int NOT NULL IDENTITY,
        [UserId] uniqueidentifier NOT NULL,
        [ClaimType] nvarchar(max) NULL,
        [ClaimValue] nvarchar(max) NULL,
        CONSTRAINT [PK_AspNetUserClaims] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_AspNetUserClaims_AspNetUsers_UserId] FOREIGN KEY ([UserId]) REFERENCES [AspNetUsers] ([Id]) ON DELETE CASCADE
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261011073859_InitialCreate'
)
BEGIN
    CREATE TABLE [AspNetUserLogins] (
        [LoginProvider] nvarchar(450) NOT NULL,
        [ProviderKey] nvarchar(450) NOT NULL,
        [ProviderDisplayName] nvarchar(max) NULL,
        [UserId] uniqueidentifier NOT NULL,
        CONSTRAINT [PK_AspNetUserLogins] PRIMARY KEY ([LoginProvider], [ProviderKey]),
        CONSTRAINT [FK_AspNetUserLogins_AspNetUsers_UserId] FOREIGN KEY ([UserId]) REFERENCES [AspNetUsers] ([Id]) ON DELETE CASCADE
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261011073859_InitialCreate'
)
BEGIN
    CREATE TABLE [AspNetUserRoles] (
        [UserId] uniqueidentifier NOT NULL,
        [RoleId] uniqueidentifier NOT NULL,
        CONSTRAINT [PK_AspNetUserRoles] PRIMARY KEY ([UserId], [RoleId]),
        CONSTRAINT [FK_AspNetUserRoles_AspNetRoles_RoleId] FOREIGN KEY ([RoleId]) REFERENCES [AspNetRoles] ([Id]) ON DELETE CASCADE,
        CONSTRAINT [FK_AspNetUserRoles_AspNetUsers_UserId] FOREIGN KEY ([UserId]) REFERENCES [AspNetUsers] ([Id]) ON DELETE CASCADE
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261011073859_InitialCreate'
)
BEGIN
    CREATE TABLE [AspNetUserTokens] (
        [UserId] uniqueidentifier NOT NULL,
        [LoginProvider] nvarchar(450) NOT NULL,
        [Name] nvarchar(450) NOT NULL,
        [Value] nvarchar(max) NULL,
        CONSTRAINT [PK_AspNetUserTokens] PRIMARY KEY ([UserId], [LoginProvider], [Name]),
        CONSTRAINT [FK_AspNetUserTokens_AspNetUsers_UserId] FOREIGN KEY ([UserId]) REFERENCES [AspNetUsers] ([Id]) ON DELETE CASCADE
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261011073859_InitialCreate'
)
BEGIN
    CREATE TABLE [OtpCodes] (
        [Id] uniqueidentifier NOT NULL,
        [UserId] uniqueidentifier NOT NULL,
        [Purpose] int NOT NULL,
        [CodeHash] nvarchar(88) NOT NULL,
        [ExpiresAt] datetime2 NOT NULL,
        [AttemptCount] int NOT NULL,
        [ConsumedAt] datetime2 NULL,
        [CreatedDate] datetime2 NOT NULL,
        CONSTRAINT [PK_OtpCodes] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_OtpCodes_AspNetUsers_UserId] FOREIGN KEY ([UserId]) REFERENCES [AspNetUsers] ([Id]) ON DELETE CASCADE
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261011073859_InitialCreate'
)
BEGIN
    CREATE TABLE [RefreshTokens] (
        [Id] uniqueidentifier NOT NULL,
        [UserId] uniqueidentifier NOT NULL,
        [TokenHash] nvarchar(88) NOT NULL,
        [ExpiresAt] datetime2 NOT NULL,
        [CreatedDate] datetime2 NOT NULL,
        [CreatedByIp] nvarchar(45) NULL,
        [RevokedAt] datetime2 NULL,
        [RevokedByIp] nvarchar(45) NULL,
        [ReplacedByTokenHash] nvarchar(88) NULL,
        CONSTRAINT [PK_RefreshTokens] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_RefreshTokens_AspNetUsers_UserId] FOREIGN KEY ([UserId]) REFERENCES [AspNetUsers] ([Id]) ON DELETE CASCADE
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261011073859_InitialCreate'
)
BEGIN
    IF EXISTS (SELECT * FROM [sys].[identity_columns] WHERE [name] IN (N'Id', N'ConcurrencyStamp', N'Description', N'FamilyId', N'IsSystem', N'Name', N'NormalizedName') AND [object_id] = OBJECT_ID(N'[AspNetRoles]'))
        SET IDENTITY_INSERT [AspNetRoles] ON;
    EXEC(N'INSERT INTO [AspNetRoles] ([Id], [ConcurrencyStamp], [Description], [FamilyId], [IsSystem], [Name], [NormalizedName])
    VALUES (''11111111-1111-1111-1111-111111111111'', N''seed-admin-template'', N''Full control over the family (template).'', NULL, CAST(1 AS bit), N''FamilyAdmin'', N''FAMILYADMIN''),
    (''22222222-2222-2222-2222-222222222222'', N''seed-member-template'', N''Read-only family member (template).'', NULL, CAST(1 AS bit), N''FamilyMember'', N''FAMILYMEMBER'')');
    IF EXISTS (SELECT * FROM [sys].[identity_columns] WHERE [name] IN (N'Id', N'ConcurrencyStamp', N'Description', N'FamilyId', N'IsSystem', N'Name', N'NormalizedName') AND [object_id] = OBJECT_ID(N'[AspNetRoles]'))
        SET IDENTITY_INSERT [AspNetRoles] OFF;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261011073859_InitialCreate'
)
BEGIN
    IF EXISTS (SELECT * FROM [sys].[identity_columns] WHERE [name] IN (N'Id', N'Code', N'Description', N'Group') AND [object_id] = OBJECT_ID(N'[Permissions]'))
        SET IDENTITY_INSERT [Permissions] ON;
    EXEC(N'INSERT INTO [Permissions] ([Id], [Code], [Description], [Group])
    VALUES (1, N''Dashboard.View'', NULL, N''Dashboard''),
    (2, N''Family.View'', NULL, N''Family''),
    (3, N''Family.Edit'', NULL, N''Family''),
    (4, N''Member.View'', NULL, N''Member''),
    (5, N''Member.Create'', NULL, N''Member''),
    (6, N''Member.Edit'', NULL, N''Member''),
    (7, N''Member.Delete'', NULL, N''Member''),
    (8, N''Role.View'', NULL, N''Role''),
    (9, N''Role.Manage'', NULL, N''Role''),
    (10, N''Account.View'', NULL, N''Account''),
    (11, N''Account.Create'', NULL, N''Account''),
    (12, N''Account.Edit'', NULL, N''Account''),
    (13, N''Account.Delete'', NULL, N''Account''),
    (14, N''Period.View'', NULL, N''Period''),
    (15, N''Period.Manage'', NULL, N''Period''),
    (16, N''Voucher.View'', NULL, N''Voucher''),
    (17, N''Voucher.Create'', NULL, N''Voucher''),
    (18, N''Voucher.Edit'', NULL, N''Voucher''),
    (19, N''Voucher.Delete'', NULL, N''Voucher''),
    (20, N''Voucher.Post'', NULL, N''Voucher''),
    (21, N''Voucher.Approve'', NULL, N''Voucher''),
    (22, N''Ledger.View'', NULL, N''Reports''),
    (23, N''CashBook.View'', NULL, N''Reports''),
    (24, N''BankBook.View'', NULL, N''Reports''),
    (25, N''TrialBalance.View'', NULL, N''Reports''),
    (26, N''BalanceSheet.View'', NULL, N''Reports''),
    (27, N''ProfitLoss.View'', NULL, N''Reports''),
    (28, N''Reports.View'', NULL, N''Reports''),
    (29, N''AuditLog.View'', NULL, N''AuditLog''),
    (30, N''Settings.View'', NULL, N''Settings''),
    (31, N''Settings.Edit'', NULL, N''Settings'')');
    IF EXISTS (SELECT * FROM [sys].[identity_columns] WHERE [name] IN (N'Id', N'Code', N'Description', N'Group') AND [object_id] = OBJECT_ID(N'[Permissions]'))
        SET IDENTITY_INSERT [Permissions] OFF;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261011073859_InitialCreate'
)
BEGIN
    IF EXISTS (SELECT * FROM [sys].[identity_columns] WHERE [name] IN (N'PermissionId', N'RoleId') AND [object_id] = OBJECT_ID(N'[RolePermissions]'))
        SET IDENTITY_INSERT [RolePermissions] ON;
    EXEC(N'INSERT INTO [RolePermissions] ([PermissionId], [RoleId])
    VALUES (1, ''11111111-1111-1111-1111-111111111111''),
    (2, ''11111111-1111-1111-1111-111111111111''),
    (3, ''11111111-1111-1111-1111-111111111111''),
    (4, ''11111111-1111-1111-1111-111111111111''),
    (5, ''11111111-1111-1111-1111-111111111111''),
    (6, ''11111111-1111-1111-1111-111111111111''),
    (7, ''11111111-1111-1111-1111-111111111111''),
    (8, ''11111111-1111-1111-1111-111111111111''),
    (9, ''11111111-1111-1111-1111-111111111111''),
    (10, ''11111111-1111-1111-1111-111111111111''),
    (11, ''11111111-1111-1111-1111-111111111111''),
    (12, ''11111111-1111-1111-1111-111111111111''),
    (13, ''11111111-1111-1111-1111-111111111111''),
    (14, ''11111111-1111-1111-1111-111111111111''),
    (15, ''11111111-1111-1111-1111-111111111111''),
    (16, ''11111111-1111-1111-1111-111111111111''),
    (17, ''11111111-1111-1111-1111-111111111111''),
    (18, ''11111111-1111-1111-1111-111111111111''),
    (19, ''11111111-1111-1111-1111-111111111111''),
    (20, ''11111111-1111-1111-1111-111111111111''),
    (21, ''11111111-1111-1111-1111-111111111111''),
    (22, ''11111111-1111-1111-1111-111111111111''),
    (23, ''11111111-1111-1111-1111-111111111111''),
    (24, ''11111111-1111-1111-1111-111111111111''),
    (25, ''11111111-1111-1111-1111-111111111111''),
    (26, ''11111111-1111-1111-1111-111111111111''),
    (27, ''11111111-1111-1111-1111-111111111111''),
    (28, ''11111111-1111-1111-1111-111111111111''),
    (29, ''11111111-1111-1111-1111-111111111111''),
    (30, ''11111111-1111-1111-1111-111111111111''),
    (31, ''11111111-1111-1111-1111-111111111111''),
    (1, ''22222222-2222-2222-2222-222222222222''),
    (2, ''22222222-2222-2222-2222-222222222222''),
    (4, ''22222222-2222-2222-2222-222222222222''),
    (10, ''22222222-2222-2222-2222-222222222222''),
    (16, ''22222222-2222-2222-2222-222222222222''),
    (22, ''22222222-2222-2222-2222-222222222222''),
    (23, ''22222222-2222-2222-2222-222222222222''),
    (24, ''22222222-2222-2222-2222-222222222222''),
    (28, ''22222222-2222-2222-2222-222222222222''),
    (30, ''22222222-2222-2222-2222-222222222222'')');
    IF EXISTS (SELECT * FROM [sys].[identity_columns] WHERE [name] IN (N'PermissionId', N'RoleId') AND [object_id] = OBJECT_ID(N'[RolePermissions]'))
        SET IDENTITY_INSERT [RolePermissions] OFF;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261011073859_InitialCreate'
)
BEGIN
    CREATE UNIQUE INDEX [IX_AccountingPeriods_FamilyId_StartDate] ON [AccountingPeriods] ([FamilyId], [StartDate]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261011073859_InitialCreate'
)
BEGIN
    CREATE UNIQUE INDEX [IX_Accounts_FamilyId_Code] ON [Accounts] ([FamilyId], [Code]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261011073859_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_Accounts_FamilyId_ParentAccountId] ON [Accounts] ([FamilyId], [ParentAccountId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261011073859_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_Accounts_ParentAccountId] ON [Accounts] ([ParentAccountId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261011073859_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_AspNetRoleClaims_RoleId] ON [AspNetRoleClaims] ([RoleId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261011073859_InitialCreate'
)
BEGIN
    EXEC(N'CREATE UNIQUE INDEX [IX_AspNetRoles_FamilyId_NormalizedName] ON [AspNetRoles] ([FamilyId], [NormalizedName]) WHERE [FamilyId] IS NOT NULL');
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261011073859_InitialCreate'
)
BEGIN
    EXEC(N'CREATE UNIQUE INDEX [RoleNameIndex] ON [AspNetRoles] ([NormalizedName]) WHERE [NormalizedName] IS NOT NULL');
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261011073859_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_AspNetUserClaims_UserId] ON [AspNetUserClaims] ([UserId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261011073859_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_AspNetUserLogins_UserId] ON [AspNetUserLogins] ([UserId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261011073859_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_AspNetUserRoles_RoleId] ON [AspNetUserRoles] ([RoleId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261011073859_InitialCreate'
)
BEGIN
    EXEC(N'CREATE UNIQUE INDEX [EmailIndex] ON [AspNetUsers] ([NormalizedEmail]) WHERE [NormalizedEmail] IS NOT NULL');
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261011073859_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_AspNetUsers_FamilyId] ON [AspNetUsers] ([FamilyId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261011073859_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_AspNetUsers_PhoneNumber] ON [AspNetUsers] ([PhoneNumber]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261011073859_InitialCreate'
)
BEGIN
    EXEC(N'CREATE UNIQUE INDEX [UserNameIndex] ON [AspNetUsers] ([NormalizedUserName]) WHERE [NormalizedUserName] IS NOT NULL');
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261011073859_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_AuditLogs_FamilyId_CreatedDate] ON [AuditLogs] ([FamilyId], [CreatedDate]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261011073859_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_AuditLogs_FamilyId_EntityName_EntityId] ON [AuditLogs] ([FamilyId], [EntityName], [EntityId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261011073859_InitialCreate'
)
BEGIN
    CREATE UNIQUE INDEX [IX_FamilySettings_FamilyId] ON [FamilySettings] ([FamilyId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261011073859_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_Notifications_UserId_IsRead_CreatedDate] ON [Notifications] ([UserId], [IsRead], [CreatedDate]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261011073859_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_OtpCodes_UserId_Purpose_ExpiresAt] ON [OtpCodes] ([UserId], [Purpose], [ExpiresAt]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261011073859_InitialCreate'
)
BEGIN
    CREATE UNIQUE INDEX [IX_Permissions_Code] ON [Permissions] ([Code]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261011073859_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_RefreshTokens_TokenHash] ON [RefreshTokens] ([TokenHash]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261011073859_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_RefreshTokens_UserId_ExpiresAt] ON [RefreshTokens] ([UserId], [ExpiresAt]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261011073859_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_RolePermissions_PermissionId] ON [RolePermissions] ([PermissionId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261011073859_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_VoucherDetails_AccountId_VoucherId] ON [VoucherDetails] ([AccountId], [VoucherId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261011073859_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_VoucherDetails_VoucherId] ON [VoucherDetails] ([VoucherId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261011073859_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_Vouchers_FamilyId_VoucherDate_Status] ON [Vouchers] ([FamilyId], [VoucherDate], [Status]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261011073859_InitialCreate'
)
BEGIN
    CREATE UNIQUE INDEX [IX_Vouchers_FamilyId_VoucherNumber] ON [Vouchers] ([FamilyId], [VoucherNumber]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261011073859_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_Vouchers_FamilyId_VoucherType_Status] ON [Vouchers] ([FamilyId], [VoucherType], [Status]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261011073859_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_Vouchers_PeriodId] ON [Vouchers] ([PeriodId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261011073859_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_Vouchers_ReversalOfVoucherId] ON [Vouchers] ([ReversalOfVoucherId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261011073859_InitialCreate'
)
BEGIN
    CREATE UNIQUE INDEX [IX_VoucherSequences_FamilyId_VoucherType_FiscalYear] ON [VoucherSequences] ([FamilyId], [VoucherType], [FiscalYear]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261011073859_InitialCreate'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20261011073859_InitialCreate', N'10.0.12');
END;

COMMIT;
GO

