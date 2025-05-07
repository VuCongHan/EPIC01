using Docnet.Core.Models;
using Docnet.Core;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Wordprocessing;
using EPIC01.Data;
using iText.Kernel.Pdf;
using iText.Kernel.Pdf.Canvas.Parser;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using Tesseract;
using SixLabors.ImageSharp;
using System.Text.RegularExpressions;
using SixLabors.ImageSharp.Processing;
using Break = DocumentFormat.OpenXml.Wordprocessing.Break;
using Table = DocumentFormat.OpenXml.Wordprocessing.Table;
using System.Diagnostics;
using System.IO.Compression;
using SharpCompress.Archives;

namespace EPIC01.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class DocumentsController : ControllerBase
    {
        private readonly ApplicationDbContext _context;
        private readonly IConfiguration _configuration;
        public DocumentsController(ApplicationDbContext context, IConfiguration configuration)
        {
            _context = context;
            _configuration = configuration;
        }

        // POST: api/documents/upload
        // Tải lên tài liệu
        [HttpPost("upload")]
        public async Task<IActionResult> UploadDocument(IFormFile file)
        {
            if (file == null || file.Length == 0)
            {
                return BadRequest(new { message = "Không có tệp tin nào được chọn để tải lên!" });
            }

            var allowedExtensions = new[] { ".pdf", ".docx", ".doc" };
            var fileExtension = Path.GetExtension(file.FileName).ToLower();
            if (!allowedExtensions.Contains(fileExtension))
            {
                return BadRequest(new { message = "Chỉ cho phép tải lên tệp tin PDF, DOC hoặc DOCX!" });
            }

            // B1: Thư mục gốc
            var rootFolder = Path.Combine(Directory.GetCurrentDirectory(), "Uploads", "VuCongHan_TTS_CTIN");
            if (!Directory.Exists(rootFolder))
            {
                Directory.CreateDirectory(rootFolder);
            }

            // B2: Tạo thư mục con theo tên file
            var originalFileName = Path.GetFileNameWithoutExtension(file.FileName);
            var subFolder = Path.Combine(rootFolder, originalFileName);
            if (!Directory.Exists(subFolder))
            {
                Directory.CreateDirectory(subFolder);
            }

            // B3: Kiểm tra trùng tên file trong thư mục con
            var fileName = originalFileName + fileExtension;
            var filePath = Path.Combine(subFolder, fileName);
            if (System.IO.File.Exists(filePath))
            {
                var timeStamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");
                fileName = $"{originalFileName}_{timeStamp}{fileExtension}";
                filePath = Path.Combine(subFolder, fileName);
            }

            try
            {
                using (var stream = new FileStream(filePath, FileMode.Create))
                {
                    await file.CopyToAsync(stream);
                }
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Có lỗi xảy ra khi lưu tệp tin.", error = ex.Message });
            }

            var document = new Data.Document
            {
                Name = fileName,
                Type = fileExtension,
                FilePath = filePath,
                UploadDate = DateTime.Now,
                Status = "pending"
            };

            _context.Documents.Add(document);
            await _context.SaveChangesAsync();

            return Ok(new { message = "Tệp tin đã được tải lên thành công!", documentId = document.Id });
        }


        // GET: api/documents
        // Lấy danh sách tài liệu
        [HttpGet]
        public async Task<IActionResult> GetDocuments([FromQuery] string? processed)
        {
            IQueryable<Data.Document> query = _context.Documents;
            if (!string.IsNullOrEmpty(processed))
            {
                query = query.Where(d => d.Status.ToLower() == processed.ToLower());
            }

            var documents = await query.ToListAsync();

            if (documents == null || documents.Count == 0)
            {
                return NotFound(new { message = "Không tìm thấy tài liệu nào!" });
            }
            return Ok(documents.Select(doc => new
            {
                doc.Id,
                doc.Name,
                doc.Type,
                doc.FilePath,
                doc.UploadDate,
                doc.Status
            }));
        }

        // GET: api/documents/{documentId}
        // Lấy thông tin chi tiết tài liệu
        [HttpGet("{documentId}")]
        public async Task<ActionResult<System.Reflection.Metadata.Document>> GetDocument(int documentId)
        {
            var document = await _context.Documents.FindAsync(documentId);

            if (document == null)
            {
                return NotFound(new { message = "Tài liệu không tồn tại." });
            }

            return Ok(new
            {
                document.Id,
                document.Name,
                document.Type,
                document.FilePath,
                document.UploadDate,
                document.Status
            });
        }

        // POST: api/documents/{documentId}/process
        // Xử lý tài liệu
        [HttpPost("{documentId}/process")]
        public async Task<IActionResult> ProcessDocument(int documentId)
         {
            var document = await _context.Documents.FindAsync(documentId);
            if (document == null)
            {
                return NotFound(new { message = "Không tìm thấy tài liệu!" });
            }

            string extractedText = "";

            try
            {
                extractedText = ExtractTextByFileType(document);

                // Sử dụng OpenAIClient để tìm yêu cầu kỹ thuật
                string apiKey = _configuration["OpenAI:ApiKey"]!;
                var openAIClient = new OpenAIClient(apiKey);

                string technicalRequirement = await openAIClient.GetTechnicalRequirements(extractedText);// Gọi hàm bên OpenAIClient
                                                                                                         // 
                var resultMarkdown = $"# Yêu cầu kỹ thuật được trích xuất\n\n{technicalRequirement}";

                var fileDirectory = Path.GetDirectoryName(document.FilePath);

                var resultPath = Path.Combine(fileDirectory!, $"{document.Id}.md");

                await System.IO.File.WriteAllTextAsync(resultPath, resultMarkdown);

                document.Status = "processed";
                await _context.SaveChangesAsync();

                return Ok(new
                {
                    status = "success",
                    message = "Xử lý tài liệu thành công!",
                    //extractedText = extractedText,
                    //technicalRequirement = technicalRequirement,
                });

            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Có lỗi xảy ra khi xử lý tài liệu.", error = ex.Message });
            }
        }

        // Trích xuất văn bản từ tài liệu theo định dạng
        private string ExtractTextByFileType(Data.Document document)
        {
            string path = document.FilePath;
            string type = document.Type.ToLower();

            if (type == ".pdf")
            {
                return IsScannedPdf(path) ? ExtractTextFromPdfUsingOcr(path) : ExtractTextFromTextPdf(path);
            }
            else if (type == ".docx")
            {
                return ExtractTextFromWord(path); // Nếu là DOCX, sử dụng phương thức ExtractTextFromWord
            }
            else if (type == ".doc")
            {
                string outputDir = Path.GetDirectoryName(path)!; // Lấy thư mục chứa file
                string docxPath = Path.Combine(outputDir, Path.GetFileNameWithoutExtension(path) + ".docx");

                // Nếu chuyển đổi thành công, tiếp tục trích xuất văn bản từ file DOCX
                if (ConvertDocToDocx(path, outputDir))
                {
                    return ExtractTextFromWord(docxPath); // Tiến hành trích xuất văn bản từ .docx
                }
                else
                {
                    throw new Exception("Không thể chuyển đổi .doc sang .docx");
                }
            }
            else
            {
                throw new NotSupportedException($"Định dạng {type} không được hỗ trợ.");
            }
        }

        // Kiểm tra xem PDF có phải là tài liệu scan hay không
        private bool IsScannedPdf(string filePath)
        {
            try
            {
                using var pdfDoc = new iText.Kernel.Pdf.PdfDocument(new PdfReader(filePath));

                // Giới hạn số trang kiểm tra để tăng hiệu suất (ví dụ: kiểm tra 5 trang đầu tiên nếu tài liệu lớn)
                int maxPagesToCheck = Math.Min(5, pdfDoc.GetNumberOfPages());

                for (int pageNumber = 1; pageNumber <= maxPagesToCheck; pageNumber++)
                {
                    var page = pdfDoc.GetPage(pageNumber);
                    var pageText = PdfTextExtractor.GetTextFromPage(page);

                    // Nếu có ký tự chữ, coi như không phải PDF scan
                    if (!string.IsNullOrWhiteSpace(pageText))
                    {
                        return false;
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Lỗi khi kiểm tra PDF: {ex.Message}");
                // Có thể throw lại nếu bạn muốn xử lý bên ngoài
            }

            // Nếu tất cả các trang kiểm tra không có text, coi là PDF scan
            return true;
        }

        // Trích xuất văn bản từ PDF sử dụng OCR Chỉ hoạt động trên Windows
        //private string ExtractTextFromPdfUsingOcr(string pdfPath)
        //{
        //    // Kiểm tra file PDF tồn tại
        //    if (!System.IO.File.Exists(pdfPath))
        //        throw new FileNotFoundException("Không tìm thấy tệp PDF: " + pdfPath);

        //    // Đường dẫn đến thư mục chứa dữ liệu ngôn ngữ Tesseract
        //    var tessDataPath = @"C:\Users\vuhan\Desktop\Nghien cuu thuc tap\EPIC01\EPIC01\Uploads\tessdata";

        //    if (!Directory.Exists(tessDataPath))
        //        throw new DirectoryNotFoundException("Không tìm thấy thư mục tessdata: " + tessDataPath);

        //    var resultText = new StringBuilder();

        //    using (var document = PdfiumViewer.PdfDocument.Load(pdfPath))
        //    using (var engine = new Tesseract.TesseractEngine(tessDataPath, "vie", Tesseract.EngineMode.Default))
        //    {
        //        for (int i = 0; i < document.PageCount; i++)
        //        {
        //            using (var image = document.Render(i, 300, 300, true))
        //            using (var stream = new MemoryStream())
        //            {
        //                image.Save(stream, System.Drawing.Imaging.ImageFormat.Png);
        //                stream.Position = 0;

        //                using (var pix = Tesseract.Pix.LoadFromMemory(stream.ToArray()))
        //                using (var page = engine.Process(pix))
        //                {
        //                    // Thêm phân trang trước mỗi trang trích xuất
        //                    resultText.AppendLine($"\n[PAGE {i + 1}]\n");
        //                    resultText.AppendLine(page.GetText());
        //                }
        //            }
        //        }
        //    }

        //    return resultText.ToString();
        //}

        // Trích xuất văn bản từ PDF sử dụng OCR Hỗ trợ Windows, Linux, macOS (đa nền tảng)và xoay ảnh để tìm văn bản tốt nhất
        private string ExtractTextFromPdfUsingOcr(string pdfPath)
        {
            if (!System.IO.File.Exists(pdfPath))
                throw new FileNotFoundException("Không tìm thấy tệp PDF: " + pdfPath);

            // Lấy thư mục gốc của ứng dụng đang chạy (relative path)
            var baseDirectory = AppContext.BaseDirectory;

            // Thư mục tessdata nằm trong thư mục "Uploads/tessdata" của dự án
            string tessDataPath = Path.Combine(baseDirectory, "Uploads", "tessdata");
            if (!Directory.Exists(tessDataPath))
                throw new DirectoryNotFoundException("Không tìm thấy thư mục tessdata: " + tessDataPath);

            // Đường dẫn tới từ điển tiếng Việt
            var dictionaryPath = Path.Combine(tessDataPath, "Viet74K.txt");
            if (!System.IO.File.Exists(dictionaryPath))
                throw new FileNotFoundException("Không tìm thấy từ điển tiếng Việt: " + dictionaryPath);

            var vietnameseWords = new HashSet<string>(
                System.IO.File.ReadLines(dictionaryPath)
                    .Select(w => w.Trim().ToLower())
                    .Where(w => !string.IsNullOrEmpty(w))
            );

            var resultText = new StringBuilder();
            var dimensions = new PageDimensions(1080, 1920);

            using var docReader = DocLib.Instance.GetDocReader(pdfPath, dimensions);
            using var engine = new TesseractEngine(tessDataPath, "vie", EngineMode.Default);

            int pageCount = docReader.GetPageCount();

            for (int i = 0; i < pageCount; i++)
            {
                using var pageReader = docReader.GetPageReader(i);
                byte[] rawImage = pageReader.GetImage();
                int width = pageReader.GetPageWidth();
                int height = pageReader.GetPageHeight();

                using var originalImage = Image.LoadPixelData<SixLabors.ImageSharp.PixelFormats.Rgba32>(rawImage, width, height);

                string bestText = "";
                int bestScore = -1;

                foreach (var angle in new[] { 0, 90, 180, 270 })
                {
                    using var rotatedImage = originalImage.Clone(Configuration.Default, ctx => ctx.Rotate(angle));
                    using var ms = new MemoryStream();
                    rotatedImage.SaveAsPng(ms);
                    ms.Position = 0;

                    using var pix = Pix.LoadFromMemory(ms.ToArray());
                    using var page = engine.Process(pix);
                    var ocrText = page.GetText();
                    var score = ScoreMeaning(ocrText, vietnameseWords);

                    if (score > bestScore)
                    {
                        bestScore = score;
                        bestText = ocrText;
                    }
                }

                resultText.AppendLine($"\n[PAGE {i + 1}]\n");
                resultText.AppendLine(bestText);
            }

            return resultText.ToString();
        }


        // Hàm đánh giá điểm “có nghĩa” dựa vào từ điển
        private int ScoreMeaning(string text, HashSet<string> vietnameseWords)
        {
            var matches = Regex.Matches(text.ToLower(), @"\b[\p{L}]+\b");
            return matches.Count(m => vietnameseWords.Contains(m.Value));
        }

        // Trích xuất văn bản từ PDF không phải scan
        private string ExtractTextFromTextPdf(string pdfPath)
        {
            StringBuilder extractedText = new StringBuilder();

            using (PdfReader reader = new PdfReader(pdfPath))
            using (iText.Kernel.Pdf.PdfDocument pdfDoc = new iText.Kernel.Pdf.PdfDocument(reader))
            {
                for (int pageNumber = 1; pageNumber <= pdfDoc.GetNumberOfPages(); pageNumber++)
                {
                    var strategy = new iText.Kernel.Pdf.Canvas.Parser.Listener.LocationTextExtractionStrategy();
                    string pageText = PdfTextExtractor.GetTextFromPage(pdfDoc.GetPage(pageNumber), strategy);

                    // Thêm phân cách rõ ràng và thống nhất hơn
                    extractedText.AppendLine($"\n[PAGE {pageNumber}]\n");
                    extractedText.AppendLine(pageText.Trim());
                }
            }

            return extractedText.ToString();
        }

        private bool ConvertDocToDocx(string inputPath, string outputDirectory)
        {
            try
            {
                string sofficePath = @"C:\Program Files\LibreOffice\program\soffice.exe";

                // Kiểm tra xem soffice.exe có tồn tại không
                if (!System.IO.File.Exists(sofficePath))
                {
                    Console.WriteLine("Không tìm thấy soffice.exe tại " + sofficePath);
                    return false;
                }

                // Kiểm tra xem thư mục output có tồn tại không
                if (!Directory.Exists(outputDirectory))
                {
                    Directory.CreateDirectory(outputDirectory);
                }

                var processInfo = new ProcessStartInfo
                {
                    FileName = sofficePath,
                    Arguments = $"--headless --convert-to docx --outdir \"{outputDirectory}\" \"{inputPath}\"",
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    UseShellExecute = false,
                    CreateNoWindow = true
                };

                using var process = Process.Start(processInfo);
                process!.WaitForExit();

                if (process.ExitCode == 0)
                    return true;
                else
                {
                    string error = process.StandardError.ReadToEnd();
                    Console.WriteLine("LibreOffice error: " + error);
                    return false;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("Exception: " + ex.Message);
                return false;
            }
        }

        private string ExtractTextFromWord(string filePath)
        {
            StringBuilder resultText = new StringBuilder();
            int pageNumber = 1;

            using (WordprocessingDocument wordDocument = WordprocessingDocument.Open(filePath, false))
            {
                Body body = wordDocument.MainDocumentPart!.Document.Body!;

                foreach (var element in body.Elements())
                {
                    // Xử lý đoạn văn
                    if (element is Paragraph para)
                    {
                        // Kiểm tra ngắt trang
                        if (para.Elements<Run>().Any(run => run.Elements<Break>().Any(br => br.Type! == BreakValues.Page)))
                        {
                            resultText.AppendLine($"\n[PAGE {pageNumber++}]");
                        }

                        foreach (var run in para.Elements<Run>())
                        {
                            foreach (var text in run.Elements<Text>())
                            {
                                resultText.Append(text.Text);
                            }
                        }

                        resultText.AppendLine(); // newline sau mỗi đoạn
                    }

                    // Xử lý bảng
                    else if (element is Table table)
                    {
                        foreach (var row in table.Elements<TableRow>())
                        {
                            foreach (var cell in row.Elements<TableCell>())
                            {
                                foreach (var cellPara in cell.Elements<Paragraph>())
                                {
                                    foreach (var run in cellPara.Elements<Run>())
                                    {
                                        foreach (var text in run.Elements<Text>())
                                        {
                                            resultText.Append(text.Text);
                                        }
                                    }
                                }

                                resultText.Append(" | "); // Ngăn cách giữa các ô
                            }

                            resultText.AppendLine(); // newline sau mỗi hàng
                        }

                        resultText.AppendLine(); // newline sau toàn bộ bảng
                    }
                }
            }

            return resultText.ToString();
        }

        // GET: api/documents/{document_id}/result
        // Lấy file kết quả đã xử lý
        [HttpGet("{document_id}/result")]
        public IActionResult GetResultFile(int document_id)
        {
            // Tìm tài liệu trong cơ sở dữ liệu
            var document = _context.Documents.FirstOrDefault(d => d.Id == document_id);
            if (document == null)
            {
                return NotFound(new { message = "Tài liệu không tồn tại." });
            }

            // Lấy đường dẫn file gốc
            var originalFilePath = document.FilePath;

            // Thư mục chứa file gốc
            var subFolder = Path.GetDirectoryName(originalFilePath);
            if (string.IsNullOrEmpty(subFolder))
            {
                return NotFound(new { message = "Không xác định được thư mục chứa tài liệu." });
            }

            // Tạo tên và đường dẫn file .md
            var markdownFileName = $"{document_id}.md";
            var markdownFilePath = Path.Combine(subFolder, markdownFileName);

            // Kiểm tra file có tồn tại không
            if (!System.IO.File.Exists(markdownFilePath))
            {
                return NotFound(new { message = "Không tìm thấy kết quả đã xử lý cho tài liệu này." });
            }

            // Đọc file và trả về
            var fileBytes = System.IO.File.ReadAllBytes(markdownFilePath);
            var downloadName = $"{document_id}_result.md";

            return File(fileBytes, "text/markdown", downloadName);
        }

        // POST: api/documents/{document_id}/standardise
        // Chuẩn hóa tài liệu
        [HttpPost("{document_id}/standardise")]
        public async Task<IActionResult> Standardise(string document_id)
        {
            if (string.IsNullOrEmpty(document_id))
                return BadRequest(new { message = "Document ID không hợp lệ." });

            // Tìm tài liệu trong database
            var document = await _context.Documents.FindAsync(int.Parse(document_id));
            if (document == null)
                return NotFound(new { message = "Không tìm thấy tài liệu trong hệ thống." });

            // Lấy đường dẫn file Markdown từ thư mục gốc tài liệu
            var fileDirectory = Path.GetDirectoryName(document.FilePath);
            var markdownPath = Path.Combine(fileDirectory!, $"{document_id}.md");

            if (!System.IO.File.Exists(markdownPath))
                return NotFound(new { message = "Không tìm thấy file Markdown để chuẩn hóa." });

            var markdownContent = await System.IO.File.ReadAllTextAsync(markdownPath);

            // Chuyển Markdown sang JSON
            var jsonData = StandardisationHelper.ConvertMarkdownToJson(markdownContent);

            // Đường dẫn lưu JSON trong cùng thư mục
            var jsonPath = Path.Combine(fileDirectory!, $"{document_id}.json");

            await System.IO.File.WriteAllTextAsync(jsonPath, jsonData);

            return Ok(new
            {
                message = "Chuẩn hóa yêu cầu kỹ thuật thành công.",
                jsonPath = jsonPath
            });
        }


        // GET: api/documents/{document_id}/standardised
        // Lấy kết quả đã chuẩn hóa
        [HttpGet("{document_id}/standardised")]
        public IActionResult GetStandardisedResult(int document_id)
        {
            // Tìm tài liệu trong cơ sở dữ liệu
            var document = _context.Documents.FirstOrDefault(d => d.Id == document_id);
            if (document == null)
            {
                return NotFound(new { message = "Tài liệu không tồn tại." });
            }

            // Lấy đường dẫn file gốc
            var originalFilePath = document.FilePath;

            // Thư mục chứa file gốc
            var subFolder = Path.GetDirectoryName(originalFilePath);
            if (string.IsNullOrEmpty(subFolder))
            {
                return NotFound(new { message = "Không xác định được thư mục chứa tài liệu." });
            }

            // Tạo đường dẫn đến file JSON chuẩn hóa
            var jsonFileName = $"{document_id}.json";
            var jsonFilePath = Path.Combine(subFolder, jsonFileName);

            // Kiểm tra file có tồn tại không
            if (!System.IO.File.Exists(jsonFilePath))
            {
                return NotFound(new { message = "Không tìm thấy kết quả chuẩn hóa cho tài liệu này." });
            }

            // Đọc và trả dữ liệu
            var jsonData = System.IO.File.ReadAllText(jsonFilePath);
            var deserialized = JsonSerializer.Deserialize<object>(jsonData);

            return Ok(new
            {
                message = "Kết quả chuẩn hóa thành công.",
                data = deserialized
            });
        }

        // DELETE: api/documents/{document_id}
        // Xóa tài liệu
        [HttpDelete("{document_id}")]
        public IActionResult DeleteDocument(int document_id)
        {
            // Tìm tài liệu trong cơ sở dữ liệu
            var document = _context.Documents.FirstOrDefault(d => d.Id == document_id);
            if (document == null)
            {
                return NotFound(new { message = "Tài liệu không tồn tại." });
            }

            // Lấy đường dẫn file gốc từ cơ sở dữ liệu
            var originalFilePath = document.FilePath;

            // Thư mục chứa file gốc
            var subFolder = Path.GetDirectoryName(originalFilePath);

            // Tạo tên mới cho file .md và .json sử dụng document_id
            var markdownFileName = $"{document_id}.md";  // Chỉ lấy document_id cho file .md
            var jsonFileName = $"{document_id}.json";    // Chỉ lấy document_id cho file .json

            // Đường dẫn file .md và .json
            var markdownFilePath = Path.Combine(subFolder!, markdownFileName);
            var jsonFilePath = Path.Combine(subFolder!, jsonFileName);

            // Tạo danh sách các file cần xóa
            var filesToDelete = new List<string> { originalFilePath, markdownFilePath, jsonFilePath };

            // Thực hiện xóa các file nếu tồn tại
            foreach (var file in filesToDelete)
            {
                try
                {
                    if (System.IO.File.Exists(file))
                    {
                        Console.WriteLine($"Đang xóa tệp: {file}");
                        System.IO.File.Delete(file);
                    }
                    else
                    {
                        Console.WriteLine($"Tệp không tồn tại: {file}");
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Lỗi khi xóa tệp: {file}. Lỗi: {ex.Message}");
                    return StatusCode(500, new { message = $"Không thể xóa file: {file}", error = ex.Message });
                }
            }

            // Kiểm tra nếu thư mục con (subFolder) trống và xóa nếu trống
            try
            {
                if (Directory.Exists(subFolder) && Directory.GetFiles(subFolder).Length == 0)
                {
                    Console.WriteLine($"Thư mục trống, xóa thư mục: {subFolder}");
                    Directory.Delete(subFolder);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Lỗi khi xóa thư mục: {subFolder}. Lỗi: {ex.Message}");
                return StatusCode(500, new { message = $"Không thể xóa thư mục: {subFolder}", error = ex.Message });
            }

            // Xóa tài liệu khỏi cơ sở dữ liệu
            _context.Documents.Remove(document);
            try
            {
                _context.SaveChanges();
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Lỗi khi lưu thay đổi vào cơ sở dữ liệu.", error = ex.Message });
            }

            return Ok(new { message = "Tài liệu đã được xóa thành công." });
        }


        // Tự động trích xuất và chuẩn hóa file upload
        // POST: api/documents/upload-and-process
        [HttpPost("upload-and-process")]
        public async Task<IActionResult> UploadAndProcessAndStandardizeDocument(IFormFile file)
        {
            if (file == null || file.Length == 0)
            {
                return BadRequest(new { message = "Không có tệp tin nào được chọn để tải lên!" });
            }

            var allowedExtensions = new[] { ".pdf", ".docx", ".doc" };
            var fileExtension = Path.GetExtension(file.FileName).ToLower();
            if (!allowedExtensions.Contains(fileExtension))
            {
                return BadRequest(new { message = "Chỉ cho phép tải lên tệp tin PDF, DOC hoặc DOCX!" });
            }

            // B1: Thư mục gốc
            var rootFolder = Path.Combine(Directory.GetCurrentDirectory(), "Uploads", "VuCongHan_TTS_CTIN");
            if (!Directory.Exists(rootFolder))
            {
                Directory.CreateDirectory(rootFolder);
            }

            // B2: Tạo thư mục con theo tên file
            var originalFileName = Path.GetFileNameWithoutExtension(file.FileName);
            var subFolder = Path.Combine(rootFolder, originalFileName);
            if (!Directory.Exists(subFolder))
            {
                Directory.CreateDirectory(subFolder);
            }

            // B3: Kiểm tra trùng tên file trong thư mục con
            var fileName = originalFileName + fileExtension;
            var filePath = Path.Combine(subFolder, fileName);
            if (System.IO.File.Exists(filePath))
            {
                var timeStamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");
                fileName = $"{originalFileName}_{timeStamp}{fileExtension}";
                filePath = Path.Combine(subFolder, fileName);
            }

            try
            {
                using (var stream = new FileStream(filePath, FileMode.Create))
                {
                    await file.CopyToAsync(stream);
                }
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Có lỗi xảy ra khi lưu tệp tin.", error = ex.Message });
            }

            var document = new Data.Document
            {
                Name = fileName,
                Type = fileExtension,
                FilePath = filePath,
                UploadDate = DateTime.Now,
                Status = "pending"
            };

            _context.Documents.Add(document);
            await _context.SaveChangesAsync();

            // Xử lý tài liệu: Trích xuất yêu cầu kỹ thuật từ file
            string extractedText = "";
            try
            {
                extractedText = ExtractTextByFileType(document);

                // Sử dụng OpenAIClient để tìm yêu cầu kỹ thuật
                string apiKey = _configuration["OpenAI:ApiKey"]!;
                var openAIClient = new OpenAIClient(apiKey);
                string technicalRequirement = await openAIClient.GetTechnicalRequirements(extractedText);

                var resultMarkdown = $"# Yêu cầu kỹ thuật được trích xuất\n\n{technicalRequirement}";
                var fileDirectory = Path.GetDirectoryName(document.FilePath);
                var resultPath = Path.Combine(fileDirectory!, $"{document.Id}.md");

                await System.IO.File.WriteAllTextAsync(resultPath, resultMarkdown);

                // Chuẩn hóa tài liệu
                var markdownContent = await System.IO.File.ReadAllTextAsync(resultPath);
                var jsonData = StandardisationHelper.ConvertMarkdownToJson(markdownContent);

                var jsonPath = Path.Combine(fileDirectory!, $"{document.Id}.json");
                await System.IO.File.WriteAllTextAsync(jsonPath, jsonData);

                // Cập nhật trạng thái tài liệu
                document.Status = "processed";
                await _context.SaveChangesAsync();

                return Ok(new
                {
                    status = "success",
                    message = "Tài liệu đã được tải lên, trích xuất và chuẩn hóa thành công!",
                    documentId = document.Id,
                    markdownPath = resultPath,
                    jsonPath = jsonPath
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Có lỗi xảy ra khi xử lý tài liệu.", error = ex.Message });
            }
        }

        // Upload File Zip hoặc Rar để tự động lấy file ra trích xuất và chuẩn hóa
        [HttpPost("upload-zip")]
        public async Task<IActionResult> UploadZipAndProcess(IFormFile zipFile)
        {
            if (zipFile == null || zipFile.Length == 0)
                return BadRequest(new { message = "Không có tệp nén nào được gửi lên!" });

            var zipExtension = Path.GetExtension(zipFile.FileName).ToLower();
            if (zipExtension != ".zip" && zipExtension != ".rar")
                return BadRequest(new { message = "Chỉ cho phép tải lên tệp .zip hoặc .rar!" });

            // B1: Lưu file nén vào thư mục tạm
            var rootFolder = Path.Combine(Directory.GetCurrentDirectory(), "Uploads", "VuCongHan_TTS_CTIN");
            var tempFolder = Path.Combine(rootFolder, "Temp");
            if (!Directory.Exists(tempFolder)) Directory.CreateDirectory(tempFolder);

            var zipFilePath = Path.Combine(tempFolder, zipFile.FileName);
            using (var stream = new FileStream(zipFilePath, FileMode.Create))
            {
                await zipFile.CopyToAsync(stream);
            }

            // B2: Tạo thư mục để giải nén
            var extractFolder = Path.Combine(tempFolder, Path.GetFileNameWithoutExtension(zipFile.FileName));
            if (Directory.Exists(extractFolder)) Directory.Delete(extractFolder, true);
            Directory.CreateDirectory(extractFolder);

            // B3: Giải nén tùy theo định dạng
            try
            {
                if (zipExtension == ".zip")
                {
                    ZipFile.ExtractToDirectory(zipFilePath, extractFolder);
                }
                else if (zipExtension == ".rar")
                {
                    using var archive = SharpCompress.Archives.Rar.RarArchive.Open(zipFilePath);
                    foreach (var entry in archive.Entries.Where(entry => !entry.IsDirectory))
                    {
                        entry.WriteToDirectory(extractFolder, new SharpCompress.Common.ExtractionOptions()
                        {
                            ExtractFullPath = true,
                            Overwrite = true
                        });
                    }
                }
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Không thể giải nén tệp.", error = ex.Message });
            }

            // B4: Lấy các file hợp lệ
            var allowedExtensions = new[] { ".pdf", ".docx", ".doc" };
            var extractedFiles = Directory.GetFiles(extractFolder, "*.*", SearchOption.AllDirectories)
                .Where(f => allowedExtensions.Contains(Path.GetExtension(f).ToLower()))
                .ToList();

            if (!extractedFiles.Any())
                return BadRequest(new { message = "Không tìm thấy file hợp lệ trong tệp nén!" });

            var results = new List<object>();

            foreach (var filePath in extractedFiles)
            {
                try
                {
                    var originalFileName = Path.GetFileNameWithoutExtension(filePath);
                    var extension = Path.GetExtension(filePath).ToLower();

                    // Tạo thư mục riêng cho file
                    var targetFolder = Path.Combine(rootFolder, originalFileName);
                    if (!Directory.Exists(targetFolder)) Directory.CreateDirectory(targetFolder);

                    // Đặt tên và sao chép file gốc vào thư mục
                    var fileName = originalFileName + extension;
                    var destFilePath = Path.Combine(targetFolder, fileName);
                    System.IO.File.Copy(filePath, destFilePath, true);

                    // Tạo đối tượng document
                    var document = new Data.Document
                    {
                        Name = fileName,
                        Type = extension,
                        FilePath = destFilePath,
                        UploadDate = DateTime.Now,
                        Status = "pending"
                    };
                    _context.Documents.Add(document);
                    await _context.SaveChangesAsync();

                    // B4: Trích xuất và chuẩn hóa
                    var extractedText = ExtractTextByFileType(document);
                    string apiKey = _configuration["OpenAI:ApiKey"]!;
                    var openAIClient = new OpenAIClient(apiKey);
                    var technicalRequirement = await openAIClient.GetTechnicalRequirements(extractedText);

                    var resultMarkdown = $"# Yêu cầu kỹ thuật được trích xuất\n\n{technicalRequirement}";
                    var markdownPath = Path.Combine(targetFolder, $"{document.Id}.md");
                    await System.IO.File.WriteAllTextAsync(markdownPath, resultMarkdown);

                    var jsonData = StandardisationHelper.ConvertMarkdownToJson(resultMarkdown);
                    var jsonPath = Path.Combine(targetFolder, $"{document.Id}.json");
                    await System.IO.File.WriteAllTextAsync(jsonPath, jsonData);

                    document.Status = "processed";
                    await _context.SaveChangesAsync();

                    results.Add(new
                    {
                        documentId = document.Id,
                        file = fileName,
                        markdownPath,
                        jsonPath
                    });
                }
                catch (Exception ex)
                {
                    results.Add(new { file = filePath, error = ex.Message });
                }
            }

            return Ok(new
            {
                message = "Xử lý file ZIP hoàn tất!",
                results
            });
        }
    }
}
