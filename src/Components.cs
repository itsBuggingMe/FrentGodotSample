using Godot;
using Frent.Components; 
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace FrentGodotSample;

[StructLayout(LayoutKind.Sequential, Pack = 4)]
internal struct PaddedTransform2D
{
    public float XX;
    public float YX;
    private float _p0;
    public float OriginX;

    public float XY;
    public float YY;
    private float _p1;
    public float OriginY;

    public static implicit operator PaddedTransform2D(Transform2D trans)
    {
        Unsafe.SkipInit(out PaddedTransform2D result);

        result.XX = trans.X.X;
        result.YX = trans.Y.X;
        result._p0 = default;
        result.OriginX = trans.Origin.X;

        result.XY = trans.X.Y;
        result.YY = trans.Y.Y;
        result._p1 = default;
        result.OriginY = trans.Origin.Y;

        return result;
    }
}

//unused in favor of PaddedTransform2D
internal struct Location
{
    public Vector2 Value;
    public static implicit operator Location(Vector2 l) => new() { Value = l };
}

internal struct Velocity : IUniformComponent<float, PaddedTransform2D>
{
    public Vector2 Value;

    public void Update(float dt, ref PaddedTransform2D arg)
    {
        var delta = Value * dt;
        arg.OriginX += delta.X;
        arg.OriginY += delta.Y;
    }

    public static implicit operator Velocity(Vector2 l) => new() { Value = l };
}