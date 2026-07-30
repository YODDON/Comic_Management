# Comic Management - Nền tảng Đọc Truyện Tranh (Backend)

Backend microservices viết bằng .NET (REST API + gRPC nội bộ), điều phối qua một
API Gateway (YARP), dùng SQL Server (mỗi service một database riêng), Redis,
RabbitMQ, và một pipeline dịch văn bản dựa trên n8n + LibreTranslate.

Đây là thư mục `backend/` trong monorepo — frontend (React/Vite) nằm ở thư mục
`frontend/` cùng cấp, xem [`../frontend/README.md`](../frontend/README.md).
Xem tổng quan toàn hệ thống ở [README gốc](../README.md).

## 1. Cấu trúc Solution

| Project | Vai trò | HTTP (dev) | HTTPS (dev) |
|---|---|---|---|
| `ApiGateway` | YARP Gateway, điểm vào duy nhất cho frontend | 5028 | 7023 |
| `UserAPI` | Auth, user, JWT | 5054 | 7231 |
| `ComicAPI` | Truyện, category, dịch văn bản (n8n) | 5023 | 7024 |
| `ChapterAPI` | Chapter, trang ảnh (Cloudinary) | 5115 | 7114 |
| `SocialAPI` | Bình luận, follow, favorite, lịch sử đọc | 5197 | 7133 |
| `MissionAPI` | Nhiệm vụ, thông báo, upload | 5288 | 7224 |
| `PaymentAPI` | Nạp tiền, VietQR/SePay | 5128 | 7071 |
| `WalletAPI` | Ví, ledger giao dịch | 5091 | 7066 |
| `BannerAPI` | Banner trang chủ | 5127 | 7053 |
| `SharedKernel` | Thư viện dùng chung (`ApiResponse`, `PagedResult`, `BaseEntity`, ...) | - | - |

Các service gọi nhau qua gRPC nội bộ (ví dụ `ComicAPI → UserAPI`,
`ChapterAPI → ComicAPI`, `MissionAPI → ChapterAPI/SocialAPI/WalletAPI`,
`PaymentAPI → ChapterAPI/WalletAPI`). URL đích lấy theo thứ tự ưu tiên:
biến môi trường (`USER_API_URL`, `COMIC_API_URL`, `CHAPTER_API_URL`,
`MISSION_API_URL`, `SOCIAL_API_URL`, `WALLET_API_URL`) → cấu hình
`GrpcEndpoints`/`GrpcSettings` trong `appsettings.json` → giá trị mặc định
`localhost` kèm port ở bảng trên. Khi chạy tất cả service trên cùng máy, bạn
**không bắt buộc** phải set các biến này.

`ApiGateway` định tuyến theo path (`/auth`, `/comics`, `/chapters`,
`/comments`, `/favorites`, `/follows`, `/reading-history`, `/payments`,
`/wallets`, `/currency`, `/withdraws`, `/banners`, `/missions`,
`/notifications`, `/uploads`, `/translations`, ...) tới từng service tương
ứng, cấu hình cứng trong `ApiGateway/appsettings.json` (`ReverseProxy`).

## 2. Yêu cầu hệ thống

