using Godot;
using JumpAndRun.scripts;
using System;
using System.Collections.Generic;
using System.Linq;

public partial class Main : Node2D
{
	[Export] public PackedScene PlatformScene { get; set; }
	[Export] public PackedScene GoldPlatformScene { get; set; }
	[Export] public PackedScene IcyPlatformScene { get; set; }
	[Export] public PackedScene RoughPlatformScene { get; set; }
	[Export] public PackedScene PowerupScene { get; set; }

	private int maxPlatforms = 100;
	private List<Platform> platforms = new List<Platform>();
	private List<PowerupMultiplier> powerups = new List<PowerupMultiplier>();
	private float lastPlatformY = 400;
	private float lastPlatformX = 400;
	private Random random = new Random();

	private bool isGameOver = false;
	private Player player;
	private Camera2D camera;
	private Node2D platformsContainer;
	private Node2D powerupsContainer;
	private Control gameOverScreen;
	private Control fallingScreen;
	private UI ui;

	private float deathTimer = 0f;
	private const float DEATH_DELAY = 2f;
	private bool isDying = false;
	private float lastValidY = 400f;
	private const float GROUND_LEVEL = 500f;
	private const float DEATH_BOUNDARY = 700f;
	private const float MAX_PLATFORM_DISTANCE = 180f;

	public override void _Ready()
	{
		PlatformScene = GD.Load<PackedScene>("res://scenes/Platform.tscn");
		GoldPlatformScene = GD.Load<PackedScene>("res://scenes/GoldPlatform.tscn");
		IcyPlatformScene = GD.Load<PackedScene>("res://scenes/IcyPlatform.tscn");
		RoughPlatformScene = GD.Load<PackedScene>("res://scenes/RoughPlatform.tscn");
		PowerupScene = GD.Load<PackedScene>("res://scenes/PowerupMultiplier.tscn");

		camera = GetNode<Camera2D>("Camera2D");
		player = GetNode<Player>("Player");
		platformsContainer = GetNode<Node2D>("Platforms");
		powerupsContainer = GetNode<Node2D>("Powerups");
		gameOverScreen = GetNode<Control>("GameOverScreen");
		fallingScreen = GetNode<Control>("FallingScreen");
		ui = GetNode<UI>("UI");

		if (player != null)
		{
			GenerateInitialPlatforms();
			lastValidY = player.GlobalPosition.Y;
			player.PlayerDied += OnPlayerDied;
		}
	}

	public override void _Process(double delta)
	{
		if (Input.IsActionJustPressed("ui_cancel"))
		{
			GetTree().Quit();
			return;
		}

		if (isGameOver)
		{
			if (Input.IsActionJustPressed("restart")) ResetGame();
			return;
		}

		if (isDying)
		{
			deathTimer += (float)delta;
			if (deathTimer >= DEATH_DELAY)
			{
				isGameOver = true;
				isDying = false;
				deathTimer = 0f;
				ShowGameOver();
			}
			return;
		}

		if (platforms.Count < maxPlatforms)
		{
			GeneratePlatforms();
		}

		foreach (var powerup in powerups.ToList())
		{
			if (powerup != null && !powerup.IsCollected && powerup.CheckCollision(player))
			{
				powerup.OnCollision(player);
				powerup.QueueFree();
				powerups.Remove(powerup);
			}
		}

		if (player.GlobalPosition.Y > DEATH_BOUNDARY ||
			(player.GlobalPosition.Y > lastValidY + GetViewportRect().Size.Y * 1.5f && player.Velocity.Y > 0))
		{
			StartDying();
			return;
		}

		if (camera != null)
		{
			Vector2 targetPos = new Vector2(player.GlobalPosition.X, Math.Min(player.GlobalPosition.Y, lastValidY));
			camera.GlobalPosition = targetPos;
		}

		if (ui != null && camera != null)
		{
			ui.GlobalPosition = camera.GlobalPosition - GetViewportRect().Size / 2;
		}

		if (player.GlobalPosition.Y < lastValidY - 50)
		{
			lastValidY = player.GlobalPosition.Y;
			CleanupPowerups();
			CleanupPlatforms();
		}

		ui?.UpdateUI(player, platforms.Count, powerups.Count(p => p != null && !p.IsCollected));
	}

