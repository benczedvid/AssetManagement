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
    WHERE [MigrationId] = N'20260724064632_InitializeCreate'
)
BEGIN
    CREATE TABLE [Users] (
        [Id] uniqueidentifier NOT NULL,
        [EntraObjectId] uniqueidentifier NOT NULL,
        [EntraTenantId] uniqueidentifier NOT NULL,
        [FirstName] nvarchar(100) NOT NULL,
        [LastName] nvarchar(100) NOT NULL,
        [DisplayName] nvarchar(200) NOT NULL,
        [IsActive] bit NOT NULL,
        CONSTRAINT [PK_Users] PRIMARY KEY ([Id])
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260724064632_InitializeCreate'
)
BEGIN
    CREATE UNIQUE INDEX [IX_Users_EntraTenantId_EntraObjectId] ON [Users] ([EntraTenantId], [EntraObjectId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260724064632_InitializeCreate'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20260724064632_InitializeCreate', N'10.0.13');
END;

COMMIT;
GO

BEGIN TRANSACTION;
IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260727174713_AddStores'
)
BEGIN
    CREATE TABLE [Stores] (
        [Id] uniqueidentifier NOT NULL,
        [StoreNumber] int NOT NULL,
        [Name] nvarchar(200) NOT NULL,
        [City] nvarchar(100) NOT NULL,
        [Address] nvarchar(200) NOT NULL,
        [TypeOfRoad] nvarchar(50) NOT NULL,
        [HouseNumber] int NOT NULL,
        CONSTRAINT [PK_Stores] PRIMARY KEY ([Id])
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260727174713_AddStores'
)
BEGIN
    CREATE UNIQUE INDEX [IX_Stores_StoreNumber] ON [Stores] ([StoreNumber]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260727174713_AddStores'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20260727174713_AddStores', N'10.0.13');
END;

COMMIT;
GO

BEGIN TRANSACTION;
IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260731154248_ExtendAssets'
)
BEGIN
    CREATE TABLE [Assets] (
        [Id] uniqueidentifier NOT NULL,
        [AssetName] nvarchar(200) NOT NULL,
        [AssetType] nvarchar(50) NOT NULL,
        [AssetStatus] nvarchar(50) NOT NULL,
        [Manufacturer] nvarchar(200) NOT NULL,
        [Model] nvarchar(200) NOT NULL,
        [SerialNumber] nvarchar(200) NOT NULL,
        [MacAddress] nvarchar(17) NULL,
        [WiFi_MacAddress] nvarchar(17) NULL,
        [Imei] nvarchar(15) NULL,
        [OperatingSystem] nvarchar(100) NULL,
        [OperatingSystemVersion] nvarchar(100) NULL,
        [AssignedStoreId] uniqueidentifier NOT NULL,
        [AssignedUserId] uniqueidentifier NULL,
        CONSTRAINT [PK_Assets] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_Assets_Stores_AssignedStoreId] FOREIGN KEY ([AssignedStoreId]) REFERENCES [Stores] ([Id]) ON DELETE NO ACTION,
        CONSTRAINT [FK_Assets_Users_AssignedUserId] FOREIGN KEY ([AssignedUserId]) REFERENCES [Users] ([Id]) ON DELETE SET NULL
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260731154248_ExtendAssets'
)
BEGIN
    CREATE INDEX [IX_Assets_AssignedStoreId] ON [Assets] ([AssignedStoreId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260731154248_ExtendAssets'
)
BEGIN
    CREATE INDEX [IX_Assets_AssignedUserId] ON [Assets] ([AssignedUserId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260731154248_ExtendAssets'
)
BEGIN
    CREATE UNIQUE INDEX [UX_Assets_SerialNumber] ON [Assets] ([SerialNumber]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260731154248_ExtendAssets'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20260731154248_ExtendAssets', N'10.0.13');
END;

COMMIT;
GO

BEGIN TRANSACTION;
IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260812131442_RenameWifiMacAddress'
)
BEGIN
    EXEC sp_rename N'[Assets].[WiFi_MacAddress]', N'WiFiMacAddress', 'COLUMN';
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260812131442_RenameWifiMacAddress'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20260812131442_RenameWifiMacAddress', N'10.0.13');
END;

COMMIT;
GO

BEGIN TRANSACTION;
IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260813084108_ExtendStore'
)
BEGIN
    EXEC sp_rename N'[Stores].[TypeOfRoad]', N'PublicSpace', 'COLUMN';
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260813084108_ExtendStore'
)
BEGIN
    DROP INDEX [IX_Stores_StoreNumber] ON [Stores];
    DECLARE @var nvarchar(max);
    SELECT @var = QUOTENAME([d].[name])
    FROM [sys].[default_constraints] [d]
    INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
    WHERE ([d].[parent_object_id] = OBJECT_ID(N'[Stores]') AND [c].[name] = N'StoreNumber');
    IF @var IS NOT NULL EXEC(N'ALTER TABLE [Stores] DROP CONSTRAINT ' + @var + ';');
    ALTER TABLE [Stores] ALTER COLUMN [StoreNumber] nvarchar(10) NOT NULL;
    CREATE UNIQUE INDEX [IX_Stores_StoreNumber] ON [Stores] ([StoreNumber]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260813084108_ExtendStore'
)
BEGIN
    DECLARE @var1 nvarchar(max);
    SELECT @var1 = QUOTENAME([d].[name])
    FROM [sys].[default_constraints] [d]
    INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
    WHERE ([d].[parent_object_id] = OBJECT_ID(N'[Stores]') AND [c].[name] = N'HouseNumber');
    IF @var1 IS NOT NULL EXEC(N'ALTER TABLE [Stores] DROP CONSTRAINT ' + @var1 + ';');
    ALTER TABLE [Stores] ALTER COLUMN [HouseNumber] nvarchar(10) NOT NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260813084108_ExtendStore'
)
BEGIN
    ALTER TABLE [Stores] ADD [CountryCode] nvarchar(5) NOT NULL DEFAULT N'';
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260813084108_ExtendStore'
)
BEGIN
    ALTER TABLE [Stores] ADD [PostalCode] nvarchar(max) NOT NULL DEFAULT N'';
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260813084108_ExtendStore'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20260813084108_ExtendStore', N'10.0.13');
END;

COMMIT;
GO

