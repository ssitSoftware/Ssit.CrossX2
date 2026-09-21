using Ssit.CrossX2.Framework.Games.Physics;

namespace Ssit.CrossX2.Framework.Games.Logic.Objects;

public class OperatorModule(ICollider detector)
{
    public ILogicOperable LogicOperable { get; private set; }
    public INpcCharacter NpcCharacter { get; private set; }
    public IPushable Pushable { get; private set; }
    
    public void Check(bool facingLeft)
    {
        float distance = float.MaxValue;

        var colliders = detector.AttachedBody.Simulation.GetColliders(detector.Aabb);

        ILogicOperable logicOperable = null;
        INpcCharacter npcCharacter = null;
        
        foreach (var collider in colliders)
        {
            if (collider.AttachedBody?.Owner is ILogicOperable lo)
            {
                var dist = (collider.Aabb.Center - detector.Aabb.Center).LengthSquared();
                if (dist < distance)
                {
                    distance = dist;
                    logicOperable = lo;
                }
            }

            if (collider.AttachedBody?.Owner is INpcCharacter npc)
            {
                var dist = (collider.Aabb.Center - detector.Aabb.Center).LengthSquared();
                if (dist < distance)
                {
                    distance = dist;
                    npcCharacter = npc;
                }
            }
        }
        
        var aabb = detector.Aabb;
        var dt = detector.AttachedBody.Simulation.SimulationParameters.TimeDelta;
        aabb.Inflate(detector.AttachedBody.Simulation.MovementEpsilon * 2 + MathF.Abs(detector.AttachedBody.Velocity.X * dt), -0.2f);

        if (facingLeft)
        {
            aabb.Right = detector.Aabb.Left;
        }
        else
        {
            aabb.Left = detector.Aabb.Right;
        }
        
        colliders = detector.AttachedBody.Simulation.GetColliders(aabb);

        Pushable = null;
        
        distance = float.MaxValue;
        foreach (var collider in colliders)
        {
            if (collider.AttachedBody?.Owner is IPushable push)
            {
                var dist = (collider.Aabb.Center - detector.Aabb.Center).LengthSquared();
                if (dist < distance)
                {
                    distance = dist;
                    Pushable = push;
                }
            }
        }

        if (Pushable is not null)
        {
            npcCharacter = null;
            logicOperable = null;
        }

        if (npcCharacter != NpcCharacter)
        {
            NpcCharacter?.SetInRange(detector.AttachedBody.Owner, false);
            NpcCharacter = npcCharacter;
            npcCharacter?.SetInRange(detector.AttachedBody.Owner, true);
        }
        
        LogicOperable = logicOperable;
    }
}