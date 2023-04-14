using System.Web;
using Newtonsoft.Json;

namespace Implementalist.Tools;

public class GoogleTool : Tool
{
    public override string Hit => "google";
    public override string SampleInput => "<search_query>";
    public override string Description => "Returns the top 5 results on google for your search query.";

    private static DateTime lastSearchTime;
    
    public override async Task<string> UseTool(Agent agent, string input)
    {
        var delay = DateTime.Now - lastSearchTime;
        
        var report = "";
        var results = await Search(input);
        int i = 0;
        if (results != null)
        {
            foreach (var result in results)
            {
                if (report.Length > 0) report += "\n";
                // var moreSnippet = await GetMoreSnippet(result);

                var message =
                    @$"[({i}) {result.title}]({result.link})
> {result.snippet}";
                report += $"{message}\n";
                i++;
            }

            return report;
        }

        return "ERROR";
    }

    private class SearchResult
    {
        public List<SearchResultItem> items;
    }
    private class SearchResultItem
    {
        public string title;
        public string link;
        public string snippet;
    }

    private HttpClient httpClient = new HttpClient();
    async Task<List<SearchResultItem>> Search(string query) 
    {
        // Create a web request for the given search query 
        var request = new HttpRequestMessage(HttpMethod.Get, $"https://www.googleapis.com/customsearch/v1?key={Secrets.GOOGLE_API_KEY}&cx={Secrets.GOOGLE_ENGINE_ID}&q={HttpUtility.UrlEncode(query)}");
        UI.WriteLine($"Googling: {request.RequestUri}");
        var response = await httpClient.SendAsync(request);
        var json = await response.Content.ReadAsStringAsync();
        return JsonConvert.DeserializeObject<SearchResult>(json).items;
    }
}