BEGIN TRANSACTION;
IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260813103259_ExtendStoreWithEnum'
)
BEGIN
    DECLARE @var2 nvarchar(max);
    SELECT @var2 = QUOTENAME([d].[name])
    FROM [sys].[default_constraints] [d]
    INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
    WHERE ([d].[parent_object_id] = OBJECT_ID(N'[Stores]') AND [c].[name] = N'CountryCode');
    IF @var2 IS NOT NULL EXEC(N'ALTER TABLE [Stores] DROP CONSTRAINT ' + @var2 + ';');
    ALTER TABLE [Stores] ALTER COLUMN [CountryCode] nvarchar(3) NOT NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260813103259_ExtendStoreWithEnum'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20260813103259_ExtendStoreWithEnum', N'10.0.13');
END;

COMMIT;
GO

BEGIN TRANSACTION;
IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260813183440_AddEmployeesEntity'
)
BEGIN
    CREATE TABLE [Employees] (
        [Id] uniqueidentifier NOT NULL,
        [EmployeeNumber] nvarchar(10) NOT NULL,
        [LastName] nvarchar(200) NOT NULL,
        [FirstName] nvarchar(200) NOT NULL,
        [StoreId] uniqueidentifier NOT NULL,
        [StoreId1] uniqueidentifier NOT NULL,
        [Position] nvarchar(200) NOT NULL,
        [Department] nvarchar(200) NOT NULL,
        CONSTRAINT [PK_Employees] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_Employees_Stores_StoreId] FOREIGN KEY ([StoreId]) REFERENCES [Stores] ([Id]) ON DELETE NO ACTION,
        CONSTRAINT [FK_Employees_Stores_StoreId1] FOREIGN KEY ([StoreId1]) REFERENCES [Stores] ([Id]) ON DELETE CASCADE
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260813183440_AddEmployeesEntity'
)
BEGIN
    CREATE UNIQUE INDEX [IX_Employees_EmployeeNumber] ON [Employees] ([EmployeeNumber]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260813183440_AddEmployeesEntity'
)
BEGIN
    CREATE INDEX [IX_Employees_StoreId] ON [Employees] ([StoreId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260813183440_AddEmployeesEntity'
)
BEGIN
    CREATE INDEX [IX_Employees_StoreId1] ON [Employees] ([StoreId1]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260813183440_AddEmployeesEntity'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20260813183440_AddEmployeesEntity', N'10.0.13');
END;

COMMIT;
GO

BEGIN TRANSACTION;
IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260813203403_AddEmployees'
)
BEGIN
    ALTER TABLE [Employees] DROP CONSTRAINT [PK_Employees];
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260813203403_AddEmployees'
)
BEGIN
    DROP INDEX [IX_Employees_EmployeeNumber] ON [Employees];
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260813203403_AddEmployees'
)
BEGIN
    DECLARE @var3 nvarchar(max);
    SELECT @var3 = QUOTENAME([d].[name])
    FROM [sys].[default_constraints] [d]
    INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
    WHERE ([d].[parent_object_id] = OBJECT_ID(N'[Employees]') AND [c].[name] = N'Id');
    IF @var3 IS NOT NULL EXEC(N'ALTER TABLE [Employees] DROP CONSTRAINT ' + @var3 + ';');
    ALTER TABLE [Employees] DROP COLUMN [Id];
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260813203403_AddEmployees'
)
BEGIN
    DECLARE @var4 nvarchar(max);
    SELECT @var4 = QUOTENAME([d].[name])
    FROM [sys].[default_constraints] [d]
    INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
    WHERE ([d].[parent_object_id] = OBJECT_ID(N'[Employees]') AND [c].[name] = N'Department');
    IF @var4 IS NOT NULL EXEC(N'ALTER TABLE [Employees] DROP CONSTRAINT ' + @var4 + ';');
    ALTER TABLE [Employees] ALTER COLUMN [Department] nvarchar(100) NOT NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260813203403_AddEmployees'
)
BEGIN
    ALTER TABLE [Employees] ADD CONSTRAINT [PK_Employees] PRIMARY KEY ([EmployeeNumber]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260813203403_AddEmployees'
)
BEGIN
    CREATE INDEX [IX_Employees_EmployeeNumber] ON [Employees] ([EmployeeNumber]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260813203403_AddEmployees'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20260813203403_AddEmployees', N'10.0.13');
END;

COMMIT;
GO

BEGIN TRANSACTION;
IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260814091145_RemovestoreId1Column'
)
BEGIN
    ALTER TABLE [Employees] DROP CONSTRAINT [FK_Employees_Stores_StoreId1];
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260814091145_RemovestoreId1Column'
)
BEGIN
    DROP INDEX [IX_Employees_EmployeeNumber] ON [Employees];
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260814091145_RemovestoreId1Column'
)
BEGIN
    DROP INDEX [IX_Employees_StoreId1] ON [Employees];
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260814091145_RemovestoreId1Column'
)
BEGIN
    DECLARE @var5 nvarchar(max);
    SELECT @var5 = QUOTENAME([d].[name])
    FROM [sys].[default_constraints] [d]
    INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
    WHERE ([d].[parent_object_id] = OBJECT_ID(N'[Employees]') AND [c].[name] = N'StoreId1');
    IF @var5 IS NOT NULL EXEC(N'ALTER TABLE [Employees] DROP CONSTRAINT ' + @var5 + ';');
    ALTER TABLE [Employees] DROP COLUMN [StoreId1];
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260814091145_RemovestoreId1Column'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20260814091145_RemovestoreId1Column', N'10.0.13');
END;

COMMIT;
GO

BEGIN TRANSACTION;
IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260814111442_AddAssetMovement'
)
BEGIN
    CREATE TABLE [AssetMovements] (
        [Id] uniqueidentifier NOT NULL,
        [AssetId] uniqueidentifier NOT NULL,
        [EmployeeNumber] nvarchar(50) NOT NULL,
        [Type] nvarchar(50) NOT NULL,
        [CreatedAtUtc] datetime2 NOT NULL,
        CONSTRAINT [PK_AssetMovements] PRIMARY KEY ([Id])
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260814111442_AddAssetMovement'
)
BEGIN
    CREATE INDEX [IX_AssetMovements_AssetId] ON [AssetMovements] ([AssetId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260814111442_AddAssetMovement'
)
BEGIN
    CREATE INDEX [IX_AssetMovements_AssetId_CreatedAtUtc] ON [AssetMovements] ([AssetId], [CreatedAtUtc]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260814111442_AddAssetMovement'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20260814111442_AddAssetMovement', N'10.0.13');
END;

COMMIT;
GO

BEGIN TRANSACTION;
IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260814194419_AddAssetEmployee'
)
BEGIN
    ALTER TABLE [Assets] ADD [AssignedEmployeeNumber] nvarchar(10) NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260814194419_AddAssetEmployee'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20260814194419_AddAssetEmployee', N'10.0.13');
