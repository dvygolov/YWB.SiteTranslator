using AngleSharp;
using AngleSharp.Dom;
using AngleSharp.Html.Parser;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using YWB.SiteTranslator.Helpers;
using YWB.SiteTranslator.Model;

namespace YWB.SiteTranslator
{
    public class HtmlProcessor
    {
        public const string Folder = "site";
        private string _fileName;
        private string _fullPath;
        private int _curRecursionLevel = 0;

        public const int MaxConvertToRecursionDepthLevel = 1000;
        public HtmlProcessor()
        {
            _fullPath = PathHelper.GetFullPath(Folder);
            _fileName = GetFileToProcess(_fullPath);
        }

        public static string GetFileToProcess(string path)
        {
            List<string> extensions = new List<string> { "htm", "html", "php" };
            var ext = extensions.FirstOrDefault(ex => File.Exists(Path.Combine(path, $"index.{ex}")));
            if (ext == null)
                throw new FileNotFoundException("Couldn't find website's index file!");
            return $"index.{ext}";
        }

        public async Task<List<TextItem>> ExtractTextAsync(string offerName)
        {
            _curRecursionLevel = 0;
            var res = new List<TextItem>();
            var config = Configuration.Default;
            var context = BrowsingContext.New(config);
            var parser = context.GetService<IHtmlParser>();
            var html = File.ReadAllText(Path.Combine(_fullPath, _fileName));
            html = Regex.Replace(html, $"<a[^>]+>{offerName}</a>", offerName);
            html = Regex.Replace(html, $@"<a[^>]+>\s*<[^>]+>\s*{offerName}\s*</[^>]+>\s*</a>", offerName);

            var doc = await parser.ParseDocumentAsync(html);
            ExtractTextRecursive(doc.DocumentElement.GetRoot(), res);
            return res;
        }

        public async Task<string> TranslateAsync(string offerName, string newOfferName, List<TextItem> txt)
        {
            _curRecursionLevel = 0;
            var config = Configuration.Default;
            var context = BrowsingContext.New(config);
            var parser = context.GetService<IHtmlParser>();
            var html = File.ReadAllText(Path.Combine(_fullPath, _fileName));

            string aStart = string.Empty;
            string aEnd = string.Empty;
            if (!string.IsNullOrEmpty(offerName))
            {
                var r1 = $"(<a[^>]+>){offerName}(</a>)";
                var r2 = $@"(<a[^>]+>\s*<[^>]+>)\s*{offerName}\s*(</[^>]+>\s*</a>)";
                //saves <a> tag info
                var match = Regex.Match(html, r1);
                if (match.Success)
                {
                    aStart = match.Groups[1].Value;
                    aEnd = match.Groups[2].Value;
                }
                else
                {
                    match = Regex.Match(html, r2);
                    if (match.Success)
                    {
                        aStart = match.Groups[1].Value;
                        aEnd = match.Groups[2].Value;
                    }
                }
                //remove all offer links
                html = Regex.Replace(html, r1, offerName);
                html = Regex.Replace(html, r2, offerName);
            }

            var doc = await parser.ParseDocumentAsync(html);
            int i = 0;
            TranslateTextRecursive(doc.DocumentElement.GetRoot(), txt, ref i);
            html = doc.ToHtml();
            if (!string.IsNullOrEmpty(newOfferName))
            {
                //add <a> tag
                html = html.Replace(newOfferName, $"{aStart}{newOfferName}{aEnd}");
            }

            html = PhpHelper.CorrectPhpVariables(html);
            var ext = ".html";
            if (_fileName.EndsWith(".php")) ext = ".php";
            if (ext == ".php") //restore php tags
                html = PhpHelper.RestorePhpTags(html);

            var newName = $"{Path.GetFileNameWithoutExtension(_fileName)}t{ext}";
            File.WriteAllText(Path.Combine(_fullPath, newName), html);
            return newName;
        }

