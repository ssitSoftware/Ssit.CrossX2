namespace Ssit.CrossX2.Framework.Games.Logic.Stering;

/// <summary>
/// Base for stateless behaviors that need only one shared instance: use <see cref="Instance"/> instead of creating new ones.
/// Any state must be stored per object (GetParameters), never in fields.
/// </summary>
public abstract class SingleSteeringBehavior<TObject, TBehavior> : SteeringBehavior<TObject> where TBehavior : SingleSteeringBehavior<TObject, TBehavior>, new()
{
    public static readonly TBehavior Instance = new();
    
    protected SingleSteeringBehavior()
    {
        // Only the static Instance may be created; any other construction is a bug
        if (Instance != null)
        {
            throw new System.Exception("SingleSteeringBehavior is a singleton");
        }
    }
}