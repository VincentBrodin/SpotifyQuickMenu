using Source;
using System.ComponentModel.Design;
using System.Text.Json;

namespace ConsoleQuickMenu;

public static class Program {
	public static async Task Main(string[] args) {
		(string id, string secret) = GetCredentials();
		//Console.WriteLine($"Id: {id}\nSecret:{secret}");

		Spotify spotify = new();

		Token token;
		// First we check if the token is stored local and is valid
		string pathToToken = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "localToken.json");
		if (File.Exists(pathToToken)) {
			string tokenJson = await File.ReadAllTextAsync(pathToToken);
			Token savedToken = JsonSerializer.Deserialize<Token>(tokenJson);
			// This is true when the token has not expired else we just grab a new token
			DateTime expiresAt = savedToken.CreatedAt.AddSeconds(savedToken.ExpiresIn);
			if (expiresAt > DateTime.Now) {
				token = savedToken;
			}
			else {
				FileStream file = File.Create(pathToToken);
				file.Close();
				token = await spotify.WebAuthAsync(id, secret, "http://localhost:8888/callback");
				await File.WriteAllTextAsync(pathToToken, JsonSerializer.Serialize(token));
			}
		}
		else {
			FileStream file = File.Create(pathToToken);
			file.Close();
			token = await spotify.WebAuthAsync(id, secret, "http://localhost:8888/callback");
			await File.WriteAllTextAsync(pathToToken, JsonSerializer.Serialize(token));
		}

		Console.WriteLine(JsonSerializer.Serialize(token));

		CurrentPlayingTrack track = await spotify.GetCurrentPlayingTrackAsync(token);
		string trackString = $"{track.Item.Name} by";

		for (int i = 0; i < track.Item.Artists.Length; i++) {
			Artist artist = track.Item.Artists[i];
			if(i == 0) {
				trackString += $" {artist.Name}";
			}
			else {
				trackString += $" and {artist.Name}";
			}
		}
		trackString += $" from {track.Item.Album.Name}";
		Console.WriteLine(trackString);

		return;

		Devices devices = await spotify.GetDevicesAsync(token);
		Console.WriteLine(devices.Values.Length);
		for (int i = 0; i < devices.Values.Length; i++) {
			Device device = devices.Values[i];
			Console.WriteLine($"{i + 1}: {device.Name} | {device.Id}");
		}

		int deviceIndex = Tools.GetInt("Choose device: ", new Range(1, devices.Values.Length)) - 1;
		Console.WriteLine(devices.Values[deviceIndex].Name);
		int volume = Tools.GetInt("Set volume: ", new Range(0, 100));
		await spotify.SetVolumeAsync(token, devices.Values[deviceIndex].Id, volume);
		//await spotify.StartPlaybackAsync(token, devices.Values[deviceIndex].Id);
		//Console.WriteLine($"{player.Device.Name}|{player.Device.IsActive}|{player.Device.VolumePercent}%");
	}

	private static (string, string) GetCredentials() {
		string path = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "secrets.txt");
		if (!File.Exists(path)) {
			throw new FileNotFoundException("Missing secrets.txt file");
		}

		string[] lines = File.ReadAllLines(path);

		if (lines.Length < 2) {
			throw new IndexOutOfRangeException("Secrets does not contain enough values");
		}

		string clientId = lines[0];
		string clientSecret = lines[1];

		return (clientId, clientSecret);
	}
}

