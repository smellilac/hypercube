﻿using Hypercube.Math.Vectors;
using Hypercube.Shared.EventBus;
using Hypercube.Shared.Physics.Events;
using Hypercube.Shared.Scenes;

namespace Hypercube.Shared.Physics;

/// <summary>
/// Analogous to a <see cref="Scene"/> in a physical representation.
/// Contains <see cref="IBody"/> that will handle their physical interactions.
/// </summary>
/// <seealso cref="Chunk"/>
public sealed class World
{
    private readonly IEventBus _eventBus;
    
    private readonly HashSet<IBody> _bodies = new();
    private readonly HashSet<Manifold> _manifolds = new();

    private Vector2 _gravity = new(0, -9.8f);

    public World(IEventBus eventBus)
    {
        _eventBus = eventBus;
    }

    public void Update(float deltaTime)
    {
        foreach (var bodyA in _bodies)
        {
            bodyA.Update(deltaTime, _gravity);
            
            foreach (var bodyB in _bodies)
            {
                if (bodyA == bodyB)
                    continue;
                
                if (bodyA.IsStatic && bodyB.IsStatic)
                    continue;
                
                ProcessCollision(bodyA, bodyB);
            }
        }

        foreach (var manifold in _manifolds)
        {
            ResolveCollision(manifold.BodyA, manifold.BodyB, manifold.Depth, manifold.Normal);
        }

        _manifolds.Clear();

        // TODO: Add works with chunks
        // ProcessChunkCollisions();
    }
    
    public void AddBody(IBody body)
    {
        _bodies.Add(body);
    }
    
    public void RemoveBody(IBody body)
    {
        _bodies.Remove(body);
    }
    
    private void ProcessCollision(IBody bodyA, IBody bodyB)
    {
        if (!IntersectsCollision(bodyA, bodyB, out var depth, out var normal))
            return;
        
        MoveBodies(bodyA, bodyB, depth, normal);

        var manifold = new Manifold(bodyA, bodyB, depth, normal, Vector2.Zero, Vector2.Zero, 0);
        
        var collisionEnterEvent = new PhysicsCollisionEntered(manifold);
        _eventBus.Raise(collisionEnterEvent);
        
        _manifolds.Add(manifold);
    }

    private void MoveBodies(IBody bodyA, IBody bodyB, float depth, Vector2 normal)
    {
        if (bodyA.IsStatic)
        {
            bodyB.Move(normal * depth / 2f);
            return;
        }
        
        if (bodyB.IsStatic)
        {
            bodyA.Move(-normal * depth / 2f);
            return;
        }
           
        bodyA.Move(-normal * depth / 2f);
        bodyB.Move(normal * depth / 2f);
    }

    private bool IntersectsCollision(IBody bodyA, IBody bodyB, out float depth, out Vector2 normal)
    {
        return bodyA.Shape.Type switch
        {
            ShapeType.Circle => bodyB.Shape.Type switch
            {
                ShapeType.Circle => Collisions.IntersectsCircles(
                    bodyA.Position + bodyA.Shape.Position, bodyA.Shape.Radius,
                    bodyB.Position + bodyB.Shape.Position, bodyB.Shape.Radius,
                    out depth, out normal),
                ShapeType.Polygon => Collisions.IntersectCirclePolygon(
                    bodyA.Position + bodyA.Shape.Position, bodyA.Shape.Radius,
                    bodyB.Position, bodyB.GetShapeVerticesTransformed(),
                    out depth, out normal),
                _ => throw new ArgumentOutOfRangeException()
            },
            ShapeType.Polygon => bodyB.Shape.Type switch
            {
                ShapeType.Circle => Collisions.IntersectCirclePolygon(
                    bodyB.Position + bodyB.Shape.Position, bodyB.Shape.Radius,
                    bodyA.Position, bodyA.GetShapeVerticesTransformed(),
                    out depth, out normal),
                ShapeType.Polygon => Collisions.IntersectsPolygon(
                    bodyA.Position, bodyA.GetShapeVerticesTransformed(),
                    bodyB.Position, bodyB.GetShapeVerticesTransformed(),
                    out depth, out normal),
                _ => throw new ArgumentOutOfRangeException()
            },
            _ => throw new ArgumentOutOfRangeException()
        };
    }

    private void ResolveCollision(IBody bodyA, IBody bodyB, float depth, Vector2 normal)
    {
        var relativeVelocity = bodyB.LinearVelocity - bodyA.LinearVelocity;
        if (Vector2.Dot(relativeVelocity, normal) > 0f)
            return;
        
        var e = MathF.Min(bodyA.Restitution, bodyB.Restitution);
        var j = -(1f + e) * Vector2.Dot(relativeVelocity, normal);
        
        j /= bodyA.InvMass + bodyB.InvMass;

        var impulse = j * normal;
        bodyA.LinearVelocity -= impulse * bodyA.InvMass;
        bodyB.LinearVelocity += impulse * bodyB.InvMass;
    }
}