- [Docker Desktop](https://www.docker.com/products/docker-desktop)
- [.NET 9.0 SDK](https://dotnet.microsoft.com/download/dotnet/9.0)
- (Tuỳ chọn) SQL Server Management Studio (SSMS) hoặc Azure Data Studio để xem DB
- (Tuỳ chọn, chỉ nếu cần test tính năng dịch) [n8n CLI](https://docs.n8n.io/) và Python (LibreTranslate) — xem mục 6

## 3. Cấu hình biến môi trường

1. Copy `.env.example` thành `.env` ở thư mục gốc repo này, rồi điền giá trị
   thật (Connection Strings, JWT Secret, Cloudinary, SMTP, Google Client ID,
   Bank/VietQR/SePay...).
2. Với Docker Compose, các giá trị mặc định (khi không set trong `.env`) là:
   - SQL Server: user `sa`, password lấy từ `SQL_SA_PASSWORD` (mặc định
     `huy492004` nếu không set — **nên đặt password riêng khi không chỉ chạy
     local**).
   - RabbitMQ: user/pass lấy từ `RABBITMQ_USER`/`RABBITMQ_PASS` (mặc định
     `guest`/`guest`).
3. Mỗi service đọc connection string DB riêng của nó
   (`AUTH_DB_CONNECTION`, `COMIC_DB_CONNECTION`, `CHAPTER_DB_CONNECTION`,
   `BANNER_DB_CONNECTION`, `MISSION_DB_CONNECTION`, `SOCIAL_DB_CONNECTION`,
   `PAYMENT_DB_CONNECTION`, `WALLET_DB_CONNECTION`) — mô hình **database
   riêng theo từng service**, không dùng chung một DB.
4. `.env.example` không có sẵn dòng cho `MISSION_API_URL`/`SOCIAL_API_URL` —
   không sao, hai biến này chỉ cần set khi các service đó chạy ở máy/host
   khác; nếu chạy tất cả trên cùng máy thì bỏ qua (xem fallback ở mục 1).

## 4. Khởi động hạ tầng với Docker Compose

```bash
docker-compose up -d
```

Lệnh này tải và chạy các container:
- **SQL Server 2022** — port `1433`
- **Redis** — port `6379`
- **RabbitMQ** — port `5672` (AMQP) và `15672` (management UI)
- **LibreTranslate** — port `5000` (dịch máy, dùng bởi n8n)
- **n8n** — port `5678` (workflow orchestration cho tính năng dịch văn bản)

Kiểm tra trạng thái container: `docker ps`.

> `n8n` và `libretranslate` chỉ cần thiết cho tính năng "dịch tên/mô tả
> truyện". Nếu không cần tính năng này khi dev, bạn có thể bỏ qua bước cấu
> hình n8n ở mục 6 — các service khác vẫn chạy bình thường, chỉ endpoint
> dịch sẽ trả lỗi 503.

## 5. Build và chạy các service

### Cách 1: Visual Studio
1. Mở `prn232_comic_api.sln`.
2. Chọn **Multiple Startup Projects** để chạy cùng lúc `ApiGateway` và các
   `*API` cần thiết (khuyên dùng launch profile `https` vì gRPC nội bộ cần
   cổng HTTPS).
3. Nhấn `F5`/`Ctrl+F5`.

### Cách 2: .NET CLI (mở một terminal cho mỗi service)
```bash
cd UserAPI
dotnet run --launch-profile https

# terminal khác
cd ComicAPI
dotnet run --launch-profile https

# tương tự cho ChapterAPI, SocialAPI, MissionAPI, PaymentAPI, WalletAPI, BannerAPI, ApiGateway
```

### Cách 3: Chạy cùng frontend bằng một lệnh
Trong thư mục `frontend/` (cùng cấp `backend/` trong monorepo này) chạy:

```bash
npm run dev
```

Script `frontend/scripts/dev.mjs` tự khởi động toàn bộ 8 API + ApiGateway
(profile `https`, trỏ tới `../backend`) rồi mới chạy Vite; tắt terminal sẽ
dừng toàn bộ tiến trình con. Xem chi tiết ở [`../frontend/README.md`](../frontend/README.md).

### Kiểm tra / dọn cổng đang chiếm dụng
```powershell
powershell -ExecutionPolicy Bypass -File .\dev-ports.ps1 Status
powershell -ExecutionPolicy Bypass -File .\dev-ports.ps1 Stop
```
`Stop` chỉ dừng đúng các tiến trình thuộc solution này (so theo đường dẫn
executable/command line), không đụng tới process khác.

## 6. Áp dụng Database Migrations

Chạy cho từng service có `DbContext` (mỗi service migrate độc lập):

```bash
cd UserAPI
dotnet ef database update

cd ComicAPI
dotnet ef database update

cd ChapterAPI
dotnet ef database update

# tương tự cho SocialAPI, MissionAPI, PaymentAPI, WalletAPI, BannerAPI
```

## 7. Tính năng dịch văn bản (n8n + LibreTranslate)

`ComicAPI` gọi một webhook n8n để dịch tên truyện/mô tả/tên chapter (không
bao giờ gửi ảnh). Thiết lập chi tiết (tạo tài khoản owner n8n lần đầu, import
workflow `n8n/workflows/comic-text-translation.json`, publish, cấu hình biến
`N8nTranslation__WebhookUrl`) nằm ở [`n8n/README.md`](n8n/README.md) — đọc
file đó trước khi bật tính năng dịch.

`.env.example` có thêm dòng `N8nTranslation__Secret` — hiện `ComicAPI` (class
`N8nTranslationSettings`) **chưa đọc** biến này (chỉ có `WebhookUrl` và
`TimeoutSeconds`), nên để trống cũng không ảnh hưởng gì; coi như chỗ dự phòng
cho việc thêm xác thực webhook sau này.

Lưu ý: node gọi LibreTranslate trong workflow trỏ cứng tới
`http://127.0.0.1:5000`, chỉ đúng khi n8n và LibreTranslate cùng chạy trên
host (theo hướng dẫn CLI trong `n8n/README.md`). Nếu chạy n8n qua
`docker-compose up n8n libretranslate`, cần sửa URL đó thành tên service
Docker (`http://libretranslate:5000/translate`) vì `127.0.0.1` trong
container `n8n` không trỏ tới container `libretranslate`.
