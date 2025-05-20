using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Windows;
using System.Windows.Threading;

namespace PythonFastApiTest;

/// <summary>
/// Interaction logic for MainWindow.xaml
/// </summary>
public partial class MainWindow : Window
{
	private DispatcherTimer timer = new();
	private readonly HttpClient httpClient = new();
	private Random random = new();

	public MainWindow()
    {
        InitializeComponent();

		timer.Interval = TimeSpan.FromMilliseconds(200);
		timer.Tick += Timer_Tick;
		timer.Start();
	}

	private async void Timer_Tick(object? sender, EventArgs e)
	{
		try
		{
			var payload = new { price = random.Next(200) };
			var content = new StringContent(JsonSerializer.Serialize(payload), Encoding.UTF8, "application/json");

			var response = await httpClient.PostAsync("http://127.0.0.1:8080/predict", content);
			var responseString = await response.Content.ReadAsStringAsync();
			var json = JsonSerializer.Deserialize<SignalResponse>(responseString);
			SignalResultText.Text = $"시그널: {json.signal}"; // price가 100 이상이면 1, 아니면 -1 시그널
		}
		catch
		{
			SignalResultText.Text = $"에러";
		}
	}

	public class SignalResponse
	{
		public int signal { get; set; }
	}
}