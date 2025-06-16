namespace MonoMSDF
{
	public class TagParser
	{
		private Trie<TagDefinition> _startTagTrie = new();

		public void AddTag(TagDefinition tag)
		{
			_startTagTrie.Add(tag.StartTag, tag);
		}

		public bool TryParseTag(ReadOnlySpan<char> input, int start, out ushort tagLength, out ushort styleId, out ushort startTagLength, out ushort endTagLength)
		{
			startTagLength = 0;
			endTagLength = 0;

			if (!_startTagTrie.TryMatch(input, start, out int matchedLen, out TagDefinition tag))
			{
				tagLength = 0;
				styleId = 0;
				return false;
			}

			int contentStart = start + matchedLen;
			// Search for end tag from contentStart
			for (int i = contentStart; i <= input.Length - tag.EndTag.Length; i++)
			{
				if (input.Slice(i, tag.EndTag.Length).SequenceEqual(tag.EndTag))
				{
					tagLength = (ushort)(i + tag.EndTag.Length - start);
					styleId = tag.StyleID;

					// Add removal ranges for start and end tags

					startTagLength = (ushort)tag.StartTag.Length;
					endTagLength = (ushort)tag.EndTag.Length;
					return true;
				}
			}

			tagLength = 0;
			styleId = 0;
			return false;
		}
	}
}
