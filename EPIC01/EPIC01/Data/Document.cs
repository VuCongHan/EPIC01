using System.ComponentModel.DataAnnotations;

namespace EPIC01.Data
{
    public class Document
    {
        [Key]
        //public Guid Id { get; set; } = Guid.NewGuid();
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Type { get; set; } = string.Empty;
        public string FilePath { get; set; } = string.Empty;
        public DateTime UploadDate { get; set; }
        public string Status { get; set; } = string.Empty;
    }
}
