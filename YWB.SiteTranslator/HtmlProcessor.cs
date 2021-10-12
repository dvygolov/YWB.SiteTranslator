using AngleSharp;
using AngleSharp.Dom;
using AngleSharp.Html.Parser;
using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using YWB.SiteTranslator.Helpers;
using YWB.SiteTranslator.Model;

namespace YWB.SiteTranslator
{
    public class HtmlProcessor
    {
        private const string Folder = "site";
        private string _fileName = "index.html";
        private string _fullPath;
        private int _curRecursionLevel = 0;

        public const int MaxConvertToRecursionDepthLevel = 1000;
        public HtmlProcessor()
        {
            _fullPath = PathHelper.GetFullPath(Folder);
            if (!File.Exists(Path.Combine(_fullPath, _fileName)))
            {
                if (!File.Exists(Path.Combine(_fullPath, "index.htm")))
                    throw new FileNotFoundException("Couldn't find website's index html file!", "index.html");
                else
                    _fileName = "index.htm";
            }
        }

        public async Task<List<TextItem>> ExtractTextAsync()
        {
            _curRecursionLevel = 0;
            var res = new List<TextItem>();
            var config = Configuration.Default;
            var context = BrowsingContext.New(config);
            var parser = context.GetService<IHtmlParser>();
            var doc = await parser.ParseDocumentAsync(File.ReadAllText(Path.Combine(_fullPath, _fileName)));
            ExtractTextRecursive(doc.DocumentElement.GetRoot(), res);
            return res;
        }

        public async Task TranslateAsync(List<TextItem> txt)
        {
            _curRecursionLevel = 0;
            var config = Configuration.Default;
            var context = BrowsingContext.New(config);
            var parser = context.GetService<IHtmlParser>();
            var doc = await parser.ParseDocumentAsync(File.ReadAllText(Path.Combine(_fullPath, _fileName)));
            int i = 0;
            TranslateTextRecursive(doc.DocumentElement.GetRoot(), txt, ref i);
            var newName = Path.GetFileNameWithoutExtension(_fileName) + "t.html";
            File.WriteAllText(Path.Combine(_fullPath, newName), doc.ToHtml());
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
                    string parentName = node.ParentElement.TagName;
                    if (parentName == "script" || parentName == "style") break;
                    // get text
                    string text = node.Text();
                    // check the text is meaningful and not a bunch of whitespaces
                    if (text.Trim().Length != 0) txt.Add(new TextItem(text));
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
                    string parentName = node.ParentElement.TagName;
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
