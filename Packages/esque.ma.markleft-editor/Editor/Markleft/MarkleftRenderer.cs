using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;
using UnityEngine;
using UnityEditor;

namespace MarkleftEditor
{
    /// <summary>
    /// Reusable Markdown-lite renderer for custom inspectors and editor windows.
    ///
    /// Supported syntax:
    ///   # Header 1
    ///   ## Header 2
    ///   ### Header 3
    ///   #### Header 4
    ///   - item                     (bullet list, dash)
    ///   * item                     (bullet list, circle)
    ///   ![alt](./path/img.png)     (image, path relative to the asset)
    ///   ![alt](/path/img.png)      (image, path relative to the Assets folder)
    ///   [label](https://unity.com) (external link, opens in the OS browser)
    ///   [label](/path/to/asset)    (internal link, pings + selects the asset, path relative to the Assets folder)
    ///   [label](./path/to/asset)   (internal link, pings + selects the asset, path relative to this asset)
    ///   **bold**
    ///   *italic* / _italic_
    ///   `inline monospace`
    /// </summary>
    public static class MarkleftRenderer
    {
        static MarkleftRenderer()
        {
            AssemblyReloadEvents.beforeAssemblyReload += DestroyCachedTextures;
        }

        private static void DestroyCachedTextures()
        {
            if (_codeBackground is not null)
            {
                UnityEngine.Object.DestroyImmediate(_codeBackground);
                _codeBackground = null;
            }
            AssemblyReloadEvents.beforeAssemblyReload -= DestroyCachedTextures;
        }
        
        private static readonly Regex SeparatorRegex = new Regex(@"---(-*)\s*$", RegexOptions.Compiled);

        private static readonly Regex InlineTokenRegex = new Regex(
            @"(?<code>`(?<codetext>.*?)`)"+
            @"|(?<bold>\*\*(?<boldtext>.+?)\*\*)"+
            @"|(?<italic>(?<!\*)\*(?<italictext>[^*\n]+?)\*)"+
            @"|(?<italic2>_(?<italictext2>.+?)_)",
            RegexOptions.Compiled);
        
        private static GUIStyle _h1, _h2, _h3, _h4;
        private static GUIStyle _h1L, _h2L, _h3L, _h4L;
        private static GUIStyle _h1C, _h2C, _h3C, _h4C;
        private static GUIStyle _body, _linkStyle, _code, _codeBlock;
        private static Texture2D _codeBackground = null;
        private static bool _stylesInitialized = false;

        private const float BulletIndent = 12f;  
        
