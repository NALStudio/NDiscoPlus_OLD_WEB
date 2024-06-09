using NDiscoPlus.Shared.Helpers;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NDiscoPlus.Shared.Models;

public readonly record struct NDPLightBounds(double MinX, double MaxX, double MinY, double MaxY, double MinZ, double MaxZ);

public sealed class NDPLightCollection : IReadOnlyList<NDPLight>
{
    private readonly NDPLight[] lights;

    public NDPLightBounds Bounds { get; }

    private NDPLightCollection(NDPLight[] lights, NDPLightBounds bounds)
    {
        this.lights = lights;
        Bounds = bounds;
    }
    /// <summary>
    /// Group lights from left to right.
    /// </summary>
    public List<NDPLight[]> GroupX(int count)
    {
        return ListHelpers.GroupCloseBy(
            lights,
            count,
            (a, b) => Math.Abs(a.Position.X - b.Position.X)
        ).ToList();
    }

    /// <summary>
    /// Group lights from back to front.
    /// </summary>
    public List<NDPLight[]> GroupY(int count)
    {
        return ListHelpers.GroupCloseBy(
            lights,
            count,
            (a, b) => Math.Abs(a.Position.Y - b.Position.Y)
        ).ToList();
    }

    /// <summary>
    /// Group lights from bottom to top.
    /// </summary>
    public List<NDPLight[]> GroupZ(int count)
    {
        return ListHelpers.GroupCloseBy(
            lights,
            count,
            (a, b) => Math.Abs(a.Position.Z - b.Position.Z)
        ).ToList();
    }


    public static NDPLightCollection Create(IList<NDPLight> lights)
    {
        NDPLight[] lightArr = new NDPLight[lights.Count];

        double minX = 0;
        double maxX = 0;
        double minY = 0;
        double maxY = 0;
        double minZ = 0;
        double maxZ = 0;

        for (int i = 0; i < lights.Count; i++)
        {
            NDPLight light = lights[i];
            lightArr[i] = light;

            double x = light.Position.X;
            double y = light.Position.Y;
            double z = light.Position.Z;

            if (x < minX)
                minX = x;
            if (x > maxX)
                maxX = x;
            if (y < minY)
                minY = y;
            if (y > maxY)
                maxY = y;
            if (z < minZ)
                minZ = z;
            if (z > maxZ)
                maxZ = z;
        }

        return new NDPLightCollection(
            lightArr,
            new NDPLightBounds(MinX: minX, MaxX: maxX, MinY: minY, MaxY: maxY, MinZ: minZ, MaxZ: maxZ)
        );
    }

    public int Count => lights.Length;

    public NDPLight this[int index] => lights[index];

    public IEnumerator<NDPLight> GetEnumerator() => ((IEnumerable<NDPLight>)lights).GetEnumerator();
    IEnumerator IEnumerable.GetEnumerator() => lights.GetEnumerator();
}
