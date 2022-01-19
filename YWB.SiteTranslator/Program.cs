using DeepL;
using System;
using System.Threading.Tasks;
using YWB.SiteTranslator.Helpers;

namespace YWB.SiteTranslator
{
    class Program
    {
        static async Task Main(string[] args)
        {
            Console.InputEncoding = System.Text.Encoding.UTF8;
            Console.OutputEncoding = System.Text.Encoding.UTF8;
            await CopyrightHelper.ShowAsync();

            Console.WriteLine("What do you want to do?");
            Console.WriteLine("1. Extract website's text to csv");
            Console.WriteLine("2. Replace website's text from csv");
            var action = YesNoSelector.GetMenuAnswer(2);
            Console.Write("Enter your offer's name (as how it is written in the html):");
            var offerName = Console.ReadLine();
            switch (action)
            {
                case 1:
                    {
                        var ex = new HtmlProcessor();
                        var txt = await ex.ExtractTextAsync(offerName);
                        var answer = YesNoSelector.ReadAnswerEqualsYes("Do you want to translate extracted text using DeepL API?");
                        if (answer)
                        {
                            var trans = new DeeplService();
                            var language = trans.SelectLanguage();
                            var newOfferName = await trans.FullTranslateAsync(offerName, txt, language);
                            Console.WriteLine("Translation complete!");
                            answer = YesNoSelector.ReadAnswerEqualsYes("Do you want to translate the website's html with the autotranslated text?");
                            if (answer)
                            {
                                var newName = await ex.TranslateAsync(offerName, newOfferName, txt);
                                Console.WriteLine($@"Tranlation saved to ""{newName}"" file in the website's directory.");
                            }
                        }
                        var csv = new CSVProcessor();
                        csv.Write(txt);
                        Console.WriteLine("Text extracted to \"translation.csv\" file in the program's directory.");
                        break;
                    }
                case 2:
                    {
                        Console.Write("Enter translated offer's name (or Enter if the same):");
                        var newOfferName = Console.ReadLine();
                        if (string.IsNullOrEmpty(newOfferName)) newOfferName = offerName;
                        var csv = new CSVProcessor();
                        var txt = csv.Read();
                        var ex = new HtmlProcessor();
                        var newName = await ex.TranslateAsync(offerName, newOfferName, txt);
                        Console.WriteLine($@"Tranlation saved to ""{newName}"" file in the website's directory.");
                        break;
                    }
            }

            Console.WriteLine("All done. Press any key to exit... and don't forget to donate!");
            Console.ReadKey();
        }
    }
}