	private void StartDying()
	{
		isDying = true;
		deathTimer = 0f;
		if (fallingScreen != null)
		{
			fallingScreen.Visible = true;
			if (camera != null)
				fallingScreen.GlobalPosition = camera.GlobalPosition - GetViewportRect().Size / 2;
		}
	}

	private void ShowGameOver()
	{
		if (fallingScreen != null) fallingScreen.Visible = false;
		if (gameOverScreen != null)
		{
			gameOverScreen.Visible = true;
			var scoreLabel = gameOverScreen.GetNodeOrNull<Label>("ScoreLabel");
			if (scoreLabel != null) scoreLabel.Text = $"Final Score: {player?.Score ?? 0:F0}";
			if (camera != null)
				gameOverScreen.GlobalPosition = camera.GlobalPosition - GetViewportRect().Size / 2;
		}
	}

	private void OnPlayerDied() => StartDying();

	private void ResetGame()
	{
		isGameOver = false;
		isDying = false;
		deathTimer = 0f;

		foreach (Node child in platformsContainer.GetChildren()) child?.QueueFree();
		foreach (Node child in powerupsContainer.GetChildren()) child?.QueueFree();

		platforms.Clear();
		powerups.Clear();
		player?.Reset();
		if (camera != null) camera.GlobalPosition = new Vector2(400, 300);

		if (ui != null) ui.GlobalPosition = Vector2.Zero;

		if (gameOverScreen != null) gameOverScreen.Visible = false;
		if (fallingScreen != null) fallingScreen.Visible = false;

		GenerateInitialPlatforms();
		if (player != null) lastValidY = player.GlobalPosition.Y;
	}

	private void GenerateInitialPlatforms()
	{
		var initialPlatforms = new[]
		{
			new { x = 200f, y = 450f, width = 120f, type = "normal" },
			new { x = 400f, y = 380f, width = 100f, type = "normal" },
			new { x = 100f, y = 320f, width = 80f, type = "normal" },
			new { x = 550f, y = 300f, width = 90f, type = "gold" },
			new { x = 300f, y = 240f, width = 110f, type = "normal" },
			new { x = 150f, y = 180f, width = 100f, type = "icy" },
			new { x = 500f, y = 120f, width = 80f, type = "rough" }
		};

		foreach (var platformData in initialPlatforms)
		{
			switch (platformData.type)
			{
				case "gold":
					CreateGoldPlatform(platformData.x, platformData.y, platformData.width);
					break;
				case "icy":
					CreateIcyPlatform(platformData.x, platformData.y, platformData.width);
					break;
				case "rough":
					CreateRoughPlatform(platformData.x, platformData.y, platformData.width);
					break;
				default:
					CreatePlatform(platformData.x, platformData.y, platformData.width);
					break;
			}
			lastPlatformX = platformData.x;
		}
		lastPlatformY = 120;
	}

	private void GeneratePlatforms()
	{
		int platformsToAdd = Math.Min(10, maxPlatforms - platforms.Count);

		for (int i = 0; i < platformsToAdd; i++)
		{
			float newY = lastPlatformY - random.Next(60, 120);
			float newWidth = random.Next(80, 150);

			bool goLeft = random.Next(0, 2) == 0;
			float distance = random.Next(80, (int)MAX_PLATFORM_DISTANCE);
			float newX = goLeft ? lastPlatformX - distance : lastPlatformX + distance;

			float screenWidth = GetViewportRect().Size.X;
			float margin = 100f;
			newX = Math.Max(margin, Math.Min(newX, screenWidth - newWidth - margin));

			if (Math.Abs(newX - lastPlatformX) > MAX_PLATFORM_DISTANCE)
			{
				newX = goLeft ? lastPlatformX + distance : lastPlatformX - distance;
				newX = Math.Max(margin, Math.Min(newX, screenWidth - newWidth - margin));
			}

			if (Math.Abs(newX - lastPlatformX) > MAX_PLATFORM_DISTANCE)
			{
				newX = lastPlatformX + (goLeft ? -MAX_PLATFORM_DISTANCE + 20 : MAX_PLATFORM_DISTANCE - 20);
				newX = Math.Max(margin, Math.Min(newX, screenWidth - newWidth - margin));
			}

			int platformType = random.Next(0, 100);
			if (platformType < 5)
				CreateGoldPlatform(newX, newY, newWidth);
			else if (platformType < 15)
				CreateIcyPlatform(newX, newY, newWidth);
			else if (platformType < 25)
				CreateRoughPlatform(newX, newY, newWidth);
			else
				CreatePlatform(newX, newY, newWidth);

			if (random.Next(0, 100) < 15)
			{
				float powerupX = newX + random.Next(0, (int)newWidth - 20);
				float powerupY = newY - 25;
				CreatePowerup(powerupX, powerupY);
			}

			lastPlatformX = newX;
			lastPlatformY = newY;
		}
	}

