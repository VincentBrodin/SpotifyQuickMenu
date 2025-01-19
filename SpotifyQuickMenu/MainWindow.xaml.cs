using Source;
using System.ComponentModel;
using System.IO;
using System.Text.Json;
using System.Windows;
using System.Windows.Media.Imaging;
using Microsoft.Web.WebView2.Core;
using System.Runtime.InteropServices;

namespace SpotifyQuickMenu;

public delegate Task MessageHandler(WebMessage webMessage);

public partial class MainWindow : Window {
	private const int BufferTime = 250;

	private readonly Spotify spotify;
	private readonly Secrets secrets;
	private readonly CancellationTokenSource cancellationTokenSource = new();
	private readonly List<Task> tasks = [];

	private readonly Dictionary<string, MessageHandler> messageHandlers = [];

	private bool ready;

	private int volume;
	private Task updateVolumeTask = Task.CompletedTask;
	private Token token = new();
	private readonly Lock volumeLock = new();
	private bool queue;

	public MainWindow() {
		InitializeComponent();
		spotify = new Spotify();

		string secretsPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "secrets.txt");
		secrets = Secrets.LoadFromFile(secretsPath) ?? throw new Exception("Could not load secrets");


		ready = false;


		AddMessageHandler("volume", VolumeChanged);
		AddMessageHandler("play", PlayClicked);
		AddMessageHandler("pause", PauseClicked);
		AddMessageHandler("next", NextClicked);
		AddMessageHandler("previous", PreviousClicked);
		AddMessageHandler("set_time", TimeChanged);
	}

	#region System
	private async void Window_Loaded(object sender, RoutedEventArgs e) {
		//Webview
		await WebContent.EnsureCoreWebView2Async(null);
		WebContent.DefaultBackgroundColor = System.Drawing.Color.Transparent;
		string assetPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Assets");
		WebContent.CoreWebView2.SetVirtualHostNameToFolderMapping("app.assets", assetPath, CoreWebView2HostResourceAccessKind.Allow);
		WebContent.CoreWebView2.Navigate("https://app.assets/index.html");

		WebContent.CoreWebView2.WebMessageReceived += CoreWebView2_WebMessageReceived;


		// Grabbing the client secrets

		// First we check if the token is stored local and is valid
		string pathToToken = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "localToken.json");
		if (File.Exists(pathToToken)) {
			string tokenJson = await File.ReadAllTextAsync(pathToToken, cancellationTokenSource.Token);
			Token savedToken = JsonSerializer.Deserialize<Token>(tokenJson);
			// This is true when the token has not expired else we just grab a new token
			DateTime expiresAt = savedToken.CreatedAt.AddSeconds(savedToken.ExpiresIn);
			if (expiresAt > DateTime.Now) {
				token = savedToken;
			}
			else {
				FileStream file = File.Create(pathToToken);
				file.Close();
				token = await spotify.WebAuthAsync(secrets, cancellationTokenSource.Token);
				await File.WriteAllTextAsync(pathToToken, JsonSerializer.Serialize(token), cancellationTokenSource.Token);
			}
		}
		else {
			FileStream file = File.Create(pathToToken);
			file.Close();
			token = await spotify.WebAuthAsync(secrets, cancellationTokenSource.Token);
			await File.WriteAllTextAsync(pathToToken, JsonSerializer.Serialize(token), cancellationTokenSource.Token);
		}

		Devices devices = await spotify.GetDevicesAsync(token, cancellationTokenSource.Token);
		foreach (Device device in devices.Values) {
			if (device.SupportsVolume) {
				//Volume.Value = device.VolumePercent;
				volume = device.VolumePercent;

				WebContent.CoreWebView2.PostWebMessageAsJson(JsonSerializer.Serialize(new WebMessage("set-volume", $"{volume}")));
				Console.WriteLine($"Volume started at {device.VolumePercent}");
				break;
			}
		}

		await UpdateTrack(cancellationTokenSource.Token);

		lock (tasks) {
			tasks.Add(Task.Run(() => UpdateTrackAutomaticly(cancellationTokenSource.Token), cancellationTokenSource.Token));
		}

		ready = true;
		Console.WriteLine("Everything is loaded ready to go");
	}

	private void AddMessageHandler(string id, MessageHandler handler) {
		messageHandlers.Add(id, handler);
	}


	protected override void OnClosing(CancelEventArgs e) {
		base.OnClosing(e);
		WebContent.CoreWebView2.ClearVirtualHostNameToFolderMapping("app");
		Console.WriteLine("Ending all tasks");
		cancellationTokenSource.Cancel();
		updateVolumeTask.Wait();

		try {
			tasks.RemoveAll(task => task.IsCompleted);
			Task.WhenAll(tasks).Wait();
		}
		catch (Exception exception) {
			Console.WriteLine($"Exception while waiting for tasks: {exception.Message}");
		}

		Console.WriteLine("All tasks ended");
		cancellationTokenSource.Dispose();
	}
	#endregion



	private async Task PreviousClicked(WebMessage webMessage) {
		if (!ready) return;

		if (!token.Valid()) {
			token = await spotify.WebAuthAsync(secrets, cancellationTokenSource.Token);
		}

		await spotify.SkipToPreviousAsync(token, cancellationTokenSource.Token);
		await Task.Delay(BufferTime);
		await UpdateTrack(cancellationTokenSource.Token);
	}

	private async Task NextClicked(WebMessage webMessage) {
		if (!ready) return;

		if (!token.Valid()) {
			token = await spotify.WebAuthAsync(secrets, cancellationTokenSource.Token);
		}

		await spotify.SkipToNextAsync(token, cancellationTokenSource.Token);
		await Task.Delay(BufferTime);
		await UpdateTrack(cancellationTokenSource.Token);
	}

	private async Task PlayClicked(WebMessage webMessage) {
		if (!ready) return;

		if (!token.Valid()) {
			token = await spotify.WebAuthAsync(secrets, cancellationTokenSource.Token);
		}

		await spotify.StartPlaybackAsync(token, cancellationTokenSource.Token);
		await Task.Delay(BufferTime);
		await UpdateTrack(cancellationTokenSource.Token);
	}

	private async Task PauseClicked(WebMessage webMessage) {
		if (!ready) return;

		if (!token.Valid()) {
			token = await spotify.WebAuthAsync(secrets, cancellationTokenSource.Token);
		}

		await spotify.PausePlaybackAsync(token, cancellationTokenSource.Token);
		await Task.Delay(BufferTime);
		await UpdateTrack(cancellationTokenSource.Token);
	}


	private async Task TimeChanged(WebMessage webMessage) {
		if (!ready) return;

		if (!token.Valid()) {
			token = await spotify.WebAuthAsync(secrets, cancellationTokenSource.Token);
		}

		Timestamp timestamp = JsonSerializer.Deserialize<Timestamp>(webMessage.Content);

		await spotify.StartPlaybackAsync(token, timestamp.Context, timestamp.Position, timestamp.PositionMs, cancellationTokenSource.Token);
		await Task.Delay(BufferTime);
		await UpdateTrack(cancellationTokenSource.Token);
	}


	#region Volume
	private async Task VolumeChanged(WebMessage webMessage) {
		if (!ready) return;
		if (!token.Valid()) {
			token = await spotify.WebAuthAsync(secrets, cancellationTokenSource.Token);
		}

		if (!int.TryParse(webMessage.Content, out volume)) {
			return;
		}

		lock (volumeLock) {
			queue = true;
		}

		if (updateVolumeTask.IsCompleted) {
			Console.WriteLine("Starting task");
			queue = true;
			updateVolumeTask = Task.Run(() => UpdateVolume(cancellationTokenSource.Token), cancellationTokenSource.Token);
		}
	}

	private async Task UpdateVolume(CancellationToken cancellationToken) {
		try {
			await Task.Delay(BufferTime, cancellationToken);
			while (queue) {
				lock (volumeLock) {
					queue = false;
				}
				Console.WriteLine("Setting volume");
				await spotify.SetVolumeAsync(token, volume, cancellationToken);
				await Task.Delay(BufferTime, cancellationToken);
			}

			Console.WriteLine("Ending task");
		}
		catch (TaskCanceledException) {
			Console.WriteLine("Update volume canceld");
		}
	}
	#endregion

	private async Task UpdateTrack(CancellationToken cancellationToken) {
		CurrentPlayingTrack track = await spotify.GetCurrentPlayingTrackAsync(token, cancellationToken);
		Application.Current.Dispatcher.Invoke(() => {
			WebMessage webMessage = new("track", JsonSerializer.Serialize(track));
			WebContent.CoreWebView2.PostWebMessageAsJson(JsonSerializer.Serialize(webMessage));
		});
	}

	private async Task UpdateTrackAutomaticly(CancellationToken cancellationToken) {
		try {
			while (!cancellationToken.IsCancellationRequested) {
				await Task.Delay(750, cancellationToken);
				if (ready) {
					await UpdateTrack(cancellationToken);
				}
			}
		}
		catch (TaskCanceledException) {
			Console.WriteLine("Continiues track update canceled");
		}
	}

	private async void CoreWebView2_WebMessageReceived(object? sender, CoreWebView2WebMessageReceivedEventArgs e) {
		WebMessage webMessage = JsonSerializer.Deserialize<WebMessage>(e.WebMessageAsJson);
		if (messageHandlers.TryGetValue(webMessage.Id, out MessageHandler? messageHandler) && messageHandler != null) {
			await messageHandler(webMessage);
		}
		else {
			Console.WriteLine($"No message handler for {webMessage.Id}");
		}
	}
}