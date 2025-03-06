using Godot;
using Frent;
using System;
using Frent.Systems;
using FrentGodotSample;
using System.Collections.Generic;
using System.Runtime.InteropServices;

public partial class Root : Node
{
    public int EntityCount
    {
        get;
        set
        {
            int delta = value - field;
            if (delta == 0)
                return;

            _childMesh.InstanceCount = field = value;

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
    }

    private Stack<Entity> _entities;

    private MultiMesh _childMesh;
    private World _world;
    private DefaultUniformProvider _uniforms;
    private Delta _dt;

    public override void _Ready()
    {
        _entities = [];
        _dt = new();
        _uniforms = new();
        _world = new World(_uniforms);
        _childMesh = GetChild<MultiMeshInstance2D>(0).Multimesh;
        _uniforms.Add(_dt);

        EntityCount = 500_000;

        base._Ready();
    }

    public override void _Process(double delta)
    {
        GD.Print($"FPS: {1 / delta}");

        _dt.Time = (float)delta;

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
                var currentLoc = new Vector2(locs[i].OriginX, locs[i].OriginY);
                if (currentLoc.X < tl.X)
                {
                    currentLoc.X = tl.X;
                    velocities[i].Value.X *= -1;
                }

                if (currentLoc.Y < tl.Y)
                {
                    currentLoc.Y = tl.Y;
                    velocities[i].Value.Y *= -1;
                }

                if (currentLoc.X > br.X)
                {
                    currentLoc.X = br.X;
                    velocities[i].Value.X *= -1;
                }

                if (currentLoc.Y > br.Y)
                {
                    currentLoc.Y = br.Y;
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

    private float RandomRange(float range)
    {
        return (Random.Shared.NextSingle() - 0.5f) * range;
    }
}

internal class Delta
{
    public float Time;
}