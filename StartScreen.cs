using Godot;

public partial class StartScreen : Control
{
	public override void _Input(InputEvent @event)
	{
		if (@event is InputEventKey keyEvent && keyEvent.Pressed && !keyEvent.Echo)
		{
			GetTree().ChangeSceneToFile("res://MainMenu.tscn");
		}
	}
}
