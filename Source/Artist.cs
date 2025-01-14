using System.Diagnostics.Contracts;
using System.Text.Json.Serialization;

namespace Source;

public struct Followers {
	[JsonPropertyName("href")]
	public string? Href { get; set; }
	[JsonPropertyName("total")]
	public int Total { get; set; }
}

public struct Artist {
	/*"external_urls": {
		"spotify": "https://open.spotify.com/artist/4Z8W4fKeB5YxbusRsdQVPb"
	}*/

	[JsonPropertyName("followers")]
	public Followers Followers { get; set; }
	[JsonPropertyName("genres")]
	public string[] Generes { get; set; }
	[JsonPropertyName("href")]
	public string Href { get; set; }
	[JsonPropertyName("id")]
	public string Id { get; set; }
	[JsonPropertyName("images")]
	public Image[] Images { get; set; }
	[JsonPropertyName("name")]
	public string Name { get; set; }
	[JsonPropertyName("popularity")]
	public int Popularity { get; set; }
	[JsonPropertyName("type")]
	public string Type { get; set; }
	[JsonPropertyName("uri")]
	public string Uri { get; set; }

}
