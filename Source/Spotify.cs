using System.Collections;
using System.Diagnostics;
using System.Dynamic;
using System.Net;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;

namespace Source;

public class Spotify {
	private readonly HttpClient httpClient;

	public Spotify() {
		httpClient = new HttpClient(new HttpClientHandler() {
			AllowAutoRedirect = true,
		});

	}


	#region Auth
	public async Task<Token> AuthAsync(string clientId, string clientSecret, CancellationToken cancellationToken = new()) {
		const string url = "https://accounts.spotify.com/api/token";

		FormUrlEncodedContent requestData = new([
			new KeyValuePair<string, string>("grant_type", "client_credentials"),
			new KeyValuePair<string, string>("client_id", clientId),
			new KeyValuePair<string, string>("client_secret", clientSecret)
		]);

		using HttpRequestMessage request = new(HttpMethod.Post, url) {
			Content = requestData
		};
		request.Content.Headers.ContentType = new MediaTypeHeaderValue("application/x-www-form-urlencoded");

		using HttpResponseMessage response = await httpClient.SendAsync(request, cancellationToken);
		response.EnsureSuccessStatusCode();

		string responseContent = await response.Content.ReadAsStringAsync(cancellationToken);
		Token token = JsonSerializer.Deserialize<Token>(responseContent);
		token.CreatedAt = DateTime.Now;
		return token;
	}

	public async Task<Token> WebAuthAsync(string clientId, string clientSecret, string redirectUrl, CancellationToken cancellationToken = new()) {
		const string url = "https://accounts.spotify.com/api/token";

		// If not valid we ask the user to login and accept
		string code = await GetAuthCode(clientId, redirectUrl, cancellationToken);

		// We then use the code we got to request a token
		FormUrlEncodedContent requestData = new([
			new KeyValuePair<string, string>("grant_type", "authorization_code"),
			new KeyValuePair<string, string>("code", code),
			new KeyValuePair<string, string>("redirect_uri", redirectUrl)
		]);

		// Request
		using HttpRequestMessage request = new(HttpMethod.Post, url) {
			Content = requestData
		};
		string credentials = Convert.ToBase64String(Encoding.ASCII.GetBytes($"{clientId}:{clientSecret}"));
		request.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Basic", credentials);
		request.Content.Headers.ContentType = new MediaTypeHeaderValue("application/x-www-form-urlencoded");

		// Response 
		using HttpResponseMessage response = await httpClient.SendAsync(request, cancellationToken);
		response.EnsureSuccessStatusCode();

		string responseContent = await response.Content.ReadAsStringAsync(cancellationToken);
		Token token = JsonSerializer.Deserialize<Token>(responseContent);
		token.CreatedAt = DateTime.Now;

		return token;
	}


	/// <summary>
	/// Opens the user's browser to prompt them to log in.
	/// </summary>
	/// <param name="clientId">The client ID used for authentication.</param>
	/// <param name="redirectUrl">The URL to which the app will redirect the user after obtaining the authorization code.</param>
	/// <param name="cancellationToken">A token to monitor for cancellation requests.</param>
	/// <returns>A code linked to the user's account, which can be exchanged for an authorization token with "player" privileges.</returns>
	/// <exception cref="Exception">Thrown if no authorization code is received.</exception>
	private static async Task<string> GetAuthCode(string clientId, string redirectUrl, CancellationToken cancellationToken = new()) {
		const string scope = "user-read-playback-state%20user-modify-playback-state";
		string redirectUri = Uri.EscapeDataString(redirectUrl);
		string authUrl = $"https://accounts.spotify.com/authorize?client_id={clientId}&response_type=code&redirect_uri={redirectUri}&scope={scope}";
		OpenUrlInBrowser(authUrl);

		// Start a http listener and get the code
		using HttpListener httpListener = new();
		httpListener.Prefixes.Add(redirectUrl + "/");
		httpListener.Start();

		HttpListenerContext context = await httpListener.GetContextAsync();

		// Request
		string? code = context.Request.QueryString["code"];
		if (string.IsNullOrEmpty(code)) {
			throw new Exception("No authorization code received.");
		}

		// Return a "you can close this page"
		const string content = "You can close this window now";
		byte[] bytes = Encoding.UTF8.GetBytes(content);
		context.Response.ContentLength64 = bytes.Length;
		context.Response.ContentType = "text/html";
		Stream output = context.Response.OutputStream;
		await output.WriteAsync(bytes, cancellationToken);

		return code;
	}
	#endregion

