
IF NOT EXISTS (SELECT name FROM sys.databases WHERE name = 'MilkStore4')
BEGIN
    CREATE DATABASE MilkStore4;
END
GO

USE MilkStore4;
GO

-- Roles
IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'Roles')
CREATE TABLE Roles (
    Id       INT          IDENTITY(1,1) PRIMARY KEY,
    RoleName NVARCHAR(50) NOT NULL
);
GO

-- Users
IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'Users')
CREATE TABLE Users (
    Id       INT           IDENTITY(1,1) PRIMARY KEY,
    RoleId   INT           NOT NULL,
    FullName NVARCHAR(150) NOT NULL,
    Email    NVARCHAR(150) NOT NULL UNIQUE,
    Password NVARCHAR(255) NOT NULL,
    Address  NVARCHAR(255) NULL,
    Phone    NVARCHAR(20)  NULL,
    CONSTRAINT FK_Users_Roles FOREIGN KEY (RoleId) REFERENCES Roles(Id)
);
GO

-- Categories
IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'Categories')
CREATE TABLE Categories (
    Id   INT           IDENTITY(1,1) PRIMARY KEY,
    Name NVARCHAR(100) NOT NULL
);
GO

-- Brands
IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'Brands')
CREATE TABLE Brands (
    Id   INT           IDENTITY(1,1) PRIMARY KEY,
    Name NVARCHAR(100) NOT NULL
);
GO

-- Products
IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'Products')
CREATE TABLE Products (
    Id            INT             IDENTITY(1,1) PRIMARY KEY,
    CategoryId    INT             NOT NULL,
    BrandId       INT             NOT NULL,
    ProductName   NVARCHAR(200)   NOT NULL,
    Description   NVARCHAR(MAX)   NULL,
    Price         DECIMAL(18, 2)  NULL,
    StockQuantity INT             NOT NULL DEFAULT 0,
    ImageUrl      NVARCHAR(500)   NULL,
    ExpiryDate    DATE            NOT NULL DEFAULT GETDATE(),
    CONSTRAINT FK_Products_Categories FOREIGN KEY (CategoryId) REFERENCES Categories(Id),
    CONSTRAINT FK_Products_Brands     FOREIGN KEY (BrandId)    REFERENCES Brands(Id)
);
GO

-- Orders
IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'Orders')
CREATE TABLE Orders (
    Id              INT             IDENTITY(1,1) PRIMARY KEY,
    UserId          INT             NOT NULL,
    OrderDate       DATETIME        NOT NULL DEFAULT GETDATE(),
    TotalAmount     DECIMAL(18, 2)  NOT NULL,
    Status          NVARCHAR(50)    NOT NULL DEFAULT 'Pending',
    PaymentMethod   NVARCHAR(50)    NOT NULL DEFAULT 'COD',
    ShippingAddress NVARCHAR(500)   NULL,
    Note            NVARCHAR(500)   NULL,
    CONSTRAINT FK_Orders_Users FOREIGN KEY (UserId) REFERENCES Users(Id)
);
GO

-- OrderItems
IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'OrderItems')
CREATE TABLE OrderItems (
    Id          INT            IDENTITY(1,1) PRIMARY KEY,
    OrderId     INT            NOT NULL,
    ProductId   INT            NOT NULL,
    Quantity    INT            NOT NULL DEFAULT 1,
    PriceAtTime DECIMAL(18, 2) NOT NULL,
    CONSTRAINT FK_OrderItems_Orders   FOREIGN KEY (OrderId)   REFERENCES Orders(Id)   ON DELETE CASCADE,
    CONSTRAINT FK_OrderItems_Products FOREIGN KEY (ProductId) REFERENCES Products(Id)
);
GO

-- CartItems
IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'CartItems')
CREATE TABLE CartItems (
    Id        INT IDENTITY(1,1) PRIMARY KEY,
    UserId    INT NOT NULL,
    ProductId INT NOT NULL,
    Quantity  INT NOT NULL DEFAULT 1,
    CONSTRAINT FK_CartItems_Users    FOREIGN KEY (UserId)    REFERENCES Users(Id)    ON DELETE CASCADE,
    CONSTRAINT FK_CartItems_Products FOREIGN KEY (ProductId) REFERENCES Products(Id) ON DELETE CASCADE
);
GO

