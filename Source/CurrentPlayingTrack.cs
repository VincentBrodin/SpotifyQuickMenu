using System.Text.Json.Serialization;

namespace Source;

public struct Context {
	[JsonPropertyName("type")]
	public string Type { get; set; }
	[JsonPropertyName("href")]
	public string Href { get; set; }
	[JsonPropertyName("uri")]
	public string Uri { get; set; }
}

public struct Actions {
	[JsonPropertyName("interrupting_playback")]
	public bool InterruptingPlayback { get; set; }
	[JsonPropertyName("pausing")]
	public bool Pausing { get; set; }
	[JsonPropertyName("resuming")]
	public bool Resuming { get; set; }
	[JsonPropertyName("seeking")]
	public bool Seeking { get; set; }
	[JsonPropertyName("skipping_next")]
	public bool SkippingNext { get; set; }
	[JsonPropertyName("skipping_prev")]
	public bool SkippingPrev { get; set; }
	[JsonPropertyName("toggling_repeat_context")]
	public bool TogglingRepeatContext { get; set; }
	[JsonPropertyName("toggling_shuffle")]
	public bool TogglingShuffle { get; set; }
	[JsonPropertyName("toggling_repeat_track")]
	public bool TogglingRepeatTrack { get; set; }
	[JsonPropertyName("transferring_playback")]
	public bool TransferringPlayback { get; set; }
}
public struct Item {
	[JsonPropertyName("album")]
	public Album Album { get; set; }
	[JsonPropertyName("artists")]
	public Artist[] Artists { get; set; }
	[JsonPropertyName("available_markets")]
	public string[] AvailableMarkets { get; set; }
	[JsonPropertyName("disc_number")]
	public int DiscNumber { get; set; }
	[JsonPropertyName("duration_ms")]
	public int DurationMs { get; set; }
	[JsonPropertyName("explicit")]
	public bool Explicit { get; set; }
	[JsonPropertyName("href")]
	public string Href { get; set; }
	[JsonPropertyName("id")]
	public string Id { get; set; }
	[JsonPropertyName("is_playable")]
	public bool IsPlayable { get; set; }
	[JsonPropertyName("name")]
	public string Name { get; set; }
	[JsonPropertyName("popularity")]
	public int Popularity { get; set; }
	[JsonPropertyName("preview_url")]
	public string PreviewUrl { get; set; }
	[JsonPropertyName("track_number")]
	public int TrackNumber { get; set; }
	[JsonPropertyName("type")]
	public string Type { get; set; }
	[JsonPropertyName("uri")]
	public string Uri { get; set; }
	[JsonPropertyName("is_local")]
	public bool IsLocal { get; set; }
}

public struct CurrentPlayingTrack {
	[JsonPropertyName("device")]
	public Device Device { get; set; }
	[JsonPropertyName("repeat_state")]
	public string RepeatState { get; set; }
	[JsonPropertyName("shuffle_state")]
	public bool ShuffleState { get; set; }
	[JsonPropertyName("context")]
	public Context Context { get; set; }
	//[JsonPropertyName("timestamp")]
	//public int Timestamp { get; set; }
	[JsonPropertyName("progress_ms")]
	public int ProgressMs { get; set; }
	[JsonPropertyName("is_playing")]
	public bool IsPlaying { get; set; }
	[JsonPropertyName("item")]
	public Item Item { get; set; }
	[JsonPropertyName("currently_playing_type")]
	public string CurrentlyPlayingType { get; set; }
	[JsonPropertyName("actions")]
	public Actions Actions { get; set; }
}
