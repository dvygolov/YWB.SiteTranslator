using DeepL;
using System;
using System.Threading.Tasks;

namespace YWB.SiteTranslator
{
    public class DeeplService
    {
        private string _apiKey;
        private bool _useFreeApi = false;
        public DeeplService()
        {
            Console.Write("Enter your Deepl Api Key:");
            _apiKey = Console.ReadLine();
            _useFreeApi = _apiKey.EndsWith(":fx");
        }

        public async Task<string> TranslateAsync(string text, Language l)
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