END;

COMMIT;
GO

BEGIN TRANSACTION;
IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260814202431_AddStoreIdToAssetMovement'
)
BEGIN
    DROP INDEX [IX_AssetMovements_AssetId] ON [AssetMovements];
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260814202431_AddStoreIdToAssetMovement'
)
BEGIN
    DECLARE @var6 nvarchar(max);
    SELECT @var6 = QUOTENAME([d].[name])
    FROM [sys].[default_constraints] [d]
    INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
    WHERE ([d].[parent_object_id] = OBJECT_ID(N'[AssetMovements]') AND [c].[name] = N'Type');
    IF @var6 IS NOT NULL EXEC(N'ALTER TABLE [AssetMovements] DROP CONSTRAINT ' + @var6 + ';');
    ALTER TABLE [AssetMovements] ALTER COLUMN [Type] nvarchar(20) NOT NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260814202431_AddStoreIdToAssetMovement'
)
BEGIN
    DECLARE @var7 nvarchar(max);
    SELECT @var7 = QUOTENAME([d].[name])
    FROM [sys].[default_constraints] [d]
    INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
    WHERE ([d].[parent_object_id] = OBJECT_ID(N'[AssetMovements]') AND [c].[name] = N'EmployeeNumber');
    IF @var7 IS NOT NULL EXEC(N'ALTER TABLE [AssetMovements] DROP CONSTRAINT ' + @var7 + ';');
    ALTER TABLE [AssetMovements] ALTER COLUMN [EmployeeNumber] nvarchar(10) NOT NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260814202431_AddStoreIdToAssetMovement'
)
BEGIN
    ALTER TABLE [AssetMovements] ADD [StoreId] uniqueidentifier NOT NULL DEFAULT '00000000-0000-0000-0000-000000000000';
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260814202431_AddStoreIdToAssetMovement'
)
BEGIN
    CREATE INDEX [IX_AssetMovements_EmployeeNumber_CreatedAtUtc] ON [AssetMovements] ([EmployeeNumber], [CreatedAtUtc]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260814202431_AddStoreIdToAssetMovement'
)
BEGIN
    CREATE INDEX [IX_AssetMovements_StoreId_CreatedAtUtc] ON [AssetMovements] ([StoreId], [CreatedAtUtc]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260814202431_AddStoreIdToAssetMovement'
)
BEGIN
    ALTER TABLE [AssetMovements] ADD CONSTRAINT [FK_AssetMovements_Assets_AssetId] FOREIGN KEY ([AssetId]) REFERENCES [Assets] ([Id]) ON DELETE NO ACTION;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260814202431_AddStoreIdToAssetMovement'
)
BEGIN
    ALTER TABLE [AssetMovements] ADD CONSTRAINT [FK_AssetMovements_Stores_StoreId] FOREIGN KEY ([StoreId]) REFERENCES [Stores] ([Id]) ON DELETE NO ACTION;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260814202431_AddStoreIdToAssetMovement'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20260814202431_AddStoreIdToAssetMovement', N'10.0.13');
END;

COMMIT;
GO

BEGIN TRANSACTION;
IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260824075904_AddApplicationUserRoleAndLoginTracking'
)
BEGIN
    ALTER TABLE [Assets] DROP CONSTRAINT [FK_Assets_Users_AssignedUserId];
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260824075904_AddApplicationUserRoleAndLoginTracking'
)
BEGIN
    ALTER TABLE [Users] DROP CONSTRAINT [PK_Users];
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260824075904_AddApplicationUserRoleAndLoginTracking'
)
BEGIN
    EXEC sp_rename N'[Users]', N'ApplicationUsers', 'OBJECT';
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260824075904_AddApplicationUserRoleAndLoginTracking'
)
BEGIN
    EXEC sp_rename N'[ApplicationUsers].[IX_Users_EntraTenantId_EntraObjectId]', N'IX_ApplicationUsers_EntraTenantId_EntraObjectId', 'INDEX';
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260824075904_AddApplicationUserRoleAndLoginTracking'
)
BEGIN
    DECLARE @var8 nvarchar(max);
    SELECT @var8 = QUOTENAME([d].[name])
    FROM [sys].[default_constraints] [d]
    INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
    WHERE ([d].[parent_object_id] = OBJECT_ID(N'[ApplicationUsers]') AND [c].[name] = N'LastName');
    IF @var8 IS NOT NULL EXEC(N'ALTER TABLE [ApplicationUsers] DROP CONSTRAINT ' + @var8 + ';');
    ALTER TABLE [ApplicationUsers] ALTER COLUMN [LastName] nvarchar(100) NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260824075904_AddApplicationUserRoleAndLoginTracking'
)
BEGIN
    DECLARE @var9 nvarchar(max);
    SELECT @var9 = QUOTENAME([d].[name])
    FROM [sys].[default_constraints] [d]
    INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
    WHERE ([d].[parent_object_id] = OBJECT_ID(N'[ApplicationUsers]') AND [c].[name] = N'FirstName');
    IF @var9 IS NOT NULL EXEC(N'ALTER TABLE [ApplicationUsers] DROP CONSTRAINT ' + @var9 + ';');
    ALTER TABLE [ApplicationUsers] ALTER COLUMN [FirstName] nvarchar(100) NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260824075904_AddApplicationUserRoleAndLoginTracking'
)
BEGIN
    ALTER TABLE [ApplicationUsers] ADD [LastLoginAtUtc] datetime2 NOT NULL DEFAULT '0001-01-01T00:00:00.0000000';
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260824075904_AddApplicationUserRoleAndLoginTracking'
)
BEGIN
    ALTER TABLE [ApplicationUsers] ADD [Role] nvarchar(50) NOT NULL DEFAULT N'';
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260824075904_AddApplicationUserRoleAndLoginTracking'
)
BEGIN
    ALTER TABLE [ApplicationUsers] ADD CONSTRAINT [PK_ApplicationUsers] PRIMARY KEY ([Id]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260824075904_AddApplicationUserRoleAndLoginTracking'
)
BEGIN
    ALTER TABLE [Assets] ADD CONSTRAINT [FK_Assets_ApplicationUsers_AssignedUserId] FOREIGN KEY ([AssignedUserId]) REFERENCES [ApplicationUsers] ([Id]) ON DELETE SET NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260824075904_AddApplicationUserRoleAndLoginTracking'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20260824075904_AddApplicationUserRoleAndLoginTracking', N'10.0.13');
