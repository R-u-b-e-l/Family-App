BEGIN TRANSACTION;
IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261015075029_MemberTrackingAndLoans'
)
BEGIN
    ALTER TABLE [Vouchers] ADD [MemberId] uniqueidentifier NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261015075029_MemberTrackingAndLoans'
)
BEGIN
    ALTER TABLE [Accounts] ADD [LinkedMemberId] uniqueidentifier NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261015075029_MemberTrackingAndLoans'
)
BEGIN
    IF EXISTS (SELECT * FROM [sys].[identity_columns] WHERE [name] IN (N'Id', N'ConcurrencyStamp', N'Description', N'FamilyId', N'IsSystem', N'Name', N'NormalizedName') AND [object_id] = OBJECT_ID(N'[AspNetRoles]'))
        SET IDENTITY_INSERT [AspNetRoles] ON;
    EXEC(N'INSERT INTO [AspNetRoles] ([Id], [ConcurrencyStamp], [Description], [FamilyId], [IsSystem], [Name], [NormalizedName])
    VALUES (''33333333-3333-3333-3333-333333333333'', N''seed-manager-template'', N''Handles all money matters: vouchers, accounts, loans, reports (template).'', NULL, CAST(1 AS bit), N''Manager'', N''MANAGER'')');
    IF EXISTS (SELECT * FROM [sys].[identity_columns] WHERE [name] IN (N'Id', N'ConcurrencyStamp', N'Description', N'FamilyId', N'IsSystem', N'Name', N'NormalizedName') AND [object_id] = OBJECT_ID(N'[AspNetRoles]'))
        SET IDENTITY_INSERT [AspNetRoles] OFF;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261015075029_MemberTrackingAndLoans'
)
BEGIN
    IF EXISTS (SELECT * FROM [sys].[identity_columns] WHERE [name] IN (N'Id', N'Code', N'Description', N'Group') AND [object_id] = OBJECT_ID(N'[Permissions]'))
        SET IDENTITY_INSERT [Permissions] ON;
    EXEC(N'INSERT INTO [Permissions] ([Id], [Code], [Description], [Group])
    VALUES (32, N''Loan.View'', NULL, N''Loan''),
    (33, N''Loan.Create'', NULL, N''Loan''),
    (34, N''Dashboard.Admin'', NULL, N''AdminDashboard'')');
    IF EXISTS (SELECT * FROM [sys].[identity_columns] WHERE [name] IN (N'Id', N'Code', N'Description', N'Group') AND [object_id] = OBJECT_ID(N'[Permissions]'))
        SET IDENTITY_INSERT [Permissions] OFF;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261015075029_MemberTrackingAndLoans'
)
BEGIN
    IF EXISTS (SELECT * FROM [sys].[identity_columns] WHERE [name] IN (N'PermissionId', N'RoleId') AND [object_id] = OBJECT_ID(N'[RolePermissions]'))
        SET IDENTITY_INSERT [RolePermissions] ON;
    EXEC(N'INSERT INTO [RolePermissions] ([PermissionId], [RoleId])
    VALUES (32, ''11111111-1111-1111-1111-111111111111''),
    (33, ''11111111-1111-1111-1111-111111111111''),
    (34, ''11111111-1111-1111-1111-111111111111''),
    (1, ''33333333-3333-3333-3333-333333333333''),
    (2, ''33333333-3333-3333-3333-333333333333''),
    (4, ''33333333-3333-3333-3333-333333333333''),
    (10, ''33333333-3333-3333-3333-333333333333''),
    (11, ''33333333-3333-3333-3333-333333333333''),
    (12, ''33333333-3333-3333-3333-333333333333''),
    (13, ''33333333-3333-3333-3333-333333333333''),
    (14, ''33333333-3333-3333-3333-333333333333''),
    (15, ''33333333-3333-3333-3333-333333333333''),
    (16, ''33333333-3333-3333-3333-333333333333''),
    (17, ''33333333-3333-3333-3333-333333333333''),
    (18, ''33333333-3333-3333-3333-333333333333''),
    (19, ''33333333-3333-3333-3333-333333333333''),
    (20, ''33333333-3333-3333-3333-333333333333''),
    (21, ''33333333-3333-3333-3333-333333333333''),
    (22, ''33333333-3333-3333-3333-333333333333''),
    (23, ''33333333-3333-3333-3333-333333333333''),
    (24, ''33333333-3333-3333-3333-333333333333''),
    (25, ''33333333-3333-3333-3333-333333333333''),
    (26, ''33333333-3333-3333-3333-333333333333''),
    (27, ''33333333-3333-3333-3333-333333333333''),
    (28, ''33333333-3333-3333-3333-333333333333''),
    (30, ''33333333-3333-3333-3333-333333333333''),
    (32, ''33333333-3333-3333-3333-333333333333''),
    (33, ''33333333-3333-3333-3333-333333333333'')');
    IF EXISTS (SELECT * FROM [sys].[identity_columns] WHERE [name] IN (N'PermissionId', N'RoleId') AND [object_id] = OBJECT_ID(N'[RolePermissions]'))
        SET IDENTITY_INSERT [RolePermissions] OFF;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261015075029_MemberTrackingAndLoans'
)
BEGIN
    CREATE INDEX [IX_Vouchers_FamilyId_MemberId] ON [Vouchers] ([FamilyId], [MemberId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261015075029_MemberTrackingAndLoans'
)
BEGIN
    CREATE INDEX [IX_Accounts_FamilyId_LinkedMemberId] ON [Accounts] ([FamilyId], [LinkedMemberId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261015075029_MemberTrackingAndLoans'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20261015075029_MemberTrackingAndLoans', N'10.0.12');
END;

COMMIT;
GO

