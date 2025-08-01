
using Microsoft.Xna.Framework;

namespace MonoMSDF
{
	public class GlyphStyle
	{
		public virtual float Spacing => 0f;
		public virtual Color[] Fill => null;
		public virtual Color[] Stroke => null;
	}

	public struct TagDefinition(string startTag, string endTag, ushort styleId)
	{
		public string StartTag = startTag;
		public string EndTag = endTag;
		public ushort StyleID = styleId;
	}

	public struct StyleRule(string word, ushort styleId)
	{
		public string Word = word;
		public ushort StyleID = styleId;
	}

	public class TextStylizer
	{
		public List<GlyphStyle> Styles = [];
		public WordMatcher WordMatcher = new WordMatcher();
		public TagParser TagParser = new TagParser();

		//private readonly List<StyleRule> _rules = [];
		//private readonly List<TagDefinition> _tags = [];

		public void AddRule(StyleRule rule)
		{
			if (string.IsNullOrWhiteSpace(rule.Word))
			{
				throw new ArgumentNullException("Rule world should not be null or whitespace");
			}
			WordMatcher.AddWord(rule.Word, rule.StyleID);
		}

		public void AddTag(TagDefinition tag)
		{
			if (string.IsNullOrEmpty(tag.StartTag) || string.IsNullOrWhiteSpace(tag.EndTag))
			{
				throw new ArgumentNullException("Both start and end tag have to have valid characters");
			}

			TagParser.AddTag(new TagDefinition(tag.StartTag, tag.EndTag, tag.StyleID));
			//_tags.Add(tag);
		}

		public bool TryMatchWord(ReadOnlySpan<char> input, int start, out ushort length, out ushort styleId)
		{
			return WordMatcher.TryMatchWord(input, start, out length, out styleId);
			//TODO: what to do with this old implementation, this one is case insensitive, which is nice. But much much slower since its a linear search.
			//The above matcher users a trie tree, super fast.
			/*
			foreach (var rule in _rules)
			{
				if (input.Length < start + rule.Word.Length)
				{
					continue;
				}
				var span = input.Slice(start, rule.Word.Length);
				if (rule.Matches(span))
				{
					length = (ushort)rule.Word.Length;
					styleId = rule.Style;
					return true;
				}
			}
			length = 0;
			styleId = 0;
			return false;*/
		}
	}
}
