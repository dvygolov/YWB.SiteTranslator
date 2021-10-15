using AngleSharp;
using AngleSharp.Dom;
using AngleSharp.Html.Parser;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using YWB.SiteTranslator.Helpers;
using YWB.SiteTranslator.Model;

namespace YWB.SiteTranslator
{
    public class HtmlProcessor
    {
        private const string Folder = "site";
        private List<string> _ext = new List<string> { "htm", "html", "php" };
        private string _fileName;
        private string _fullPath;
        private int _curRecursionLevel = 0;

        public const int MaxConvertToRecursionDepthLevel = 1000;
        public HtmlProcessor()
        {
            _fullPath = PathHelper.GetFullPath(Folder);
            bool fileExists = false;
            foreach (var ex in _ext)
            {
                if (File.Exists(Path.Combine(_fullPath, $"index.{ex}")))
                {
                    fileExists = true;
                    _fileName = $"index.{ex}";
                    break;
                }
            }
            if (!fileExists)
                throw new FileNotFoundException("Couldn't find website's index file!");
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
            if (!string.IsNullOrEmpty(offerName))
            {
                //saves <a> tag info
                var match = Regex.Match(html, $"(<a[^>]+>){offerName}</a>");
                aStart = match.Groups[1].Value;
                //remove all offer links
                html = Regex.Replace(html, $"(<a[^>]+>){offerName}</a>", offerName);
            }

            var doc = await parser.ParseDocumentAsync(html);
            int i = 0;
            TranslateTextRecursive(doc.DocumentElement.GetRoot(), txt, ref i);
            html = doc.ToHtml();
            if (!string.IsNullOrEmpty(newOfferName))
            {
                //add <a> tag
                html = html.Replace(newOfferName, $"{aStart}{newOfferName}</a>");
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
                        var clean = text.Trim();
                        clean = Regex.Replace(clean, @"\s+", " ");
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
