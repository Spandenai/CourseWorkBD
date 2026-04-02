USE [InternetProviderDB];
GO

SET NOCOUNT ON;
GO

BEGIN TRY
    BEGIN TRANSACTION;

    /* =========================
       1. Клиенты
       ========================= */
    INSERT INTO [dbo].[Clients]
        ([last_name], [first_name], [middle_name], [phone], [email], [connection_address])
    VALUES
        (N'Абрамовский', N'Артём', N'Валерьевич', N'+7-900-111-11-11', N'artem.abramovsky@example.com', N'г. Томск, ул. Ленина, д. 10, кв. 5'),
        (N'Иванов',      N'Иван',   N'Сергеевич',   N'+7-900-222-22-22', N'ivanov@example.com',           N'г. Томск, пр. Мира, д. 25, кв. 14'),
        (N'Петрова',     N'Анна',   N'Олеговна',    N'+7-900-333-33-33', N'petrova@example.com',          N'г. Томск, ул. Советская, д. 8, кв. 21'),
        (N'Смирнов',     N'Дмитрий',N'Андреевич',   N'+7-900-444-44-44', N'smirnov@example.com',          N'г. Томск, ул. Учебная, д. 17, кв. 9'),
        (N'Кузнецова',   N'Елена',  N'Викторовна',  N'+7-900-555-55-55', N'kuznetsova@example.com',       N'г. Томск, ул. Нахимова, д. 33, кв. 42');

    /* =========================
       2. Тарифы
       ========================= */
    INSERT INTO [dbo].[Tariffs]
        ([tariff_name], [internet_speed], [monthly_fee], [description])
    VALUES
        (N'Базовый 100',  N'100 Мбит/с', 500.00, N'Базовый тариф для домашнего использования'),
        (N'Комфорт 200',  N'200 Мбит/с', 700.00, N'Оптимальный тариф для семьи и онлайн-кинотеатров'),
        (N'Турбо 300',    N'300 Мбит/с', 900.00, N'Повышенная скорость для активных пользователей'),
        (N'Геймер 500',   N'500 Мбит/с', 1200.00, N'Тариф с высокой скоростью и стабильным соединением'),
        (N'Премиум 1000', N'1 Гбит/с',   1600.00, N'Максимальная скорость для дома и офиса');

    /* =========================
       3. Договоры
       ========================= */
    INSERT INTO [dbo].[Contracts]
        ([contract_number], [date_signed], [contract_status], [client_id], [tariff_id])
    VALUES
        (
            N'CNT-2026-001',
            '2026-04-01',
            N'Активен',
            (SELECT client_id FROM [dbo].[Clients] WHERE phone = N'+7-900-111-11-11'),
            (SELECT tariff_id FROM [dbo].[Tariffs] WHERE tariff_name = N'Комфорт 200')
        ),
        (
            N'CNT-2026-002',
            '2026-04-02',
            N'Активен',
            (SELECT client_id FROM [dbo].[Clients] WHERE phone = N'+7-900-222-22-22'),
            (SELECT tariff_id FROM [dbo].[Tariffs] WHERE tariff_name = N'Базовый 100')
        ),
        (
            N'CNT-2026-003',
            '2026-04-03',
            N'Активен',
            (SELECT client_id FROM [dbo].[Clients] WHERE phone = N'+7-900-333-33-33'),
            (SELECT tariff_id FROM [dbo].[Tariffs] WHERE tariff_name = N'Турбо 300')
        ),
        (
            N'CNT-2026-004',
            '2026-04-04',
            N'Приостановлен',
            (SELECT client_id FROM [dbo].[Clients] WHERE phone = N'+7-900-444-44-44'),
            (SELECT tariff_id FROM [dbo].[Tariffs] WHERE tariff_name = N'Геймер 500')
        ),
        (
            N'CNT-2026-005',
            '2026-04-05',
            N'Активен',
            (SELECT client_id FROM [dbo].[Clients] WHERE phone = N'+7-900-555-55-55'),
            (SELECT tariff_id FROM [dbo].[Tariffs] WHERE tariff_name = N'Премиум 1000')
        );

    /* =========================
       4. Платежи
       ========================= */
    INSERT INTO [dbo].[Payments]
        ([contract_id], [payment_date], [amount], [payment_method])
    VALUES
        (
            (SELECT contract_id FROM [dbo].[Contracts] WHERE contract_number = N'CNT-2026-001'),
            '2026-04-06',
            700.00,
            N'Банковская карта'
        ),
        (
            (SELECT contract_id FROM [dbo].[Contracts] WHERE contract_number = N'CNT-2026-002'),
            '2026-04-06',
            500.00,
            N'Наличные'
        ),
        (
            (SELECT contract_id FROM [dbo].[Contracts] WHERE contract_number = N'CNT-2026-003'),
            '2026-04-07',
            900.00,
            N'Онлайн-оплата'
        ),
        (
            (SELECT contract_id FROM [dbo].[Contracts] WHERE contract_number = N'CNT-2026-004'),
            '2026-04-08',
            1200.00,
            N'Банковская карта'
        ),
        (
            (SELECT contract_id FROM [dbo].[Contracts] WHERE contract_number = N'CNT-2026-005'),
            '2026-04-09',
            1600.00,
            N'СБП'
        );

    /* =========================
       5. Оборудование
       ========================= */
    INSERT INTO [dbo].[Equipment]
        ([equipment_name], [equipment_type], [serial_number], [cost], [equipment_status], [contract_id])
    VALUES
        (
            N'Keenetic Viva',
            N'Роутер',
            N'SN-ROUTER-001',
            4500.00,
            N'Установлено',
            (SELECT contract_id FROM [dbo].[Contracts] WHERE contract_number = N'CNT-2026-001')
        ),
        (
            N'TP-Link Archer C6',
            N'Роутер',
            N'SN-ROUTER-002',
            3900.00,
            N'Установлено',
            (SELECT contract_id FROM [dbo].[Contracts] WHERE contract_number = N'CNT-2026-002')
        ),
        (
            N'Huawei EchoLife HG8245',
            N'ONT-терминал',
            N'SN-ONT-003',
            5200.00,
            N'Установлено',
            (SELECT contract_id FROM [dbo].[Contracts] WHERE contract_number = N'CNT-2026-003')
        ),
        (
            N'ZTE F660',
            N'Модем',
            N'SN-MODEM-004',
            3100.00,
            N'На обслуживании',
            (SELECT contract_id FROM [dbo].[Contracts] WHERE contract_number = N'CNT-2026-004')
        ),
        (
            N'Xiaomi Router AX3000',
            N'Роутер',
            N'SN-ROUTER-005',
            6100.00,
            N'Установлено',
            (SELECT contract_id FROM [dbo].[Contracts] WHERE contract_number = N'CNT-2026-005')
        );

    /* =========================
       6. Заявки
       ========================= */
    INSERT INTO [dbo].[Requests]
        ([client_id], [contract_id], [request_date], [request_type], [description], [request_status])
    VALUES
        (
            (SELECT client_id FROM [dbo].[Clients] WHERE phone = N'+7-900-111-11-11'),
            (SELECT contract_id FROM [dbo].[Contracts] WHERE contract_number = N'CNT-2026-001'),
            '2026-04-10',
            N'Подключение',
            N'Первичное подключение интернета по адресу клиента',
            N'Выполнена'
        ),
        (
            (SELECT client_id FROM [dbo].[Clients] WHERE phone = N'+7-900-222-22-22'),
            (SELECT contract_id FROM [dbo].[Contracts] WHERE contract_number = N'CNT-2026-002'),
            '2026-04-11',
            N'Техническая поддержка',
            N'Низкая скорость интернета в вечернее время',
            N'В обработке'
        ),
        (
            (SELECT client_id FROM [dbo].[Clients] WHERE phone = N'+7-900-333-33-33'),
            (SELECT contract_id FROM [dbo].[Contracts] WHERE contract_number = N'CNT-2026-003'),
            '2026-04-12',
            N'Смена тарифа',
            N'Клиент хочет перейти на более быстрый тариф',
            N'Новая'
        ),
        (
            (SELECT client_id FROM [dbo].[Clients] WHERE phone = N'+7-900-444-44-44'),
            (SELECT contract_id FROM [dbo].[Contracts] WHERE contract_number = N'CNT-2026-004'),
            '2026-04-13',
            N'Ремонт оборудования',
            N'Неисправность модема, требуется диагностика',
            N'В обработке'
        ),
        (
            (SELECT client_id FROM [dbo].[Clients] WHERE phone = N'+7-900-555-55-55'),
            (SELECT contract_id FROM [dbo].[Contracts] WHERE contract_number = N'CNT-2026-005'),
            '2026-04-14',
            N'Консультация',
            N'Консультация по настройке Wi-Fi роутера',
            N'Закрыта'
        );

    COMMIT TRANSACTION;
    PRINT N'Тестовые данные успешно добавлены во все таблицы.';
END TRY
BEGIN CATCH
    IF @@TRANCOUNT > 0
        ROLLBACK TRANSACTION;

    PRINT N'Ошибка при добавлении тестовых данных.';
    PRINT ERROR_MESSAGE();
END CATCH;
GO