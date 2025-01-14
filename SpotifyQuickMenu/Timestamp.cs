using System.Text.Json.Serialization;

namespace SpotifyQuickMenu;

public struct Timestamp {
	[JsonPropertyName("context_uri")]
	public string Context { get; set; }
	[JsonPropertyName("position")]
	public int Position { get; set; }
	[JsonPropertyName("position_ms")]
	public int PositionMs { get; set; }
}
