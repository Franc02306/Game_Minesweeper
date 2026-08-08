using Godot;
using System;

public partial class Game : Control
{
	private GridContainer _board;
	private Label _infoLabel;
	private Label _resultOptionsLabel;

	private Button[,] _buttons;
	private bool[,] _mines;
	private bool[,] _revealed;
	private bool[,] _flagged;
	private int[,] _adjacentMines;

	private int _rows;
	private int _columns;
	private int _mineCount;
	private int _flagsPlaced = 0;

	private bool _minesPlaced = false;
	private bool _gameOver = false;

	public override void _Ready()
	{
		_infoLabel = GetNode<Label>(
            "BoardCenter/GameContainer/GameInfoLabel"
		);

		_resultOptionsLabel = GetNode<Label>(
            "BoardCenter/GameContainer/ResultOptionsLabel"
		);

		_resultOptionsLabel.Text = "";

		_board = GetNode<GridContainer>(
            "BoardCenter/GameContainer/Board"
		);

		_rows = GameSettings.Rows;
		_columns = GameSettings.Columns;
		_mineCount = GameSettings.Mines;

		_buttons = new Button[_rows, _columns];
		_mines = new bool[_rows, _columns];
		_revealed = new bool[_rows, _columns];
		_flagged = new bool[_rows, _columns];
		_adjacentMines = new int[_rows, _columns];

		UpdateHeader();

		CreateBoard();
	}

	// =====================================================
	// TAMAÑO DE LAS CASILLAS
	// =====================================================
	private float GetCellSize()
	{
		if (_columns <= 9)
			return 32;

		if (_columns <= 16)
			return 28;

		return 24;
	}

	// =====================================================
	// CREAR TABLERO
	// =====================================================
	private void CreateBoard()
	{
		_board.Columns = _columns;

		// Separación uniforme entre todas las casillas
		_board.AddThemeConstantOverride("h_separation", 2);
		_board.AddThemeConstantOverride("v_separation", 2);

		float cellSize = GetCellSize();

		for (int row = 0; row < _rows; row++)
		{
			for (int column = 0; column < _columns; column++)
			{
				Button cell = new Button();

				// El texto jamás debe agrandar la casilla
				cell.ClipText = true;

				// Tamaño completamente fijo
				Vector2 fixedSize = new Vector2(cellSize, cellSize);

				cell.CustomMinimumSize = fixedSize;
				cell.CustomMaximumSize = fixedSize;

				// Evita que el foco del teclado modifique visualmente
				// la casilla mientras jugamos con mouse
				cell.FocusMode = Control.FocusModeEnum.None;

				int currentRow = row;
				int currentColumn = column;

				// Clic izquierdo
				cell.Pressed += () =>
					RevealCell(currentRow, currentColumn);

				// Clic derecho
				cell.GuiInput += (@event) =>
				{
					if (@event is InputEventMouseButton mouseEvent &&
						mouseEvent.ButtonIndex == MouseButton.Right &&
						mouseEvent.Pressed)
					{
						ToggleFlag(currentRow, currentColumn);
						cell.AcceptEvent();
					}
				};

				_buttons[row, column] = cell;

				_board.AddChild(cell);
			}
		}
	}

	// =====================================================
	// COLOCAR MINAS
	// =====================================================
	private void PlaceMines(int safeRow, int safeColumn)
	{
		Random random = new Random();

		int placedMines = 0;

		while (placedMines < _mineCount)
		{
			int row = random.Next(_rows);
			int column = random.Next(_columns);

			// El primer clic siempre será seguro
			if (row == safeRow && column == safeColumn)
				continue;

			if (_mines[row, column])
				continue;

			_mines[row, column] = true;

			placedMines++;
		}

		_minesPlaced = true;
	}

	// =====================================================
	// CALCULAR MINAS ALREDEDOR
	// =====================================================
	private void CalculateAdjacentMines()
	{
		for (int row = 0; row < _rows; row++)
		{
			for (int column = 0; column < _columns; column++)
			{
				if (_mines[row, column])
					continue;

				int count = 0;

				for (int rowOffset = -1; rowOffset <= 1; rowOffset++)
				{
					for (int columnOffset = -1;
						 columnOffset <= 1;
						 columnOffset++)
					{
						if (rowOffset == 0 && columnOffset == 0)
							continue;

						int neighborRow = row + rowOffset;
						int neighborColumn = column + columnOffset;

						if (!IsInsideBoard(
								neighborRow,
								neighborColumn))
						{
							continue;
						}

						if (_mines[neighborRow, neighborColumn])
							count++;
					}
				}

				_adjacentMines[row, column] = count;
			}
		}
	}

