using Godot;
using System.Threading.Tasks;

public partial class Transition : CanvasLayer
{
    [Export] public ColorRect Overlay;
    [Export] public float Duration = 0.3f;

    private bool _active;

    public override void _Ready()
    {
        ProcessMode = Node.ProcessModeEnum.Always;
        Overlay.Modulate = new Color(0, 0, 0, 0);
        Overlay.MouseFilter = Control.MouseFilterEnum.Ignore;
    }

    public async Task FadeOut()
    {
        if (_active) return;
        _active = true;
        var tween = CreateTween();
        tween.TweenProperty(Overlay, "modulate:a", 1.0f, Duration);
        await ToSignal(tween, Tween.SignalName.Finished);
    }

    public async Task FadeIn()
    {
        var tween = CreateTween();
        tween.TweenProperty(Overlay, "modulate:a", 0.0f, Duration);
        await ToSignal(tween, Tween.SignalName.Finished);
        _active = false;
    }

    public async Task TransitionToAsync(System.Action action)
    {
        await FadeOut();
        action?.Invoke();
        await FadeIn();
    }
}   