	private void CreatePlatform(float x, float y, float width)
	{
		if (PlatformScene == null) return;

		var platformInstance = PlatformScene.Instantiate<Platform>();
		if (platformInstance == null) return;

		platformInstance.GlobalPosition = new Vector2(x, y);
		platformInstance.SetSize(width, 15);
		platformsContainer.AddChild(platformInstance);
		platforms.Add(platformInstance);
	}

	private void CreateGoldPlatform(float x, float y, float width)
	{
		if (GoldPlatformScene == null) return;

		var platformInstance = GoldPlatformScene.Instantiate<GoldPlatform>();
		if (platformInstance == null) return;

		platformInstance.GlobalPosition = new Vector2(x, y);
		platformInstance.SetSize(width, 15);
		platformsContainer.AddChild(platformInstance);
		platforms.Add(platformInstance);
	}

	private void CreateIcyPlatform(float x, float y, float width)
	{
		if (IcyPlatformScene == null) return;

		var platformInstance = IcyPlatformScene.Instantiate<IcyPlatform>();
		if (platformInstance == null) return;

		platformInstance.GlobalPosition = new Vector2(x, y);
		platformInstance.SetSize(width, 15);
		platformsContainer.AddChild(platformInstance);
		platforms.Add(platformInstance);
	}

	private void CreateRoughPlatform(float x, float y, float width)
	{
		if (RoughPlatformScene == null) return;

		var platformInstance = RoughPlatformScene.Instantiate<RoughPlatform>();
		if (platformInstance == null) return;

		platformInstance.GlobalPosition = new Vector2(x, y);
		platformInstance.SetSize(width, 15);
		platformsContainer.AddChild(platformInstance);
		platforms.Add(platformInstance);
	}

	private void CreatePowerup(float x, float y)
	{
		if (PowerupScene == null) return;

		var powerupInstance = PowerupScene.Instantiate<PowerupMultiplier>();
		if (powerupInstance == null) return;

		powerupInstance.GlobalPosition = new Vector2(x, y);
		powerupsContainer.AddChild(powerupInstance);
		powerups.Add(powerupInstance);
	}

	private void CleanupPlatforms()
	{
		float cleanupThreshold = Math.Max(player.GlobalPosition.Y + GetViewportRect().Size.Y * 2, lastValidY + GetViewportRect().Size.Y * 2);
		var platformsToRemove = platforms.Where(platform => platform != null && platform.GlobalPosition.Y > cleanupThreshold).ToList();

		foreach (var platform in platformsToRemove)
		{
			platform?.QueueFree();
			platforms.Remove(platform);
		}
	}

	private void CleanupPowerups()
	{
		float cleanupThreshold = Math.Max(player.GlobalPosition.Y + GetViewportRect().Size.Y * 2, lastValidY + GetViewportRect().Size.Y * 2);
		var powerupsToRemove = powerups.Where(powerup => powerup != null && (powerup.GlobalPosition.Y > cleanupThreshold || powerup.IsCollected)).ToList();

		foreach (var powerup in powerupsToRemove)
		{
			powerup?.QueueFree();
			powerups.Remove(powerup);
		}
	}
}
