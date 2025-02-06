using System;
using System.Net.Http;
using System.Text.Json;
using Fleck;
using DiscordRPC;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

class Program
{
    static DiscordRpcClient client;

    static void Main()
    {
        try
        {
            client = new DiscordRpcClient("1289759628720209920");
            client.Initialize();

            Console.WriteLine("WebSocket server running on ws://localhost:8080...");
            var server = new WebSocketServer("ws://0.0.0.0:8080");

            server.Start(socket =>
            {
                socket.OnMessage = message =>
                {
                    Console.WriteLine("Received raw message: " + message);
                    UpdateDiscordRPC(message);
                };
            });

            Console.ReadLine();
        }
        catch (Exception ex)
        {
            Console.WriteLine("Unhandled Exception: " + ex.Message);
            Console.WriteLine(ex.StackTrace);
        }
    }

    static async void UpdateDiscordRPC(string jsonData)
    {
        try
        {
            Console.WriteLine($"Received JSON Data: {jsonData}");  // Debugging output
            var data = JsonSerializer.Deserialize<TabData>(jsonData);

            // Debugging: Check if URL is correctly deserialized
            Console.WriteLine($"Deserialized Data: Title = {data.Title}, URL = {data.Url}");

            if (string.IsNullOrEmpty(data.Url))
            {
                Console.WriteLine("Error: URL is empty or null.");
                return;
            }

            // Ensure URL is valid
            Uri uri;
            if (!Uri.TryCreate(data.Url, UriKind.Absolute, out uri))
            {
                Console.WriteLine("Error: Invalid URL format.");
                return;
            }

            // Handle 'about:' URLs (internal Firefox pages) - skip favicon fetching
            if (uri.Scheme == "about")
            {
                Console.WriteLine("Skipping favicon for 'about:' URL.");
                SetDiscordPresence(data.Title, "https://i.pinimg.com/736x/fd/49/13/fd491367639b002c1f4cb7ba1862559f.jpg", data.Url);
                return;
            }

            // Fetch the favicon URL
            string faviconUrl = await GetFaviconUrl(uri);

            // Set the Discord RPC presence with the URL as an argument
            SetDiscordPresence(data.Title, faviconUrl, data.Url);

            Console.WriteLine($"Updated Discord RPC: {data.Title} [{faviconUrl}]");
        }
        catch (Exception ex)
        {
            Console.WriteLine("Error parsing JSON: " + ex.Message);
        }
    }


    static async Task<string> GetFaviconUrl(Uri uri)
    {
        try
        {
            // Create HTTP client to fetch website's content
            using (var client = new HttpClient())
            {
                var response = await client.GetStringAsync(uri.ToString());

                // Find the favicon in the HTML <link> tags
                var faviconLinkStart = response.IndexOf("<link rel=\"icon\" href=\"") + "<link rel=\"icon\" href=\"".Length;
                var faviconLinkEnd = response.IndexOf("\"", faviconLinkStart);

                if (faviconLinkStart != -1 && faviconLinkEnd != -1)
                {
                    string faviconPath = response.Substring(faviconLinkStart, faviconLinkEnd - faviconLinkStart);

                    // If the favicon path is relative, combine with the base URL
                    if (!faviconPath.StartsWith("http"))
                    {
                        Uri baseUri = new Uri(uri.GetLeftPart(UriPartial.Authority));
                        Uri fullUri = new Uri(baseUri, faviconPath);
                        return fullUri.ToString();
                    }

                    return faviconPath; // Return the full URL if already absolute
                }
                else
                {
                    Console.WriteLine("Favicon not found, using default image.");
                    return "https://i.pinimg.com/736x/fd/49/13/fd491367639b002c1f4cb7ba1862559f.jpg"; // Default fallback image
                }
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine("Error fetching favicon: " + ex.Message);
            return "https://i.pinimg.com/736x/fd/49/13/fd491367639b002c1f4cb7ba1862559f.jpg"; // Default fallback image on error
        }
    }



    static void SetDiscordPresence(string title, string faviconUrl, string url)
    {
        // Truncate the title to 128 characters to avoid Discord's limit
        string truncatedTitle = title.Length > 128 ? title.Substring(0, 128) : title;

        // Truncate the URL to 128 characters as well, to avoid Discord's limit
        string truncatedUrl = url.Length > 128 ? url.Substring(0, 128) : url;

        // Set the Discord RPC presence
        client.SetPresence(new RichPresence()
        {
            Details = truncatedTitle, // Title displayed in the RPC
            State = $"Browsing: {truncatedUrl}", // Add the URL as part of the state
            Timestamps = new Timestamps(DateTime.UtcNow),
            Assets = new Assets()
            {
                LargeImageKey = faviconUrl, // Use the favicon URL
                LargeImageText = truncatedTitle, // Show title on hover
            }
        });
    }

}

class TabData
{
    [JsonPropertyName("title")]
    public string Title { get; set; }

    [JsonPropertyName("url")]
    public string Url { get; set; }

    [JsonPropertyName("timestamp")]
    public long Timestamp { get; set; }
}
