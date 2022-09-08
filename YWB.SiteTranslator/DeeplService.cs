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
        private DeepLClient _client;

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
            var useFreeApi = apiKey.EndsWith(":fx");
            _client = new DeepLClient(apiKey, useFreeApi: useFreeApi);
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
            while (true)
            {
                try
                {
                    Translation translation = await _client.TranslateAsync(text, l);
                    Console.WriteLine(translation.DetectedSourceLanguage);
                    Console.WriteLine(translation.Text);
                    return translation.Text;
                }
                catch (Exception exception)
                {
                    Console.WriteLine($"An error occurred: {exception.Message}");
                }
            }
        }
    }
}