END;

COMMIT;
GO

BEGIN TRANSACTION;
IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260901110248_ExtendAssetEntity'
)
BEGIN
    ALTER TABLE [Assets] ADD [CreatedAtUtc] datetime2 NOT NULL DEFAULT '0001-01-01T00:00:00.0000000';
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260901110248_ExtendAssetEntity'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20260901110248_ExtendAssetEntity', N'10.0.13');
END;

COMMIT;
GO

BEGIN TRANSACTION;
IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260902065939_AddVendorsAndContacts'
)
BEGIN
    DROP INDEX [IX_Stores_StoreNumber] ON [Stores];
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260902065939_AddVendorsAndContacts'
)
BEGIN
    EXEC sp_rename N'[Stores].[CountryCode]', N'COUNTRYCODE', 'COLUMN';
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260902065939_AddVendorsAndContacts'
)
BEGIN
    EXEC sp_rename N'[Stores].[City]', N'CITY', 'COLUMN';
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260902065939_AddVendorsAndContacts'
)
BEGIN
    EXEC sp_rename N'[Stores].[PublicSpace]', N'PUBLIC_SPACE', 'COLUMN';
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260902065939_AddVendorsAndContacts'
)
BEGIN
    EXEC sp_rename N'[Stores].[PostalCode]', N'POSTAL_CODE', 'COLUMN';
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260902065939_AddVendorsAndContacts'
)
BEGIN
    EXEC sp_rename N'[Stores].[HouseNumber]', N'HOUSE_NUMBER', 'COLUMN';
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260902065939_AddVendorsAndContacts'
)
BEGIN
    EXEC sp_rename N'[Stores].[Address]', N'STREET', 'COLUMN';
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260902065939_AddVendorsAndContacts'
)
BEGIN
    DECLARE @var10 nvarchar(max);
    SELECT @var10 = QUOTENAME([d].[name])
    FROM [sys].[default_constraints] [d]
    INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
    WHERE ([d].[parent_object_id] = OBJECT_ID(N'[Stores]') AND [c].[name] = N'COUNTRYCODE');
    IF @var10 IS NOT NULL EXEC(N'ALTER TABLE [Stores] DROP CONSTRAINT ' + @var10 + ';');
    ALTER TABLE [Stores] ALTER COLUMN [COUNTRYCODE] nvarchar(5) NOT NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260902065939_AddVendorsAndContacts'
)
BEGIN
    DECLARE @var11 nvarchar(max);
    SELECT @var11 = QUOTENAME([d].[name])
    FROM [sys].[default_constraints] [d]
    INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
    WHERE ([d].[parent_object_id] = OBJECT_ID(N'[Stores]') AND [c].[name] = N'PUBLIC_SPACE');
    IF @var11 IS NOT NULL EXEC(N'ALTER TABLE [Stores] DROP CONSTRAINT ' + @var11 + ';');
    ALTER TABLE [Stores] ALTER COLUMN [PUBLIC_SPACE] nvarchar(20) NOT NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260902065939_AddVendorsAndContacts'
)
BEGIN
    DECLARE @var12 nvarchar(max);
    SELECT @var12 = QUOTENAME([d].[name])
    FROM [sys].[default_constraints] [d]
    INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
    WHERE ([d].[parent_object_id] = OBJECT_ID(N'[Stores]') AND [c].[name] = N'POSTAL_CODE');
    IF @var12 IS NOT NULL EXEC(N'ALTER TABLE [Stores] DROP CONSTRAINT ' + @var12 + ';');
    ALTER TABLE [Stores] ALTER COLUMN [POSTAL_CODE] nvarchar(5) NOT NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260902065939_AddVendorsAndContacts'
)
BEGIN
    ALTER TABLE [Assets] ADD [VendorId] uniqueidentifier NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260902065939_AddVendorsAndContacts'
)
BEGIN
    CREATE TABLE [Vendors] (
        [VendorId] uniqueidentifier NOT NULL,
        [Name] nvarchar(200) NOT NULL,
        [Webpage] nvarchar(2048) NULL,
        [City] nvarchar(100) NOT NULL,
        [CountryCode] nvarchar(3) NOT NULL,
        [HouseNumber] nvarchar(10) NOT NULL,
        [PostalCode] nvarchar(10) NOT NULL,
        [PublicSpace] nvarchar(50) NOT NULL,
        [Street] nvarchar(100) NOT NULL,
        CONSTRAINT [PK_Vendors] PRIMARY KEY ([VendorId])
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260902065939_AddVendorsAndContacts'
)
BEGIN
    CREATE TABLE [Contacts] (
        [ContactId] uniqueidentifier NOT NULL,
        [VendorId] uniqueidentifier NOT NULL,
        [FirstName] nvarchar(100) NOT NULL,
        [LastName] nvarchar(100) NOT NULL,
        [JobTitle] nvarchar(100) NOT NULL,
        [EmailAddress] nvarchar(200) NOT NULL,
        [PhoneNumber] nvarchar(30) NOT NULL,
        CONSTRAINT [PK_Contacts] PRIMARY KEY ([ContactId]),
        CONSTRAINT [FK_Contacts_Vendors_ContactId] FOREIGN KEY ([ContactId]) REFERENCES [Vendors] ([VendorId]) ON DELETE NO ACTION
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260902065939_AddVendorsAndContacts'
)
BEGIN
    CREATE INDEX [IX_Assets_VendorId] ON [Assets] ([VendorId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260902065939_AddVendorsAndContacts'
)
BEGIN
    CREATE UNIQUE INDEX [IX_Contacts_VendorId_PhoneNumber] ON [Contacts] ([VendorId], [PhoneNumber]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260902065939_AddVendorsAndContacts'
)
BEGIN
    ALTER TABLE [Assets] ADD CONSTRAINT [FK_Assets_Vendors_VendorId] FOREIGN KEY ([VendorId]) REFERENCES [Vendors] ([VendorId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260902065939_AddVendorsAndContacts'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20260902065939_AddVendorsAndContacts', N'10.0.13');
END;

COMMIT;
GO

BEGIN TRANSACTION;
IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260902191020_ModifyAssetWithVendor'
)
BEGIN
    ALTER TABLE [Assets] DROP CONSTRAINT [FK_Assets_Vendors_VendorId];
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260902191020_ModifyAssetWithVendor'
)
BEGIN
    DROP INDEX [IX_Assets_VendorId] ON [Assets];
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260902191020_ModifyAssetWithVendor'
)
BEGIN
    EXEC sp_rename N'[Assets].[VendorId]', N'AssignedVendorId', 'COLUMN';
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260902191020_ModifyAssetWithVendor'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20260902191020_ModifyAssetWithVendor', N'10.0.13');
END;