-- Reviews
IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'Reviews')
CREATE TABLE Reviews (
    Id        INT          IDENTITY(1,1) PRIMARY KEY,
    UserId    INT          NOT NULL,
    ProductId INT          NOT NULL,
    Rating    INT          NOT NULL DEFAULT 5 CHECK (Rating BETWEEN 1 AND 5),
    Comment   NVARCHAR(MAX) NULL,
    CreatedAt DATETIME     NOT NULL DEFAULT GETDATE(),
    CONSTRAINT FK_Reviews_Users    FOREIGN KEY (UserId)    REFERENCES Users(Id),
    CONSTRAINT FK_Reviews_Products FOREIGN KEY (ProductId) REFERENCES Products(Id)
);
GO

--  3. DỮ LIỆU MẪU

-- Roles
IF NOT EXISTS (SELECT 1 FROM Roles)
BEGIN
    INSERT INTO Roles (RoleName) VALUES
        (N'Admin'),
        (N'Customer');
END
GO

IF NOT EXISTS (SELECT 1 FROM Users)
BEGIN
    INSERT INTO Users (RoleId, FullName, Email, Password, Phone, Address) VALUES
        (1, N'Quản Trị Viên',  'admin@milkstore.vn',   '12346', '0901234567', N'Hà Nội, Việt Nam'),
        (2, N'Nguyễn Văn An',  'an.nguyen@gmail.com',  '3345', '0912345678', N'Quận Hoàn Kiếm, Hà Nội'),
        (2, N'Trần Thị Bình',  'binh.tran@gmail.com',  '5667', '0923456789', N'Quận Cầu Giấy, Hà Nội'),
        (2, N'Lê Minh Châu',   'chau.le@gmail.com',    '7788', '0934567890', N'TP. Hồ Chí Minh'),
        (2, N'Phạm Thị Dung',  'dung.pham@gmail.com',  '6677', '0945678901', N'Đà Nẵng'),
        (2, N'Hoàng Văn Em',   'em.hoang@gmail.com',   '1122', '0956789012', N'Hải Phòng');
END
GO

-- Categories
IF NOT EXISTS (SELECT 1 FROM Categories)
BEGIN
    INSERT INTO Categories (Name) VALUES
        (N'Sữa tươi'),
        (N'Sữa bột'),
        (N'Sữa chua'),
        (N'Sữa đặc');
END
GO

-- Brands
IF NOT EXISTS (SELECT 1 FROM Brands)
BEGIN
    INSERT INTO Brands (Name) VALUES
        (N'Vinamilk'),
        (N'TH True Milk'),
        (N'Mead Johnson'),
        (N'Nestlé');
END
GO