        private static void InitStyles()
        {
            if (_stylesInitialized) return;
            
            _body = new GUIStyle(EditorStyles.label)
            {
                margin = new RectOffset(0, 0, 0, 0),
                padding = new RectOffset(0, 0, 0, 0),
                wordWrap = false
            };
            _linkStyle = new GUIStyle(EditorStyles.linkLabel)
            {
                margin = new RectOffset(0, 0, 0, 0),
                padding = new RectOffset(0, 0, 0, 0),
                wordWrap = false
            };

            var fontsToFind = new[] { "Consolas", "Menlo", "Courier New", "DejaVu Sans Mono", "monospace" };
            var fonts = Font.GetOSInstalledFontNames().Intersect(fontsToFind).ToArray();

            var monoFont = Font.CreateDynamicFontFromOSFont(fonts, _body.fontSize);

            var codebg = EditorGUIUtility.isProSkin ? new Color(1f, 1f, 1f, 0.08f) : new Color(0f, 0f, 0f, 0.6f);
            _codeBackground = new Texture2D(1, 1, TextureFormat.RGBA32, false)
            {
                hideFlags = HideFlags.HideAndDontSave
            };
            _codeBackground.SetPixel(0, 0, codebg);
            _codeBackground.Apply(false, true);
            _code = new GUIStyle(EditorStyles.label)
            {
                font = monoFont,
                wordWrap = false,
                margin = new RectOffset(0, 0, 0, 0),
                padding = new RectOffset(2, 0, 2, 0),
                normal =
                {
                    background = _codeBackground,
                    textColor = _body.normal.textColor
                }
            };
            _codeBlock = new GUIStyle(_code)
            {
                margin = new RectOffset(5, 5, 5, 5),
                padding = new RectOffset(5, 5, 5, 6),
                wordWrap = true,
            };
            
            _h1 = new GUIStyle(EditorStyles.boldLabel)
            {
                fontSize = 20,
                margin = new RectOffset(0, 0, 10, 4),
                wordWrap = false
            };
            _h1L = new GUIStyle(EditorStyles.linkLabel)
            {
                fontSize = 20,
                fontStyle = FontStyle.Bold,
                margin = new RectOffset(0, 0, 10, 4),
                wordWrap = false
            };
            _h1C = new GUIStyle(_code)
            {
                fontSize = 20,
                fontStyle = FontStyle.Bold,
                margin = new RectOffset(0, 0, 10, 4),
            };
            _h2 = new GUIStyle(EditorStyles.boldLabel)
            {
                fontSize = 16,
                margin = new RectOffset(0, 0, 8, 3),
                wordWrap = false
            };
            _h2L = new GUIStyle(EditorStyles.linkLabel)
            {
                fontSize = 16,
                fontStyle = FontStyle.Bold,
                margin = new RectOffset(0, 0, 8, 3),
                wordWrap = false
            };
            _h2C = new GUIStyle(_code)
            {
                fontSize = 16,
                fontStyle = FontStyle.Bold,
                margin = new RectOffset(0, 0, 8, 3),
                wordWrap = false
            };
            _h3 = new GUIStyle(EditorStyles.boldLabel)
            {
                fontSize = 14,
                margin = new RectOffset(0, 0, 6, 2),
                wordWrap = false
            };
            _h3L = new GUIStyle(EditorStyles.linkLabel)
            {
                fontSize = 14,
                fontStyle = FontStyle.Bold,
                margin = new RectOffset(0, 0, 6, 2),
                wordWrap = false
            };
            _h3C = new GUIStyle(_code)
            {
                fontSize = 14,
                fontStyle = FontStyle.Bold,
                margin = new RectOffset(0, 0, 6, 2),
                wordWrap = false
            };
            _h4 = new GUIStyle(EditorStyles.boldLabel)
            {
                fontSize = 12,
                margin = new RectOffset(0, 0, 4, 1),
                wordWrap = false
            };
            _h4L = new GUIStyle(EditorStyles.linkLabel)
            {
                fontSize = 12,
                fontStyle = FontStyle.Bold,
                margin = new RectOffset(0, 0, 4, 1),
                wordWrap = false
            };
            _h4C = new GUIStyle(_code)
            {
                fontSize = 12,
                fontStyle = FontStyle.Bold,
                margin = new RectOffset(0, 0, 4, 1),
                wordWrap = false
            };
            
            _stylesInitialized = true;
        }

        private struct StyleVariants
        {
            public GUIStyle BaseStyle;
            public GUIStyle BoldStyle;
            public GUIStyle ItalicStyle;
        }

        private static GUIStyle StyleVariant(GUIStyle baseStyle, FontStyle fontStyle)
        {
            return new GUIStyle(baseStyle)
            {
                fontStyle = fontStyle switch
                {
                    FontStyle.Italic => baseStyle.fontStyle switch
                    {
                        FontStyle.Bold or FontStyle.BoldAndItalic => FontStyle.BoldAndItalic,
                        _ => FontStyle.Italic
                    },
                    FontStyle.Bold => baseStyle.fontStyle switch
                    {
                        FontStyle.Italic or FontStyle.BoldAndItalic => FontStyle.BoldAndItalic,
                        _ => FontStyle.Bold
                    },
                    FontStyle.BoldAndItalic => FontStyle.BoldAndItalic,
                    _ => baseStyle.fontStyle
                },
            };
        }

        private static StyleVariants MakeStyleVariants(GUIStyle baseStyle)
        {
            return new StyleVariants()
            {
                BaseStyle = baseStyle,
                BoldStyle = StyleVariant(baseStyle, FontStyle.Bold),
                ItalicStyle = StyleVariant(baseStyle, FontStyle.Italic),
            };
        }

        [Flags]
        private enum TokenType: int
        {
            Body = 0,
            Bold =  1 << 0,
            Italic = 1 << 1,
            Link = 1 << 2,
            Code = 1 << 3,
        }

        private struct InlineToken
        {
            public string Text;
            public GUIStyle Style;
            public TokenType Type;
            public string ImageUrl; // null unless it is an image
            public string LinkUrl; // null unless it is a link

            public InlineToken(InlineToken other)
            {
                Text = other.Text;
                Style = other.Style;
                Type = other.Type;
                ImageUrl = other.ImageUrl;
                LinkUrl = other.LinkUrl;
            }
        }

        private static bool IsMenuCall(string url) =>
            url.StartsWith("menu:", StringComparison.Ordinal);
        private static bool IsProjectCall(string url) =>
            url.StartsWith("project:", StringComparison.Ordinal);
        private static bool IsPrefsCall(string url) =>
            url.StartsWith("prefs:", StringComparison.Ordinal);
        private static bool IsStaticMethodCall(string url) =>
            url.StartsWith("static:", StringComparison.Ordinal);
        