COMMIT;
GO

BEGIN TRANSACTION;
IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260908092436_ExtendApplicationUserWithEntraProfile'
)
BEGIN
    DECLARE @var13 nvarchar(max);
    SELECT @var13 = QUOTENAME([d].[name])
    FROM [sys].[default_constraints] [d]
    INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
    WHERE ([d].[parent_object_id] = OBJECT_ID(N'[ApplicationUsers]') AND [c].[name] = N'LastLoginAtUtc');
    IF @var13 IS NOT NULL EXEC(N'ALTER TABLE [ApplicationUsers] DROP CONSTRAINT ' + @var13 + ';');
    ALTER TABLE [ApplicationUsers] ALTER COLUMN [LastLoginAtUtc] datetime2 NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260908092436_ExtendApplicationUserWithEntraProfile'
)
BEGIN
    ALTER TABLE [ApplicationUsers] ADD [Department] nvarchar(200) NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260908092436_ExtendApplicationUserWithEntraProfile'
)
BEGIN
    ALTER TABLE [ApplicationUsers] ADD [JobTitle] nvarchar(200) NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260908092436_ExtendApplicationUserWithEntraProfile'
)
BEGIN
    ALTER TABLE [ApplicationUsers] ADD [Mail] nvarchar(320) NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260908092436_ExtendApplicationUserWithEntraProfile'
)
BEGIN
    ALTER TABLE [ApplicationUsers] ADD [MobilePhone] nvarchar(50) NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260908092436_ExtendApplicationUserWithEntraProfile'
)
BEGIN
    CREATE INDEX [IX_Assets_AssignedVendorId] ON [Assets] ([AssignedVendorId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260908092436_ExtendApplicationUserWithEntraProfile'
)
BEGIN
    ALTER TABLE [Assets] ADD CONSTRAINT [FK_Assets_Vendors_AssignedVendorId] FOREIGN KEY ([AssignedVendorId]) REFERENCES [Vendors] ([VendorId]) ON DELETE SET NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260908092436_ExtendApplicationUserWithEntraProfile'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20260908092436_ExtendApplicationUserWithEntraProfile', N'10.0.13');
END;

COMMIT;
GO

BEGIN TRANSACTION;
IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260910075238_AddAssetRfidTagId'
)
BEGIN
    ALTER TABLE [Assets] ADD [RfidTagId] nvarchar(max) NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260910075238_AddAssetRfidTagId'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20260910075238_AddAssetRfidTagId', N'10.0.13');
END;

COMMIT;
GO

BEGIN TRANSACTION;
IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260910093329_ModifyEmplyooForView'
)
BEGIN
    DROP TABLE [Employees];
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260910093329_ModifyEmplyooForView'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20260910093329_ModifyEmplyooForView', N'10.0.13');
END;

COMMIT;
GO

BEGIN TRANSACTION;
IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260910093916_ModifyEmployee'
)
BEGIN
    CREATE TABLE [Employees] (
        [EmployeeNumber] nvarchar(10) NOT NULL,
        [FullName] nvarchar(200) NOT NULL,
        [EmploymentStartDate] datetime2 NOT NULL,
        [EmploymentEndDate] datetime2 NOT NULL,
        [StoreNumber] nvarchar(20) NULL,
        [OrganizationType] nvarchar(100) NOT NULL,
        [PositionCode] nvarchar(50) NOT NULL,
        [Position] nvarchar(200) NOT NULL,
        [PhoneNumber] nvarchar(50) NULL,
        [Email] nvarchar(320) NULL,
        CONSTRAINT [PK_Employees] PRIMARY KEY ([EmployeeNumber])
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260910093916_ModifyEmployee'
)
BEGIN
    CREATE INDEX [IX_Employees_Email] ON [Employees] ([Email]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260910093916_ModifyEmployee'
)
BEGIN
    CREATE INDEX [IX_Employees_FullName] ON [Employees] ([FullName]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260910093916_ModifyEmployee'
)
BEGIN
    CREATE INDEX [IX_Employees_StoreNumber] ON [Employees] ([StoreNumber]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260910093916_ModifyEmployee'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20260910093916_ModifyEmployee', N'10.0.13');
END;

COMMIT;
GO

BEGIN TRANSACTION;
IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260910102228_ModifyEmployeeColumn'
)
BEGIN
    DECLARE @var14 nvarchar(max);
    SELECT @var14 = QUOTENAME([d].[name])
    FROM [sys].[default_constraints] [d]
    INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
    WHERE ([d].[parent_object_id] = OBJECT_ID(N'[Employees]') AND [c].[name] = N'EmploymentEndDate');
    IF @var14 IS NOT NULL EXEC(N'ALTER TABLE [Employees] DROP CONSTRAINT ' + @var14 + ';');
    ALTER TABLE [Employees] ALTER COLUMN [EmploymentEndDate] datetime2 NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260910102228_ModifyEmployeeColumn'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20260910102228_ModifyEmployeeColumn', N'10.0.13');
END;

COMMIT;
GO

BEGIN TRANSACTION;
IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260910122249_AddStoreIdToApplicationUser'
)
BEGIN
    ALTER TABLE [ApplicationUsers] ADD [StoreId] uniqueidentifier NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260910122249_AddStoreIdToApplicationUser'
)
BEGIN
    CREATE INDEX [IX_ApplicationUsers_StoreId] ON [ApplicationUsers] ([StoreId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260910122249_AddStoreIdToApplicationUser'
)
BEGIN
    ALTER TABLE [ApplicationUsers] ADD CONSTRAINT [FK_ApplicationUsers_Stores_StoreId] FOREIGN KEY ([StoreId]) REFERENCES [Stores] ([Id]) ON DELETE NO ACTION;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260910122249_AddStoreIdToApplicationUser'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20260910122249_AddStoreIdToApplicationUser', N'10.0.13');
