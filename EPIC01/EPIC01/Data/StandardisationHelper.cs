using Newtonsoft.Json;
using System.Text.RegularExpressions;

namespace EPIC01.Data
{
    public static class StandardisationHelper
    {
        public static string ConvertMarkdownToJson(string markdown)
        {
            var items = new List<Item>();
            var lines = markdown.Split(new[] { "\r\n", "\n" }, StringSplitOptions.RemoveEmptyEntries);

            Item? currentItem = null;

            foreach (var line in lines)
            {
                var trimmedLine = line.Trim();

                // Bỏ qua các tiêu đề Markdown cấp 1 đến cấp 6
                if (Regex.IsMatch(trimmedLine, @"^#{1,6}\s")) continue;

                // Nếu là dòng đánh số đầu mục chính (1., 2., ...)
                if (Regex.IsMatch(trimmedLine, @"^\d+\.\s"))
                {
                    string page = "Không xác định";
                    string content = trimmedLine;

                    // Tìm và tách số trang nếu có
                    var match = Regex.Match(trimmedLine, @"\(Trang\s*(\d+)\)", RegexOptions.IgnoreCase);
                    if (match.Success)
                    {
                        page = match.Groups[1].Value;
                        content = trimmedLine.Substring(0, match.Index).Trim();
                    }

                    currentItem = new Item
                    {
                        item_title = content,
                        item_requirements = new List<ItemRequirement>()
                    };
                    items.Add(currentItem);
                }
                // Nếu là dòng "-" hoặc "+" và đang có item hiện tại
                else if ((trimmedLine.StartsWith("-") || trimmedLine.StartsWith("+")) && currentItem != null)
                {
                    string page = "Không xác định";

                    var match = Regex.Match(trimmedLine, @"\(Trang\s*(\d+)\)", RegexOptions.IgnoreCase);
                    if (match.Success)
                    {
                        page = match.Groups[1].Value;
                    }

                    var contentWithoutPage = Regex.Replace(trimmedLine, @"\(Trang\s*\d+\)", "", RegexOptions.IgnoreCase).Trim();

                    currentItem.item_requirements.Add(new ItemRequirement
                    {
                        page = page,
                        content = contentWithoutPage
                    });
                }
            }

            return JsonConvert.SerializeObject(items, Formatting.Indented);
        }
    }

    // Định nghĩa đối tượng Item
    public class Item
    {
        public string item_title { get; set; } = string.Empty;
        public List<ItemRequirement> item_requirements { get; set; } = new List<ItemRequirement>();
    }

    // Định nghĩa đối tượng yêu cầu
    public class ItemRequirement
    {
        public string page { get; set; } = string.Empty;
        public string content { get; set; } = string.Empty;
    }

}
