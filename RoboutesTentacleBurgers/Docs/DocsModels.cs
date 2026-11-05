using System.Collections.Concurrent;
using System.Net;
using System.Text;
using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Components;

namespace GhostDocs;

// ─────────────────────────────────────────────────────────────
//  MODELS
// ─────────────────────────────────────────────────────────────

public enum BlockKind { Hero, H2, H3, P, Callout, Code, Table, Steps, Cards, List, Checklist, Stats, Diagram, Faq, Roadmap }

public sealed record CodeTab(string Label, string Lang, string Code);

public sealed record Card(string Icon, string Title, string Body, string? Link = null);

public sealed record Block
{
    public BlockKind Kind { get; init; }
    public string Id { get; init; } = "";          // anchor id (H2) or callout title
    public string Text { get; init; } = "";
    public string Tone { get; init; } = "note";    // note | tip | warn | danger | placeholder | easter
    public string[] Items { get; init; } = [];
    public string[] Headers { get; init; } = [];   // table headers / diagram arrow labels
    public string[][] Rows { get; init; } = [];
    public CodeTab[] Tabs { get; init; } = [];
    public Card[] Cards { get; init; } = [];
    public bool Keys { get; init; }                // table: render `x` in first column as <kbd>
    public bool Filter { get; init; }              // table: show a live filter box
}

public sealed record Category(string Id, string Title, string Icon);

public sealed record DocSection(
    string Id, string Category, string Title, string Icon, string Summary,
    string[] Tags, Block[] Blocks, bool Placeholder = false)
{
    public int ReadMinutes
    {
        get
        {
            var chars = Summary.Length;
            foreach (var b in Blocks)
            {
                chars += b.Text.Length + b.Items.Sum(i => i.Length) + b.Rows.Sum(r => r.Sum(c => c.Length));
                chars += b.Tabs.Sum(t => t.Code.Length / 2);
            }
            return Math.Max(1, chars / 900);
        }
    }
}

public sealed record RoadmapItem(string Phase, string Title, bool Done);
public sealed record RoadmapGroup(string Title, string Icon, RoadmapItem[] Items)
{
    public int Done => Items.Count(i => i.Done);
}

// ─────────────────────────────────────────────────────────────
//  INLINE TEXT  (`code`, **bold**)  →  safe HTML
// ─────────────────────────────────────────────────────────────

public static class Md
{
    static readonly Regex Code = new("`([^`]+)`", RegexOptions.Compiled);
    static readonly Regex Bold = new(@"\*\*(.+?)\*\*", RegexOptions.Compiled);

    public static MarkupString Inline(string s, bool kbd = false)
    {
        var enc = WebUtility.HtmlEncode(s);
        enc = Code.Replace(enc, kbd ? "<kbd>$1</kbd>" : "<code>$1</code>");
        enc = Bold.Replace(enc, "<strong>$1</strong>");
        return new MarkupString(enc);
    }
}

// ─────────────────────────────────────────────────────────────
//  TINY SYNTAX HIGHLIGHTER  (C#, Java, C++, HLSL, JSON, Razor, JS, bash)
//  Output is HTML-encoded token spans:  tok-kw tok-ty tok-fn tok-str tok-num tok-com tok-key tok-pun tok-tag tok-dir
// ─────────────────────────────────────────────────────────────

public static class CodeHighlighter
{
    static readonly ConcurrentDictionary<(string, string), MarkupString> Cache = new();

    static readonly Dictionary<string, HashSet<string>> Keywords = new()
    {
        ["csharp"] = Set("abstract as async await base bool break byte case catch char checked class const continue decimal default delegate do double else enum event explicit extern false finally fixed float for foreach goto if implicit in int interface internal is lock long namespace new null object operator out override params private protected public readonly record ref return sbyte sealed short sizeof static string struct switch this throw true try typeof uint ulong unchecked unsafe ushort using var virtual void volatile while get set init required with when yield partial global"),
        ["java"] = Set("abstract assert boolean break byte case catch char class const continue default do double else enum extends final finally float for goto if implements import instanceof int interface long native new null package private protected public return short static strictfp super switch synchronized this throw throws transient true false try var void volatile while record sealed permits yield"),
        ["cpp"] = Set("alignas auto bool break case catch char class const constexpr continue default delete do double else enum explicit export extern false float for friend goto if inline int long mutable namespace new noexcept nullptr operator override private protected public return short signed sizeof static struct switch template this throw true try typedef typename union unsigned using virtual void volatile while final"),
        ["hlsl"] = Set("cbuffer struct return if else for while do break continue discard register static const in out inout uniform float float2 float3 float4 float4x4 float3x3 int uint bool void Texture2D SamplerState Texture2DArray TextureCube"),
        ["js"] = Set("async await break case catch class const continue default delete do else export extends false finally for function if import in instanceof let new null return static super switch this throw true try typeof undefined var void while yield window"),
        ["json"] = Set("true false null"),
        ["bash"] = Set("dotnet cd cp mkdir git npm if then fi echo"),
        ["razor"] = Set("await async bool class else false foreach if int new null private public return string this true var void while using inject code page"),
    };

