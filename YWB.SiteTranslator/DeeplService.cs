using DeepL;
using System;
using System.Threading.Tasks;

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
