# Comic Management

Nền tảng đọc truyện tranh — full-stack: backend microservices .NET (REST +
gRPC nội bộ, YARP API Gateway) và frontend React/Vite. Repo này là monorepo,
gộp 2 phần vào cùng một chỗ để xem toàn bộ hệ thống trong một lần clone.

```
Comic_Management/
├── backend/     <- .NET microservices (ApiGateway + 8 API), SQL Server/Redis/RabbitMQ/n8n
└── frontend/    <- React + Vite SPA
```

## Kiến trúc tổng quan

- **Frontend** (`frontend/`) — React SPA gọi toàn bộ API qua một cổng duy nhất:
  **API Gateway** (YARP).
- **API Gateway** (`backend/ApiGateway`) — định tuyến request theo path tới
  từng microservice phía sau.
- **8 microservice** (`UserAPI`, `ComicAPI`, `ChapterAPI`, `SocialAPI`,
  `MissionAPI`, `PaymentAPI`, `WalletAPI`, `BannerAPI`) — mỗi service có
  database SQL Server riêng (mô hình database-per-service), gọi nhau qua
  gRPC nội bộ khi cần.
- **Hạ tầng dùng chung**: Redis, RabbitMQ, n8n + LibreTranslate (dịch tên
  truyện/mô tả/chapter — không đụng tới ảnh).

## Bắt đầu nhanh

1. Đọc [`backend/README.md`](backend/README.md) — cấu hình `.env`, chạy
   Docker Compose (SQL Server, Redis, RabbitMQ, LibreTranslate, n8n), chạy
   từng service, migrate database.
2. Đọc [`frontend/README.md`](frontend/README.md) — cấu hình, chạy dev
   server. Cách nhanh nhất: từ thư mục `frontend/` chạy `npm run dev` — lệnh
   này tự khởi động toàn bộ backend rồi mới chạy Vite.

## Tech stack

| Phần | Công nghệ |
|---|---|
| Backend | .NET 9, ASP.NET Core, EF Core, gRPC, YARP, AutoMapper |
| Frontend | React, React Router, Vite |
| Data/Infra | SQL Server, Redis, RabbitMQ, Docker Compose |
| Tích hợp | Cloudinary (ảnh), n8n + LibreTranslate (dịch văn bản), SePay/VietQR (thanh toán), Gmail SMTP, Google OAuth |
