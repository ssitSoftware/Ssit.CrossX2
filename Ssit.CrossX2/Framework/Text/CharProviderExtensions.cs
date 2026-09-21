namespace Ssit.CrossX2.Framework.Text;

public static class CharProviderExtensions
{
    public static string ToString(this ICharProvider provider, int start, int length)
    {
        length = Math.Min(length, provider.Length - start);
        
        var chars = new char[length];
        for (var i = 0; i < length; i++)
        {
            chars[i] = provider[i+start];
        }

        return new string(chars);
    }
}