-- Products
IF NOT EXISTS (SELECT 1 FROM Products)
BEGIN
    INSERT INTO Products (CategoryId, BrandId, ProductName, Description, Price, StockQuantity, ImageUrl, ExpiryDate) VALUES
    
    (1, 1, N'Sữa tươi tiệt trùng Vinamilk 100% Có Đường 1L',
        N'Sữa tươi tiệt trùng Vinamilk được làm từ 100% sữa bò tươi nguyên chất, giàu canxi và protein, thích hợp cho cả gia đình.',
        32000, 150, 'https://images.unsplash.com/photo-1600718374662-0483d2b9da44?w=400', '2025-12-31'),

    (1, 1, N'Sữa tươi tiệt trùng Vinamilk Không Đường 1L',
        N'Sữa tươi không đường phù hợp cho người ăn kiêng và người tiểu đường, giữ nguyên dưỡng chất tự nhiên.',
        32000, 120, 'https://images.unsplash.com/photo-1563636619-e9143da7973b?w=400', '2025-12-31'),

    (1, 2, N'Sữa tươi TH True Milk Có Đường 1L',
        N'Sữa tươi sạch TH True Milk từ trang trại bò sữa công nghệ cao, không chất bảo quản, không kháng sinh.',
        35000, 100, 'https://images.unsplash.com/photo-1550583724-b2692b85b150?w=400', '2025-12-31'),

    (1, 2, N'Sữa tươi TH True Milk Không Đường 180ml (lốc 4 hộp)',
        N'Hộp nhỏ tiện lợi cho trẻ em đi học, giàu vitamin D và canxi giúp phát triển xương.',
        38000, 200, 'https://images.unsplash.com/photo-1565688534245-05d6b5be184a?w=400', '2025-12-31'),

 
    (2, 3, N'Sữa bột Enfamil A+ Số 1 (0-6 tháng) 400g',
        N'Công thức dinh dưỡng toàn diện cho trẻ sơ sinh, bổ sung DHA và ARA hỗ trợ phát triển não bộ.',
        285000, 80, 'https://images.unsplash.com/photo-1543353071-873f17a7a088?w=400', '2026-06-30'),

    (2, 3, N'Sữa bột Enfagrow A+ Số 3 (1-3 tuổi) 900g',
        N'Dinh dưỡng đầy đủ cho trẻ từ 1 đến 3 tuổi với hệ MFGM và DHA giúp phát triển trí não.',
        520000, 60, 'https://images.unsplash.com/photo-1571771894821-ce9b6c11b08e?w=400', '2026-06-30'),

    (2, 4, N'Sữa bột Nestlé NAN Optipro Số 1 400g',
        N'Công thức Optipro với hàm lượng protein tối ưu, dễ tiêu hóa, phù hợp cho trẻ từ 0-6 tháng.',
        310000, 75, 'https://images.unsplash.com/photo-1600718374662-0483d2b9da44?w=400', '2026-03-31'),

    (2, 1, N'Sữa bột Vinamilk Sure Prevent Gold 900g',
        N'Sữa dinh dưỡng cho người lớn tuổi, tăng cường hệ miễn dịch và sức khỏe xương khớp.',
        395000, 55, 'https://images.unsplash.com/photo-1563636619-e9143da7973b?w=400', '2026-09-30'),


    (3, 2, N'Sữa chua ăn TH True Yogurt Dâu 100g (lốc 6 hộp)',
        N'Sữa chua làm từ sữa bò tươi TH True Milk, vị dâu tươi ngon, probiotics giúp tiêu hóa tốt.',
        68000, 180, 'https://images.unsplash.com/photo-1565688534245-05d6b5be184a?w=400', '2025-10-31'),

    (3, 1, N'Sữa chua uống Vinamilk Probeauty 130ml (lốc 4 chai)',
        N'Sữa chua uống kết hợp collagen và vitamin C, tốt cho da và hệ tiêu hóa.',
        42000, 220, 'https://images.unsplash.com/photo-1550583724-b2692b85b150?w=400', '2025-10-31'),

    (3, 2, N'Sữa chua TH True Yogurt Không Đường 100g (lốc 6 hộp)',
        N'Sữa chua nguyên chất không đường, thích hợp cho người ăn kiêng, giàu protein và men vi sinh.',
        72000, 140, 'https://images.unsplash.com/photo-1571771894821-ce9b6c11b08e?w=400', '2025-10-31'),


    (4, 1, N'Sữa đặc có đường Ngôi Sao Phương Nam 380g',
        N'Sữa đặc truyền thống hương vị thơm ngon, dùng để pha cà phê, làm bánh, hoặc ăn trực tiếp.',
        18000, 300, 'https://images.unsplash.com/photo-1600718374662-0483d2b9da44?w=400', '2026-12-31'),

    (4, 4, N'Sữa đặc có đường Nestlé 380g',
        N'Sữa đặc Nestlé với hương vị ngọt ngào đặc trưng, chất lượng quốc tế, phù hợp nhiều món ăn.',
        19500, 280, 'https://images.unsplash.com/photo-1543353071-873f17a7a088?w=400', '2026-12-31'),

    (4, 1, N'Sữa đặc không đường Vinamilk 380g',
        N'Sữa đặc không đường dành cho người tiểu đường và người ăn kiêng, vẫn giữ nguyên dinh dưỡng.',
        21000, 160, 'https://images.unsplash.com/photo-1565688534245-05d6b5be184a?w=400', '2026-12-31');
END
GO

