using System.Text.Json.Serialization;

namespace SpotifyQuickMenu;
public struct WebMessage {
	[JsonPropertyName("id")]
	public string Id { get; set; }
	[JsonPropertyName("content")]
	public string Content { get; set; }

	public WebMessage(string id, string content) {
		Id = id;
		Content = content;
	}
}
