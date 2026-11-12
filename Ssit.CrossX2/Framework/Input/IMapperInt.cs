namespace Ssit.CrossX2.Framework.Input;

/// <summary>
/// Internal extension of an input mapper that exposes mapped axis and button names.
/// </summary>
internal interface IMapperInt
{
    string[] GetMappedButtons();
    string[] GetMappedAxes();
}
