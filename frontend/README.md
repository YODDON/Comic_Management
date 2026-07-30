# Comico Frontend

Giao diện web (React + Vite) cho nền tảng đọc truyện tranh Comico. Gọi API
qua **API Gateway** của backend (YARP, port `5028`).

Đây là thư mục `frontend/` trong monorepo — backend (.NET microservices) nằm
ở thư mục `backend/` cùng cấp, xem [`../backend/README.md`](../backend/README.md).
Xem tổng quan toàn hệ thống ở [README gốc](../README.md).

## 1. Yêu cầu hệ thống

- Node.js (khuyến nghị bản LTS mới nhất) và npm
- .NET 9 SDK + Docker Desktop (để chạy backend) — xem `../backend/README.md`.
- Script `scripts/dev.mjs` (chạy bởi `npm run dev`) tự khởi động backend bằng
  cách trỏ đường dẫn tương đối `../backend` — đúng sẵn với cấu trúc monorepo
  này, không cần chỉnh gì thêm.

## 2. Cài đặt

```bash
npm install
cp .env.example .env
```

`.env` chỉ có một biến:

```env
# Để trống khi dev (dùng Vite proxy). Khi build/deploy production, set thành
# URL public của API Gateway, ví dụ https://api.example.com
VITE_API_URL=
```

Khi dev, các request tới `/auth`, `/comics`, `/categories`, `/translations`,
`/chapters`, `/comments`, `/favorites`, `/follows`, `/reading-history`,
`/payments`, `/withdraws`, `/missions`, `/notifications` được Vite proxy
sang `http://127.0.0.1:5028` (ApiGateway) — cấu hình trong `vite.config.js`.

## 3. Scripts

| Lệnh | Mô tả |
|---|---|
| `npm run dev` (= `npm run dev:full`) | Tự khởi động toàn bộ 8 API + ApiGateway của backend (profile `https`, chờ từng service sẵn sàng qua `/swagger/index.html`), sau đó chạy Vite. `Ctrl+C` sẽ dừng toàn bộ tiến trình con (kể cả process con của `dotnet run` trên Windows), tránh để sót cổng bị chiếm. |
| `npm run dev:ui` | Chỉ chạy Vite (port `5173`) — dùng khi bạn đã tự chạy backend bằng Visual Studio hoặc cách khác. |
| `npm run build` | Build production vào `dist/`. |
| `npm run preview` | Preview bản build production. |
| `npm run lint` | Chạy ESLint (`eslint.config.js`: React Hooks + React Refresh rules). |

Kiểm tra/dọn cổng của các service backend: chạy `../backend/dev-ports.ps1` —
xem [`../backend/README.md`](../backend/README.md), mục "Kiểm tra / dọn cổng".

## 4. Tech stack

- React + React Router DOM
- Vite (dev server port `5173`, có proxy API — xem mục 2)
- ESLint (`eslint-plugin-react-hooks`, `eslint-plugin-react-refresh`)

## 5. Cấu trúc thư mục chính

- `src/services/*.js` — các module gọi API (mỗi file tương ứng một domain:
  `authService`, `catalogService`, `bannerService`, `missionService`,
  `walletService`, `translationService`, ...), dùng chung biến
  `VITE_API_URL` làm base URL.
- `scripts/dev.mjs` — orchestrator cho `npm run dev`/`dev:full`.
- `vite.config.js` — cấu hình dev server + proxy sang ApiGateway.
