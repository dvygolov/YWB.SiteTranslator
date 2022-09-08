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
        private string _apiKey;
        private bool _useFreeApi = false;
        public DeeplService()
        {
            var fullPath = Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location), _fileName);
            if (File.Exists(fullPath))
                _apiKey = File.ReadAllText(fullPath).TrimEnd();
            else
            {
                Console.Write("Enter your Deepl Api Key:");
                _apiKey = Console.ReadLine();
            }
            _useFreeApi = _apiKey.EndsWith(":fx");
        }

        public Language SelectLanguage()
        {
            Console.WriteLine("To which language do you want to translate?");
            Console.WriteLine("1.English");
            Console.WriteLine("2.Russian");
            Console.WriteLine("3.Custom");
            var l = YesNoSelector.GetMenuAnswer(3);

            if (l == 3) Console.Write("Enter language name:");

            var language = l switch
            {
                1 => Language.English,
                2 => Language.Russian,
                _ => Enum.Parse<Language>(Console.ReadLine()),
            };
            return language;
        }

        internal async Task<string> FullTranslateAsync(string offerName, List<TextItem> txt, Language language)
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

        private async Task<string> TranslateTextAsync(string text, Language l)
        {
            using (DeepLClient client = new DeepLClient(_apiKey, useFreeApi: _useFreeApi))
            {
                try
                {
                    Translation translation = await client.TranslateAsync(text, l);
                    Console.WriteLine(translation.DetectedSourceLanguage);
                    Console.WriteLine(translation.Text);
                    return translation.Text;
                }
                catch (DeepLException exception)
                {
                    Console.WriteLine($"An error occurred: {exception.Message}");
                    return null;
                }
            }
        }
    }
}