    static HashSet<string> Set(string words) => new(words.Split(' '));

    const string Common =
        @"(?<com>//[^\n]*|/\*[\s\S]*?\*/)" +
        @"|(?<str>@?""(?:[^""\\\n]|\\.)*""|'(?:[^'\\\n]|\\.)')" +
        @"|(?<num>\b0x[0-9A-Fa-f]+\b|\b\d+(?:\.\d+)?[fFdDuUlL]*\b)";

    static readonly Regex CLike = new(
        @"(?<dir>^[ \t]*#[ \t]*\w+)|" + Common + @"|(?<w>[A-Za-z_][A-Za-z0-9_]*)|(?<pun>[{}()\[\];,.:<>=+\-*/!&|?~^%])",
        RegexOptions.Compiled | RegexOptions.Multiline);

    static readonly Regex Razor = new(
        @"(?<com><!--[\s\S]*?-->|//[^\n]*)|(?<str>""(?:[^""\\\n]|\\.)*"")|(?<tag></?[A-Za-z][\w.\-]*|/?>)|(?<dir>@[A-Za-z]+)|(?<num>\b\d+(?:\.\d+)?\b)|(?<w>[A-Za-z_][A-Za-z0-9_]*)|(?<pun>[{}()\[\];,.:=+\-*/!&|?])",
        RegexOptions.Compiled);

    static readonly Regex Bash = new(
        @"(?<com>#[^\n]*)|(?<str>""[^""\n]*""|'[^'\n]*')|(?<w>[A-Za-z_][A-Za-z0-9_\-]*)|(?<pun>[|&;<>=/\\.])",
        RegexOptions.Compiled);

    public static MarkupString Highlight(string code, string lang) =>
        Cache.GetOrAdd((lang, code), k => new MarkupString(Render(k.Item2, k.Item1)));

    static string Render(string code, string lang)
    {
        lang = lang.ToLowerInvariant();
        if (lang is "text" or "txt" or "") return WebUtility.HtmlEncode(code);

        var rx = lang switch { "razor" or "html" => Razor, "bash" or "sh" => Bash, _ => CLike };
        var kw = Keywords.GetValueOrDefault(lang == "html" ? "razor" : lang) ?? [];
        var sb = new StringBuilder(code.Length * 2);
        int last = 0;

        foreach (Match m in rx.Matches(code))
        {
            if (m.Index > last) sb.Append(WebUtility.HtmlEncode(code[last..m.Index]));
            last = m.Index + m.Length;
            var text = WebUtility.HtmlEncode(m.Value);

            string? cls = null;
            if (m.Groups["com"].Success) cls = "com";
            else if (m.Groups["str"].Success)
            {
                cls = "str";
                if (lang == "json")
                {
                    int j = last;
                    while (j < code.Length && (code[j] == ' ' || code[j] == '\t')) j++;
                    if (j < code.Length && code[j] == ':') cls = "key";
                }
            }
            else if (m.Groups["num"].Success) cls = "num";
            else if (m.Groups["dir"].Success && m.Value.Trim().Length > 0) cls = "dir";
            else if (m.Groups["tag"].Success) cls = "tag";
            else if (m.Groups["pun"].Success) cls = "pun";
            else if (m.Groups["w"].Success)
            {
                var w = m.Value;
                if (kw.Contains(w)) cls = "kw";
                else if (last < code.Length && code[last] == '(') cls = "fn";
                else if (lang != "bash" && lang != "json" && char.IsUpper(w[0])) cls = "ty";
            }

            if (cls is null) sb.Append(text);
            else sb.Append("<span class=\"tok-").Append(cls).Append("\">").Append(text).Append("</span>");
        }
        if (last < code.Length) sb.Append(WebUtility.HtmlEncode(code[last..]));
        return sb.ToString();
    }
}
