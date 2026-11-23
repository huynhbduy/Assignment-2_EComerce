-- Run the entire file in SSMS or the VS Code MSSQL extension.
USE master;
GO
IF DB_ID(N'B2BWholesale') IS NULL
    EXEC(N'CREATE DATABASE B2BWholesale');
GO
USE B2BWholesale;
GO
SET XACT_ABORT ON;
BEGIN TRY
    IF DB_NAME() <> N'B2BWholesale'
        THROW 50001, 'Incorrect database context.', 1;
    BEGIN TRANSACTION;

    CREATE TABLE dbo.Item (
        ItemID int IDENTITY PRIMARY KEY,
        ItemName nvarchar(120) NOT NULL CHECK (LEN(LTRIM(ItemName)) > 0),
        Size nvarchar(50) NOT NULL CHECK (LEN(LTRIM(Size)) > 0),
        Price decimal(18,2) NOT NULL CHECK (Price > 0),
        StockQuantity int NOT NULL DEFAULT 0 CHECK (StockQuantity >= 0)
    );
    CREATE TABLE dbo.Agent (
        AgentID int IDENTITY PRIMARY KEY,
        AgentName nvarchar(120) NOT NULL CHECK (LEN(LTRIM(AgentName)) > 0),
        Address nvarchar(240) NOT NULL CHECK (LEN(LTRIM(Address)) > 0),
        Phone varchar(20) NOT NULL CHECK (LEN(LTRIM(Phone)) > 0)
    );
    CREATE TABLE dbo.[Order] (
        OrderID int IDENTITY PRIMARY KEY,
        OrderDate datetime2(0) NOT NULL DEFAULT SYSDATETIME(),
        AgentID int NOT NULL REFERENCES dbo.Agent(AgentID) ON DELETE NO ACTION,
        OrderStatus varchar(10) NOT NULL DEFAULT 'Pending'
            CHECK (OrderStatus IN ('Pending','Shipped'))
    );
    CREATE TABLE dbo.OrderDetail (
        ID int IDENTITY PRIMARY KEY,
        OrderID int NOT NULL REFERENCES dbo.[Order](OrderID) ON DELETE CASCADE,
        ItemID int NOT NULL REFERENCES dbo.Item(ItemID) ON DELETE NO ACTION,
        Quantity int NOT NULL CHECK (Quantity > 0),
        UnitAmount decimal(18,2) NOT NULL CHECK (UnitAmount > 0),
        UNIQUE (OrderID, ItemID)
    );
    CREATE TABLE dbo.Users (
        UserID int IDENTITY PRIMARY KEY,
        UserName nvarchar(80) NOT NULL UNIQUE CHECK (LEN(LTRIM(UserName)) > 0),
        email varchar(254) NOT NULL UNIQUE CHECK (email LIKE '%_@_%._%'),
        [password] varchar(512) NOT NULL CHECK (LEN([password]) > 0),
        [lock] bit NOT NULL DEFAULT 0,
        Role varchar(10) NOT NULL CHECK (Role IN ('Admin','Agent')),
        AgentID int NULL REFERENCES dbo.Agent(AgentID) ON DELETE NO ACTION,
        CHECK ((Role = 'Admin' AND AgentID IS NULL) OR
               (Role = 'Agent' AND AgentID IS NOT NULL))
    );

    INSERT dbo.Item (ItemName, Size, Price, StockQuantity) VALUES
        (N'Giấy in A4', N'Ram 500 tờ', 72000, 120),
        (N'Giấy in A3', N'Ram 500 tờ', 145000, 125),
        (N'Bút bi xanh', N'Hộp 20 cây', 64000, 130),
        (N'Bút chì HB', N'Hộp 12 cây', 38000, 135),
        (N'Sổ tay', N'Lốc 10 cuốn', 120000, 140),
        (N'Bìa hồ sơ', N'Tập 20 cái', 42000, 145),
        (N'Kẹp giấy', N'Hộp 100 cái', 14000, 150),
        (N'Băng keo', N'Lốc 6 cuộn', 51000, 155),
        (N'Bút đánh dấu', N'Hộp 10 cây', 81000, 160),
        (N'Giấy ghi chú', N'Lốc 12 tập', 60000, 165),
        (N'Phong bì', N'Tập 100 cái', 46000, 170),
        (N'Kim bấm', N'Hộp 1000 kim', 9000, 175),
        (N'Túi đựng tài liệu', N'Tập 10 túi', 36000, 180),
        (N'Mực in đen', N'Chai 100 ml', 98000, 185),
        (N'Bút lông bảng', N'Hộp 10 cây', 75000, 190);

    INSERT dbo.Agent (AgentName, Address, Phone) VALUES
        (N'Minh An', N'21 Nguyễn Trãi, TP.HCM', N'0900000001'),
        (N'Hồng Hà', N'27 Lê Lợi, TP.HCM', N'0900000002'),
        (N'Tân Phú', N'33 Trần Hưng Đạo, TP.HCM', N'0900000003'),
        (N'Đại Thành', N'39 Nguyễn Trãi, TP.HCM', N'0900000004'),
        (N'An Khang', N'45 Lê Lợi, TP.HCM', N'0900000005'),
        (N'Bình Minh', N'51 Trần Hưng Đạo, TP.HCM', N'0900000006'),
        (N'Thiên Phúc', N'57 Nguyễn Trãi, TP.HCM', N'0900000007'),
        (N'Nam Sơn', N'63 Lê Lợi, TP.HCM', N'0900000008'),
        (N'Phú Quý', N'69 Trần Hưng Đạo, TP.HCM', N'0900000009'),
        (N'Hoàng Gia', N'75 Nguyễn Trãi, TP.HCM', N'0900000010'),
        (N'Đông Hải', N'81 Lê Lợi, TP.HCM', N'0900000011'),
        (N'Kim Long', N'87 Trần Hưng Đạo, TP.HCM', N'0900000012'),
        (N'Trường An', N'93 Nguyễn Trãi, TP.HCM', N'0900000013'),
        (N'Vạn Phát', N'99 Lê Lợi, TP.HCM', N'0900000014'),
        (N'Gia Bảo', N'105 Trần Hưng Đạo, TP.HCM', N'0900000015');

    INSERT dbo.[Order] (OrderDate, AgentID, OrderStatus)
    SELECT DATEADD(day, AgentID - 1, CAST('20261101' AS date)), AgentID,
           CASE WHEN AgentID % 3 = 0 THEN 'Pending' ELSE 'Shipped' END
    FROM dbo.Agent;

    -- Two different products per order. UnitAmount snapshots the sale price in VND.
    INSERT dbo.OrderDetail (OrderID, ItemID, Quantity, UnitAmount)
    SELECT o.OrderID, i.ItemID, 2 + o.AgentID % 5, i.Price
    FROM dbo.[Order] AS o
    JOIN dbo.Item AS i ON i.ItemID IN (o.AgentID, 1 + o.AgentID % 15);

    UPDATE i SET StockQuantity = i.StockQuantity - sold.TotalQuantity
    FROM dbo.Item AS i
    JOIN (SELECT ItemID, SUM(Quantity) AS TotalQuantity
          FROM dbo.OrderDetail GROUP BY ItemID) AS sold ON sold.ItemID = i.ItemID;

    -- Demo password: Wholesale!Demo26 (ASP.NET Core Identity V3 hashes).
    INSERT dbo.Users (UserName, email, [password], [lock], Role, AgentID) VALUES
        (N'admin', N'admin@example.com', N'AQAAAAIAAYagAAAAEDFsTnygRG2oahasSCPFCEeC/o8AueG6sE3xXwr811ONbH6zy6LIqaLarRk11qqaqw==', 0, N'Admin', NULL),
        (N'agent01', N'agent01@example.com', N'AQAAAAIAAYagAAAAEHmZcO5zjjQH53WuhspzEVlipOdLb2LTKzAuboQ4JWCZX3RtSRIT3+9YY7XJQ9ecYA==', 0, N'Agent', 1),
        (N'agent02', N'agent02@example.com', N'AQAAAAIAAYagAAAAEN5VFfBHW3bufqGDw9z5HF7a8hTZ8FxjFEmevF+50mxiVifMYUUKqmcsJ6xYNJos5A==', 0, N'Agent', 2),
        (N'agent03', N'agent03@example.com', N'AQAAAAIAAYagAAAAEKKVMfESA4oezd+MPiBUXt4p21hmb8itp1AvGvGasN5PtFKfg7aGMdlcFUDPHktZMQ==', 0, N'Agent', 3),
        (N'agent04', N'agent04@example.com', N'AQAAAAIAAYagAAAAEMaEe8Gvr2xBBa5ZMRpdVlQ+8L9gS9RlB0xC6CFnSImD87tpqNpIBcn8peaXOjmEUg==', 0, N'Agent', 4),
        (N'agent05', N'agent05@example.com', N'AQAAAAIAAYagAAAAEKTSxxBJ3jMQeVv33lzfqkjguevH6TN7X3L9zeFGVSRvEGkCXAHsqsQtSuffyfKcZA==', 0, N'Agent', 5),
        (N'agent06', N'agent06@example.com', N'AQAAAAIAAYagAAAAEJbuh6oDUSHDSEOxhKe5v0hMaZtZeji/0zkTOBzy9WfNf/wMddKURKzol6JSuNo4mw==', 0, N'Agent', 6),
        (N'agent07', N'agent07@example.com', N'AQAAAAIAAYagAAAAENYFLfGAb8+8p5QVpzlgoDs08khJUpDo9wRKLri2QqbuLBqvU4BdoMX6XPB45hCRpA==', 0, N'Agent', 7),
        (N'agent08', N'agent08@example.com', N'AQAAAAIAAYagAAAAEERp10csv8RSLBmRqcfKGYFShCT3ZuFz4TuJZ+6jgjy+PGm0opl63+kBTDzx1qjvGQ==', 0, N'Agent', 8),
        (N'agent09', N'agent09@example.com', N'AQAAAAIAAYagAAAAEFPjiYHlocyRAod/Q6zVOl9mackduAppTKtgQd0IFbV601UcEg8esO2UZ8IQXsMR3g==', 0, N'Agent', 9),
        (N'agent10', N'agent10@example.com', N'AQAAAAIAAYagAAAAEFUXAN+IK3Z1M+0hZAam5F1P752Ac4e/n+BhioPrMY5mBbtxpJChhrsovOiE6TF2xQ==', 0, N'Agent', 10),
        (N'agent11', N'agent11@example.com', N'AQAAAAIAAYagAAAAEI5Y3FXgat6Zy3D7hvmmSXWDWBsccETWpdF8327qsWoBUON9XpDN7bLqSnbcPDqUQQ==', 0, N'Agent', 11),
        (N'agent12', N'agent12@example.com', N'AQAAAAIAAYagAAAAEAxDTAlfshGmJFI7hI+1pQrlGMAACNHeC+MTZC6ZGOkrqWp8WwuTlt+Ov1boPuHyYA==', 0, N'Agent', 12),
        (N'agent13', N'agent13@example.com', N'AQAAAAIAAYagAAAAELiZDCs2dE2PY1dHaD3UEBox+NKY4miyuF1REjdCxsIYoz1JXE3JIHhrtzhC9sRulA==', 0, N'Agent', 13),
        (N'agent14', N'agent14@example.com', N'AQAAAAIAAYagAAAAENxZwBRZsQawtT/axD8zgAPqGq72wHPrbOlf6ZPxiLlkTXJN9Z3iZOTUukiMDs6hTA==', 0, N'Agent', 14),
        (N'agent15', N'agent15@example.com', N'AQAAAAIAAYagAAAAEFlkCItW5TA03JXNWHkjGIVNvatNG53vx2T51ha9GfyB1Srk0tTWMreZ5wmMgqMQ4g==', 1, N'Agent', 15);

    COMMIT TRANSACTION;
END TRY
BEGIN CATCH
    IF XACT_STATE() <> 0 ROLLBACK TRANSACTION;
    THROW;
END CATCH;

SELECT 'Item' AS TableName, COUNT(*) AS RowCountActual FROM dbo.Item
UNION ALL SELECT 'Agent', COUNT(*) FROM dbo.Agent
UNION ALL SELECT 'Order', COUNT(*) FROM dbo.[Order]
UNION ALL SELECT 'OrderDetail', COUNT(*) FROM dbo.OrderDetail
UNION ALL SELECT 'Users', COUNT(*) FROM dbo.Users;