        private static bool IsInternalCall(string url) => 
            IsMenuCall(url) || IsProjectCall(url) || IsPrefsCall(url) || IsStaticMethodCall(url);
        
        private static bool IsRemoteURL(string url) =>
            url.StartsWith("http://", StringComparison.Ordinal) 
            || url.StartsWith("https://", StringComparison.Ordinal)
            || url.StartsWith("mailto:", StringComparison.Ordinal);

        private static bool IsNonAsset(string url) => IsRemoteURL(url) || IsInternalCall(url);

        private static void OpenLink(string url, UnityEngine.Object scriptExecContext)
        {
            if (string.IsNullOrEmpty(url)) return;
            if (IsRemoteURL(url))
            {
                Application.OpenURL(url);
                return;
            }

            if (IsMenuCall(url))
            {
                EditorApplication.ExecuteMenuItem(url[5..]);
                return;
            }
            if (IsProjectCall(url))
            {
                SettingsService.OpenProjectSettings(url[8..]);
                return;
            }
            if (IsPrefsCall(url))
            {
                SettingsService.OpenUserPreferences(url[6..]);
                return;
            }

            if (IsStaticMethodCall(url))
            {
                var wholeName = url[7..];
                
                List<Assembly> assemblies = new List<Assembly>();
                if (scriptExecContext is not null)
                {
                    assemblies.Add(scriptExecContext.GetType().Assembly);
                } 
                else
                {
                    assemblies.Add(typeof(EditorUtility).Assembly);
                    assemblies.Add(typeof(UnityEngine.Object).Assembly);
                    assemblies.Add(typeof(MarkleftRenderer).Assembly);
                }
                var splitLocation = wholeName.LastIndexOf('.');
                if (splitLocation == -1)
                {
                    Debug.LogError($"[MarkleftRenderer] Could not resolve method call: {wholeName}");
                    return;
                }
                var className = wholeName[..splitLocation];
                var methodName = wholeName[(splitLocation + 1)..];
                Type t = null;
                foreach (var assembly in assemblies)
                {
                    t = assembly.GetType(className);
                    if (t is not null) break;
                }
                if (t is null)
                {
                    Debug.LogError($"[MarkleftRenderer] Could not resolve type: {className}");
                    return;
                }
                var m = t.GetMethod(methodName, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static);
                if (m is null)
                {
                    Debug.LogError($"[MarkleftRenderer] Could not resolve method: {className}.{methodName}");
                    return;
                }

                m.Invoke(null, new object[] { });
                return;
            }
            
            UnityEngine.Object asset = AssetDatabase.LoadAssetAtPath<UnityEngine.Object>(url);

            if (asset != null)
            {
                EditorGUIUtility.PingObject(asset);
                Selection.activeObject = asset;
            }
            else
            {
                Debug.LogWarning($"[MarkleftRenderer] Could not resolve internal link target: {url}");
            }
        }
        
        /// <summary>
        /// Checks whether the path is already an absolute asset path
        /// </summary>
        /// <param name="assetPath">the asset path to test</param>
        /// <returns>true if absolute (begins in Assets/ or Packages/), else false</returns>
        private static bool IsAbsoluteAssetPath(string assetPath) => 
            assetPath.StartsWith("Assets/", StringComparison.OrdinalIgnoreCase) ||
            assetPath.StartsWith("Packages/",  StringComparison.OrdinalIgnoreCase);

        private static List<string> SplitSegments(string path) => path.Split('/', StringSplitOptions.RemoveEmptyEntries).ToList();

        /// <summary>
        /// Checks the path to see how many segments to always keep
        /// </summary>
        /// <param name="segments">list of path segments</param>
        /// <returns>amount of segments that the relative path resolution cannot navigate past</returns>
        private static int GetRootDepth(List<string> segments)
        {
            if (segments.Count == 0) return -1;
            if (segments[0].Equals("Assets", StringComparison.OrdinalIgnoreCase)) return 1;
            if (segments[0].Equals("Packages", StringComparison.OrdinalIgnoreCase) && segments.Count >= 2) return 2;
            return -1;
        }

