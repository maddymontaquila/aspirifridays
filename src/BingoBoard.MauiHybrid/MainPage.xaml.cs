namespace BingoBoard.MauiHybrid;

public partial class MainPage : ContentPage
{
	private readonly string _adminUrl;

	public MainPage()
	{
		InitializeComponent();
		
		// Get the admin backend URL from Aspire's injected environment variable
		_adminUrl = Environment.GetEnvironmentVariable("services__boardadmin__https__0") 
		         ?? Environment.GetEnvironmentVariable("services__boardadmin__http__0")
		         ?? "https://localhost:7207"; // Fallback for development

		Console.WriteLine($"[MAUI] Backend URL: {_adminUrl}");
		
		// Intercept web requests to inject configuration
		hybridWebView.WebResourceRequested += OnWebResourceRequested;
	}

	private void OnWebResourceRequested(object? sender, WebViewWebResourceRequestedEventArgs e)
	{
		// Intercept the index.html request to inject the backend configuration
		if (e.Uri.ToString().EndsWith("index.html") || e.Uri.ToString().EndsWith("/"))
		{
			e.Handled = true;
			
			try
			{
				// Read the original HTML
				var originalHtml = File.ReadAllText(Path.Combine(FileSystem.AppDataDirectory, "../Resources/Raw/wwwroot/index.html"));
				
				// Inject the configuration script before other scripts
				var configScript = $@"
<script>
    window.BACKEND_CONFIG = {{
        adminUrl: '{_adminUrl}'
    }};
    console.log('[MAUI] Backend URL configured:', '{_adminUrl}');
</script>";
				
				// Insert the config script right after the <head> tag
				var modifiedHtml = originalHtml.Replace("<head>", "<head>" + configScript);
				
				// Return the modified HTML
				var stream = new MemoryStream(System.Text.Encoding.UTF8.GetBytes(modifiedHtml));
				e.SetResponse(200, "OK", "text/html", stream);
				
				Console.WriteLine("[MAUI] Successfully injected backend configuration into HTML");
			}
			catch (Exception ex)
			{
				Console.WriteLine($"[MAUI] Failed to inject configuration: {ex.Message}");
				// Let the request proceed normally if injection fails
				e.Handled = false;
			}
		}
	}
}
