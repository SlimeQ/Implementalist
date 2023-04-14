using System.Reflection;
using Newtonsoft.Json.Linq;

namespace Implementalist;
public static class Secrets
{
    private const string Path = "conf/secrets.json";
    public static void Load()
    {
        var json = File.ReadAllText(Path);
        var jsonObject = JObject.Parse(json);

        foreach (var field in typeof(Secrets).GetFields(BindingFlags.Static | BindingFlags.Public))
        {
            string fieldName = field.Name;
            if (jsonObject.TryGetValue(fieldName, out JToken value))
            {
                field.SetValue(null, value.ToString());
            }
        }
    }
    
    public static string OPENAI_API_KEY;
    public static string GOOGLE_API_KEY;
    public static string GOOGLE_ENGINE_ID;
    
    public static string LINUX_HOST;
    public static string LINUX_USER;
    public static string LINUX_PASS;
}