        /// <summary>
        /// Resolves a project/package path relative to another asset
        /// </summary>
        /// <param name="link">the relative or absolute path to resolve</param>
        /// <param name="assetPath">the absolute path to resolve the relative path from</param>
        /// <returns>the fully resolved absolute path</returns>
        private static string NormalizeLink(string link, string assetPath)
        {
            if (string.IsNullOrEmpty(link)) return link;
            if (IsNonAsset(link)) return link;
            var normalizedLink = link.Replace('\\', '/').Trim();
            if (string.IsNullOrEmpty(normalizedLink)) return link;
            if (IsAbsoluteAssetPath(normalizedLink)) return normalizedLink;
            var baseSegments = SplitSegments(assetPath.Replace('\\', '/').Trim());
            var rootDepth = GetRootDepth(baseSegments);
            
            if (rootDepth < 0) return link; // assetPath not under Assets/ or Packages/<name>/, can't normalize
            
            var dirSegments = baseSegments.Count > rootDepth ? 
                baseSegments.GetRange(0, baseSegments.Count - 1) :
                new List<string>(baseSegments);

            foreach (var segment in SplitSegments(normalizedLink))
            {
                if (segment == ".") continue;
                if (segment == "..")
                {
                    // never pop past the root prefix (Assets/ or Packages/<name>/)
                    if (dirSegments.Count > rootDepth)
                        dirSegments.RemoveAt(dirSegments.Count - 1);
                    continue;
                }
                dirSegments.Add(segment);
            }
            
            return string.Join("/", dirSegments);
        }

        private static bool TryParseBracketToken(string content, int start, out bool isImage, out string label,
            out string url, out int length)
        {
            isImage = false;
            label = null;
            url = null;
            length = 0;

            var i = start;
            if (i < content.Length && content[i] == '!')
            {
                isImage = true;
                i++;
            }
            if (i >= content.Length || content[i] != '[') return false;

            var labelStart = i + 1;
            var depth = 1;
            i++; // skip past '['
            while (i < content.Length && depth > 0)
            {
                if (content[i] == '[') depth++;
                else if (content[i] == ']') depth--;
                if (depth>0) i++;
            }
            // no matching ']' found on this line
            if (depth != 0) return false;

            var labelEnd = i;
            i++; // skip past ']'
            
            // not a correctly-formed []() or ![]() token
            if (i >= content.Length || content[i] != '(') return false;
            var urlStart = i + 1;
            depth = 1;
            i++; // skip past '('
            while (i < content.Length && depth > 0)
            {
                if (content[i] == '(') depth++;
                else if (content[i] == ')') depth--;
                if (depth > 0) i++;
            }
            if (depth != 0) return false; // no matching ')' found on this line
            var urlEnd = i;
            i++;  // skip past ')'
            
            label = content.Substring(labelStart, labelEnd - labelStart);
            url = content.Substring(urlStart, urlEnd - urlStart);
            length = i - start;
            return true;
        }

        /// <summary>
        /// Scan the string for possible images or links, and if any candidate is found, try a full parse
        /// </summary>
        /// <param name="content">the input string to parse</param>
        /// <param name="start">the starting point to try parsing from</param>
        /// <param name="foundAt">the output location if found</param>
        /// <param name="isImage">true if we found an image here</param>
        /// <param name="label">the label side of the tag</param>
        /// <param name="url">the url side of the tag</param>
        /// <param name="length">the length of the parsed tag</param>
        /// <returns>true if either an image or link is found, false otherwise</returns>
        private static bool FindNextBracketToken(string content, int start, out int foundAt, out bool isImage,
            out string label, out string url, out int length)
        {
            for (int i = start; i < content.Length; i++)
            {
                if (content[i] != '!' && content[i] != '[') continue;
                if (!TryParseBracketToken(content, i, out isImage, out label, out url, out length)) continue;
                foundAt = i;
                return true;
            }
            foundAt = -1;
            isImage = false;
            label = null;
            url = null;
            length = 0;
            return false;
        }

