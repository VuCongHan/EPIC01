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
            //var requestBody = new
            //{
            //    model = "gpt-4o-mini",
            //    messages = new[]
            //    {
            //        new
            //        {
            //            role = "system",
            //            content = """
            //                Bạn là một chuyên gia kỹ thuật. Nhiệm vụ của bạn là trích xuất các yêu cầu kỹ thuật

            //                Yêu cầu:
            //                - Trả về định dạng Markdown chuẩn như sau:

            //                1. Chính (Trang X)
            //                   - Chi tiết nếu có (Trang X)

            //                - Ghi rõ số trang tại **cuối từng dòng**, bao gồm cả dòng chính và các dòng gạch đầu dòng (ví dụ: Trang 1, Trang 2...)

            //                Tránh:
            //                - Tuyệt đối không bịa ra thông tin hoặc thêm bất kỳ thông tin nào không có trong văn bản.
            //                - Không liệt kê tuần tự 1-2-3… mà không nhóm lại.
            //                - Không để lặp lại từ hoặc đoạn giống nhau giữa các nhóm.
            //                """
            //        },
            //        new
            //        {
            //            role = "user",
            //            content = $"""
            //                Dưới đây là nội dung một tài liệu kỹ thuật, trong đó mỗi trang được đánh dấu bằng nhãn [PAGE n] (ví dụ: [PAGE 1], [PAGE 2], ...).

            //                Yêu cầu bạn:
            //                1. Trích xuất tất cả các **yêu cầu kỹ thuật** từ văn bản dưới đây.
            //                2. Sử dụng cú pháp Markdown chuẩn.
            //                3. Các yêu cầu kỹ thuật phải bao gồm số trang tại cuối mỗi yêu cầu.
            //                4. Các yêu cầu kỹ thuật được liệt kê dưới dạng danh sách với được đánh số thứ tự (1, 2, 3, ...) và chi tiết với dấu gạch đầu dòng (-).

            //                **Chỉ lấy các yêu cầu kỹ thuật. Loại bỏ mọi thông tin không liên quan và tuyệt đối không bịa ra thông tin hoặc thêm bất kỳ thông tin nào không có trong văn bản**

            //                Nội dung văn bản như sau:
            //                {extractedText}
            //                """
            //        }
            //    },
            //    temperature = 0.3,
            //    max_tokens = 1000
            //};

            var requestBody = new
            {
                model = "gpt-4o-mini",
                messages = new[]
                {
                    new {
                     role = "system",
                     content = @"Bạn là trợ lý AI chuyên xử lý các tài liệu kỹ thuật và hồ sơ mời thầu.
                        Nhiệm vụ của bạn là **chỉ trích xuất chính xác các 'yêu cầu kỹ thuật' của gói thầu** từ tài liệu được cung cấp, **bao gồm cả trong bảng và văn bản mô tả**.

                        - Chỉ tập trung vào các phần kỹ thuật như: thông số thiết bị, chức năng phần mềm, dịch vụ hỗ trợ kỹ thuật, bảo mật, bảo hành,...
                        - Nếu tài liệu có bảng, chỉ trích xuất các cột/dòng có thông tin kỹ thuật và mô tả kỹ thuật, không lấy STT hoặc cột định danh.
                        - Không liệt kê các thông tin như: địa điểm, thời gian, chủ đầu tư, vốn, mục tiêu, kiểm tra nghiệm thu, hoặc các nội dung hành chính khác.
                        - Nếu có thông tin kỹ thuật chi tiết dưới từng thiết bị/phần mềm thì trình bày theo dạng:
                            1. Tên thiết bị/phần mềm (Trang X)
                                - Mô tả kỹ thuật 1 (Trang X)
                                - Mô tả kỹ thuật 2 (Trang X)
                        - Kết quả phải bắt đầu bằng tiêu đề: '## Yêu cầu kỹ thuật'
                        - Ghi rõ số trang tại **cuối từng dòng** (ví dụ: Trang 1, Trang 2...)

                        Chỉ trả về phần kỹ thuật, không giải thích thêm.
                        **Nếu văn bản không có nghĩa trả về Lỗi không đọc được văn bản**
                     "
                    },
                    new {
                        role = "user",
                        content = $"Dưới đây là nội dung một tài liệu kỹ thuật, trong đó mỗi trang được đánh dấu bằng nhãn [PAGE n] (ví dụ: [PAGE 1], [PAGE 2], ...): \n\n{extractedText}"
                    }
                },
                temperature = 0.2,
                max_tokens = 1000
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
