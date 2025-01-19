namespace Source;
public class Secrets {
	public string ClientId { get; set; } = string.Empty;
	private const string ClientIdFormat = "SPOTIFY_CLIENT_ID";
	public string ClientSecret { get; set; } = string.Empty;
	private const string ClientSecretsFormat = "SPOTIFY_CLIENT_SECRET";
	public string RedirectUri { get; set; } = string.Empty;
	private const string RedirectUriFormat = "SPOTIFY_REDIRECT_URI";

	public static Secrets? LoadFromFile(string filePath) {
		using FileStream stream = File.OpenRead(filePath);
		string[] lines = File.ReadAllLines(filePath);
		stream.Close();

		string? clientId = null;
		string? clientSecrets = null;
		string? redirectUri = null;
		foreach (string line in lines) {
			if (BeginsWith(line, ClientIdFormat)) {
				clientId = RemoveBegining(line, ClientIdFormat);
			}
			else if (BeginsWith(line, ClientSecretsFormat)) {
				clientSecrets = RemoveBegining(line, ClientSecretsFormat);
			}
			else if (BeginsWith(line, RedirectUriFormat)) {
				redirectUri = RemoveBegining(line, RedirectUriFormat);
			}
		}

		if (clientId == null || clientSecrets == null || redirectUri == null) {
			return null;
		}

		return new Secrets() {
			ClientId = clientId,
			ClientSecret = clientSecrets,
			RedirectUri = redirectUri
		};
	}

	private static bool BeginsWith(string input, string compare) {
		if (input.Length < compare.Length) {
			return false;
		}
		string result = input[..(compare.Length)];
		return result == compare;
	}

	private static string RemoveBegining(string input, string begining) {
		return input[(begining.Length + 1)..];
	}


}
