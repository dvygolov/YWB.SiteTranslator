using DeepL;
using System;
using System.Threading.Tasks;
using YWB.SiteTranslator.Helpers;

namespace YWB.SiteTranslator
{
    public class DeeplService
    {
        private string _apiKey;
        public DeeplService()
        {
            Console.Write("Enter your Deepl Api Key:");
            _apiKey = Console.ReadLine();
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

        public async Task<string> TranslateAsync(string text, Language l)
        {
            using (DeepLClient client = new DeepLClient(_apiKey, useFreeApi: true))
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
