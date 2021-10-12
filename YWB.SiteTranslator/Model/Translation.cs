namespace YWB.SiteTranslator.Model
{
    public class TextItem
    {
        public TextItem() { }
        public TextItem(string text) => Text = text;

        public string Text { get; set; }
        public string Translation { get; set; }
    }
}