        /// <summary>
        /// Splits each paragraph into word or image chunks, and attaches the style 
        /// </summary>
        /// <param name="content">string to tokenize</param>
        /// <param name="style">default style to apply</param>
        /// <param name="linkStyle">style to apply when drawing links</param>
        /// <param name="codeStyle">style to apply when drawing code</param>
        /// <param name="baseType">base token type (normal text, link, code)</param>
        /// <param name="assetPath">the readme asset's location to resolve asset links against</param>
        /// <returns>list of tokens parsed from the paragraph</returns>
        private static List<InlineToken> Tokenize(string content, StyleVariants style, StyleVariants linkStyle, StyleVariants codeStyle, TokenType baseType, string assetPath)
        {
            var tokens = new List<InlineToken>();

            void AddWords(string text, GUIStyle style, TokenType type, string url = null)
            {
                foreach (var word in text.Split((char[])null, StringSplitOptions.None)) 
                    tokens.Add(new InlineToken { Type = type, Text = word, Style = style, LinkUrl = url });
            }
            
            void AddImage(string alt, string imagePath, GUIStyle style, TokenType type, string url = null)
            {
                tokens.Add(new InlineToken { Type = type, Text = alt, ImageUrl = NormalizeLink(imagePath, assetPath), Style = style, LinkUrl = url });
            }

            InlineToken ApplyLinkUrl(InlineToken token, string url)
            {
                token.LinkUrl = url;
                return token;
            }
            
            int cursor = 0;

            while (cursor < content.Length)
            {
                var regexMatch = InlineTokenRegex.Match(content, cursor);
                var regexMatchSuccess = regexMatch.Length > 0;
                var hasBracket = FindNextBracketToken(content, cursor, out var bracketAt, out var isImage,
                    out var label, out var url, out var bracketLength);

                var parseBracket = hasBracket && (!regexMatchSuccess || bracketAt <= regexMatch.Index);

                if (parseBracket)
                {
                    if (bracketAt > cursor)
                        AddWords(content.Substring(cursor, bracketAt - cursor), style.BaseStyle, baseType);
                    if (isImage)
                    {
                        AddImage(label, url, style.BaseStyle, baseType);
                    }
                    else
                    {
                        url = NormalizeLink(url, assetPath);
                        tokens.AddRange(
                            Tokenize(label, linkStyle, linkStyle, codeStyle, baseType | TokenType.Link, assetPath)
                                .Select((token) => ApplyLinkUrl(token, url)));
                    }
                    cursor = bracketAt + bracketLength;
                } 
                else if (regexMatchSuccess)
                {
                    if (regexMatch.Index > cursor)
                        AddWords(content.Substring(cursor, regexMatch.Index - cursor), style.BaseStyle, baseType);
                    if (regexMatch.Groups["code"].Success)
                        tokens.AddRange(TokenizeCode(regexMatch.Groups["codetext"].Value, MakeStyleVariants(codeStyle.BaseStyle), MakeStyleVariants(codeStyle.BaseStyle), assetPath));
                    else if (regexMatch.Groups["bold"].Success)
                        tokens.AddRange(Tokenize(regexMatch.Groups["boldtext"].Value, MakeStyleVariants(style.BoldStyle), MakeStyleVariants(linkStyle.BoldStyle), codeStyle, baseType | TokenType.Bold, assetPath));
                    else if (regexMatch.Groups["italic"].Success)
                        tokens.AddRange(Tokenize(regexMatch.Groups["italictext"].Value, MakeStyleVariants(style.ItalicStyle), MakeStyleVariants(linkStyle.ItalicStyle), codeStyle, baseType | TokenType.Italic, assetPath));
                    else if (regexMatch.Groups["italic2"].Success)
                        tokens.AddRange(Tokenize(regexMatch.Groups["italictext2"].Value, MakeStyleVariants(style.ItalicStyle), MakeStyleVariants(linkStyle.ItalicStyle), codeStyle, baseType | TokenType.Italic, assetPath));
                    cursor = regexMatch.Index + regexMatch.Length;
                }
                else
                {
                    AddWords(content.Substring(cursor), style.BaseStyle, baseType);
                    break;
                }
            }

            return tokens;
        }

        private static List<InlineToken> TokenizeCode(string content, StyleVariants style, StyleVariants linkStyle, string assetPath)
        {
            var tokens = new List<InlineToken>();
            foreach (var word in content.Split((char[])null, StringSplitOptions.None)) 
                tokens.Add(new InlineToken { Type = TokenType.Code, Text = word, Style = style.BaseStyle });
            return tokens;
        }

        private enum LinePrefixType
        {
            None,
            Circle,
            Dash,
            TaskUnchecked,
            TaskChecked,
        }

        private struct LinePrefix
        {
            public LinePrefixType PrefixType;
            public RangeInt TextRange;
        }

