USE [InternetProviderDB];
GO

SET NOCOUNT ON;
GO

BEGIN TRY
    BEGIN TRANSACTION;

    -- Сначала очищаем дочерние таблицы
    DELETE FROM [dbo].[Payments];
    DELETE FROM [dbo].[Equipment];
    DELETE FROM [dbo].[Requests];
    DELETE FROM [dbo].[Contracts];

    -- Затем родительские таблицы
    DELETE FROM [dbo].[Tariffs];
    DELETE FROM [dbo].[Clients];

    -- Сбрасываем IDENTITY, чтобы следующий ID снова был 1
    DBCC CHECKIDENT ('[dbo].[Payments]', RESEED, 0);
    DBCC CHECKIDENT ('[dbo].[Equipment]', RESEED, 0);
    DBCC CHECKIDENT ('[dbo].[Requests]', RESEED, 0);
    DBCC CHECKIDENT ('[dbo].[Contracts]', RESEED, 0);
    DBCC CHECKIDENT ('[dbo].[Tariffs]', RESEED, 0);
    DBCC CHECKIDENT ('[dbo].[Clients]', RESEED, 0);

    COMMIT TRANSACTION;
    PRINT N'Все таблицы очищены. ID сброшены, следующая запись будет начинаться с 1.';
END TRY
BEGIN CATCH
    IF @@TRANCOUNT > 0
        ROLLBACK TRANSACTION;

    PRINT N'Произошла ошибка при очистке таблиц.';
    PRINT ERROR_MESSAGE();
END CATCH;
GO