
namespace ConsoleQuickMenu;
public static class Tools {
	public static int GetInt(string prompt, Range? range) {
		while (true) {
			Console.Write(prompt);
			string? input = Console.ReadLine();
			if (string.IsNullOrEmpty(input)) {
				Console.WriteLine("Can't be a empty");
				continue;
			}

			if (!int.TryParse(input, out int value)) {
				Console.WriteLine("Must be an integer");
				continue;
			}

			if (range == null) {
				return value;
			}

			if (!range.InRage(value)) {
				Console.WriteLine("Out of range");
				continue;
			}

			return value;
		}
	}
}

public class Range {
	public int Start { get; set; }
	public int End { get; set; }

	public Range(int start, int end) {
		Start = start;
		End = end;
	}

	public bool InRage(int value) {
		return Start <= value && value <= End;
	}

}
