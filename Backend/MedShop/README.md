# MedShop REST API - Лабораторна робота №3

## Опис

REST API для роботи з базою даних MedShop, створеної на попередніх лабораторних роботах.

## Технології

- .NET 9.0
- Entity Framework Core 9.0
- SQL Server
- Swagger/OpenAPI

## Налаштування

### 1. Підключення до бази даних

Відредагуйте `appsettings.json` і вкажіть правильний connection string:

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Server=ВАШ_СЕРВЕР;Database=MedShop;Trusted_Connection=True;TrustServerCertificate=True;"
  }
}
```

### 2. Створення функцій (якщо не існують)

Виконайте SQL скрипт `createFunctions.sql` у вашій базі даних. Скрипт створює:

- Скалярну функцію `COUNT_ORDERS` - підраховує кількість замовлень з сумою менше за задану
- Табличну функцію `GetUsersWithExpensiveProductsInCategory` - визначає користувачів, які купили товари дорожчі за задану ціну в заданій категорії

### 3. Запуск проекту

```bash
cd API
dotnet restore
dotnet run
```

API буде доступне за адресою: `https://localhost:5001` або `http://localhost:5000`
Swagger UI: `https://localhost:5001/swagger` або `http://localhost:5000/swagger`

## API Endpoints

### Products

- `GET /api/products` - Отримати всі товари
- `GET /api/products/{id}` - Отримати детальну інформацію про товар з усіма продажами
- `POST /api/products/purchase` - Виконати покупку товару (виклик процедури `usp_PurchaseProduct`)
- `GET /api/products/users-with-expensive-products?minPrice={price}&categoryId={id}` - Отримати користувачів з дорогими товарами (таблична функція)
- `GET /api/products/count-orders?maxAmount={amount}` - Підрахувати замовлення (скалярна функція)

### Orders

- `GET /api/orders` - Отримати всі замовлення
- `GET /api/orders/{id}` - Отримати замовлення за ID
- `POST /api/orders` - Створити замовлення (демонстрація обробки виключень з тригерів)

### Categories

- `GET /api/categories` - Отримати всі категорії
- `GET /api/categories/{id}` - Отримати категорію за ID

### Users

- `GET /api/users` - Отримати всіх користувачів
- `GET /api/users/{id}` - Отримати користувача за ID

## Реалізовані вимоги

✅ 1. Виведення інформації з пов'язаних таблиць (GET /api/products/{id})
✅ 2. Виконання процедур з параметрами (POST /api/products/purchase)
✅ 3. Виконання скалярної та табличної функцій (GET /api/products/count-orders, GET /api/products/users-with-expensive-products)
✅ 4. Обробка виключень з БД (POST /api/orders - обробка RAISERROR з тригерів)

### Деталі обробки виключень (вимога №4)

При виникненні помилки з RAISERROR в тригері або процедурі:

- Текст помилки з RAISERROR доступний в полі `message` відповіді
- Користувацькі виключення (RAISERROR з severity 16) мають `errorNumber >= 50000`
- Відповідь містить додаткову інформацію: `errorNumber`, `severity`, `source`

**Приклад відповіді при помилці з тригера:**

```json
{
  "error": "Помилка створення замовлення",
  "message": "Замовлення створюються тільки в робочі дні.",
  "errorNumber": 50000,
  "severity": 16,
  "source": "Database Trigger",
  "details": "Це виключення було згенеровано тригером на стороні сервера БД через RAISERROR"
}
```