END;

COMMIT;
GO

BEGIN TRANSACTION;
IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260921114758_SyncCurrentModel'
)
BEGIN
    ALTER TABLE [Contacts] DROP CONSTRAINT [FK_Contacts_Vendors_ContactId];
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260921114758_SyncCurrentModel'
)
BEGIN
    ALTER TABLE [Contacts] ADD CONSTRAINT [FK_Contacts_Vendors_VendorId] FOREIGN KEY ([VendorId]) REFERENCES [Vendors] ([VendorId]) ON DELETE NO ACTION;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260921114758_SyncCurrentModel'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20260921114758_SyncCurrentModel', N'10.0.13');
END;

COMMIT;
GO

BEGIN TRANSACTION;
IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260921143758_AddIndexToStoreNumber'
)
BEGIN
    EXEC sp_rename N'[Stores].[STREET]', N'Street', 'COLUMN';
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260921143758_AddIndexToStoreNumber'
)
BEGIN
    EXEC sp_rename N'[Stores].[COUNTRYCODE]', N'CountryCode', 'COLUMN';
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260921143758_AddIndexToStoreNumber'
)
BEGIN
    EXEC sp_rename N'[Stores].[CITY]', N'City', 'COLUMN';
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260921143758_AddIndexToStoreNumber'
)
BEGIN
    EXEC sp_rename N'[Stores].[PUBLIC_SPACE]', N'PublicSpace', 'COLUMN';
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260921143758_AddIndexToStoreNumber'
)
BEGIN
    EXEC sp_rename N'[Stores].[POSTAL_CODE]', N'PostalCode', 'COLUMN';
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260921143758_AddIndexToStoreNumber'
)
BEGIN
    EXEC sp_rename N'[Stores].[HOUSE_NUMBER]', N'HouseNumber', 'COLUMN';
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260921143758_AddIndexToStoreNumber'
)
BEGIN
    CREATE UNIQUE INDEX [IX_Stores_StoreNumber] ON [Stores] ([StoreNumber]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260921143758_AddIndexToStoreNumber'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20260921143758_AddIndexToStoreNumber', N'10.0.13');
END;

COMMIT;
GO

BEGIN TRANSACTION;
IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260922124251_AddAssetHistoryAudit'
)
BEGIN
    CREATE TABLE [AssetHistories] (
        [Id] uniqueidentifier NOT NULL,
        [AssetId] uniqueidentifier NOT NULL,
        [Action] nvarchar(50) NOT NULL,
        [PropertyName] nvarchar(100) NOT NULL,
        [OldValue] nvarchar(2000) NULL,
        [NewValue] nvarchar(2000) NULL,
        [ChangedByUserId] uniqueidentifier NOT NULL,
        [ChangedByUserName] nvarchar(256) NOT NULL,
        [ChangedAtUtc] datetime2 NOT NULL,
        CONSTRAINT [PK_AssetHistories] PRIMARY KEY ([Id])
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260922124251_AddAssetHistoryAudit'
)
BEGIN
    CREATE INDEX [IX_AssetHistories_AssetId] ON [AssetHistories] ([AssetId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260922124251_AddAssetHistoryAudit'
)
BEGIN
    CREATE INDEX [IX_AssetHistories_AssetId_ChangedAtUtc] ON [AssetHistories] ([AssetId], [ChangedAtUtc]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260922124251_AddAssetHistoryAudit'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20260922124251_AddAssetHistoryAudit', N'10.0.13');
END;

COMMIT;
GO

BEGIN TRANSACTION;
IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260925191329_ExtendAssetWithWarranty'
)
BEGIN
    ALTER TABLE [Assets] ADD [WarrantyEndDate] date NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260925191329_ExtendAssetWithWarranty'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20260925191329_ExtendAssetWithWarranty', N'10.0.13');
END;

COMMIT;
GO