        /// <summary>
        /// Tokenizes and draws a markleft section, wrapping long lines along spaces.
        /// </summary>
        /// <param name="content">the line to tokenize and draw</param>
        /// <param name="baseStyle">base style to apply</param>
        /// <param name="linkStyle">link version of the style to apply</param>
        /// <param name="indent">indentation to apply to this section</param>
        /// <param name="assetPath">path of the current asset to resolve relative paths against</param>
        /// <param name="prefix">glyph to prepend if in a bullet list</param>
        /// <param name="scriptExecContext">static script execution context: usually any C# object instance in the Editor space</param>
        private static void DrawWrappedInline(string content, GUIStyle baseStyle, GUIStyle linkStyle, GUIStyle codeStyle, float indent, string assetPath, UnityEngine.Object scriptExecContext,
            LinePrefix prefix)
        {
            var tokens = Tokenize(content, MakeStyleVariants(baseStyle), MakeStyleVariants(linkStyle), MakeStyleVariants(codeStyle), TokenType.Body, assetPath);
            if (tokens.Count == 0) return;

            const float spacing = 3f;
            const float bulletWidth = 12f;
            var reserved = indent + (prefix.PrefixType != LinePrefixType.None ? bulletWidth + spacing : 0f);
            var availableWidth = Math.Max(60f, EditorGUIUtility.currentViewWidth - 40f - reserved);

            var lineOpen = false;
            var firstLine = true;
            var lineWidth = 0f;

            void BeginLine()
            {
                EditorGUILayout.BeginHorizontal();
                if (indent > 0f) GUILayout.Space(indent);
                switch (prefix.PrefixType)
                {
                    case LinePrefixType.Circle or LinePrefixType.Dash:
                        GUILayout.Label(firstLine? prefix.PrefixType switch
                        {
                            LinePrefixType.Circle => "\u2022",
                            LinePrefixType.Dash => "-",
                            _ => string.Empty
                        } : string.Empty, baseStyle, GUILayout.Width(bulletWidth));
                        break;
                    case LinePrefixType.TaskChecked or LinePrefixType.TaskUnchecked:
                        if (firstLine)
                            GUILayout.Toggle(prefix.PrefixType == LinePrefixType.TaskChecked, string.Empty, GUILayout.Width(bulletWidth + spacing));
                        else
                            GUILayout.Label(string.Empty, baseStyle, GUILayout.Width(bulletWidth + spacing));
                        break;
                }
                lineOpen = true;
                lineWidth = 0f;
                firstLine = false;
            }

            void EndLine()
            {
                if (!lineOpen) return;
                EditorGUILayout.EndHorizontal();
                lineOpen = false;
            }

            Vector2 CalcSizeForToken(InlineToken token, out Texture2D textureIfLoaded)
            {
                if (token.Style is null)
                {
                    textureIfLoaded = null;
                    return new Vector2();
                }

                if (string.IsNullOrEmpty(token.ImageUrl))
                {
                    textureIfLoaded = null;
                }
                else
                {
                    textureIfLoaded = AssetDatabase.LoadAssetAtPath<Texture2D>(token.ImageUrl);
                    if (textureIfLoaded is not null)
                    {
                        float aspect = textureIfLoaded.height > 0 ? (float)textureIfLoaded.width / textureIfLoaded.height : 1f;
                        float maxWidth = EditorGUIUtility.currentViewWidth - 40f;
                        float height = Mathf.Min(200f, maxWidth / aspect);
                        float width = height * aspect;
                        return new Vector2(width, height);
                    }
                }
                var gc = new GUIContent(token.Text);
                return token.Style.CalcSize(gc);
            }

            void DrawTokenNonWrappedLine(InlineToken token, Texture2D textureIfLoaded, Vector2 size)
            {
                if (token.Style is null) return;
                
                if (token.LinkUrl != null)
                {
                    if (string.IsNullOrEmpty(token.ImageUrl))
                    {
                        var gc = new GUIContent(token.Text);
                        if (GUILayout.Button(gc, token.Style, GUILayout.ExpandWidth(false)))
                        {
                            OpenLink(token.LinkUrl, scriptExecContext);
                        }

                        EditorGUIUtility.AddCursorRect(GUILayoutUtility.GetLastRect(), MouseCursor.Link);
                    }
                    else
                    {
                        if (textureIfLoaded is not null)
                        {
                            var gc = new GUIContent(textureIfLoaded, token.Text);
                            if (GUILayout.Button(gc, GUILayout.Width(size.x), GUILayout.Height(size.y), GUILayout.ExpandWidth(false)))
                            {
                                OpenLink(token.LinkUrl, scriptExecContext);
                            }
                            EditorGUIUtility.AddCursorRect(GUILayoutUtility.GetLastRect(), MouseCursor.Link);
                        }
                        else
                        {
                            EditorGUILayout.HelpBox($"Image not found at path: {token.ImageUrl}",
                                MessageType.Warning);
                        }
                    }
                }
                else
                {
                    if (string.IsNullOrEmpty(token.ImageUrl))
                    {
                        if (string.IsNullOrEmpty(token.Text)) return;
                        var gc = new GUIContent(token.Text);
                        GUILayout.Label(gc, token.Style, GUILayout.ExpandWidth(false));
                    }
                    else
                    {
                        if (textureIfLoaded is not null)
                        {
                            var gc = new GUIContent(textureIfLoaded, token.Text)
                            {
                                text = null 
                            };
                            GUILayout.Box(gc, GUILayout.Width(size.x), GUILayout.Height(size.y), GUILayout.ExpandWidth(false));
                        }
                        else
                        {
                            EditorGUILayout.HelpBox($"Image not found at path: {token.ImageUrl}",
                                MessageType.Warning);
                        }
                    }
                }
            }

            var lastToken = new InlineToken()
            {
                ImageUrl = null,
                LinkUrl = null,
                Text = string.Empty,
                Style = null,
            };

            bool FlushPendingToken(bool closeLine = false)
            {
                if (lastToken.Style is null) return false;
                var size = CalcSizeForToken(lastToken, out var loaded);
                if (lineWidth + size.x > availableWidth + 10)
                {
                    // not enough space in the current line, let's close this one and open a new line
                    EndLine();
                    BeginLine();
                    return false;
                }
                else
                {
                    DrawTokenNonWrappedLine(lastToken, loaded, size);
                    if (closeLine)
                    {
                        EndLine();
                        BeginLine();
                    } else {
                        lineWidth += size.x;
                    }
                    return true;
                }
            }
            
            try
            {
                BeginLine();

                foreach (var token in tokens)
                {
                    // if the new token has the same style _and_ the same link url as the last token,
                    // _and_ neither token is an image, we can try accumulating them
                    if (lastToken.Style is not null &&
                        (lastToken.Type == token.Type) &&
                        string.Equals(lastToken.LinkUrl, token.LinkUrl) && 
                        string.IsNullOrEmpty(lastToken.ImageUrl) &&
                        string.IsNullOrEmpty(token.ImageUrl))
                    {
                        var attempt = new InlineToken(lastToken)
                        {
                            Text = $"{lastToken.Text} {token.Text}",
                        };
                        var size = CalcSizeForToken(attempt, out _);
                        if (lineWidth + size.x > availableWidth)
                        {
                            // not enough space in the current line, let's flush the last token,
                            // as it would still fit, and start a new line with the new token
                            if (FlushPendingToken(true))
                            {
                                // Flushed completely, last token is drawn, replace it with current
                                lastToken = token;
                            }
                            else
                            {
                                // Line was still too long for last token, so push the collated token instead
                                lastToken = attempt;
                            }
                        }
                        else
                        {
                            // still enough space, we can try adding a new token in the next iteration
                            lastToken = attempt;
                        }
                    }
                    else
                    {
                        // incompatible token, flush the last token accumulated and start a new one
                        FlushPendingToken();
                        lastToken = token;
                    }
                }
                
                // finish off the last token
                FlushPendingToken();
            }
            finally
            {
                EndLine();
            }
        }
        
