using System;
using System.Collections.Generic;
using System.Text;

namespace Microtone.Services.Parser
{
    /// <summary>先頭形エントリ（先頭トークンのマッチに使用）。</summary>
    internal record StemEntry(int Dim, int Count, string Text);

    /// <summary>語尾形エントリ（2番目以降のトークンのマッチに使用）。</summary>
    internal record SuffixEntry(int Dim, int Count, string Text);
    //UNUSED
    //internal enum TokenType { Stem, Suffix }
    //internal record TokenEntry(TokenType Type, int Dim, int Count, string Text);
    internal static class HarmononymTable
    {
        // ----------------------------------------------------------------
        // 接頭辞テーブル: Count → prefix文字列（Count=3〜12）
        // ----------------------------------------------------------------
        public static readonly IReadOnlyDictionary<int, string> Prefixes =
            new Dictionary<int, string>
            {
                [3] = "tra",
                [4] = "cva",
                [5] = "da",
                [6] = "tui",
                [7] = "sa",
                [8] = "chla",
                [9] = "yu",
                [10] = "xy",
                [11] = "nui",
                [12] = "ky",
            };

        // ----------------------------------------------------------------
        // 専用名（×1）
        // ----------------------------------------------------------------
        private static readonly StemEntry[] Spec1Stems =
        [
            new(0, 0, "Ah"),
            new(1, 1, "Yh"),
            new(1, -1, "Uh"),
            new(2, 1, "Chy"),
            new(2, -1, "Fu"),
            new(3, 1, "Ly"),
            new(3, -1, "Su"),
            new(4, 1, "My"),
            new(4, -1, "Pu"),
            new(5, 1, "Zy"),
            new(5, -1, "Tschu"),
        ];

        private static readonly SuffixEntry[] Spec1Suffixes =
        [
            new(1, 1, "yh"),
            new(1, -1, "uh"),
            new(2, 1, "chi"),
            new(2, -1, "f"),
            new(3, 1, "li"),
            new(3, -1, "s"),
            new(4, 1, "mi"),
            new(4, -1, "p"),
            new(5, 1, "zi"),
            new(5, -1, "k"),
    ];

        // ----------------------------------------------------------------
        // 専用名（×2）
        // ----------------------------------------------------------------
        private static readonly StemEntry[] Spec2Stems =
        [
            new(2, 2, "Scy"),
            new(2, -2, "Schu"),
            new(3, 2, "Dry"),
            new(3, -2, "Sru"),
            new(4, 2, "Mry"),
            new(4, -2, "Pru"),
            new(5, 2, "Zry"),
            new(5, -2, "Kru"),
    ];

        private static readonly SuffixEntry[] Spec2Suffixes =
        [
            new(2, 2, "sci"),
            new(2, -2, "sch"),
            new(3, 2, "dri"),
            new(3, -2, "sr"),
            new(4, 2, "mri"),
            new(4, -2, "pr"),
            new(5, 2, "zri"),
            new(5, -2, "kr"),
    ];

        // ----------------------------------------------------------------
        // 専用名（×3）
        // ----------------------------------------------------------------
        private static readonly StemEntry[] Spec3Stems =
        [
            new(2, 3, "Xcy"),
            new(2, -3, "Ju"),
            new(3, 3, "Drvy"),
            new(3, -3, "Srvu"),
            new(4, 3, "Mrvy"),
            new(4, -3, "Prvu"),
            new(5, 3, "Zrvy"),
            new(5, -3, "Krvu"),
    ];

        private static readonly SuffixEntry[] Spec3Suffixes =
        [
            new(2, 3, "xci"),
            new(2, -3, "j"),
            new(3, 3, "drvi"),
            new(3, -3, "srv"),
            new(4, 3, "mrvi"),
            new(4, -3, "prv"),
            new(5, 3, "zrvi"),
            new(5, -3, "krv"),
    ];

        // ----------------------------------------------------------------
        // 公開テーブル（長い順ソート済み）
        // 専用名を接頭辞形より優先するため専用名を先に追加してからソート
        // ----------------------------------------------------------------

        public static readonly IReadOnlyList<StemEntry> AllStems = BuildStems();
        public static readonly IReadOnlyList<SuffixEntry> AllSuffixes = BuildSuffixes();

        // ×1 専用名（接頭辞生成のベース）への参照用
        public static readonly IReadOnlyList<StemEntry> Base1Stems = Spec1Stems;
        public static readonly IReadOnlyList<SuffixEntry> Base1Suffixes = Spec1Suffixes;

        private static List<StemEntry> BuildStems()
        {
            var list = new List<StemEntry>();

            // 専用名（優先）
            list.AddRange(Spec3Stems);
            list.AddRange(Spec2Stems);
            list.AddRange(Spec1Stems);

            // 接頭辞形（×3〜×12）— 解釈用代替形
            foreach (var b in Spec1Stems)
            {
                if (b.Dim == 0) continue; // Ah は接頭辞形なし
                foreach (var (count, pfx) in Prefixes)
                {
                    var raw = pfx + b.Text.ToLower();
                    var text = char.ToUpper(raw[0]) + raw[1..];
                    list.Add(new(b.Dim, count, text));
                }
            }

            // 長い順
            list.Sort((a, b) => b.Text.Length - a.Text.Length);
            return list;
        }

        private static List<SuffixEntry> BuildSuffixes()
        {
            var list = new List<SuffixEntry>();

            // 専用名（優先）
            list.AddRange(Spec3Suffixes);
            list.AddRange(Spec2Suffixes);
            list.AddRange(Spec1Suffixes);

            // 接頭辞形（×3〜×12）
            foreach (var b in Spec1Suffixes)
            {
                foreach (var (count, pfx) in Prefixes)
                    list.Add(new(b.Dim, count, pfx + b.Text));
            }

            // 長い順
            list.Sort((a, b) => b.Text.Length - a.Text.Length);
            return list;
        }
    }
}
