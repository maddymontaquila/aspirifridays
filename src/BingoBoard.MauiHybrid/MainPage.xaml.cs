namespace BingoBoard.MauiHybrid;

public partial class MainPage : ContentPage
{
	private readonly string _adminUrl;

	public MainPage()
	{
		InitializeComponent();

		// Get admin URL from Aspire service discovery
		_adminUrl = Environment.GetEnvironmentVariable("services__boardadmin__https__0") ?? "https://localhost:7207";

		hybridWebView.SetInvokeJavaScriptTarget(this);
	}

	public async void DownloadBoard(string imageDataUrl)
	{
		var base64Data = imageDataUrl[(imageDataUrl.IndexOf(',') + 1)..];

		var tempFile = Path.Combine(FileSystem.CacheDirectory, "bingo-board.png");

		await File.WriteAllBytesAsync(tempFile, Convert.FromBase64String(base64Data));

		await Launcher.OpenAsync(new OpenFileRequest
		{
			Title = "AspiriFridays Bingo Board",
			File = new ReadOnlyFile(tempFile)
		});
	}

	private void OnWebResourceRequested(object? sender, WebViewWebResourceRequestedEventArgs e)
	{
		// Intercept the root request to inject backend configuration
		if (e.Uri.ToString() == "app://0.0.0.1/" || e.Uri.ToString() == "https://0.0.0.1/")
		{
			e.Handled = true;
			e.SetResponse(200, "OK", "text/html", GetModifiedHtmlStreamAsync());
		}

		async Task<Stream?> GetModifiedHtmlStreamAsync()
		{
			// Read the original HTML from the app package
			using var stream = await FileSystem.OpenAppPackageFileAsync("wwwroot/index.html");
			using var reader = new StreamReader(stream);
			var originalHtml = await reader.ReadToEndAsync();

			// Inject hybridwebview and configuration scripts as the first script in <head>
			var modifiedHtml = originalHtml.Replace("<head>",
				"<head>" +
				"<script src=\"_framework/hybridwebview.js\"></script>" +
				$"<script>window.BACKEND_CONFIG={{adminUrl:'{_adminUrl}'}};</script>");

			// Return modified HTML
			return new MemoryStream(System.Text.Encoding.UTF8.GetBytes(modifiedHtml));
		}
	}
}
