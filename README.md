# OnlineShopSystem. Контрольная работа № 3 Асинхронное межсервисное взаимодействие.

Система микросервисов для обработки платежей и заказов с асинхронным взаимодействием через RabbitMQ.

## Архитектура системы

Система состоит из следующих компонентов:

- **API Gateway**  
  Единая точка входа в систему. Маршрутизирует запросы к соответствующим сервисам.

- **Orders Service**  
  Управление заказами:
  - Создание заказов
  - Просмотр списка заказов
  - Просмотр статуса заказа
  - Transactional Outbox для надежной доставки событий

- **Payments Service**  
  Управление платежами:
  - Создание счета
  - Пополнение баланса
  - Просмотр баланса
  - Transactional Inbox/Outbox для обработки платежей

- **RabbitMQ**  
  Брокер сообщений для асинхронного взаимодействия между сервисами

- **PostgreSQL**  
  Отдельные базы данных для каждого сервиса:
  - `ordersdb` — для хранения заказов
  - `paymentsdb` — для хранения платежей

![Снимок экрана 2025-06-16 231003](https://github.com/user-attachments/assets/c5d461fd-95d8-4c54-9c00-ac9dca8baa7c)

## API

- `POST /api/orders` - создание заказа
- `GET /api/orders/{Id}` - статус заказа
- `GET api/orders/user/{userId}` - список заказов

- `POST /api/payments/accounts` - создание счета
- `POST /api/payments/deposit` - пополнение счета
- `GET /api/payments/accounts/{userId}` - просмотр баланса

![Снимок экрана 2025-06-16 231431](https://github.com/user-attachments/assets/0dcee3bb-60f8-4c0b-9aa6-9218844f4a26)


## Основной сценарий

### Создание заказа
1. API Gateway получает запрос и перенаправляет в Orders Service
2. Orders Service создает заказ и записывает событие в outbox
3. Событие публикуется в RabbitMQ
4. Payments Service получает событие и проверяет возможность оплаты
5. Результат оплаты отправляется обратно через RabbitMQ
6. Orders Service обновляет статус заказа

## Запуск проекта

### Требования
- Docker Desktop
- .NET 7 SDK (для разработки)

### Запуск
```bash
docker-compose up --build
```
![Снимок экрана 2025-06-16 231110](https://github.com/user-attachments/assets/1dd49753-a392-4ae4-bc38-9d9845a9a92a)
![Снимок экрана 2025-06-16 230928](https://github.com/user-attachments/assets/3e403c13-7c3f-491c-b0f0-94c6394747e6)

## Сервисы будут доступны:

- API Gateway: http://localhost:5000
- Orders Service: http://localhost:5001
- Payments Service: http://localhost:5003
- RabbitMQ UI: http://localhost:15672 (guest/guest)


## Особенности реализации
- Полная изоляция данных между сервисами
- Гарантированная доставка сообщений через Outbox паттерн
- Идемпотентная обработка сообщений через Inbox паттерн
- Атомарное обновление баланса для предотвращения race conditions
- Масштабируемость через Docker Compose
- Мониторинг через health checks

## Тестирование 

![Снимок экрана 2025-06-16 233429](https://github.com/user-attachments/assets/a48f7352-7fa1-4462-accf-af71a5925336)