-- Orders
IF NOT EXISTS (SELECT 1 FROM Orders)
BEGIN
    INSERT INTO Orders (UserId, OrderDate, TotalAmount, Status, PaymentMethod, ShippingAddress, Note) VALUES
        (2, DATEADD(DAY, -30, GETDATE()), 160000,  N'Completed', N'COD',          N'Quận Hoàn Kiếm, Hà Nội',     NULL),
        (3, DATEADD(DAY, -25, GETDATE()), 285000,  N'Completed', N'BankTransfer', N'Quận Cầu Giấy, Hà Nội',      N'Giao giờ hành chính'),
        (4, DATEADD(DAY, -20, GETDATE()), 520000,  N'Completed', N'COD',          N'TP. Hồ Chí Minh',             NULL),
        (2, DATEADD(DAY, -15, GETDATE()), 74000,   N'Completed', N'COD',          N'Quận Hoàn Kiếm, Hà Nội',     NULL),
        (5, DATEADD(DAY, -10, GETDATE()), 395000,  N'Completed', N'BankTransfer', N'Đà Nẵng',                     N'Gọi trước 30 phút'),
        (3, DATEADD(DAY,  -7, GETDATE()), 340000,  N'Shipped',   N'COD',          N'Quận Cầu Giấy, Hà Nội',      NULL),
        (6, DATEADD(DAY,  -5, GETDATE()), 136000,  N'Shipped',   N'BankTransfer', N'Hải Phòng',                   NULL),
        (4, DATEADD(DAY,  -3, GETDATE()), 695000,  N'Processing',N'COD',          N'TP. Hồ Chí Minh',             N'Để trước cửa'),
        (2, DATEADD(DAY,  -1, GETDATE()), 110000,  N'Pending',   N'COD',          N'Quận Hoàn Kiếm, Hà Nội',     NULL),
        (5, GETDATE(),                    567000,  N'Pending',   N'BankTransfer', N'Đà Nẵng',                     NULL);
END
GO

-- OrderItems
IF NOT EXISTS (SELECT 1 FROM OrderItems)
BEGIN
    INSERT INTO OrderItems (OrderId, ProductId, Quantity, PriceAtTime) VALUES
        (1,  1, 3, 32000),  
        (1,  9, 1, 68000),  
        (2,  5, 1, 285000), 
        (3,  6, 1, 520000), 
        (4,  9, 1, 68000),  
        (4, 10, 1, 42000), 
        (5,  8, 1, 395000), 
        (6,  3, 2, 35000),  
        (6,  7, 1, 310000), 
        (7, 12, 4, 18000),  
        (7, 10, 2, 42000), 
        (8,  6, 1, 520000), 
        (8,  4, 3, 38000), 
        (9,  1, 2, 32000), 
        (9,  9, 1, 68000),  
        (10, 8, 1, 395000),
        (10, 3, 4, 35000),  
        (10, 5, 1, 285000); 
END
GO

-- Reviews
IF NOT EXISTS (SELECT 1 FROM Reviews)
BEGIN
    INSERT INTO Reviews (UserId, ProductId, Rating, Comment, CreatedAt) VALUES
        (2, 1, 5, N'Sữa rất ngon, con tôi rất thích uống hàng ngày. Giao hàng nhanh!', DATEADD(DAY, -28, GETDATE())),
        (3, 5, 5, N'Sữa bột chính hãng, con bé nhà tôi ăn tốt hơn sau khi dùng. Rất hài lòng.', DATEADD(DAY, -22, GETDATE())),
        (4, 6, 4, N'Sản phẩm tốt, giá hơi cao nhưng chất lượng xứng đáng. Sẽ mua lại.', DATEADD(DAY, -18, GETDATE())),
        (2, 9, 5, N'Sữa chua ngon, vị dâu tự nhiên không quá ngọt. Đóng gói cẩn thận.', DATEADD(DAY, -13, GETDATE())),
        (5, 8, 4, N'Mẹ tôi uống thấy khỏe hơn, tiêu hóa tốt hơn. Shop tư vấn nhiệt tình.', DATEADD(DAY, -8, GETDATE())),
        (3, 3, 5, N'TH True Milk ngon hơn nhiều so với các loại khác. Sẽ mua thường xuyên.', DATEADD(DAY, -5, GETDATE())),
        (6, 12, 3, N'Sữa đặc bình thường, vừa đủ ngọt. Dùng để pha cà phê thì ok.', DATEADD(DAY, -3, GETDATE())),
        (4, 10, 5, N'Sữa chua uống rất ngon, mua cho cả nhà uống mỗi buổi sáng.', DATEADD(DAY, -1, GETDATE()));
END
GO

--  4. VIEWS HỮU ÍCH

