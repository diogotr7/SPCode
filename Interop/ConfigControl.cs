using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Newtonsoft.Json;
using SPCode.Utils;
using Formatting = Newtonsoft.Json.Formatting;

namespace SPCode.Interop;

public static class ConfigLoader
{
    private static readonly JsonSerializerSettings ConfigSerializerSettings =
        new() { DefaultValueHandling = DefaultValueHandling.Populate };

    public static List<Config> Load()
    {
        List<Config> configs;
        if (File.Exists(PathsHelper.ConfigFilePath))
        {
            configs = JsonConvert.DeserializeObject<List<Config>>(File.ReadAllText(PathsHelper.ConfigFilePath),
                ConfigSerializerSettings
            );
        }
        else
        {
            configs = new List<Config> { new() { Name = "Default config" } };

            File.WriteAllText(PathsHelper.ConfigFilePath, JsonConvert.SerializeObject(
                configs,
                Formatting.Indented,
                ConfigSerializerSettings));
        }

        configs.FirstOrDefault(c => c.Standard)?.LoadSMDef();
        return configs;
    }

    public static async Task SaveAsync(IEnumerable<Config> configs)
    {
        await File.WriteAllTextAsync(PathsHelper.ConfigFilePath, JsonConvert.SerializeObject(
            configs,
            Formatting.Indented,
            ConfigSerializerSettings));
    }
}