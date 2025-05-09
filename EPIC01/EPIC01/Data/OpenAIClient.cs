using Newtonsoft.Json;
using System.Text;

namespace EPIC01.Data
{
    public class OpenAIClient
    {
        private readonly HttpClient _client;
        private readonly string _apiKey;

        public OpenAIClient(string apiKey)
        {
            _client = new HttpClient();
            _apiKey = apiKey;
        }

        public async Task<string> GetTechnicalRequirements(string extractedText)
        {
            var requestBody = new
            {
                model = "gpt-4o-mini",
                messages = new[] {
                new {
                    role = "system",
                    content = @"
                        Bạn là trợ lý AI chuyên xử lý các tài liệu kỹ thuật và hồ sơ mời thầu. 
                        Nhiệm vụ của bạn là **chỉ trích xuất chính xác các 'yêu cầu kỹ thuật' của gói thầu** từ tài liệu được cung cấp, **bao gồm cả trong bảng và văn bản mô tả**.

                        **Quy tắc trích xuất:**
                        1. **Chỉ trích xuất các phần kỹ thuật** như:
                           - Yêu cầu chung
                           - Thông số thiết bị
                           - Chức năng phần mềm
                           - Năng lực của hệ thống / giải pháp
                           - Môi trường cài đặt / triển khai
                           - Giao diện quản trị
                           - Tính năng
                           - Tên hàng hóa / dịch vụ liên quan 
                           - Dịch vụ hỗ trợ kỹ thuật
                           - Bảo mật
                           - Bảo hành
                           - Yêu cầu khác
                        2. **Không trích xuất** các thông tin không liên quan đến yêu cầu kỹ thuật, ví dụ:
                           - Địa điểm
                           - Thời gian
                           - Chủ đầu tư
                           - Vốn
                           - Mục tiêu
                           - Giải pháp và phương pháp luận
                           - Quy định về kiểm tra, nghiệm thu sản phẩm
                           - Các nội dung hành chính khác

                        **Nếu tài liệu có bảng**:
                        - **Chỉ trích xuất** các cột và dòng có thông tin kỹ thuật và mô tả kỹ thuật, không lấy cột STT hoặc các cột định danh.

                        **Nếu có thông tin kỹ thuật chi tiết dưới từng thiết bị/phần mềm**:
                        3. Trình bày như sau: (Nếu có nhiều phân cấp)
                           1. Tên thiết bị/phần mềm (Trang X)  
                                - Nhóm mô tả kỹ thuật 1: (Trang X)  
                                    + Chi tiết kỹ thuật 1 (Trang X)  
                                    + Chi tiết kỹ thuật 2 (Trang X)  
                                - Nhóm mô tả kỹ thuật 2: (Trang X)   
                                    + Chi tiết kỹ thuật ... (Trang X)
   
                        **Định dạng kết quả:**
                        - Bắt đầu kết quả bằng tiêu đề: `## Yêu cầu kỹ thuật`
                        - Trình bày kết quả theo **dạng danh sách phân cấp rõ ràng** như ví dụ bên trên, **tuyệt đối không trình bày dưới dạng bảng** (markdown table hoặc văn bản dạng bảng).
                        - Với mỗi thông tin kỹ thuật, **ghi rõ số trang** tại cuối mỗi dòng (ví dụ: Trang 1, Trang 2,...)

                        **Lưu ý quan trọng:**
                        - Nếu không có thông tin kỹ thuật hoặc không thể đọc được văn bản, trả về: `Lỗi không đọc được văn bản`

                        **Yêu cầu nghiêm ngặt:**
                        - **Không được rút gọn, tóm tắt, hoặc tự ý bỏ thông tin kỹ thuật.**
                        - **Không được suy diễn hoặc bịa thêm nội dung không có trong tài liệu.**
                        - Chỉ sử dụng đúng các thông tin có trong văn bản đầu vào, giữ nguyên nghĩa gốc.
                    "
                },
                new {
                    role = "user",
                    content = $"Dưới đây là nội dung một tài liệu kỹ thuật, trong đó mỗi trang được đánh dấu bằng nhãn [PAGE n] (ví dụ: [PAGE 1], [PAGE 2], ...): \n\n{extractedText}"
                }
            },
            temperature = 0.2,
                max_tokens = 2000
            };

            var content = new StringContent(JsonConvert.SerializeObject(requestBody), Encoding.UTF8, "application/json");

            _client.DefaultRequestHeaders.Clear();
            _client.DefaultRequestHeaders.Add("Authorization", $"Bearer {_apiKey}");

            try
            {
                var response = await _client.PostAsync("https://api.openai.com/v1/chat/completions", content);
                var responseString = await response.Content.ReadAsStringAsync();

                if (!response.IsSuccessStatusCode)
                {
                    throw new Exception($"Lỗi từ OpenAI API: {response.StatusCode} - {responseString}");
                }

                dynamic json = JsonConvert.DeserializeObject(responseString)!;
                return json!.choices[0].message.content.ToString().Trim();
            }
            catch (HttpRequestException ex)
            {
                throw new Exception("Có lỗi khi gọi API OpenAI: " + ex.Message);
            }
            catch (Exception ex)
            {
                throw new Exception("Đã xảy ra lỗi: " + ex.Message);
            }
        }
    }
}