	// =====================================================
	// REVELAR CASILLA
	// =====================================================
	private void RevealCell(int row, int column)
	{
		if (_gameOver)
			return;

		// No abrir casillas con bandera
		if (_flagged[row, column])
			return;

		// Las minas se generan en el primer clic
		if (!_minesPlaced)
		{
			PlaceMines(row, column);
			CalculateAdjacentMines();
		}

		if (_revealed[row, column])
			return;

		// Pisó una mina
		if (_mines[row, column])
		{
			_buttons[row, column].Text = "X";

			_infoLabel.Text = "¡PERDISTE!";

			_resultOptionsLabel.Text =
				"R - REINICIAR\nESC - VOLVER AL MENÚ";

			_gameOver = true;

			RevealAllMines();

			return;
		}

		RevealSafeCell(row, column);

		CheckVictory();
	}

	// =====================================================
	// REVELAR CASILLA SEGURA
	// =====================================================
	private void RevealSafeCell(int row, int column)
	{
		if (!IsInsideBoard(row, column))
			return;

		if (_revealed[row, column])
			return;

		if (_mines[row, column])
			return;

		if (_flagged[row, column])
			return;

		_revealed[row, column] = true;

		Button cell = _buttons[row, column];

		cell.Disabled = true;

		int adjacent = _adjacentMines[row, column];

		// Tiene minas alrededor
		if (adjacent > 0)
		{
			cell.Text = adjacent.ToString();
			return;
		}

		// Casilla vacía
		cell.Text = "";

		// Abrir automáticamente todos los vecinos
		for (int rowOffset = -1; rowOffset <= 1; rowOffset++)
		{
			for (int columnOffset = -1;
				 columnOffset <= 1;
				 columnOffset++)
			{
				if (rowOffset == 0 && columnOffset == 0)
					continue;

				int neighborRow = row + rowOffset;
				int neighborColumn = column + columnOffset;

				RevealSafeCell(
					neighborRow,
					neighborColumn
				);
			}
		}
	}

	// =====================================================
	// BANDERAS
	// =====================================================
	private void ToggleFlag(int row, int column)
	{
		if (_gameOver)
			return;

		// No marcar una casilla ya descubierta
		if (_revealed[row, column])
			return;

		Button cell = _buttons[row, column];

		if (_flagged[row, column])
		{
			// Quitar bandera
			_flagged[row, column] = false;

			_flagsPlaced--;

			cell.Text = "";
		}
		else
		{
			// No permitir más banderas que minas
			if (_flagsPlaced >= _mineCount)
				return;

			_flagged[row, column] = true;

			_flagsPlaced++;

			cell.Text = "⚑";
		}

		UpdateHeader();
	}

	// =====================================================
	// ENCABEZADO
	// =====================================================
	private void UpdateHeader()
	{
		int remainingMines =
			_mineCount - _flagsPlaced;

		_infoLabel.Text =
			$"{_columns} x {_rows} | " +
			$"MINAS: {remainingMines}";
	}

	// =====================================================
	// MOSTRAR MINAS AL PERDER
	// =====================================================
	private void RevealAllMines()
	{
		for (int row = 0; row < _rows; row++)
		{
			for (int column = 0;
				 column < _columns;
				 column++)
			{
				if (_mines[row, column])
				{
					_buttons[row, column].Text = "X";
				}

				_buttons[row, column].Disabled = true;
			}
		}
	}

	// =====================================================
	// VALIDAR POSICIÓN
	// =====================================================
	private bool IsInsideBoard(int row, int column)
	{
		return row >= 0 &&
			   row < _rows &&
			   column >= 0 &&
			   column < _columns;
	}

	// =====================================================
	// VICTORIA
	// =====================================================
	private void CheckVictory()
	{
		for (int row = 0; row < _rows; row++)
		{
			for (int column = 0;
				 column < _columns;
				 column++)
			{
				// Si queda una casilla segura por descubrir,
				// todavía no ganó
				if (!_mines[row, column] &&
					!_revealed[row, column])
				{
					return;
				}
			}
		}

		_gameOver = true;

		_infoLabel.Text = "¡GANASTE!";

		_resultOptionsLabel.Text =
			"R - JUGAR DE NUEVO\nESC - VOLVER AL MENÚ";

		for (int row = 0; row < _rows; row++)
		{
			for (int column = 0;
				 column < _columns;
				 column++)
			{
				if (_mines[row, column])
				{
					_buttons[row, column].Text = "⚑";
				}

				_buttons[row, column].Disabled = true;
			}
		}
	}

	// =====================================================
	// TECLADO
	// =====================================================
	public override void _Input(InputEvent @event)
	{
		if (@event is not InputEventKey keyEvent)
			return;

		if (!keyEvent.Pressed || keyEvent.Echo)
			return;

		// R = reiniciar
		if (keyEvent.Keycode == Key.R)
		{
			GetTree().ReloadCurrentScene();
			return;
		}

		// ESC = volver al menú
		if (keyEvent.Keycode == Key.Escape)
		{
			GetTree().ChangeSceneToFile(
                "res://MainMenu.tscn"
			);
		}
	}
}