        private static void DrawTaskListItem(string content, bool taskFlag, GUIStyle baseStyle, GUIStyle linkStyle, GUIStyle codeStyle, float indent, string assetPath, UnityEngine.Object context) => 
            DrawWrappedInline(content, baseStyle, linkStyle, codeStyle, indent + BulletIndent, assetPath, context, new LinePrefix()
            {
                PrefixType = taskFlag ? LinePrefixType.TaskChecked : LinePrefixType.TaskUnchecked
            });

        private static void DrawDashListItem(string content, GUIStyle baseStyle, GUIStyle linkStyle, GUIStyle codeStyle, float indent, string assetPath, UnityEngine.Object context) => 
            DrawWrappedInline(content, baseStyle, linkStyle, codeStyle, indent + BulletIndent, assetPath, context, new LinePrefix()
            {
                PrefixType = LinePrefixType.Dash,
            });

        private static void DrawBulletListItem(string content, GUIStyle baseStyle, GUIStyle linkStyle, GUIStyle codeStyle, float indent, string assetPath, UnityEngine.Object context) => 
            DrawWrappedInline(content, baseStyle, linkStyle, codeStyle, indent + BulletIndent, assetPath, context, new LinePrefix()
            {
                PrefixType = LinePrefixType.Circle
            });

        private static void DrawParagraph(string content, GUIStyle style, GUIStyle linkStyle, GUIStyle codeStyle, float indent, string assetPath, UnityEngine.Object context) => 
            DrawWrappedInline(content, style, linkStyle, codeStyle, indent, assetPath, context, new LinePrefix());

