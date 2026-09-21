namespace Ssit.CrossX2.Framework.Games.Logic;

public class GameInterfaces : IGameInterfaces 
{
    public GameInterfaces(IGameInstance instance, IGameDialogsUi dialogs = null)
    {
        Instance = instance;
        Dialogs = dialogs;
    }

    public IGameInstance Instance { get; }
    public IGameDialogsUi Dialogs { get; }
}