namespace MonoMSDF
{
	public class Trie<TValue>
	{
		private class TrieNode
		{
			public Dictionary<char, TrieNode> Children = new();
			public TValue Value = default;
			public bool HasValue = false;
		}

		private readonly TrieNode _root = new();

		// Add a key (word/tag) with associated value
		public void Add(string key, TValue value)
		{
			var node = _root;
			foreach (var ch in key)
			{
				if (!node.Children.TryGetValue(ch, out var child))
				{
					child = new TrieNode();
					node.Children[ch] = child;
				}
				node = child;
			}
			node.Value = value;
			node.HasValue = true;
		}

		// Try to match starting from input[start], returning the longest matched value if any
		public bool TryMatch(ReadOnlySpan<char> input, int start, out int matchedLength, out TValue matchedValue)
		{
			var node = _root;
			matchedLength = 0;
			matchedValue = default;

			for (int i = start; i < input.Length; i++)
			{
				if (!node.Children.TryGetValue(input[i], out node))
					break;

				if (node.HasValue)
				{
					matchedLength = i - start + 1;
					matchedValue = node.Value;
				}
			}

			return matchedLength > 0;
		}
	}
}
