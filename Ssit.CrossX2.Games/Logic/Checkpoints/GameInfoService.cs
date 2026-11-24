using Ssit.CrossX2.Framework.Services;

namespace Ssit.CrossX2.Framework.Games.Logic.Checkpoints;

/// <summary>
/// Total game time of the save slot being played (<see cref="IGameClock.TotalGameTime"/>) - how long the player has
/// actually played the game in the slot. Unlike the game time (<see cref="IGameClock.Time"/>) it isn't restored from
/// checkpoints: it keeps running when the game is restored after death or continues on another map.
/// <para>
/// Stored apart from the save data, in "GameInfo.json" of the slot directory (<see cref="GameInfoData"/>,
/// <see cref="CheckpointStorage.GameInfoFileName"/>). Written with every save of the game
/// (<see cref="ICheckpointService.Save"/>, <see cref="ICheckpointService.SaveImmediate"/> - map links too), when the
/// player leaves the game (<see cref="End"/>) and when the application is closed during the game.
/// Read only when the game is started from the menu (<see cref="Begin"/>).
/// </para>
/// Registered in the application services - shared by all game instances of the slot.
/// </summary>
public class GameInfoService
{
    private readonly CheckpointStorage _storage;

    /// <summary>
    /// Slot of the game being played - its total game time is counted. Null when no game is played (e.g. in the menu).
    /// </summary>
    public int? Slot { get; private set; }

    /// <summary>
    /// Total game time (seconds) of <see cref="Slot"/>.
    /// </summary>
    public double TotalGameTime { get; private set; }

    /// <param name="windowManager">The total game time is stored when the application is closed during the game.</param>
    public GameInfoService(CheckpointStorage storage, IAppWindowManager windowManager = null)
    {
        _storage = storage;

        if (windowManager is not null)
        {
            windowManager.Closing += OnAppClosing;
        }
    }

    /// <summary>
    /// Starts counting the total game time of the <paramref name="slot"/> from the stored value (0 if there is none,
    /// e.g. a new game in an archived slot). Called when the game is started from the menu - not when it is restored
    /// from a checkpoint during the play.
    /// </summary>
    public void Begin(int slot)
    {
        Slot = slot;
        TotalGameTime = _storage.ReadGameInfo(slot)?.TotalGameTime ?? 0;
    }

    /// <summary>
    /// Adds the played <paramref name="time"/> (seconds) if the <paramref name="slot"/> is the one being played.
    /// Called by the game clock on each update of the game.
    /// </summary>
    public void Add(int slot, double time)
    {
        if (Slot == slot && time > 0)
        {
            TotalGameTime += time;
        }
    }

    /// <summary>
    /// Writes the total game time into the GameInfo file of the <paramref name="slot"/> in the background
    /// (<see cref="CheckpointStorage.WriteGameInfoAsync"/>). Does nothing if the slot isn't the one being played.
    /// </summary>
    public Task Save(int slot)
    {
        if (Slot != slot)
            return Task.CompletedTask;

        return _storage.WriteGameInfoAsync(slot, new GameInfoData { TotalGameTime = TotalGameTime });
    }

    /// <summary>
    /// The player left the game (e.g. to the menu): stores the total game time and stops counting it.
    /// </summary>
    public void End()
    {
        if (Slot is not { } slot)
            return;

        Save(slot);
        Slot = null;
    }

    private void OnAppClosing(WindowClosingEventArgs args)
    {
        if (args.Cancel || Slot is not { } slot)
            return;

        // The application ends right after - wait for the write (and the pending checkpoint writes queued before it).
        if (!Save(slot).Wait(TimeSpan.FromSeconds(5)))
        {
            Console.WriteLine("Writing the game info timed out - the total game time may not be stored.");
        }
    }
}
