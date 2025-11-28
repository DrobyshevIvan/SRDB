USE MedShop;
GO

-- Створення скалярної функції COUNT_ORDERS, якщо вона не існує
IF OBJECT_ID('dbo.COUNT_ORDERS', 'FN') IS NOT NULL
    DROP FUNCTION dbo.COUNT_ORDERS;
GO

CREATE FUNCTION COUNT_ORDERS(@money MONEY)
RETURNS INT
AS
BEGIN
    DECLARE @COUNT INT
    SET @COUNT = (SELECT COUNT(*) FROM Orders WHERE TotalAmount < @money)
    RETURN @COUNT
END;
GO

-- Створення табличної функції GetUsersWithExpensiveProductsInCategory, якщо вона не існує
IF OBJECT_ID('dbo.GetUsersWithExpensiveProductsInCategory', 'TF') IS NOT NULL
    DROP FUNCTION dbo.GetUsersWithExpensiveProductsInCategory;
GO

CREATE FUNCTION GetUsersWithExpensiveProductsInCategory(
    @min_price MONEY, 
    @category_id INT
)
RETURNS TABLE
AS
RETURN
(
    SELECT DISTINCT 
        o.UserId, 
        u.UserName, 
        u.FullName
    FROM Orders o
    INNER JOIN OrderItems oi ON o.Id = oi.OrderId
    INNER JOIN Products p ON oi.ProductId = p.Id
    INNER JOIN Users u ON o.UserId = u.Id
    WHERE oi.UnitPrice > @min_price
      AND p.CategoryId = @category_id
);
GO

-- Перевірка скалярної функції
SELECT dbo.COUNT_ORDERS(800.00) AS OrderCount;

-- Перевірка табличної функції
SELECT * FROM dbo.GetUsersWithExpensiveProductsInCategory(500, 1);

