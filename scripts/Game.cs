using Godot;
using System;

public partial class Game : Control
{
	private GridContainer _board;
	private Label _infoLabel;
	private Label _resultOptionsLabel;
	
	// Audios
	private AudioStreamPlayer _numberSound;
	private AudioStreamPlayer _flagSound;
	private AudioStreamPlayer _mineSound;
	private AudioStreamPlayer _winSound;

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

	private double _elapsedTime = 0;
	private bool _timerRunning = false;

	public override void _Ready()
	{
		_infoLabel = GetNode<Label>("BoardCenter/GameContainer/GameInfoLabel");

		_resultOptionsLabel = GetNode<Label>(
			"BoardCenter/GameContainer/ResultOptionsLabel"
		);

		_resultOptionsLabel.Text = "";

		_board = GetNode<GridContainer>("BoardCenter/GameContainer/Board");

		_rows = GameSettings.Rows;
		_columns = GameSettings.Columns;
		_mineCount = GameSettings.Mines;

		_buttons = new Button[_rows, _columns];
		_mines = new bool[_rows, _columns];
		_revealed = new bool[_rows, _columns];
		_flagged = new bool[_rows, _columns];
		_adjacentMines = new int[_rows, _columns];
		
		SetupAudio();

		_timerRunning = true;

		UpdateHeader();
		CreateBoard();
	}

	public override void _Process(double delta)
	{
		if (!_timerRunning)
			return;

		_elapsedTime += delta;

		UpdateHeader();
	}

