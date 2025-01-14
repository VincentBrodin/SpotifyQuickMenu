using System.Text.Json.Serialization;

namespace Source;

public struct Token {
	[JsonPropertyName("access_token")]
	public string AccessToken { get; set; }
	[JsonPropertyName("token_type")]
	public string TokenType { get; set; }
	[JsonPropertyName("expires_in")]
	public int ExpiresIn { get; set; }
	public DateTime CreatedAt { get; set; }

	public bool Valid() {
		DateTime expiresAt = CreatedAt.AddSeconds(ExpiresIn);
		return expiresAt > DateTime.Now;
	}
}