        /// <summary>
        /// Draws the Markleft string, using <paramref name="assetPath"/> as the base address for asset references, and
        /// <paramref name="scriptExecContext"/> for the assembly to query for static methods
        /// </summary>
        /// <param name="markdown">Markleft file string</param>
        /// <param name="assetPath">base address for relative asset references</param>
        /// <param name="scriptExecContext">object instance to query the assembly for static script methods</param>
        public static void Draw(string markdown, string assetPath, UnityEngine.Object scriptExecContext = null)
        {
            InitStyles();
            
            if (string.IsNullOrEmpty(markdown))
                return;

            var lines = markdown.Replace("\r\n", "\n").Split("\n");

            var inCodeBlock = false;

            var codeBlockIndent = 0;
            var codeBlockLines = new List<string>();

            foreach (var rawLine in lines)
            {
                var line = rawLine.TrimEnd();

                if (string.IsNullOrWhiteSpace(line))
                {
                    GUILayout.Space(4);
                    continue;
                }

                if (SeparatorRegex.IsMatch(line))
                {
                    GUILayout.Space(4);
                    var rect = EditorGUILayout.GetControlRect(false, 1);
                    rect.height = 1;
                    EditorGUI.DrawRect(rect, new Color(0, 0, 0, 1));
                    GUILayout.Space(4);
                    continue;
                }

                if (line.StartsWith("#### ", StringComparison.Ordinal))
                {
                    DrawParagraph(line[5..], _h4, _h4L, _h4C, 0f, assetPath, scriptExecContext);
                    continue;
                }
                if (line.StartsWith("### ", StringComparison.Ordinal))
                {
                    DrawParagraph(line[4..], _h3, _h3L, _h3C, 0f, assetPath, scriptExecContext);
                    continue;
                }
                if (line.StartsWith("## ", StringComparison.Ordinal))
                {
                    DrawParagraph(line[3..], _h2, _h2L, _h2C, 0f, assetPath, scriptExecContext);
                    continue;
                }
                if (line.StartsWith("# ", StringComparison.Ordinal))
                {
                    DrawParagraph(line[2..], _h1, _h1L, _h1C, 0f, assetPath, scriptExecContext);
                    continue;
                }

                string trimmedStart = line.TrimStart();
                float indent = (line.Length == trimmedStart.Length) ? 0f : BulletIndent * (line.Length - trimmedStart.Length + 2) * 0.5f;
                if (trimmedStart.StartsWith("- [ ] ", StringComparison.Ordinal))
                {
                    DrawTaskListItem(trimmedStart[5..], false, _body, _linkStyle, _code, indent, assetPath, scriptExecContext);
                    continue;
                }
                if (trimmedStart.StartsWith("- [X] ", StringComparison.Ordinal) || trimmedStart.StartsWith("- [x] ", StringComparison.Ordinal))
                {
                    DrawTaskListItem(trimmedStart[5..], true, _body, _linkStyle, _code, indent, assetPath, scriptExecContext);
                    continue;
                }
                if (trimmedStart.StartsWith("- ", StringComparison.Ordinal))
                {
                    DrawDashListItem(trimmedStart[2..], _body, _linkStyle, _code, indent, assetPath, scriptExecContext);
                    continue;
                }
                if (trimmedStart.StartsWith("* ", StringComparison.Ordinal))
                {
                    DrawBulletListItem(trimmedStart[2..], _body, _linkStyle, _code, indent, assetPath, scriptExecContext);
                    continue;
                }
                if (trimmedStart.StartsWith("```", StringComparison.Ordinal))
                {
                    if (inCodeBlock)
                    {
                        EditorGUILayout.BeginHorizontal();
                        if (indent > 0f) GUILayout.Space(indent);
                        var gc = new GUIContent(string.Join("\n", codeBlockLines));
                        GUILayout.Label(gc, _codeBlock, GUILayout.ExpandWidth(true));
                        EditorGUILayout.EndHorizontal();
                        codeBlockLines.Clear();
                    }
                    inCodeBlock = !inCodeBlock;
                    codeBlockIndent = line.Length - trimmedStart.Length;
                    var trimmedEnd = trimmedStart.TrimEnd();
                    if (trimmedEnd.Length > 3 && trimmedEnd.EndsWith("```", StringComparison.Ordinal))
                    {
                        inCodeBlock = false;
                    }
                    else if (trimmedEnd.Length == 3)
                    {
                        continue;
                    }
                }
                
                if (inCodeBlock)
                {
                    codeBlockLines.Add(line[codeBlockIndent..]);
                }
                else
                {
                    DrawParagraph(trimmedStart, _body, _linkStyle, _code, indent, assetPath, scriptExecContext);
                }
            }
        }
    }
    
}