-- View: Thông tin đơn hàng đầy đủ
CREATE OR ALTER VIEW vw_OrderDetails AS
SELECT
    o.Id            AS OrderId,
    o.OrderDate,
    o.Status,
    o.PaymentMethod,
    o.TotalAmount,
    o.ShippingAddress,
    u.FullName      AS CustomerName,
    u.Email         AS CustomerEmail,
    u.Phone         AS CustomerPhone,
    oi.Quantity,
    oi.PriceAtTime,
    oi.Quantity * oi.PriceAtTime AS LineTotal,
    p.ProductName,
    c.Name          AS CategoryName,
    b.Name          AS BrandName
FROM Orders o
JOIN Users u        ON o.UserId    = u.Id
JOIN OrderItems oi  ON oi.OrderId  = o.Id
JOIN Products p     ON oi.ProductId = p.Id
JOIN Categories c   ON p.CategoryId = c.Id
JOIN Brands b       ON p.BrandId    = b.Id;
GO

-- View: Thống kê sản phẩm 
CREATE OR ALTER VIEW vw_ProductStats AS
SELECT
    p.Id,
    p.ProductName,
    c.Name          AS Category,
    b.Name          AS Brand,
    p.Price,
    p.StockQuantity,
    ISNULL(SUM(oi.Quantity), 0)                           AS TotalSold,
    ISNULL(SUM(oi.Quantity * oi.PriceAtTime), 0)          AS TotalRevenue,
    ISNULL(CAST(AVG(CAST(r.Rating AS FLOAT)) AS DECIMAL(3,1)), 0) AS AvgRating,
    COUNT(DISTINCT r.Id)                                   AS ReviewCount
FROM Products p
JOIN Categories c   ON p.CategoryId = c.Id
JOIN Brands b       ON p.BrandId    = b.Id
LEFT JOIN OrderItems oi ON oi.ProductId = p.Id
LEFT JOIN Orders o      ON oi.OrderId   = o.Id AND o.Status = 'Completed'
LEFT JOIN Reviews r     ON r.ProductId  = p.Id
GROUP BY p.Id, p.ProductName, c.Name, b.Name, p.Price, p.StockQuantity;
GO

-- View: Thống kê khách hàng
CREATE OR ALTER VIEW vw_CustomerStats AS
SELECT
    u.Id,
    u.FullName,
    u.Email,
    u.Phone,
    COUNT(DISTINCT o.Id)    AS TotalOrders,
    ISNULL(SUM(o.TotalAmount), 0) AS TotalSpent,
    MAX(o.OrderDate)        AS LastOrderDate
FROM Users u
LEFT JOIN Orders o ON o.UserId = u.Id AND o.Status = 'Completed'
WHERE u.RoleId = 2
GROUP BY u.Id, u.FullName, u.Email, u.Phone;
GO

--  5. STORED PROCEDURES

-- SP: Tìm kiếm sản phẩm
CREATE OR ALTER PROCEDURE sp_SearchProducts
    @Keyword      NVARCHAR(200) = NULL,
    @CategoryId   INT           = NULL,
    @BrandId      INT           = NULL,
    @MinPrice     DECIMAL(18,2) = NULL,
    @MaxPrice     DECIMAL(18,2) = NULL,
    @SortBy       NVARCHAR(20)  = 'Name',   
    @PageNumber   INT           = 1,
    @PageSize     INT           = 12
AS
BEGIN
    SET NOCOUNT ON;

    DECLARE @Offset INT = (@PageNumber - 1) * @PageSize;

    SELECT
        p.Id, p.ProductName, p.Price, p.StockQuantity, p.ImageUrl,
        c.Name AS Category, b.Name AS Brand,
        ISNULL(CAST(AVG(CAST(r.Rating AS FLOAT)) AS DECIMAL(3,1)), 0) AS AvgRating,
        COUNT(DISTINCT r.Id) AS ReviewCount
    FROM Products p
    JOIN Categories c ON p.CategoryId = c.Id
    JOIN Brands b     ON p.BrandId    = b.Id
    LEFT JOIN Reviews r ON r.ProductId = p.Id
    WHERE
        (@Keyword    IS NULL OR p.ProductName LIKE '%' + @Keyword + '%' OR p.Description LIKE '%' + @Keyword + '%')
        AND (@CategoryId IS NULL OR p.CategoryId = @CategoryId)
        AND (@BrandId    IS NULL OR p.BrandId    = @BrandId)
        AND (@MinPrice   IS NULL OR p.Price >= @MinPrice)
        AND (@MaxPrice   IS NULL OR p.Price <= @MaxPrice)
    GROUP BY p.Id, p.ProductName, p.Price, p.StockQuantity, p.ImageUrl, c.Name, b.Name
    ORDER BY
        CASE WHEN @SortBy = 'Price_ASC'  THEN p.Price END ASC,
        CASE WHEN @SortBy = 'Price_DESC' THEN p.Price END DESC,
        CASE WHEN @SortBy = 'Rating'     THEN AVG(CAST(r.Rating AS FLOAT)) END DESC,
        CASE WHEN @SortBy = 'Name'       THEN p.ProductName END ASC,
        p.Id DESC
    OFFSET @Offset ROWS FETCH NEXT @PageSize ROWS ONLY;
