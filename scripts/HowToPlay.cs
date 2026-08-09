using Godot;

public partial class HowToPlay : Control
{
	public override void _Input(InputEvent @event)
	{
		if (@event is not InputEventKey keyEvent)
			return;

		if (!keyEvent.Pressed || keyEvent.Echo)
			return;

		if (keyEvent.Keycode == Key.Escape)
		{
			GetTree().ChangeSceneToFile("res://scenes//MainMenu.tscn");
		}
	}
}
