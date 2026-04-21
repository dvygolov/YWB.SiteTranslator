using DeepL;
using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Threading.Tasks;
using YWB.SiteTranslator.Helpers;
using YWB.SiteTranslator.Model;

namespace YWB.SiteTranslator
{
    public class DeeplService
    {
        private const string _fileName = "deepl.txt";
        private readonly Translator _client;

        public DeeplService()
        {
            var fullPath = Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location), _fileName);
            string apiKey;

            if (File.Exists(fullPath))
                apiKey = File.ReadAllText(fullPath).TrimEnd();
            else
            {
                Console.Write("Enter your Deepl Api Key:");
                apiKey = Console.ReadLine();
            }
            var options = new TranslatorOptions
            {
                MaximumNetworkRetries = 5,
                PerRetryConnectionTimeout = TimeSpan.FromSeconds(10),
            };
            _client = new Translator(apiKey, options);
        }

        public async Task<string> SelectLanguageAsync()
        {
            var targetLanguages = await _client.GetTargetLanguagesAsync();
            var selected = SelectHelper.Select(targetLanguages, l => l.Name);
            return selected.Code;
        }

        internal async Task TranslateWebsiteFileAsync(FileInfo fi, FileInfo fo, string language)
        {
            var extension = fi.Extension.ToLowerInvariant();
            if (extension == ".html" || extension == ".htm" || extension == ".txt")
            {
                await TranslateMarkupFileAsync(fi, fo, language, extension);
                return;
            }

            await _client.TranslateDocumentAsync(fi, fo, null, language);
        }

        internal async Task<string> TranslateExtractedTextAsync(string offerName, List<TextItem> txt, string language)
        {
            var newOfferName = offerName;
            var answer = YesNoSelector.ReadAnswerEqualsYes("Do you want to change the offer in the autotranslated text?");
            if (answer)
            {
                Console.Write("Enter new offer name:");
                newOfferName = Console.ReadLine();
            }
            foreach (var ti in txt)
            {
                if (ti.Text.Length < 2) continue;
                var tt = ti.Text;
                if (!string.IsNullOrEmpty(offerName) && !string.IsNullOrEmpty(newOfferName))
                    tt = tt.Replace(offerName, newOfferName);
                ti.Translation = await TranslateTextAsync(tt, language);
            }
            return newOfferName;
        }

        private async Task<string> TranslateTextAsync(string text, string l)
        {
            while (true)
            {
                try
                {
                    var translation = await _client.TranslateTextAsync(text, null, l);
                    Console.WriteLine(translation.DetectedSourceLanguageCode);
                    Console.WriteLine(translation.Text);
                    return translation.Text;
                }
                catch (Exception exception)
                {
                    Console.WriteLine($"An error occurred: {exception.Message}");
                }
            }
        }

        private async Task TranslateMarkupFileAsync(FileInfo fi, FileInfo fo, string language, string extension)
        {
            var sourceText = await File.ReadAllTextAsync(fi.FullName);
            var options = new TextTranslateOptions
            {
                PreserveFormatting = true,
                TagHandling = extension == ".txt" ? null : "html",
            };
            var translated = await _client.TranslateTextAsync(sourceText, null, language, options);
            await File.WriteAllTextAsync(fo.FullName, translated.Text);
            Console.WriteLine($@"Translation saved to ""{fo.FullName}"".");
        }
    }
}
