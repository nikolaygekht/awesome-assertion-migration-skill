using System;
using System.Collections.Generic;

namespace LabelKit
{
    public enum TokenAlignment
    {
        Left,
        Right,
        Center,
    }

    /// <summary>
    /// One rendered fragment of a label. Tokens form a linked list in render order.
    /// </summary>
    public class LabelToken
    {
        public string Text { get; set; }
        public bool Bold { get; set; }
        public TokenAlignment Alignment { get; set; } = TokenAlignment.Left;
        public int Offset { get; set; }
        public int Line { get; set; }
        public LabelToken Next { get; set; }
    }

    public class LabelComposer
    {
        private LabelToken first;
        private LabelToken last;
        private int line;

        public LabelToken First => first;

        public LabelComposer Add(string text, bool bold = false,
            TokenAlignment alignment = TokenAlignment.Left, int offset = 0)
        {
            if (string.IsNullOrWhiteSpace(text))
            {
                throw new ArgumentException("A label token needs visible text", nameof(text));
            }

            var token = new LabelToken
            {
                Text = text,
                Bold = bold,
                Alignment = alignment,
                Offset = offset,
                Line = line,
            };

            if (first == null)
            {
                first = token;
            }
            else
            {
                last.Next = token;
            }

            last = token;
            return this;
        }

        public LabelComposer NewLine()
        {
            line++;
            return this;
        }

        public IEnumerable<LabelToken> Tokens()
        {
            for (LabelToken t = first; t != null; t = t.Next)
            {
                yield return t;
            }
        }

        public LabelToken Find(Predicate<LabelToken> predicate)
        {
            for (LabelToken t = first; t != null; t = t.Next)
            {
                if (predicate(t))
                {
                    return t;
                }
            }

            return null;
        }
    }
}