END
GO

-- SP: Đặt hàng
CREATE OR ALTER PROCEDURE sp_PlaceOrder
    @UserId          INT,
    @ShippingAddress NVARCHAR(500),
    @PaymentMethod   NVARCHAR(50) = 'COD',
    @Note            NVARCHAR(500) = NULL,
    @NewOrderId      INT OUTPUT
AS
BEGIN
    SET NOCOUNT ON;
    BEGIN TRANSACTION;

    BEGIN TRY
        -- Tính tổng tiền từ giỏ hàng
        DECLARE @Total DECIMAL(18,2);
        SELECT @Total = SUM(p.Price * ci.Quantity)
        FROM CartItems ci
        JOIN Products p ON ci.ProductId = p.Id
        WHERE ci.UserId = @UserId;

        IF @Total IS NULL OR @Total = 0
            THROW 50001, N'Giỏ hàng trống!', 1;

        -- Tạo đơn hàng
        INSERT INTO Orders (UserId, TotalAmount, Status, PaymentMethod, ShippingAddress, Note)
        VALUES (@UserId, @Total, 'Pending', @PaymentMethod, @ShippingAddress, @Note);

        SET @NewOrderId = SCOPE_IDENTITY();

        -- Thêm OrderItems và trừ kho
        INSERT INTO OrderItems (OrderId, ProductId, Quantity, PriceAtTime)
        SELECT @NewOrderId, ci.ProductId, ci.Quantity, p.Price
        FROM CartItems ci
        JOIN Products p ON ci.ProductId = p.Id
        WHERE ci.UserId = @UserId;

        UPDATE p
        SET p.StockQuantity = p.StockQuantity - ci.Quantity
        FROM Products p
        JOIN CartItems ci ON ci.ProductId = p.Id
        WHERE ci.UserId = @UserId;

        -- Xóa giỏ hàng
        DELETE FROM CartItems WHERE UserId = @UserId;

        COMMIT TRANSACTION;
    END TRY
    BEGIN CATCH
        ROLLBACK TRANSACTION;
        THROW;
    END CATCH
END
GO

-- SP: Cập nhật trạng thái đơn hàng
CREATE OR ALTER PROCEDURE sp_UpdateOrderStatus
    @OrderId   INT,
    @NewStatus NVARCHAR(50)
AS
BEGIN
    SET NOCOUNT ON;

    IF @NewStatus NOT IN ('Pending', 'Processing', 'Shipped', 'Completed', 'Cancelled')
        THROW 50002, N'Trạng thái không hợp lệ!', 1;

    UPDATE Orders SET Status = @NewStatus WHERE Id = @OrderId;

    -- Nếu hủy đơn hàng → hoàn lại kho
    IF @NewStatus = 'Cancelled'
    BEGIN
        UPDATE p
        SET p.StockQuantity = p.StockQuantity + oi.Quantity
        FROM Products p
        JOIN OrderItems oi ON oi.ProductId = p.Id
        WHERE oi.OrderId = @OrderId;
    END
END
GO

-- SP: Báo cáo doanh thu theo tháng
CREATE OR ALTER PROCEDURE sp_RevenueReport
    @Year INT = NULL
