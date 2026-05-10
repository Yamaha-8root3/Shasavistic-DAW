using Microtone.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Microtone.Services.Parser
{
    internal class HarmononymParser
    {
        /// <summary>単一ハーモニム文字列をHarmonographに変換する。</summary>
        public static ParseResult<Harmonograph> TryParseToHarmonograph(string input)
        {
            if (string.IsNullOrEmpty(input))
                return ParseResult<Harmonograph>.Fail("空文字列です", 0);

            int pos = 0;
            var stem = TryMatchStem(input, pos);
            if (stem is null)
                return ParseResult<Harmonograph>.Fail($"認識できない先頭形: '{Peek(input, pos)}'", pos);

            if (stem.Dim == 0)
                return ParseResult<Harmonograph>.Ok(new Harmonograph([]));

            var result = new Harmonograph([]);
            result.Add(stem.Dim, stem.Count);
            pos += stem.Text.Length;

            while (pos < input.Length)
            {
                var suffix = TryMatchSuffixWithLookahead(input, pos);
                if (suffix is null)
                    return ParseResult<Harmonograph>.Fail(result, $"認識できない語尾形: '{Peek(input, pos)}'", pos);
                result.Add(suffix.Dim, suffix.Count);
                pos += suffix.Text.Length;
            }

            return ParseResult<Harmonograph>.Ok(result);
        }

        /// <summary>複数ハーモニムの連続文字列をList<Harmonograph></Harmonograph>に変換する。</summary>
        public static ParseResult<List<Harmonograph>> TryParseToHarmonographs(string input)
        {
            if (string.IsNullOrEmpty(input))
                return ParseResult<List<Harmonograph>>.Fail("空文字列です", 0);

            var result = new List<Harmonograph>();
            int pos = 0;

            while (pos < input.Length)
            {
                var stem = TryMatchStem(input, pos);
                if (stem is null)
                    return ParseResult<List<Harmonograph>>.Fail(result, $"認識できない先頭形: '{Peek(input, pos)}'", pos);

                pos += stem.Text.Length;

                if (stem.Dim == 0)
                {
                    result.Add(new Harmonograph([]));
                    continue;
                }

                var current = new Harmonograph([]);
                current.Add(stem.Dim, stem.Count);

                // Suffixを繰り返し消費（次のStemが始まるまで）
                while (pos < input.Length)
                {
                    var suffix = TryMatchSuffixWithLookahead(input, pos);
                    if (suffix is null) break;
                    current.Add(suffix.Dim, suffix.Count);
                    pos += suffix.Text.Length;
                }

                result.Add(current);
            }

            return ParseResult<List<Harmonograph>>.Ok(result);
        }

        // ----------------------------------------------------------------

        private static StemEntry? TryMatchStem(string input, int pos)
        {
            var span = input.AsSpan(pos);
            // AllStemsは長い順ソート済みなので最初の一致が最長一致
            foreach (var entry in HarmononymTable.AllStems)
                if (span.StartsWith(entry.Text, StringComparison.OrdinalIgnoreCase))
                    return entry;
            return null;
        }

        /// <summary>
        /// Suffixを試みる。採用後の残り文字列が有効な続きになるか先読みして判断する。
        /// 有効な続き = 終端 or StemまたはSuffixで始まる
        /// </summary>
        private static SuffixEntry? TryMatchSuffixWithLookahead(string input, int pos)
        {
            var span = input.AsSpan(pos);
            foreach (var entry in HarmononymTable.AllSuffixes)
            {
                if (!span.StartsWith(entry.Text, StringComparison.OrdinalIgnoreCase))
                    continue;

                int nextPos = pos + entry.Text.Length;

                // 終端なら無条件採用
                if (nextPos >= input.Length)
                    return entry;

                // 残りがStemまたはSuffixで始まるなら採用
                if (TryMatchStem(input, nextPos) is not null ||
                    TryMatchSuffixWithLookahead(input, nextPos) is not null)
                    return entry;

                // 先読み失敗 → 次の候補へ
            }
            return null;
        }

        private static string Peek(string input, int pos, int len = 6) =>
            input[pos..System.Math.Min(pos + len, input.Length)];
    }
}
