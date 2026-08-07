using Godot;

public partial class Game : Control
{
	private GridContainer _board;
	private Label _infoLabel;

	public override void _Ready()
	{
		_infoLabel = GetNode<Label>(
            "BoardCenter/GameContainer/GameInfoLabel"
		);

		_board = GetNode<GridContainer>(
            "BoardCenter/GameContainer/Board"
		);

		_infoLabel.Text =
			$"{GameSettings.Columns} x {GameSettings.Rows} | " +
			$"{GameSettings.Mines} MINAS";

		CreateBoard();
	}

	private void CreateBoard()
	{
		_board.Columns = GameSettings.Columns;

		int totalCells =
			GameSettings.Rows * GameSettings.Columns;

		for (int i = 0; i < totalCells; i++)
		{
			Button cell = new Button();

			cell.CustomMinimumSize =
				new Vector2(32, 32);

			_board.AddChild(cell);
		}
	}
}