AS
BEGIN
    SET NOCOUNT ON;
    IF @Year IS NULL SET @Year = YEAR(GETDATE());

    SELECT
        @Year                                          AS [Year],
        MONTH(o.OrderDate)                             AS [Month],
        DATENAME(MONTH, o.OrderDate)                   AS MonthName,
        COUNT(DISTINCT o.Id)                           AS TotalOrders,
        SUM(o.TotalAmount)                             AS Revenue,
        COUNT(DISTINCT o.UserId)                       AS UniqueCustomers
    FROM Orders o
    WHERE YEAR(o.OrderDate) = @Year AND o.Status = 'Completed'
    GROUP BY MONTH(o.OrderDate), DATENAME(MONTH, o.OrderDate)
    ORDER BY MONTH(o.OrderDate);
END
GO

-- SP: Thêm/cập nhật sản phẩm vào giỏ hàng
CREATE OR ALTER PROCEDURE sp_AddToCart
    @UserId    INT,
    @ProductId INT,
    @Quantity  INT = 1
AS
BEGIN
    SET NOCOUNT ON;

    -- Kiểm tra tồn kho
    DECLARE @Stock INT;
    SELECT @Stock = StockQuantity FROM Products WHERE Id = @ProductId;

    IF @Stock IS NULL THROW 50003, N'Sản phẩm không tồn tại!', 1;

    DECLARE @Existing INT;
    SELECT @Existing = Quantity FROM CartItems WHERE UserId = @UserId AND ProductId = @ProductId;

    IF @Existing IS NOT NULL
    BEGIN
        IF (@Existing + @Quantity) > @Stock
            THROW 50004, N'Vượt quá số lượng tồn kho!', 1;

        UPDATE CartItems
        SET Quantity = Quantity + @Quantity
        WHERE UserId = @UserId AND ProductId = @ProductId;
    END
    ELSE
    BEGIN
        IF @Quantity > @Stock
            THROW 50004, N'Vượt quá số lượng tồn kho!', 1;

        INSERT INTO CartItems (UserId, ProductId, Quantity)
        VALUES (@UserId, @ProductId, @Quantity);
    END
END
GO

--  6. TRUY VẤN BÁO CÁO

--  Top 5 sản phẩm bán chạy nhất
SELECT TOP 5
    p.ProductName,
    b.Name      AS Brand,
    c.Name      AS Category,
    SUM(oi.Quantity)                        AS TotalSold,
    SUM(oi.Quantity * oi.PriceAtTime)       AS TotalRevenue,
    p.StockQuantity                         AS RemainingStock
FROM OrderItems oi
JOIN Orders o     ON oi.OrderId   = o.Id
JOIN Products p   ON oi.ProductId = p.Id
JOIN Brands b     ON p.BrandId    = b.Id
JOIN Categories c ON p.CategoryId = c.Id
WHERE o.Status = 'Completed'
GROUP BY p.Id, p.ProductName, b.Name, c.Name, p.StockQuantity
ORDER BY TotalSold DESC;
GO

-- Doanh thu theo danh mục
SELECT
    c.Name              AS Category,
    COUNT(DISTINCT p.Id)        AS ProductCount,
    SUM(oi.Quantity)            AS TotalUnitsSold,
    SUM(oi.Quantity * oi.PriceAtTime) AS TotalRevenue,
    CAST(
        SUM(oi.Quantity * oi.PriceAtTime) * 100.0
        / SUM(SUM(oi.Quantity * oi.PriceAtTime)) OVER()
    AS DECIMAL(5,2))            AS RevenuePercent
FROM Categories c
JOIN Products p     ON p.CategoryId = c.Id
JOIN OrderItems oi  ON oi.ProductId = p.Id
JOIN Orders o       ON oi.OrderId   = o.Id AND o.Status = 'Completed'
GROUP BY c.Id, c.Name
ORDER BY TotalRevenue DESC;
GO

--  Khách hàng VIP (chi tiêu nhiều nhất)
SELECT TOP 10
    u.FullName,
    u.Email,
    u.Phone,
    COUNT(DISTINCT o.Id)    AS TotalOrders,
    SUM(o.TotalAmount)      AS TotalSpent,
    CAST(AVG(o.TotalAmount) AS DECIMAL(18,2)) AS AvgOrderValue,
    MAX(o.OrderDate)        AS LastOrder
FROM Users u
JOIN Orders o ON o.UserId = u.Id AND o.Status = 'Completed'
GROUP BY u.Id, u.FullName, u.Email, u.Phone
ORDER BY TotalSpent DESC;
GO

