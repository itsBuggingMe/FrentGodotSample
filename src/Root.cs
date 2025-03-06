using Godot;
using Frent;
using System;
using Frent.Systems;
using FrentGodotSample;
using System.Collections.Generic;
using System.Runtime.InteropServices;

public partial class Root : Node, IUniformProvider
{
    private int _entityCount = 500_000;

    [Export]
    public int EntityCount
    {
        get => _entityCount;
        set
        {
            _entityCount = value;
            EnsureEntityCount(value);
        }
    }

    private Stack<Entity> _entities;

    private MultiMesh _childMesh;
    private World _world;

    private float _deltaTime;

    public override void _Ready()
    {
        _entities = [];
        _world = new World(this);
        _childMesh = GetChild<MultiMeshInstance2D>(0).Multimesh;

        EnsureEntityCount(EntityCount);

        base._Ready();
    }

    public override void _Process(double delta)
    {
        GD.Print($"FPS: {1 / delta}");

        _deltaTime = (float)delta;

        _world.Update();

        Collisions();

        Render();

        base._Process(delta);
    }

    private void Render()
    {
        var baseTransform = Transform2D.Identity
                .Scaled(Vector2.One * 10);

        foreach (var chunk in _world.Query<With<PaddedTransform2D>>()
            .EnumerateChunks<PaddedTransform2D>())
        {
            RenderingServer.MultimeshSetBuffer(_childMesh.GetRid(), MemoryMarshal.Cast<PaddedTransform2D, float>(chunk.Span));
        }
    }

    private void Collisions()
    {
        var bounds = GetViewport().GetVisibleRect();
        var tl = bounds.Position;
        var br = bounds.Size + bounds.Position;

        foreach ((var locs, var vels) in _world.Query<With<PaddedTransform2D>, With<Velocity>>()
            .EnumerateChunks<PaddedTransform2D, Velocity>())
        {
            //elide bounds checks
            Span<Velocity> velocities = vels[..locs.Length];
            for (int i = 0; i < locs.Length; i++)
            {
                ref float x = ref locs[i].OriginX;
                ref float y = ref locs[i].OriginY;

                if (x < tl.X)
                {
                    x = tl.X;
                    velocities[i].Value.X *= -1;
                }

                if (y < tl.Y)
                {
                    y = tl.Y;
                    velocities[i].Value.Y *= -1;
                }

                if (x > br.X)
                {
                    x = br.X;
                    velocities[i].Value.X *= -1;
                }

                if (y > br.Y)
                {
                    y = br.Y;
                    velocities[i].Value.Y *= -1;
                }
            }
        }
    }

    private Entity CreateEntity()
    {
        var bounds = GetViewport().GetVisibleRect();
        var tl = bounds.Position;
        var br = bounds.Size + bounds.Position;

        return _world.Create<PaddedTransform2D, Velocity>(
            Transform2D.Identity
                .Scaled(Vector2.One * 10)
                .Translated(new Vector2(Random.Shared.Next((int)tl.X, (int)br.X), Random.Shared.Next((int)tl.Y, (int)br.Y))),
            new Vector2(RandomRange(500), RandomRange(500))
            );
    }

    private void EnsureEntityCount(int count)
    {
        int delta = count - _entities.Count;
        _childMesh.InstanceCount = count;

        if (delta > 0)
        {
            for (int i = 0; i < delta; i++)
                _entities.Push(CreateEntity());
        }
        else
        {
            for (int i = 0; i < -delta; i++)
                _entities.Pop().Delete();
        }
    }

    private float RandomRange(float range)
    {
        return (Random.Shared.NextSingle() - 0.5f) * range;
    }

    public T GetUniform<T>() => typeof(T) == typeof(float) ? (T)(object)_deltaTime : throw new NotSupportedException();
}