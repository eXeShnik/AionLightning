using System.Text.RegularExpressions;
using AionLightning.Game.World.Geo.Bounding;
using AionLightning.Game.World.Geo.Collision;
using AionLightning.Game.World.Geo.Math;

namespace AionLightning.Game.World.Geo.Scene;

/// <summary>
/// Port of the Java geoEngine <c>Node</c> — an internal scene-graph node maintaining a list of
/// children and merging their bounds. GL/quantity bookkeeping from the Java original is dropped
/// (headless collision graph only).
/// </summary>
internal class Node(string? name = null) : Spatial(name)
{
    protected readonly List<Spatial> Children = [];

    public override short CollisionFlags { get; set; } = (short)((byte)CollisionIntention.All);

    public IReadOnlyList<Spatial> GetChildren() => Children;

    public virtual int AttachChild(Spatial child)
    {
        if (child.Parent != this && !ReferenceEquals(child, this))
        {
            child.Parent?.DetachChild(child);
            child.Parent = this;
            Children.Add(child);
        }

        return Children.Count;
    }

    public int DetachChild(Spatial child)
    {
        if (child.Parent != this)
            return -1;

        var index = Children.IndexOf(child);
        if (index != -1)
            DetachChildAt(index);
        return index;
    }

    private void DetachChildAt(int index)
    {
        var child = Children[index];
        Children.RemoveAt(index);
        child.Parent = null;
    }

    /// <summary>
    /// Non-recursive, name-only match against every descendant (Java's <c>descendantMatches
    /// (String)</c> convenience overload — no class filter is ever used by this port).
    /// </summary>
    public List<Spatial> DescendantMatches(string nameRegex)
    {
        var anchored = $"^(?:{nameRegex})$";
        var result = new List<Spatial>();
        CollectDescendantMatches(anchored, result);
        return result;
    }

    private void CollectDescendantMatches(string anchoredRegex, List<Spatial> result)
    {
        foreach (var child in Children)
        {
            if (child.Name is not null && Regex.IsMatch(child.Name, anchoredRegex))
                result.Add(child);

            if (child is Node childNode)
                childNode.CollectDescendantMatches(anchoredRegex, result);
        }
    }

    public override int CollideWith(ICollidable other, CollisionResults results)
    {
        if ((Intentions & results.Intentions) == 0)
            return 0;

        if (other is Ray ray && (WorldBound == null || !WorldBound.Intersects(ray)))
            return 0;

        var total = 0;
        foreach (var child in Children)
        {
            if (child is Geometry)
            {
                if ((child.Intentions & results.Intentions) == 0 || (child.Intentions & (byte)CollisionIntention.Event) != 0)
                    continue;
                if ((results.Intentions & (byte)CollisionIntention.Material) != 0 && child.MaterialId <= 0)
                    continue;
            }

            total += child.CollideWith(other, results);
            if (total > 0 && results.IsOnlyFirst)
                break;
        }

        return total;
    }

    public override void SetModelBound(BoundingVolume? modelBound)
    {
        foreach (var child in Children)
            child.SetModelBound(modelBound?.Clone(null));
    }

    public override void UpdateModelBound()
    {
        BoundingVolume? resultBound = null;
        foreach (var child in Children)
        {
            child.UpdateModelBound();
            if (resultBound != null)
            {
                resultBound.MergeLocal(child.WorldBound!);
            }
            else if (child.WorldBound != null)
            {
                resultBound = child.WorldBound.Clone(WorldBound);
            }
        }

        WorldBound = resultBound;
    }

    public override void SetTransform(Matrix3f rotation, Vector3f loc, float scale)
    {
        foreach (var child in Children)
            child.SetTransform(rotation, loc, scale);
    }

    public override Spatial DeepClone()
    {
        var node = new Node(Name) { CollisionFlags = CollisionFlags };
        foreach (var child in Children)
            node.AttachChild(child.DeepClone());
        return node;
    }
}
