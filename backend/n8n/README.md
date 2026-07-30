# Dịch phần chữ của truyện bằng n8n

Workflow `workflows/comic-text-translation.json` chỉ nhận hai trường:

- `targetLanguage`: mã ngôn ngữ đích.
- `texts`: danh sách `{ key, value }` chứa tên truyện, mô tả và tên chapter.

Workflow chủ động từ chối mọi trường khác. `imageUrl`, file ảnh, base64 và nội dung
trang ảnh không được gửi đến n8n, vì vậy ảnh chapter luôn được giữ nguyên.

## Cấu hình

1. Mở terminal thứ nhất và chạy LibreTranslate tại `http://127.0.0.1:5000`:

   ```cmd
   cd /d D:\Tools\libretranslate
   .venv\Scripts\activate.bat
   libretranslate --load-only vi,en
   ```

2. Mở terminal thứ hai rồi chạy n8n:

   ```cmd
   n8n start
   ```

3. Mở `http://localhost:5678`, sau đó import file
   `n8n/workflows/comic-text-translation.json`.
4. Trong node **Translation Webhook**, giữ `Authentication = None`, sau đó
   **Publish** workflow. Cách này dành cho môi trường local; không công khai cổng
   `5678` ra Internet.
5. Cấu hình ComicAPI (không cần `N8nTranslation__Secret`):

   ```env
   N8nTranslation__WebhookUrl=http://localhost:5678/webhook/comic-text-translate
   N8nTranslation__TimeoutSeconds=65
   ```

Nếu n8n không chạy trong Docker cùng LibreTranslate, hãy đổi URL trong node
**Translate with LibreTranslate** thành URL LibreTranslate mà n8n truy cập được.

## Luồng xử lý

```text
Frontend chọn ngôn ngữ
        ↓
POST /translations (chỉ gửi dữ liệu chữ)
        ↓
ComicAPI kiểm tra ngôn ngữ, số lượng và độ dài văn bản
        ↓
n8n Webhook local từ chối trường dữ liệu ngoài targetLanguage/texts
        ↓
LibreTranslate tự nhận diện ngôn ngữ nguồn và dịch mảng văn bản
        ↓
n8n ghép kết quả lại theo key
        ↓
ComicAPI trả bản dịch cho frontend
        ↓
Frontend thay tên/mô tả hiển thị; các thẻ <img> tiếp tục dùng imageUrl gốc
```