-- Sản phẩm sắp hết hàng (tồn kho dưới 20)
SELECT
    p.Id,
    p.ProductName,
    b.Name          AS Brand,
    c.Name          AS Category,
    p.StockQuantity AS Stock,
    p.Price,
    CASE
        WHEN p.StockQuantity = 0  THEN N'Hết hàng'
        WHEN p.StockQuantity < 10 THEN N'Nguy hiểm'
        ELSE N'Sắp hết'
    END AS StockStatus
FROM Products p
JOIN Brands b     ON p.BrandId    = b.Id
JOIN Categories c ON p.CategoryId = c.Id
WHERE p.StockQuantity < 20
ORDER BY p.StockQuantity ASC;
GO

--  Doanh thu 30 ngày gần nhất (theo ngày)
SELECT
    CAST(o.OrderDate AS DATE)       AS [Date],
    COUNT(DISTINCT o.Id)            AS Orders,
    SUM(o.TotalAmount)              AS Revenue,
    COUNT(DISTINCT o.UserId)        AS Customers
FROM Orders o
WHERE o.OrderDate >= DATEADD(DAY, -30, GETDATE())
  AND o.Status = 'Completed'
GROUP BY CAST(o.OrderDate AS DATE)
ORDER BY [Date] DESC;
GO

--  Thống kê trạng thái đơn hàng
SELECT
    Status,
    COUNT(*)            AS OrderCount,
    SUM(TotalAmount)    AS TotalValue,
    CAST(COUNT(*) * 100.0 / SUM(COUNT(*)) OVER() AS DECIMAL(5,2)) AS Percentage
FROM Orders
GROUP BY Status
ORDER BY OrderCount DESC;
GO

--  Sản phẩm được đánh giá cao nhất
SELECT
    p.ProductName,
    b.Name  AS Brand,
    CAST(AVG(CAST(r.Rating AS FLOAT)) AS DECIMAL(3,1)) AS AvgRating,
    COUNT(r.Id)         AS ReviewCount,
    SUM(CASE WHEN r.Rating = 5 THEN 1 ELSE 0 END) AS FiveStarCount
FROM Reviews r
JOIN Products p ON r.ProductId = p.Id
JOIN Brands b   ON p.BrandId   = b.Id
GROUP BY p.Id, p.ProductName, b.Name
HAVING COUNT(r.Id) >= 1
ORDER BY AvgRating DESC, ReviewCount DESC;
GO

--  7. INDEX 

IF NOT EXISTS (SELECT name FROM sys.indexes WHERE name='IX_Products_CategoryId')
CREATE INDEX IX_Products_CategoryId ON Products(CategoryId);

IF NOT EXISTS (SELECT name FROM sys.indexes WHERE name='IX_Products_BrandId')
CREATE INDEX IX_Products_BrandId ON Products(BrandId);

IF NOT EXISTS (SELECT name FROM sys.indexes WHERE name='IX_Products_Price')
CREATE INDEX IX_Products_Price ON Products(Price);

IF NOT EXISTS (SELECT name FROM sys.indexes WHERE name='IX_Orders_UserId')
CREATE INDEX IX_Orders_UserId ON Orders(UserId);

IF NOT EXISTS (SELECT name FROM sys.indexes WHERE name='IX_Orders_Status')
CREATE INDEX IX_Orders_Status ON Orders(Status);

IF NOT EXISTS (SELECT name FROM sys.indexes WHERE name='IX_Orders_OrderDate')
CREATE INDEX IX_Orders_OrderDate ON Orders(OrderDate);

IF NOT EXISTS (SELECT name FROM sys.indexes WHERE name='IX_OrderItems_OrderId')
CREATE INDEX IX_OrderItems_OrderId ON OrderItems(OrderId);

IF NOT EXISTS (SELECT name FROM sys.indexes WHERE name='IX_OrderItems_ProductId')
CREATE INDEX IX_OrderItems_ProductId ON OrderItems(ProductId);

IF NOT EXISTS (SELECT name FROM sys.indexes WHERE name='IX_CartItems_UserId')
CREATE INDEX IX_CartItems_UserId ON CartItems(UserId);

IF NOT EXISTS (SELECT name FROM sys.indexes WHERE name='IX_Reviews_ProductId')
CREATE INDEX IX_Reviews_ProductId ON Reviews(ProductId);

PRINT N' MilkStore database đã được khởi tạo thành công!';
GO