	#region Artist
	public async Task<Artist> GetArtistAsync(Token token, string artistId, CancellationToken cancellationToken = new()) {
		string url = $"https://api.spotify.com/v1/artists/{artistId}";

		using HttpRequestMessage request = new(HttpMethod.Get, url);
		request.Headers.Authorization = new(token.TokenType, token.AccessToken);

		using HttpResponseMessage response = await httpClient.SendAsync(request, cancellationToken);
		response.EnsureSuccessStatusCode();

		string responseContent = await response.Content.ReadAsStringAsync(cancellationToken);

		return JsonSerializer.Deserialize<Artist>(responseContent);
	}
	#endregion

	public async Task<Player> GetPlayerAsync(Token token, CancellationToken cancellationToken = new()) {
		const string url = "https://api.spotify.com/v1/me/player";

		using HttpRequestMessage request = new(HttpMethod.Get, url);
		request.Headers.Authorization = new(token.TokenType, token.AccessToken);

		using HttpResponseMessage response = await httpClient.SendAsync(request, cancellationToken);
		try {

			response.EnsureSuccessStatusCode();

			string responseContent = await response.Content.ReadAsStringAsync(cancellationToken);

			return JsonSerializer.Deserialize<Player>(responseContent);
		}
		catch (Exception exception) {
			Console.WriteLine(exception);
			return new Player();
		}
	}

	public async Task<Devices> GetDevicesAsync(Token token, CancellationToken cancellationToken = new()) {
		const string url = "https://api.spotify.com/v1/me/player/devices";

		using HttpRequestMessage request = new(HttpMethod.Get, url);
		request.Headers.Authorization = new(token.TokenType, token.AccessToken);

		using HttpResponseMessage response = await httpClient.SendAsync(request, cancellationToken);

		response.EnsureSuccessStatusCode();

		string responseContent = await response.Content.ReadAsStringAsync(cancellationToken);
		return JsonSerializer.Deserialize<Devices>(responseContent);
	}

	public async Task SetVolumeAsync(Token token, string deviceId, int volume, CancellationToken cancellationToken = new()) {
		string url = $"https://api.spotify.com/v1/me/player/volume?volume_percent={volume}&device_id={deviceId}";

		using HttpRequestMessage request = new(HttpMethod.Put, url);
		request.Headers.Authorization = new(token.TokenType, token.AccessToken);

		using HttpResponseMessage response = await httpClient.SendAsync(request, cancellationToken);
		response.EnsureSuccessStatusCode();
	}

	public async Task SetVolumeAsync(Token token, int volume, CancellationToken cancellationToken = new()) {
		string url = $"https://api.spotify.com/v1/me/player/volume?volume_percent={volume}";

		using HttpRequestMessage request = new(HttpMethod.Put, url);
		request.Headers.Authorization = new(token.TokenType, token.AccessToken);

		using HttpResponseMessage response = await httpClient.SendAsync(request, cancellationToken);
		response.EnsureSuccessStatusCode();
	}

	#region Start
	public async Task StartPlaybackAsync(Token token, CancellationToken cancellationToken = new()) {
		const string url = "https://api.spotify.com/v1/me/player/play";

		using HttpRequestMessage request = new(HttpMethod.Put, url) {
			Content = new StringContent("")
		};
		request.Headers.Authorization = new(token.TokenType, token.AccessToken);
		request.Content.Headers.ContentType = new MediaTypeHeaderValue("application/json");

		using HttpResponseMessage response = await httpClient.SendAsync(request, cancellationToken);
		response.EnsureSuccessStatusCode();
	}

	public async Task StartPlaybackAsync(Token token, string context, int position, int positionMs, CancellationToken cancellationToken = new()) {
		const string url = "https://api.spotify.com/v1/me/player/play";

		object json = new {
			context_uri = context,
			position_ms = positionMs,
			offset = new {
				position,
			},
		};

		using HttpRequestMessage request = new(HttpMethod.Put, url) {
			Content = new StringContent(JsonSerializer.Serialize(json))
		};
		request.Headers.Authorization = new(token.TokenType, token.AccessToken);
		request.Content.Headers.ContentType = new MediaTypeHeaderValue("application/json");

		using HttpResponseMessage response = await httpClient.SendAsync(request, cancellationToken);
		response.EnsureSuccessStatusCode();
	}