	private string GetFormattedTime()
	{
		int totalSeconds = (int)_elapsedTime;

		int minutes = totalSeconds / 60;
		int seconds = totalSeconds % 60;

		return $"{minutes:00}:{seconds:00}";
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

	private int GetCellFontSize()
	{
		if (_columns <= 9)
			return 18;

		if (_columns <= 16)
			return 16;

		return 14;
	}

	// =====================================================
	// CREAR TABLERO
	// =====================================================
	private void CreateBoard()
	{
		_board.Columns = _columns;

		_board.AddThemeConstantOverride("h_separation", 2);
		_board.AddThemeConstantOverride("v_separation", 2);

		float cellSize = GetCellSize();

		for (int row = 0; row < _rows; row++)
		{
			for (int column = 0; column < _columns; column++)
			{
				Button cell = new Button();

				cell.ClipText = true;

				Vector2 fixedSize = new Vector2(cellSize, cellSize);

				cell.CustomMinimumSize = fixedSize;
				cell.CustomMaximumSize = fixedSize;

				cell.FocusMode = Control.FocusModeEnum.None;

				cell.AddThemeFontSizeOverride("font_size", GetCellFontSize());

				int currentRow = row;
				int currentColumn = column;

				cell.Pressed += () => RevealCell(currentRow, currentColumn);

				cell.GuiInput += (@event) =>
				{
					if (@event is InputEventMouseButton mouseEvent &&
						mouseEvent.ButtonIndex == MouseButton.Right &&
						mouseEvent.Pressed)
					{
						ToggleFlag(currentRow,currentColumn);

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

			if (row == safeRow && column == safeColumn)
			{
				continue;
			}

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
					for (int columnOffset = -1; columnOffset <= 1; columnOffset++)
					{
						if (rowOffset == 0 && columnOffset == 0)
						{
							continue;
						}

						int neighborRow = row + rowOffset;

						int neighborColumn = column + columnOffset;

						if (!IsInsideBoard(neighborRow, neighborColumn))
						{
							continue;
						}

						if (_mines[neighborRow, neighborColumn])
						{
							count++;
						}
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

		if (_flagged[row, column])
			return;

		if (!_minesPlaced)
		{
			PlaceMines(row, column);
			CalculateAdjacentMines();
		}

		if (_revealed[row, column])
			return;

		if (_mines[row, column])
		{
			_gameOver = true;
			_timerRunning = false;
			
			_mineSound.Play();

			_infoLabel.Text = "¡PERDISTE!";

			_resultOptionsLabel.Text = $"TIEMPO: {GetFormattedTime()}\n\n" +
				"R - REINICIAR\n" + "ESC - VOLVER AL MENÚ";

			RevealAllMines();

			return;
		}

		RevealSafeCell(row, column);

		CheckVictory();
		
		if (!_gameOver)
		{
			_numberSound.Play();
		}
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

		int adjacent = _adjacentMines[row, column];

		StyleRevealedCell(cell, adjacent);

		cell.Disabled = true;

		if (adjacent > 0)
		{
			cell.Text = adjacent.ToString();

			return;
		}

		cell.Text = "";
 
		for (int rowOffset = -1; rowOffset <= 1; rowOffset++)
		{
			for (int columnOffset = -1; columnOffset <= 1; columnOffset++)
			{
				if (rowOffset == 0 && columnOffset == 0)
				{
					continue;
				}

				int neighborRow = row + rowOffset;

				int neighborColumn = column + columnOffset;

				RevealSafeCell(neighborRow, neighborColumn);
			}
		}
	}

	// =====================================================
	// ESTILO CASILLA REVELADA
	// =====================================================
	private void StyleRevealedCell(Button cell, int adjacent)
	{
		StyleBoxFlat style = new StyleBoxFlat();

		style.BgColor = new Color(0.38f, 0.38f, 0.38f, 1.0f);

		style.CornerRadiusTopLeft = 2;
		style.CornerRadiusTopRight = 2;
		style.CornerRadiusBottomLeft = 2;
		style.CornerRadiusBottomRight = 2;

		cell.AddThemeStyleboxOverride("disabled", style);

		if (adjacent > 0)
		{
			Color numberColor = GetNumberColor(adjacent);

			cell.AddThemeColorOverride("font_disabled_color", numberColor);
		}
	}

	// =====================================================
	// COLORES DE LOS NÚMEROS
	// =====================================================
	private Color GetNumberColor(int number)
	{
		return number switch
		{
			1 => new Color(0.35f, 0.65f, 1.00f),

			2 => new Color(0.35f, 0.85f, 0.45f),

			3 => new Color(1.00f, 0.35f, 0.35f),

			4 => new Color(0.75f, 0.45f, 1.00f),

			5 => new Color(1.00f, 0.60f, 0.25f),

			6 => new Color(0.30f, 0.90f, 0.90f),

			7 => new Color(0.95f, 0.95f, 0.95f),

			8 => new Color(0.70f, 0.70f, 0.70f),

			_ => Colors.White
		};
	}

	// =====================================================
	// BANDERAS
	// =====================================================
	private void ToggleFlag(int row, int column)
	{
		if (_gameOver)
			return;

		if (_revealed[row, column])
			return;

		Button cell = _buttons[row, column];

		if (_flagged[row, column])
		{
			_flagged[row, column] = false;
			_flagsPlaced--;

			cell.Text = "";

			RemoveFlagStyle(cell);
		}
		else
		{
			if (_flagsPlaced >= _mineCount)
			{
				return;
			}

			_flagged[row, column] = true;
			_flagsPlaced++;

			cell.Text = "⚑";

			StyleFlagCell(cell);
		}

		_flagSound.Play();
		
		UpdateHeader();
	}

	private void StyleFlagCell(Button cell)
	{
		Color flagColor = new Color(1.0f, 0.70f, 0.20f);

		cell.AddThemeColorOverride("font_color", flagColor);

		cell.AddThemeColorOverride("font_hover_color", flagColor);

		cell.AddThemeColorOverride("font_pressed_color", flagColor);
	}

	private void RemoveFlagStyle(Button cell)
	{
		cell.RemoveThemeColorOverride("font_color");

		cell.RemoveThemeColorOverride("font_hover_color");

		cell.RemoveThemeColorOverride("font_pressed_color");
	}

	// =====================================================
	// ENCABEZADO
	// =====================================================
	private void UpdateHeader()
	{
		int remainingMines = _mineCount - _flagsPlaced;
		_infoLabel.Text = $"{_columns} x {_rows} | " + $"MINAS: {remainingMines} | " + $"TIEMPO: {GetFormattedTime()}";
	}

	// =====================================================
	// MOSTRAR MINAS AL PERDER
	// =====================================================
	private void RevealAllMines()
	{
		for (int row = 0; row < _rows; row++)
		{
			for (int column = 0; column < _columns; column++)
			{
				Button cell = _buttons[row, column];

				if (_mines[row, column])
				{
					cell.Text = "●";
					StyleMineCell(cell);
				}

				cell.Disabled = true;
			}
		}
	}

	// =====================================================
	// ESTILO DE MINA
	// =====================================================
	private void StyleMineCell(Button cell)
	{
		StyleBoxFlat style = new StyleBoxFlat();

		style.BgColor = new Color(0.60f, 0.16f, 0.16f, 1.0f);

		style.CornerRadiusTopLeft = 2;
		style.CornerRadiusTopRight = 2;
		style.CornerRadiusBottomLeft = 2;
		style.CornerRadiusBottomRight = 2;

		cell.AddThemeStyleboxOverride("disabled", style);

		cell.AddThemeColorOverride("font_disabled_color", Colors.White);
	}

	// =====================================================
	// VALIDAR POSICIÓN
	// =====================================================
	private bool IsInsideBoard(int row, int column)
	{
		return row >= 0 && row < _rows && column >= 0 && column < _columns;
	}

	// =====================================================
	// VICTORIA
	// =====================================================
	private void CheckVictory()
	{
		for (int row = 0; row < _rows; row++)
		{
			for (int column = 0; column < _columns; column++)
			{
				if (!_mines[row, column] && !_revealed[row, column])
				{
					return;
				}
			}
		}

		_gameOver = true;
		_timerRunning = false;
		
		_winSound.Play();

		_infoLabel.Text = "¡GANASTE!";

		_resultOptionsLabel.Text = $"TIEMPO: {GetFormattedTime()}\n\n" +
			"R - JUGAR DE NUEVO\n" + "ESC - VOLVER AL MENÚ";

		for (int row = 0; row < _rows; row++)
		{
			for (int column = 0; column < _columns; column++)
			{
				Button cell = _buttons[row, column];

				if (_mines[row, column])
				{
					cell.Text = "⚑";

					StyleFlagCell(cell);
				}

				cell.Disabled = true;
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
		{
			return;
		}

		if (keyEvent.Keycode == Key.R)
		{
			GetTree().ReloadCurrentScene();

			return;
		}

		if (keyEvent.Keycode == Key.Escape)
		{
			GetTree().ChangeSceneToFile("res://scenes//MainMenu.tscn");
		}
	}
	
	// =====================================================
	// CARGAR AUDIOS
	// =====================================================
	private void SetupAudio()
	{
		_numberSound = CreateAudioPlayer("res://audio/number_sound.mp3");
		_flagSound = CreateAudioPlayer("res://audio/flag_sound.mp3");
		_mineSound = CreateAudioPlayer("res://audio/mine_sound.mp3");
		_winSound = CreateAudioPlayer("res://audio/win_sound.mp3");
	}

	private AudioStreamPlayer CreateAudioPlayer(string path)
	{
		AudioStreamPlayer player = new AudioStreamPlayer();
		player.Stream = GD.Load<AudioStream>(path);
		AddChild(player);

		return player;
	}
}
