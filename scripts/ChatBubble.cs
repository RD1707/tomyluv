namespace Alpheratz;

public partial class ChatBubble : Label
{
	[Export]
	Control handle;

	public bool isPlaying = false;
	
	private Vector2 originalPosition;
	private Vector2 originalHandlePosition;

	public override void _Ready()
	{
		Hide();
		originalPosition = Position;
		originalHandlePosition = handle.Position;

		// Editor may not work, so we set it here.
		// bug report: https://github.com/godotengine/godot/issues/76668
		GrowVertical = GrowDirection.Begin;

		if (HasNode("/root/Console"))
		{
			GetNode("/root/Console").Call("register_env", Name + GetInstanceId(), this);
		}
	}

	public override void _PhysicsProcess(double delta)
	{
		Camera2D cam = GetViewport().GetCamera2D();

		float camHalfW = GetViewport().GetVisibleRect().Size.X / cam.Zoom.X / 2.0f;
		float left = cam.GetScreenCenterPosition().X - camHalfW;
		float right = cam.GetScreenCenterPosition().X + camHalfW;

		float chatboxLeftX = GetGlobalRect().Position.X;
		float targetX = Mathf.Clamp(chatboxLeftX, left, right - GetGlobalRect().Size.X);

		SetGlobalPosition(GlobalPosition with {X = targetX});
		float hx = (originalHandlePosition + (originalPosition - Position)).X;
		handle.Position = handle.Position with {X = hx};
	}

	public void PlayText(string text)
	{
		if (isPlaying) 
		{
			GD.PushError("ChatBubble is already playing.");
			return;
		}
		isPlaying = true;
		SetProcess(true);
		Position = originalPosition + Vector2.Down * 16;
		Text = text;
		VisibleRatio = 0.0f;
		Modulate = Modulate with {A = 0};
		Show();
		Tween tween = CreateTween();
		tween.TweenProperty(this, "position", originalPosition, 0.4f)
			.SetEase(Tween.EaseType.Out)
			.SetTrans(Tween.TransitionType.Back);
		tween.SetParallel(true);
		tween.TweenProperty(this, "modulate:a", 1.0f, 0.4f);
		tween.SetParallel(false);
		tween.SetParallel(true);
		tween.TweenProperty(this, "visible_ratio", 1.0f, 0.02f * text.Length);
		tween.SetParallel(false);
		tween.TweenInterval(1.2f);
		tween.TweenCallback(Callable.From(()=>{
			Hide();
			isPlaying = false;
			SetProcess(false);
		}));
	}

	public override void _Process(double delta)
	{
		// Update the size of the bubble
		Size = Vector2.Zero;
	}
}
