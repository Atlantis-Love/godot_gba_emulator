using Godot;
using System;
using System.Collections;

public partial class Emulator : Node
{
	public byte[] _romAsset;
	public byte[] _biosAsset;

	private ImageTexture _displayTexture;
	private GBALib.System.GbaManager _gbaManager = null;

	private String _storagePrefix = "";

	private Image _displayTable = Image.Create(240, 160, false, Image.Format.Rgba8);
	private Color[] _colorLookupTable = new Color[65536];

	// public GameObject _button_A;
	// public GameObject _button_B;
	// public GameObject _button_UP;
	// public GameObject _button_DOWN;
	// public GameObject _button_LEFT;
	// public GameObject _button_RIGHT;
	// public GameObject _button_SHOULDER_LEFT;
	// public GameObject _button_SHOULDER_RIGHT;
	// public GameObject _button_SELECT;
	// public GameObject _button_START;

	// Called when the node enters the scene tree for the first time.
	public override void _Ready()
	{
		for (int i = 0; i < 32768; i++)
		{
			_colorLookupTable[i] = new Color(
				(float)(i & 0x001F) / (float)0x001F,
				(float)(i & 0x03E0) / (float)0x03E0,
				(float)(i & 0x7C00) / (float)0x7C00,
				1.0f
				);

			_colorLookupTable[i+32768] = _colorLookupTable[i];
		}

		_displayTexture = ImageTexture.CreateFromImage(_displayTable);
		var screen = GetNode<TextureRect>("Screen");
		screen.Texture = _displayTexture;

		_gbaManager = new GBALib.System.GbaManager();

		_gbaManager.KeyState = 0x3FF;

		_gbaManager.VideoManager.Presenter = GBAUpdated;

		GBALib.Graphics.Renderer renderer = new GBALib.Graphics.Renderer();
		renderer.Initialize(null);

		_gbaManager.VideoManager.Renderer = renderer;

		_biosAsset = FileAccess.GetFileAsBytes("res://gba_bios");
		var file = FileAccess.Open("res://gba_bios", FileAccess.ModeFlags.Read);
		var _biosAsset2 = file.GetBuffer((long)file.GetLength());
		file.Close();
		LoadBios();
		if(_romAsset.IsEmpty()) _romAsset = FileAccess.GetFileAsBytes("res://test3.gba");
		LoadROM((byte[])_romAsset);
	}

	// Called every frame. 'delta' is the elapsed time since the previous frame.
	public override void _Process(double delta)
	{
		ushort keystate = 0x3FFF;

		// keystate ^= BitsIfActive(_button_A, (ushort)0x0001);
		// keystate ^= BitsIfActive(_button_B, (ushort)0x0002);
		// keystate ^= BitsIfActive(_button_SELECT, (ushort)0x0004);
		// keystate ^= BitsIfActive(_button_START, (ushort)0x0008);
		// keystate ^= BitsIfActive(_button_RIGHT, (ushort)0x0010);
		// keystate ^= BitsIfActive(_button_LEFT, (ushort)0x0020);
		// keystate ^= BitsIfActive(_button_UP, (ushort)0x0040);
		// keystate ^= BitsIfActive(_button_DOWN, (ushort)0x0080);
		// keystate ^= BitsIfActive(_button_SHOULDER_RIGHT, (ushort)0x0100);
		// keystate ^= BitsIfActive(_button_SHOULDER_LEFT, (ushort)0x0200);

		_gbaManager.KeyState = keystate;

		if (0 != _updateStack.Count)
		{
			uint[] b = (uint[])_updateStack.Pop();

			_updateStack.Clear();

			if (_displayTexture != null)
			{

				for (int i = 0; i < (240 * 160); ++i)
				{
					_displayTable.SetPixel(i/240, i%240, _colorLookupTable[b[i]]);
				}

				_displayTexture.Update(_displayTable);
			}
		}
	}

	private void LoadBios()
	{
		try
		{
			_gbaManager.LoadBios(_biosAsset);
			GD.Print("LOADED BIOS!!!");
		}
		catch (Exception ex)
		{
			GD.Print("Unable to load bios file, disabling bios (irq's will not work)\n" + ex.Message);
		}
	}

	public void LoadROM(byte[] rom)
	{
		int romSize = 1;
		while(romSize < rom.Length)
		{
			romSize <<= 1;
		}

		byte[] romBytes = new byte[romSize];

		rom.CopyTo(romBytes, 0);

		_gbaManager.Halt();

		_gbaManager.LoadRom(romBytes);

		GD.Print("LOADED ROM!!!");

		_gbaManager.Reset();
		_gbaManager.Resume();
	}

	private int _updateCounter = 0;
	
	private Stack _updateStack = new Stack();

	public void GBAUpdated(object data)
	{
		uint[] buffer = (uint[])data;

		_updateStack.Push(buffer);

		++_updateCounter;
	}

	// ushort BitsIfActive(GameObject source, ushort bitmask)
	// {
	// 	if (null == source) return 0;
	// 	LiveButton s = source.GetComponent<LiveButton>();
	// 	if (!s.isActive) return (ushort)0;
	// 	return bitmask;
	// }
}
