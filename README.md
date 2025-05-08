# 📄 EPIC01 – Document Processing API

Một API RESTful được xây dựng bằng ASP.NET Core (.NET 8), hỗ trợ **tải lên, xử lý, trích xuất và chuẩn hóa các tài liệu** định dạng `.pdf`, `.doc`, `.docx`. Hệ thống hướng đến ứng dụng trong các trợ lý AI và hệ thống phân tích dữ liệu kỹ thuật.

## 🚀 Tính năng nổi bật

- ✅ Tải lên tài liệu (.pdf, .doc, .docx)
- ✅ Tự động xử lý và trích xuất yêu cầu kỹ thuật
- ✅ Xuất kết quả dưới dạng Markdown (`.md`)
- ✅ Chuẩn hóa và xuất dưới dạng JSON (`.json`)
- ✅ Xóa tài liệu cùng các kết quả liên quan
- ✅ Dễ dàng tích hợp vào hệ thống AI hoặc frontend

## 🛠️ Công nghệ sử dụng

- ASP.NET Core Web API (.NET 8)
- Entity Framework Core
- SQL Server hoặc SQLite
- LibreOffice (dùng để chuyển đổi định dạng)
- Hỗ trợ tích hợp OpenAI (qua API Key)

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
    "ApiKey": "luumytran + Vũ Công Hân"
  },
  "AllowedHosts": "*",
  "ConnectionStrings": {
    "DefaultConnection": "Server=VUHAN14-01-2001;Database=EPIC1(CTIN);User Id=sa;Password=vuhan1401;Trusted_Connection=True;TrustServerCertificate=True;MultipleActiveResultSets=true;"
  }
}
```

> 🔐 **Lưu ý:** Không nên commit `ApiKey` và chuỗi kết nối thật. Dùng User Secrets hoặc biến môi trường để bảo mật.

## 📦 Cài đặt & chạy

### Yêu cầu

- [.NET 8 SDK](https://dotnet.microsoft.com/download)
- [LibreOffice](https://www.libreoffice.org/)
- Visual Studio 2022+ hoặc VS Code

### Clone và chạy

```bash
git clone https://github.com/VuCongHan/EPIC01.git
cd EPIC01
dotnet run
```

## 📂 Cấu trúc thư mục sau xử lý

Khi xử lý thành công, các file kết quả được lưu cùng cấp với file gốc:

```
/Documents
├── 123.pdf / .docx              // File gốc
├── 123.md                       // Kết quả Markdown
└── 123.json                     // Dữ liệu chuẩn hóa
```

## 🔌 Các API chính

| Phương thức | Endpoint                                      | Mô tả |
|------------|-----------------------------------------------|-------|
| `POST`     | `/api/documents`                              | Tải lên tài liệu |
| `POST`     | `/api/documents/{document_id}/process`        | Xử lý và trích xuất dữ liệu |
| `GET`      | `/api/documents/{document_id}/result`         | Trả về file kết quả `.md` |
| `GET`      | `/api/documents/{document_id}/standardised`   | Trả về kết quả chuẩn hóa `.json` |
| `DELETE`   | `/api/documents/{document_id}`                | Xóa tài liệu và file liên quan |

## 📚 Danh sách đầy đủ các API

| Phương thức | Endpoint                                         | Mô tả |
|------------|--------------------------------------------------|-------|
| `POST`     | `/api/documents/upload`                          | Tải lên tài liệu |
| `GET`      | `/api/documents`                                 | Lấy danh sách tài liệu |
| `GET`      | `/api/documents/{documentId}`                    | Lấy thông tin chi tiết tài liệu |
| `POST`     | `/api/documents/{documentId}/process`            | Xử lý tài liệu |
| `GET`      | `/api/documents/{document_id}/result`            | Lấy file kết quả đã xử lý (Markdown) |
| `POST`     | `/api/documents/{document_id}/standardise`       | Chuẩn hóa tài liệu |
| `GET`      | `/api/documents/{document_id}/standardised`      | Lấy kết quả chuẩn hóa (JSON) |
| `DELETE`   | `/api/documents/{document_id}`                   | Xóa tài liệu và kết quả liên quan |
| `POST`     | `/api/documents/upload-and-process`              | Tự động trích xuất và chuẩn hóa khi tải lên |
| `POST`     | `/api/documents/upload-zip`                      | Tải lên file ZIP/RAR và xử lý hàng loạt |

## 🧪 Kiểm thử API

Mở trình duyệt tại:

Dùng Postman để gửi request đến các endpoint phía trên.

## 👤 Tác giả

- **Vũ Công Hân** – [github.com/VuCongHan](https://github.com/VuCongHan)
- **Lưu Mỹ Trân** – [github.com/TranLuu3001](https://github.com/TranLuu3001)

---

> 💡 *EPIC01* là một nền tảng mở dành cho xử lý tài liệu thông minh, có thể mở rộng cho chatbot, OCR hoặc trích xuất dữ liệu bán cấu trúc.