        private void ExtractTextRecursive(INode node, List<TextItem> txt)
        {
            if (_curRecursionLevel > MaxConvertToRecursionDepthLevel)
                throw new ApplicationException("Got to maximum recursion depth level!");

            switch (node.NodeType)
            {
                case NodeType.Comment:
                    // don't output comments
                    break;

                case NodeType.Document:
                    if (node.HasChildNodes)
                    {
                        _curRecursionLevel++;
                        var subRecursionLevel = _curRecursionLevel;
                        foreach (var subnode in node.ChildNodes)
                        {
                            ExtractTextRecursive(subnode, txt);
                            _curRecursionLevel = subRecursionLevel;
                        }
                    }
                    break;

                case NodeType.Text:
                    // script and style must not be output
                    string parentName = node.ParentElement.TagName.ToLowerInvariant();
                    if (parentName == "script" || parentName == "style") break;
                    // get text
                    string text = node.Text();
                    // check the text is meaningful and not a bunch of whitespaces
                    if (text.Trim().Length != 0)
                    {
                        var clean = Regex.Replace(text, @"\s+", " ");
                        txt.Add(new TextItem(clean));
                    }
                    break;
                case NodeType.Element:
                    switch (node.NodeName.ToLowerInvariant())
                    {
                        case "input":
                            var placeholder = ((IElement)node).Attributes["placeholder"];
                            if (placeholder != null)
                            {
                                txt.Add(new TextItem(placeholder.Value));
                            }
                            break;
                    }
                    if (node.HasChildNodes)
                    {
                        _curRecursionLevel++;
                        var subRecursionLevel = _curRecursionLevel;
                        foreach (var subnode in node.ChildNodes)
                        {
                            ExtractTextRecursive(subnode, txt);
                            _curRecursionLevel = subRecursionLevel;
                        }
                    }
                    break;
            }
        }

        private void TranslateTextRecursive(INode node, List<TextItem> txt, ref int i)
        {
            if (_curRecursionLevel > MaxConvertToRecursionDepthLevel)
                throw new ApplicationException("Got to maximum recursion depth level!");

            switch (node.NodeType)
            {
                case NodeType.Comment:
                    // don't output comments
                    break;

                case NodeType.Document:
                    if (node.HasChildNodes)
                    {
                        _curRecursionLevel++;
                        var subRecursionLevel = _curRecursionLevel;
                        foreach (var subnode in node.ChildNodes)
                        {
                            TranslateTextRecursive(subnode, txt, ref i);
                            _curRecursionLevel = subRecursionLevel;
                        }
                    }
                    break;

                case NodeType.Text:
                    // script and style must not be output
                    string parentName = node.ParentElement.TagName.ToLowerInvariant();
                    if (parentName == "script" || parentName == "style") break;
                    // get text
                    string text = node.Text();
                    // check the text is meaningful and not a bunch of whitespaces
                    if (text.Trim().Length != 0)
                    {
                        if (!string.IsNullOrEmpty(txt[i].Translation))
                            node.TextContent = txt[i].Translation;
                        i++;
                    }
                    break;
                case NodeType.Element:
                    switch (node.NodeName.ToLowerInvariant())
                    {
                        case "input":
                            var placeholder = (node as IElement).Attributes["placeholder"];
                            if (placeholder != null)
                            {
                                if (!string.IsNullOrEmpty(txt[i].Translation))
                                    (node as IElement).Attributes["placeholder"].Value = txt[i].Translation;
                                i++;
                            }
                            break;
                    }
                    if (node.HasChildNodes)
                    {
                        _curRecursionLevel++;
                        var subRecursionLevel = _curRecursionLevel;
                        foreach (var subnode in node.ChildNodes)
                        {
                            TranslateTextRecursive(subnode, txt, ref i);
                            _curRecursionLevel = subRecursionLevel;
                        }
                    }
                    break;
            }
        }
    }
}
