namespace Ssit.CrossX2.Framework.Games.Logic.Checkpoints;

/// <summary>
/// File stored by <see cref="CheckpointStorage"/> - the fields it validates when the file is read.
/// </summary>
internal interface ICheckpointFile
{
    int Version { get; }
    
    string MapPath { get; }
}
