using System.Globalization;

namespace Ssit.CrossX2.Framework.Xml;

public static class XNodeExtensions
{
    public static void AddAttribute<T>(this XNode node, string name, T value) where T : struct, IConvertible => node.AddAttribute(name, value.ToString(CultureInfo.InvariantCulture));
    public static void AddAttribute(this XNode node, string name, IConvertible value) => node.AddAttribute(name, value.ToString(CultureInfo.InvariantCulture));
}