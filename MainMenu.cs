using Godot;

public static class GameSettings
{
	public static int Rows;
	public static int Columns;
	public static int Mines;
}

public partial class MainMenu : Control
{
	public override void _Ready()
	{
		GetNode<Button>("DifficultyContainer/SmallButton").Pressed += () => StartGame(9, 9, 10);

		GetNode<Button>("DifficultyContainer/MediumButton").Pressed += () => StartGame(16, 16, 40);

		GetNode<Button>("DifficultyContainer/LargeButton").Pressed += () => StartGame(16, 30, 99);
	}

	private void StartGame(int rows, int columns, int mines)
	{
		GameSettings.Rows = rows;
		GameSettings.Columns = columns;
		GameSettings.Mines = mines;

		GetTree().ChangeSceneToFile("res://Game.tscn");
	}
}
