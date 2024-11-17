using System.Text.Json.Serialization;

namespace APIModels
{
    public class BaseApiModel
    {
        public int Id { get; set; }

        [JsonIgnore]
        public int? CreatedBy { get; set; }

        [JsonIgnore]
        public DateTime? CreatedDate { get; set; }

        [JsonIgnore]
        public int? UpdatedBy { get; set; }

        [JsonIgnore]

        public DateTime? UpdatedDate { get; set; }
    }
}