BEGIN TRANSACTION;
IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261009093129_AddServiceCases'
)
BEGIN
    CREATE TABLE [ServiceCases] (
        [Id] uniqueidentifier NOT NULL,
        [CaseNumber] nvarchar(50) NOT NULL,
        [AssetId] uniqueidentifier NOT NULL,
        [Status] nvarchar(50) NOT NULL,
        [IssueType] nvarchar(50) NOT NULL,
        [OtherIssueDescription] nvarchar(250) NULL,
        [Description] nvarchar(2000) NOT NULL,
        [OriginStoreId] uniqueidentifier NULL,
        [VendorId] uniqueidentifier NULL,
        [CreatedByUserId] uniqueidentifier NOT NULL,
        [CreatedByUserName] nvarchar(250) NOT NULL,
        [CreatedAtUtc] datetime2 NOT NULL,
        [ClosedByUserId] uniqueidentifier NULL,
        [ClosedAtUtc] datetime2 NULL,
        CONSTRAINT [PK_ServiceCases] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_ServiceCases_ApplicationUsers_ClosedByUserId] FOREIGN KEY ([ClosedByUserId]) REFERENCES [ApplicationUsers] ([Id]) ON DELETE NO ACTION,
        CONSTRAINT [FK_ServiceCases_ApplicationUsers_CreatedByUserId] FOREIGN KEY ([CreatedByUserId]) REFERENCES [ApplicationUsers] ([Id]) ON DELETE NO ACTION,
        CONSTRAINT [FK_ServiceCases_Assets_AssetId] FOREIGN KEY ([AssetId]) REFERENCES [Assets] ([Id]) ON DELETE NO ACTION,
        CONSTRAINT [FK_ServiceCases_Stores_OriginStoreId] FOREIGN KEY ([OriginStoreId]) REFERENCES [Stores] ([Id]) ON DELETE NO ACTION,
        CONSTRAINT [FK_ServiceCases_Vendors_VendorId] FOREIGN KEY ([VendorId]) REFERENCES [Vendors] ([VendorId]) ON DELETE NO ACTION
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261009093129_AddServiceCases'
)
BEGIN
    CREATE TABLE [ServiceCaseEvents] (
        [Id] uniqueidentifier NOT NULL,
        [ServiceCaseId] uniqueidentifier NOT NULL,
        [Type] nvarchar(50) NOT NULL,
        [PreviousStatus] nvarchar(50) NULL,
        [NewStatus] nvarchar(50) NOT NULL,
        [Comment] nvarchar(2000) NULL,
        [PerformedByUserId] uniqueidentifier NOT NULL,
        [PerformedByUserName] nvarchar(250) NOT NULL,
        [OccurredAtUtc] datetime2 NOT NULL,
        CONSTRAINT [PK_ServiceCaseEvents] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_ServiceCaseEvents_ApplicationUsers_PerformedByUserId] FOREIGN KEY ([PerformedByUserId]) REFERENCES [ApplicationUsers] ([Id]) ON DELETE NO ACTION,
        CONSTRAINT [FK_ServiceCaseEvents_ServiceCases_ServiceCaseId] FOREIGN KEY ([ServiceCaseId]) REFERENCES [ServiceCases] ([Id]) ON DELETE CASCADE
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261009093129_AddServiceCases'
)
BEGIN
    CREATE INDEX [IX_ServiceCaseEvents_PerformedByUserId] ON [ServiceCaseEvents] ([PerformedByUserId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261009093129_AddServiceCases'
)
BEGIN
    CREATE INDEX [IX_ServiceCaseEvents_ServiceCaseId] ON [ServiceCaseEvents] ([ServiceCaseId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261009093129_AddServiceCases'
)
BEGIN
    CREATE INDEX [IX_ServiceCaseEvents_ServiceCaseId_OccurredAtUtc] ON [ServiceCaseEvents] ([ServiceCaseId], [OccurredAtUtc]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261009093129_AddServiceCases'
)
BEGIN
    CREATE INDEX [IX_ServiceCaseEvents_Type] ON [ServiceCaseEvents] ([Type]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261009093129_AddServiceCases'
)
BEGIN
    CREATE INDEX [IX_ServiceCases_AssetId] ON [ServiceCases] ([AssetId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261009093129_AddServiceCases'
)
BEGIN
    CREATE INDEX [IX_ServiceCases_AssetId_Status] ON [ServiceCases] ([AssetId], [Status]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261009093129_AddServiceCases'
)
BEGIN
    CREATE INDEX [IX_ServiceCases_ClosedByUserId] ON [ServiceCases] ([ClosedByUserId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261009093129_AddServiceCases'
)
BEGIN
    CREATE INDEX [IX_ServiceCases_CreatedByUserId] ON [ServiceCases] ([CreatedByUserId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261009093129_AddServiceCases'
)
BEGIN
    CREATE INDEX [IX_ServiceCases_OriginStoreId] ON [ServiceCases] ([OriginStoreId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261009093129_AddServiceCases'
)
BEGIN
    CREATE INDEX [IX_ServiceCases_Status] ON [ServiceCases] ([Status]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261009093129_AddServiceCases'
)
BEGIN
    CREATE INDEX [IX_ServiceCases_VendorId] ON [ServiceCases] ([VendorId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261009093129_AddServiceCases'
)
BEGIN
    CREATE UNIQUE INDEX [UX_ServiceCases_CaseNumber] ON [ServiceCases] ([CaseNumber]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261009093129_AddServiceCases'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20261009093129_AddServiceCases', N'10.0.13');
END;

COMMIT;
GO

BEGIN TRANSACTION;
IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261009113116_AddCaseNumberSequences'
)
BEGIN
    CREATE TABLE [CaseNumberSequences] (
        [Prefix] nvarchar(10) NOT NULL,
        [Year] int NOT NULL,
        [LastNumber] int NOT NULL,
        [RowVersion] rowversion NOT NULL,
        CONSTRAINT [PK_CaseNumberSequences] PRIMARY KEY ([Prefix], [Year])
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261009113116_AddCaseNumberSequences'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20261009113116_AddCaseNumberSequences', N'10.0.13');
END;

COMMIT;
GO

BEGIN TRANSACTION;
IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261031181357_AddPackageShipping'
)
BEGIN
    CREATE TABLE [Packages] (
        [Id] uniqueidentifier NOT NULL,
        [PackageNumber] nvarchar(50) NOT NULL,
        [SourceStoreId] uniqueidentifier NOT NULL,
        [DestinationStoreId] uniqueidentifier NOT NULL,
        [Status] nvarchar(50) NOT NULL,
        [CourierUserId] uniqueidentifier NULL,
        [Comment] nvarchar(2000) NULL,
        [CreatedAtUtc] datetime2 NOT NULL,
        [CreatedByUserId] uniqueidentifier NOT NULL,
        [DispatchedAtUtc] datetime2 NULL,
        [DispatchedByUserId] uniqueidentifier NULL,
        [ReceivedAtUtc] datetime2 NULL,
        [ReceivedByUserId] uniqueidentifier NULL,
        [CancelledAtUtc] datetime2 NULL,
        [CancelledByUserId] uniqueidentifier NULL,
        CONSTRAINT [PK_Packages] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_Packages_ApplicationUsers_CancelledByUserId] FOREIGN KEY ([CancelledByUserId]) REFERENCES [ApplicationUsers] ([Id]) ON DELETE NO ACTION,
        CONSTRAINT [FK_Packages_ApplicationUsers_CourierUserId] FOREIGN KEY ([CourierUserId]) REFERENCES [ApplicationUsers] ([Id]) ON DELETE NO ACTION,
        CONSTRAINT [FK_Packages_ApplicationUsers_CreatedByUserId] FOREIGN KEY ([CreatedByUserId]) REFERENCES [ApplicationUsers] ([Id]) ON DELETE NO ACTION,
        CONSTRAINT [FK_Packages_ApplicationUsers_DispatchedByUserId] FOREIGN KEY ([DispatchedByUserId]) REFERENCES [ApplicationUsers] ([Id]) ON DELETE NO ACTION,
        CONSTRAINT [FK_Packages_ApplicationUsers_ReceivedByUserId] FOREIGN KEY ([ReceivedByUserId]) REFERENCES [ApplicationUsers] ([Id]) ON DELETE NO ACTION,
        CONSTRAINT [FK_Packages_Stores_DestinationStoreId] FOREIGN KEY ([DestinationStoreId]) REFERENCES [Stores] ([Id]) ON DELETE NO ACTION,
        CONSTRAINT [FK_Packages_Stores_SourceStoreId] FOREIGN KEY ([SourceStoreId]) REFERENCES [Stores] ([Id]) ON DELETE NO ACTION
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261031181357_AddPackageShipping'
)
BEGIN
    CREATE TABLE [PackageEvents] (
        [Id] uniqueidentifier NOT NULL,
        [PackageId] uniqueidentifier NOT NULL,
        [Type] nvarchar(50) NOT NULL,
        [PreviousStatus] nvarchar(50) NULL,
        [NewStatus] nvarchar(50) NOT NULL,
        [Comment] nvarchar(2000) NULL,
        [PerformedByUserId] uniqueidentifier NOT NULL,
        [PerformedByUserName] nvarchar(250) NOT NULL,
        [OccurredAtUtc] datetime2 NOT NULL,
        [RelatedAssetId] uniqueidentifier NULL,
        [RelatedUserId] uniqueidentifier NULL,
        CONSTRAINT [PK_PackageEvents] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_PackageEvents_ApplicationUsers_PerformedByUserId] FOREIGN KEY ([PerformedByUserId]) REFERENCES [ApplicationUsers] ([Id]) ON DELETE NO ACTION,
        CONSTRAINT [FK_PackageEvents_ApplicationUsers_RelatedUserId] FOREIGN KEY ([RelatedUserId]) REFERENCES [ApplicationUsers] ([Id]) ON DELETE NO ACTION,
        CONSTRAINT [FK_PackageEvents_Assets_RelatedAssetId] FOREIGN KEY ([RelatedAssetId]) REFERENCES [Assets] ([Id]) ON DELETE NO ACTION,
        CONSTRAINT [FK_PackageEvents_Packages_PackageId] FOREIGN KEY ([PackageId]) REFERENCES [Packages] ([Id]) ON DELETE CASCADE
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261031181357_AddPackageShipping'
)
BEGIN
    CREATE TABLE [PackageItems] (
        [Id] uniqueidentifier NOT NULL,
        [PackageId] uniqueidentifier NOT NULL,
        [AssetId] uniqueidentifier NOT NULL,
        [AddedAtUtc] datetime2 NOT NULL,
        [AddedByUserId] uniqueidentifier NOT NULL,
        CONSTRAINT [PK_PackageItems] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_PackageItems_ApplicationUsers_AddedByUserId] FOREIGN KEY ([AddedByUserId]) REFERENCES [ApplicationUsers] ([Id]) ON DELETE NO ACTION,
        CONSTRAINT [FK_PackageItems_Assets_AssetId] FOREIGN KEY ([AssetId]) REFERENCES [Assets] ([Id]) ON DELETE NO ACTION,
        CONSTRAINT [FK_PackageItems_Packages_PackageId] FOREIGN KEY ([PackageId]) REFERENCES [Packages] ([Id]) ON DELETE CASCADE
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261031181357_AddPackageShipping'
)
BEGIN
    CREATE INDEX [IX_PackageEvents_OccurredAtUtc] ON [PackageEvents] ([OccurredAtUtc]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261031181357_AddPackageShipping'
)
BEGIN
    CREATE INDEX [IX_PackageEvents_PackageId] ON [PackageEvents] ([PackageId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261031181357_AddPackageShipping'
)
BEGIN
    CREATE INDEX [IX_PackageEvents_PackageId_OccurredAtUtc] ON [PackageEvents] ([PackageId], [OccurredAtUtc]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261031181357_AddPackageShipping'
)
BEGIN
    CREATE INDEX [IX_PackageEvents_PerformedByUserId] ON [PackageEvents] ([PerformedByUserId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261031181357_AddPackageShipping'
)
BEGIN
    CREATE INDEX [IX_PackageEvents_RelatedAssetId] ON [PackageEvents] ([RelatedAssetId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261031181357_AddPackageShipping'
)
BEGIN
    CREATE INDEX [IX_PackageEvents_RelatedUserId] ON [PackageEvents] ([RelatedUserId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261031181357_AddPackageShipping'
)
BEGIN
    CREATE INDEX [IX_PackageEvents_Type] ON [PackageEvents] ([Type]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261031181357_AddPackageShipping'
)
BEGIN
    CREATE INDEX [IX_PackageItems_AddedByUserId] ON [PackageItems] ([AddedByUserId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261031181357_AddPackageShipping'
)
BEGIN
    CREATE INDEX [IX_PackageItems_AssetId] ON [PackageItems] ([AssetId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261031181357_AddPackageShipping'
)
BEGIN
    CREATE INDEX [IX_PackageItems_PackageId] ON [PackageItems] ([PackageId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261031181357_AddPackageShipping'
)
BEGIN
    CREATE UNIQUE INDEX [UX_PackageItems_PackageId_AssetId] ON [PackageItems] ([PackageId], [AssetId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261031181357_AddPackageShipping'
)
BEGIN
    CREATE INDEX [IX_Packages_CancelledByUserId] ON [Packages] ([CancelledByUserId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261031181357_AddPackageShipping'
)
BEGIN
    CREATE INDEX [IX_Packages_CourierUserId] ON [Packages] ([CourierUserId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261031181357_AddPackageShipping'
)
BEGIN
    CREATE INDEX [IX_Packages_CreatedByUserId] ON [Packages] ([CreatedByUserId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261031181357_AddPackageShipping'
)
BEGIN
    CREATE INDEX [IX_Packages_DestinationStoreId] ON [Packages] ([DestinationStoreId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261031181357_AddPackageShipping'
)
BEGIN
    CREATE INDEX [IX_Packages_DestinationStoreId_Status] ON [Packages] ([DestinationStoreId], [Status]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261031181357_AddPackageShipping'
)
BEGIN
    CREATE INDEX [IX_Packages_DispatchedByUserId] ON [Packages] ([DispatchedByUserId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261031181357_AddPackageShipping'
)
BEGIN
    CREATE INDEX [IX_Packages_ReceivedByUserId] ON [Packages] ([ReceivedByUserId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261031181357_AddPackageShipping'
)
BEGIN
    CREATE INDEX [IX_Packages_SourceStoreId] ON [Packages] ([SourceStoreId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261031181357_AddPackageShipping'
)
BEGIN
    CREATE INDEX [IX_Packages_SourceStoreId_Status] ON [Packages] ([SourceStoreId], [Status]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261031181357_AddPackageShipping'
)
BEGIN
    CREATE INDEX [IX_Packages_Status] ON [Packages] ([Status]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261031181357_AddPackageShipping'
)
BEGIN
    CREATE UNIQUE INDEX [UX_Packages_PackageNumber] ON [Packages] ([PackageNumber]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261031181357_AddPackageShipping'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20261031181357_AddPackageShipping', N'10.0.13');
END;

COMMIT;
GO

