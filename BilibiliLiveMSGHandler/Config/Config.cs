using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace BilibiliLiveMSGHandler.Config
{
    public static class Config
    {
        public static ConfigRootobject GetConfig()
        {
            ConfigRootobject? config;
            if (!File.Exists("config.json"))
            {
                config = new();
                JsonSerializerOptions options = new()
                {
                    Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
                    WriteIndented = true,
                };
                File.WriteAllText("config.json", JsonSerializer.Serialize(config, options));
                Console.WriteLine("请填写config.json后再次运行。");
                Environment.Exit(0);
            }
            else
            {
                config = JsonSerializer.Deserialize<ConfigRootobject>(File.ReadAllText("config.json"));
            }
            if (config != null)
            {
                return config;
            }
            else
            {
                throw new ArgumentNullException("config is null.");
            }
        }
    }

    public class ConfigRootobject
    {
        [JsonPropertyName("cookie")]
        public string Cookie { get; set; } = string.Empty;
        [JsonPropertyName("ConfigMessage")]
        public string ConfigMessage { get; set; } = "请输入Cookie。";
        [JsonIgnore]
        public int Uid
        {
            get
            {
                foreach (string item in Cookie.Split("; "))
                {
                    if (item.StartsWith("DedeUserID="))
                    {
                        return int.Parse(item[11..]);
                    }
                }
                return 0;
            }
        }
    }
}
