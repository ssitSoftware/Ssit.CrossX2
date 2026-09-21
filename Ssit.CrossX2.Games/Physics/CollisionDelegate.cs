using System.Numerics;

namespace Ssit.CrossX2.Framework.Games.Physics;

public delegate void CollisionDelegate(bool byMyMovement, ICollider other, Vector2 impact);