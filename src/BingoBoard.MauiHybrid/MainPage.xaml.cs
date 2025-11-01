namespace BingoBoard.MauiHybrid;

public partial class MainPage : ContentPage
{
	private readonly string _adminUrl;

	public MainPage()
	{
		InitializeComponent();
		
		// Get admin URL from Aspire service discovery
		_adminUrl = Environment.GetEnvironmentVariable("services__boardadmin__https__0") ?? "https://localhost:7207";

		// Intercept web requests to inject configuration into HTML before JavaScript runs
		hybridWebView.WebResourceRequested += OnWebResourceRequested;
	}

	private async void OnWebResourceRequested(object? sender, WebViewWebResourceRequestedEventArgs e)
	{
		// Intercept the root request to inject backend configuration
		if (e.Uri.ToString() == "app://0.0.0.1/" || e.Uri.ToString().EndsWith("index.html"))
		{
			e.Handled = true;
			
			try
			{
				// Read the original HTML from the app package
				using var stream = await FileSystem.OpenAppPackageFileAsync("wwwroot/index.html");
				using var reader = new StreamReader(stream);
				var originalHtml = await reader.ReadToEndAsync();
				
				// Inject configuration script as the first script in <head>
				var configScript = $"<script>window.BACKEND_CONFIG={{adminUrl:'{_adminUrl}'}};</script>";
				var modifiedHtml = originalHtml.Replace("<head>", "<head>" + configScript);
				
				// Return modified HTML
				var responseStream = new MemoryStream(System.Text.Encoding.UTF8.GetBytes(modifiedHtml));
				e.SetResponse(200, "OK", "text/html", responseStream);
			}
			catch (Exception ex)
			{
				Console.WriteLine($"[MAUI] Failed to inject configuration: {ex.Message}");
				e.Handled = false;
			}
		}
	}
}
