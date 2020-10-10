namespace DCrafting.Textorize
{
    public interface ITextorizer
    {
        string Textorize(in string textToSanitize);
    }
}