namespace MonoMSDF
{

	public struct WordMatchInfo
	{
		public ushort StyleId;
		public ushort WordLength;
	}

	public class WordMatcher
	{
		private Trie<WordMatchInfo> _trie = new();

		public void AddWord(string word, ushort styleId)
		{
			_trie.Add(word, new WordMatchInfo { StyleId = styleId, WordLength = (ushort)word.Length });
		}

		public bool TryMatchWord(ReadOnlySpan<char> input, int start, out ushort length, out ushort styleId)
		{
			if (_trie.TryMatch(input, start, out int matchedLen, out WordMatchInfo info))
			{
				length = (ushort)matchedLen;
				styleId = info.StyleId;
				return true;
			}
			length = 0;
			styleId = 0;
			return false;
		}
	}
}
