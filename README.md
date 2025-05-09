# 📄 EPIC01 – Document Processing API

Một API RESTful được xây dựng bằng ASP.NET Core (.NET 8), hỗ trợ **tải lên, xử lý, trích xuất và chuẩn hóa các tài liệu** định dạng `.pdf`, `.doc`, `.docx`. Hệ thống hướng đến ứng dụng trong các trợ lý AI và hệ thống phân tích dữ liệu kỹ thuật.

---

## 🚀 Tính năng nổi bật

- ✅ Tải lên tài liệu (.pdf, .doc, .docx)
- ✅ Tự động xử lý và trích xuất yêu cầu kỹ thuật
- ✅ Xuất kết quả dưới dạng Markdown (`.md`)
- ✅ Chuẩn hóa và xuất dưới dạng JSON (`.json`)
- ✅ Xóa tài liệu cùng các kết quả liên quan
- ✅ Dễ dàng tích hợp vào hệ thống AI hoặc frontend

---

## 🛠️ Công nghệ sử dụng

- ASP.NET Core Web API (.NET 8)
- Entity Framework Core
- SQL Server hoặc SQLite
- LibreOffice (dùng để chuyển đổi định dạng)
- Tesseract OCR (trích xuất văn bản từ ảnh)
- Tích hợp OpenAI (qua API Key)

---

## ⚙️ Cấu hình ban đầu (`appsettings.json`)

Trước khi chạy, bạn cần cấu hình `appsettings.json`:

```json
{
  "Logging": {
    "LogLevel": {
      "Default": "Information",
      "Microsoft.AspNetCore": "Warning"
    }
  },
  "OpenAI": {
    "ApiKey": "YOUR_OPENAI_API_KEY"
  },
  "AllowedHosts": "*",
  "ConnectionStrings": {
    "DefaultConnection": "Server=.;Database=EPIC01;User Id=your_user;Password=your_password;"
  }
}
```

> 🔐 **Lưu ý:** Không nên commit `ApiKey` và chuỗi kết nối thật. Hãy sử dụng [User Secrets](https://learn.microsoft.com/en-us/aspnet/core/security/app-secrets) hoặc biến môi trường để bảo mật.

---

## 📦 Cài đặt & chạy

### Yêu cầu

- [.NET 8 SDK](https://dotnet.microsoft.com/download)
- [LibreOffice](https://www.libreoffice.org/)
- Visual Studio 2022+ hoặc VS Code
- **Tesseract OCR:** Đặt thư mục `tessdata` tại:  
  ```
  <project_root>/Uploads/tessdata
  ```

### Clone và chạy

```bash
git clone https://github.com/VuCongHan/EPIC01.git
cd EPIC01
dotnet run
```

---

## 🧠 Cơ chế xử lý tài liệu

- Với PDF dạng hình ảnh, hệ thống sử dụng **Tesseract OCR** để trích xuất văn bản tiếng Việt.
- Sau đó, hệ thống phân tích nội dung và trích xuất **các yêu cầu kỹ thuật** dựa trên từ khóa, mẫu câu và cấu trúc.
- Kết quả được xuất ra file `.md` để dễ đọc, đồng thời chuẩn hóa sang `.json` cho việc tích hợp hệ thống khác.

---

## 📂 Cấu trúc thư mục sau xử lý

Khi xử lý thành công, các file kết quả được lưu dưới dạng sau:

```
/VuCongHan_TTS_CTIN
├── 123.pdf / .docx / .doc        // File gốc
├── 123.md                        // Kết quả Markdown
└── 123.json                      // Dữ liệu chuẩn hóa
```

---

## 🔌 Danh sách API

| Phương thức | Endpoint                                         | Mô tả |
|------------|--------------------------------------------------|-------|
| `POST`     | `/api/documents/upload`                          | Tải lên tài liệu |
| `POST`     | `/api/documents/upload-and-process`              | Tải lên và xử lý tự động |
| `POST`     | `/api/documents/upload-zip`                      | Tải lên file ZIP/RAR và xử lý hàng loạt |
| `GET`      | `/api/documents`                                 | Lấy danh sách tài liệu |
| `GET`      | `/api/documents/{documentId}`                    | Lấy thông tin chi tiết tài liệu |
| `POST`     | `/api/documents/{documentId}/process`            | Xử lý tài liệu và trích xuất dữ liệu |
| `GET`      | `/api/documents/{documentId}/result`             | Trả về file kết quả `.md` |
| `POST`     | `/api/documents/{documentId}/standardise`        | Chuẩn hóa dữ liệu |
| `GET`      | `/api/documents/{documentId}/standardised`       | Trả về kết quả chuẩn hóa `.json` |
| `DELETE`   | `/api/documents/{documentId}`                    | Xóa tài liệu và các file liên quan |

---

## 🧪 Kiểm thử API

- Truy cập Swagger UI tại: `https://localhost:{port}/swagger`
- Hoặc dùng [Postman](https://www.postman.com/) để gửi request tùy chỉnh.

---

## 👥 Tác giả

- **Vũ Công Hân** – [github.com/VuCongHan](https://github.com/VuCongHan)
- **Lưu Mỹ Trân** – [github.com/TranLuu3001](https://github.com/TranLuu3001)

---

## 📌 Kế hoạch phát triển

- [ ] Hỗ trợ triển khai bằng Docker
- [ ] Kiểm thử tự động bằng xUnit hoặc MSTest
- [ ] Giao diện quản lý frontend (Blazor hoặc Vue.js)
- [ ] Phân tích dữ liệu nâng cao bằng AI/LLM

---

> 💡 *EPIC01* là một nền tảng mở dành cho xử lý tài liệu thông minh, có thể mở rộng cho chatbot, OCR hoặc trích xuất dữ liệu bán cấu trúc.
