﻿using System.Text.Json.Serialization;

namespace Source;
public struct Image {
	[JsonPropertyName("height")]
	public int Height { get; set; }
	[JsonPropertyName("width")]
	public int Width { get; set; }
	[JsonPropertyName("url")]
	public string Url { get; set; }
}