	public async Task StartPlaybackAsync(Token token, string deviceId, CancellationToken cancellationToken = new()) {
		string url = $"https://api.spotify.com/v1/me/player/play?device_id={deviceId}";

		using HttpRequestMessage request = new(HttpMethod.Put, url) {
			Content = new StringContent("")
		};
		request.Headers.Authorization = new(token.TokenType, token.AccessToken);
		request.Content.Headers.ContentType = new MediaTypeHeaderValue("application/json");

		using HttpResponseMessage response = await httpClient.SendAsync(request, cancellationToken);
		response.EnsureSuccessStatusCode();
	}

	public async Task StartPlaybackAsync(Token token, string deviceId, string context, int positionMs, CancellationToken cancellationToken = new()) {
		string url = $"https://api.spotify.com/v1/me/player/play?device_id={deviceId}&position_ms={positionMs}";

		object json = new {
			context_uri = context,
			position_ms = positionMs,
		};

		using HttpRequestMessage request = new(HttpMethod.Put, url) {
			Content = new StringContent(JsonSerializer.Serialize(json))
		};

		request.Headers.Authorization = new(token.TokenType, token.AccessToken);
		request.Content.Headers.ContentType = new MediaTypeHeaderValue("application/json");

		using HttpResponseMessage response = await httpClient.SendAsync(request, cancellationToken);
		response.EnsureSuccessStatusCode();
	}
	#endregion

	#region Pause
	public async Task PausePlaybackAsync(Token token, CancellationToken cancellationToken = new()) {
		string url = $"https://api.spotify.com/v1/me/player/pause";

		using HttpRequestMessage request = new(HttpMethod.Put, url);
		request.Headers.Authorization = new(token.TokenType, token.AccessToken);

		using HttpResponseMessage response = await httpClient.SendAsync(request, cancellationToken);
		response.EnsureSuccessStatusCode();
	}


	public async Task PausePlaybackAsync(Token token, string deviceId, CancellationToken cancellationToken = new()) {
		string url = $"https://api.spotify.com/v1/me/player/pause?device_id={deviceId}";

		using HttpRequestMessage request = new(HttpMethod.Put, url);
		request.Headers.Authorization = new(token.TokenType, token.AccessToken);

		using HttpResponseMessage response = await httpClient.SendAsync(request, cancellationToken);
		response.EnsureSuccessStatusCode();
	}
	#endregion

	#region Next
	public async Task SkipToNextAsync(Token token, CancellationToken cancellationToken = new()) {
		const string url = "https://api.spotify.com/v1/me/player/next";

		using HttpRequestMessage request = new(HttpMethod.Post, url);
		request.Headers.Authorization = new(token.TokenType, token.AccessToken);

		using HttpResponseMessage response = await httpClient.SendAsync(request, cancellationToken);
		response.EnsureSuccessStatusCode();
	}

	public async Task SkipToNextAsync(Token token, string deviceId, CancellationToken cancellationToken = new()) {
		string url = $"https://api.spotify.com/v1/me/player/next?device_id={deviceId}";

		using HttpRequestMessage request = new(HttpMethod.Post, url);
		request.Headers.Authorization = new(token.TokenType, token.AccessToken);

		using HttpResponseMessage response = await httpClient.SendAsync(request, cancellationToken);
		response.EnsureSuccessStatusCode();
	}
	#endregion

	#region Previous
	public async Task SkipToPreviousAsync(Token token, CancellationToken cancellationToken = new()) {
		const string url = "https://api.spotify.com/v1/me/player/previous";

		using HttpRequestMessage request = new(HttpMethod.Post, url);
		request.Headers.Authorization = new(token.TokenType, token.AccessToken);

		using HttpResponseMessage response = await httpClient.SendAsync(request, cancellationToken);
		response.EnsureSuccessStatusCode();
	}

