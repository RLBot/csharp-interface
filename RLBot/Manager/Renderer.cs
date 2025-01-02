using System.Diagnostics;
using RLBot.Flat;
using RLBot.Util;
using Color = System.Drawing.Color;
using Vector3 = System.Numerics.Vector3;
using Vector2 = System.Numerics.Vector2;

namespace RLBot.Manager;

public class Renderer(Interface gameInterface)
{
    private const string DEFAULT_GROUP = "DEFAULT";

    public bool IsRendering { get; private set; } = false;
    public string CurrentGroupId { get; private set; } = DEFAULT_GROUP;
    public Color Color = Color.White;

    private readonly List<RenderMessageT> _groupContent = new();
    private readonly HashSet<string> _previousGroupIds = new();
    public HashSet<string>.Enumerator PreviousGroupIds => _previousGroupIds.GetEnumerator();

    public void Begin(string groupId = DEFAULT_GROUP, Color? color = null)
    {
        Debug.Assert(!IsRendering, "Renderer::Begin was called twice without Renderer::End in between");
        _groupContent.Clear();
        CurrentGroupId = groupId;
        Color = color ?? Color;
        IsRendering = true;
    }

    public void End()
    {
        Debug.Assert(IsRendering, "Renderer::End was called without a call to Renderer::Begin first");
        var group = new RenderGroupT { Id = CurrentGroupId.GetHashCode(), RenderMessages = _groupContent };
        gameInterface.SendRenderGroup(group);
        IsRendering = false;
    }

    public bool Clear(string groupId = DEFAULT_GROUP)
    {
        gameInterface.SendRemoveRenderGroup(new() { Id = groupId.GetHashCode() });
        return _previousGroupIds.Remove(groupId);
    }

    public void ClearAll()
    {
        foreach (string id in _previousGroupIds)
        {
            gameInterface.SendRemoveRenderGroup(new() { Id = id.GetHashCode() });
        }

        _previousGroupIds.Clear();
    }

    public void Draw(RenderMessageT msg)
    {
        Debug.Assert(IsRendering,
            "Attempting to draw without a render group. Please call Renderer::Begin first and Renderer::End afterwards.");
        _groupContent.Add(msg);
    }

    public void DrawLine3D(RenderAnchorT start, RenderAnchorT end, Color? color = null)
    {
        Draw(new RenderMessageT
        {
            Variety = RenderTypeUnion.FromLine3D(new Line3DT
            {
                Start = start,
                End = end,
                Color = (color ?? Color).ToFlatBuf(),
            })
        });
    }

    public void DrawLine3D(RenderAnchorT start, RenderAnchorT end, Flat.Color color) =>
        DrawLine3D(start, end, Color.FromArgb(color.A, color.R, color.G, color.B));

    public void DrawLine3D(Vector3 start, Vector3 end, Color? color = null) =>
        DrawLine3D(new RenderAnchorT { World = start.ToFlatBuf() }, new RenderAnchorT { World = end.ToFlatBuf() },
            color);

    public void DrawPolyLine3D(IEnumerable<Vector3T> points, Color? color = null)
    {
        Draw(new RenderMessageT
        {
            Variety = RenderTypeUnion.FromPolyLine3D(new PolyLine3DT
            {
                Points = points.ToList(),
                Color = (color ?? Color).ToFlatBuf(),
            })
        });
    }

    public void DrawPolyLine3D(IEnumerable<Vector3> points, Color? color = null) =>
        DrawPolyLine3D(points.Select(v => v.ToFlatBuf()), color);

    public void DrawText2D(string text, float x, float y, float scale = 1f, Color? foreground = null,
        Color? background = null, TextHAlign hAlign = TextHAlign.Left,
        TextVAlign vAlign = TextVAlign.Top)
    {
        Draw(new RenderMessageT
        {
            Variety = RenderTypeUnion.FromString2D(new String2DT
            {
                Text = text,
                X = x,
                Y = y,
                Scale = scale,
                Foreground = (foreground ?? Color).ToFlatBuf(),
                Background = (background ?? Color.Transparent).ToFlatBuf(),
                HAlign = hAlign,
                VAlign = vAlign,
            })
        });
    }

    public void DrawText2D(string text, Vector2 position, float scale = 1f, Color? foreground = null,
        Color? background = null, TextHAlign hAlign = TextHAlign.Left, TextVAlign vAlign = TextVAlign.Top) =>
        DrawText2D(text, position.X, position.Y, scale, foreground, background, hAlign,
            vAlign);

    public void DrawText3D(string text, RenderAnchorT anchor, float scale = 1f, Color? foreground = null,
        Color? background = null, TextHAlign hAlign = TextHAlign.Left,
        TextVAlign vAlign = TextVAlign.Top)
    {
        Draw(new RenderMessageT
        {
            Variety = RenderTypeUnion.FromString3D(new String3DT
            {
                Text = text,
                Anchor = anchor,
                Scale = scale,
                Foreground = (foreground ?? Color).ToFlatBuf(),
                Background = (background ?? Color.Transparent).ToFlatBuf(),
                HAlign = hAlign,
                VAlign = vAlign,
            })
        });
    }

    public void DrawText3D(string text, Vector3 position, float scale = 1f, Color? foreground = null,
        Color? background = null, TextHAlign hAlign = TextHAlign.Left, TextVAlign vAlign = TextVAlign.Top) =>
        DrawText3D(text, new RenderAnchorT { World = position.ToFlatBuf() }, scale, foreground, background, hAlign,
            vAlign);

    public void DrawRect2D(float x, float y, float width, float height, Color? color = null, bool centered = true)
    {
        Draw(new RenderMessageT
        {
            Variety = RenderTypeUnion.FromRect2D(new Rect2DT
            {
                X = x,
                Y = y,
                Width = width,
                Height = height,
                Color = (color ?? Color).ToFlatBuf(),
            })
        });
    }

    public void DrawRect2D(Vector2 position, Vector2 size, Color? color = null, bool centered = true) =>
        DrawRect2D(position.X, position.Y, size.X, size.Y, color, centered);

    public void DrawRect3D(RenderAnchorT anchor, float width, float height, Color? color = null)
    {
        Draw(new RenderMessageT
        {
            Variety = RenderTypeUnion.FromRect3D(new Rect3DT
            {
                Anchor = anchor,
                Width = width,
                Height = height,
                Color = (color ?? Color).ToFlatBuf(),
            })
        });
    }

    public void DrawRect3D(RenderAnchorT anchor, Vector2 size, Color? color = null) =>
        DrawRect3D(anchor, size.X, size.Y, color);

    public void DrawRect3D(Vector3 position, float width, float height, Color? color = null) =>
        DrawRect3D(new RenderAnchorT { World = position.ToFlatBuf() }, width, height, color);

    public void DrawRect3D(Vector3 position, Vector2 size, Color? color = null) =>
        DrawRect3D(new RenderAnchorT { World = position.ToFlatBuf() }, size.X, size.Y, color);
}
