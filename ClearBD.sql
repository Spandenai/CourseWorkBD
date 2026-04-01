USE InternetProviderDB;
GO

BEGIN TRY
    BEGIN TRANSACTION;

    DELETE FROM dbo.Payments;
    DELETE FROM dbo.Equipment;
    DELETE FROM dbo.Requests;
    DELETE FROM dbo.Contracts;
    DELETE FROM dbo.Tariffs;
    DELETE FROM dbo.Clients;

    DBCC CHECKIDENT ('dbo.Payments', RESEED, 0);
    DBCC CHECKIDENT ('dbo.Equipment', RESEED, 0);
    DBCC CHECKIDENT ('dbo.Requests', RESEED, 0);
    DBCC CHECKIDENT ('dbo.Contracts', RESEED, 0);
    DBCC CHECKIDENT ('dbo.Tariffs', RESEED, 0);
    DBCC CHECKIDENT ('dbo.Clients', RESEED, 0);

    COMMIT TRANSACTION;
END TRY
BEGIN CATCH
    IF @@TRANCOUNT > 0
        ROLLBACK TRANSACTION;

    THROW;
END CATCH;
GO