	public async Task SkipToPreviousAsync(Token token, string deviceId, CancellationToken cancellationToken = new()) {
		string url = $"https://api.spotify.com/v1/me/player/previous?device_id={deviceId}";

		using HttpRequestMessage request = new(HttpMethod.Post, url);
		request.Headers.Authorization = new(token.TokenType, token.AccessToken);

		using HttpResponseMessage response = await httpClient.SendAsync(request, cancellationToken);
		response.EnsureSuccessStatusCode();
	}
	#endregion

	#region Repeat
	/// <summary>
	/// Sets the repeat mode
	/// </summary>
	/// <param name="token"></param>
	/// <param name="state">Can be track, context or off</param>
	/// <param name="cancellationToken"></param>
	/// <returns></returns>
	public async Task SetRepeatModeAsync(Token token, string state, CancellationToken cancellationToken = new()) {
		string[] options = { "track", "context", "off" };
		Trace.Assert(options.Contains(state), "State is not a valid state");

		string url = $"https://api.spotify.com/v1/me/player/repeat?state={state}";

		using HttpRequestMessage request = new(HttpMethod.Put, url);
		request.Headers.Authorization = new(token.TokenType, token.AccessToken);

		using HttpResponseMessage response = await httpClient.SendAsync(request, cancellationToken);
		response.EnsureSuccessStatusCode();
	}


	/// <summary>
	/// Sets the repeat mode
	/// </summary>
	/// <param name="token"></param>
	/// <param name="state">Can be track, context or off</param>
	/// <param name="deviceId"></param>
	/// <param name="cancellationToken"></param>
	/// <returns></returns>
	public async Task SetRepeatModeAsync(Token token, string state, string deviceId, CancellationToken cancellationToken = new()) {
		string[] options = { "track", "context", "off" };
		Trace.Assert(options.Contains(state), "State is not a valid state");

		string url = $"https://api.spotify.com/v1/me/player/repeat?state={state}&device_id={deviceId}";

		using HttpRequestMessage request = new(HttpMethod.Put, url);
		request.Headers.Authorization = new(token.TokenType, token.AccessToken);

		using HttpResponseMessage response = await httpClient.SendAsync(request, cancellationToken);
		response.EnsureSuccessStatusCode();
	}
	#endregion

	#region Shuffle
	public async Task ToggleShuffleAsync(Token token, bool state, CancellationToken cancellationToken = new()) {
		string stateString = state.ToString().ToLower();
		string url = $"https://api.spotify.com/v1/me/player/shuffle?state={stateString}";

		using HttpRequestMessage request = new(HttpMethod.Put, url);
		request.Headers.Authorization = new(token.TokenType, token.AccessToken);

		using HttpResponseMessage response = await httpClient.SendAsync(request, cancellationToken);
		response.EnsureSuccessStatusCode();
	}

	public async Task ToggleShuffleAsync(Token token, bool state, string deviceId, CancellationToken cancellationToken = new()) {
		string stateString = state.ToString().ToLower();
		string url = $"https://api.spotify.com/v1/me/player/shuffle?state={stateString}&device_id={deviceId}";

		using HttpRequestMessage request = new(HttpMethod.Put, url);
		request.Headers.Authorization = new(token.TokenType, token.AccessToken);

		using HttpResponseMessage response = await httpClient.SendAsync(request, cancellationToken);
		response.EnsureSuccessStatusCode();
	}
	#endregion


	public async Task<CurrentPlayingTrack> GetCurrentPlayingTrackAsync(Token token, CancellationToken cancellationToken = new()) {
		const string url = "https://api.spotify.com/v1/me/player/currently-playing";

		using HttpRequestMessage request = new(HttpMethod.Get, url);
		request.Headers.Authorization = new(token.TokenType, token.AccessToken);

		using HttpResponseMessage response = await httpClient.SendAsync(request, cancellationToken);
		response.EnsureSuccessStatusCode();

		string responseContent = await response.Content.ReadAsStringAsync(cancellationToken);
		return JsonSerializer.Deserialize<CurrentPlayingTrack>(responseContent);
	}

	private static void OpenUrlInBrowser(string url) {
		try {
			Process.Start(new ProcessStartInfo(url) { UseShellExecute = true });
		}
		catch {
			Console.WriteLine($"Please open {url} in your browser");
		}
	}
}
