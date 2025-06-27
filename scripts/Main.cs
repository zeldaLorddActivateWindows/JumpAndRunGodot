using Godot;
using System;
using System.Collections.Generic;
using System.Linq;

public partial class Main : Node2D
{
	[Export] public PackedScene PlatformScene { get; set; }
	[Export] public PackedScene PowerupScene { get; set; }
	
	private int maxPlatforms = 100;
	private List<Platform> platforms = new List<Platform>();
	private List<PowerupMultiplier> powerups = new List<PowerupMultiplier>();
	private float lastPlatformY = 400;
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

	public override void _Ready()
	{
		if (ResourceLoader.Exists("res://scenes/Platform.tscn")) PlatformScene = GD.Load<PackedScene>("res://scenes/Platform.tscn");
		else GD.PrintErr("Platform scene not found!");
		if (ResourceLoader.Exists("res://scenes/PowerupMultiplier.tscn")) PowerupScene = GD.Load<PackedScene>("res://scenes/PowerupMultiplier.tscn");
		camera = GetNodeOrNull<Camera2D>("Camera2D") ?? new Camera2D();
		player = GetNodeOrNull<Player>("Player");
		platformsContainer = GetNodeOrNull<Node2D>("Platforms") ?? new Node2D();
		powerupsContainer = GetNodeOrNull<Node2D>("Powerups") ?? new Node2D();
		gameOverScreen = GetNodeOrNull<Control>("GameOverScreen");
		fallingScreen = GetNodeOrNull<Control>("FallingScreen");
		ui = GetNodeOrNull<UI>("UI");
		
		if (player == null) 
		{
			GD.PrintErr("Player node not found!");
			return;
		}

		GenerateInitialPlatforms();
		lastValidY = player.GlobalPosition.Y;
		player.PlayerDied += OnPlayerDied;
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

		maxPlatforms++;

		foreach (var powerup in powerups.ToList())
		{
			if (powerup?.CheckCollision(player) == true)
			{
				powerup.OnCollision(player);
				if (powerup.IsCollected)
				{
					powerup.QueueFree();
					powerups.Remove(powerup);
				}
			}
		}

		if (player.GlobalPosition.Y > DEATH_BOUNDARY)
		{
			StartDying();
			return;
		}

		if (player.GlobalPosition.Y > lastValidY + GetViewportRect().Size.Y * 1.5f && player.Velocity.Y > 0)
		{
			StartDying();
			return;
		}

		if (camera != null) camera.GlobalPosition = new Vector2(camera.GlobalPosition.X, Math.Min(player.GlobalPosition.Y, lastValidY));

		if (player.GlobalPosition.Y < lastValidY - 50)
		{
			lastValidY = player.GlobalPosition.Y;
			CleanupPowerups();
			CleanupPlatforms();
			GeneratePlatforms();
		}
			ui?.UpdateUI(player, platforms.Count, powerups.Count(p => p?.IsCollected == false));
	}

	private void StartDying()
	{
		isDying = true;
		deathTimer = 0f;
		fallingScreen?.CallDeferred("set_visible", true);
	}

	private void ShowGameOver()
	{
		fallingScreen?.CallDeferred("set_visible", false);
		gameOverScreen?.CallDeferred("set_visible", true);
		var scoreLabel = gameOverScreen?.GetNodeOrNull<Label>("ScoreLabel");
		if (scoreLabel != null) scoreLabel.Text = $"Final Score: {player?.Score ?? 0:F0}";
		
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
		gameOverScreen?.CallDeferred("set_visible", false);
		fallingScreen?.CallDeferred("set_visible", false);
		
		GenerateInitialPlatforms();
		if (player != null) lastValidY = player.GlobalPosition.Y;
	}

	private void GenerateInitialPlatforms()
	{
		var initialPlatforms = new[]
		{
			new { x = 200f, y = 450f, width = 120f },
			new { x = 400f, y = 380f, width = 100f },
			new { x = 100f, y = 320f, width = 80f },
			new { x = 550f, y = 300f, width = 90f },
			new { x = 300f, y = 240f, width = 110f },
			new { x = 150f, y = 180f, width = 100f },
			new { x = 500f, y = 120f, width = 80f }
		};

		foreach (var platformData in initialPlatforms) CreatePlatform(platformData.x, platformData.y, platformData.width);
		lastPlatformY = 120;
	}

	private void GeneratePlatforms()
	{
		while (platforms.Count < maxPlatforms)
		{
			float newY = lastPlatformY - random.Next(60, 120);
			float newX = random.Next(50, (int)GetViewportRect().Size.X - 200);
			float newWidth = random.Next(80, 150);
			newX = Math.Max(0, Math.Min(newX, GetViewportRect().Size.X - newWidth));

			bool tooFar = true;
			int attempts = 0;
			while (tooFar && attempts < 10)
			{
				float minX = Math.Max(0, newX - 200);
				float maxX = Math.Min(GetViewportRect().Size.X - newWidth, newX + 200);
				newX = random.Next((int)minX, (int)maxX);

				tooFar = platforms.Any(p => p != null && Math.Abs(p.GlobalPosition.X - newX) > 300);
				attempts++;
			}

			CreatePlatform(newX, newY, newWidth);
			if (random.Next(0, 100) < 15)
			{
				float powerupX = newX + random.Next(0, (int)newWidth - 20);
				float powerupY = newY - 25;
				CreatePowerup(powerupX, powerupY);
			}
			lastPlatformY = newY;
		}
	}

	private void CreatePlatform(float x, float y, float width)
	{
		if (PlatformScene == null)
		{
			GD.PrintErr("Platform scene not loaded!");
			return;
		}

		try
		{
			var platformInstance = PlatformScene.Instantiate<Platform>();
			if (platformInstance == null)
			{
				GD.PrintErr("Failed to instantiate platform");
				return;
			}

			platformInstance.GlobalPosition = new Vector2(x, y);
			platformInstance.SetSize(width, 15);
			platformsContainer.AddChild(platformInstance);
			platforms.Add(platformInstance);
		}
		catch (Exception e)
		{
			GD.PrintErr($"Error during platform gen: {e.Message}");
		}
	}

	private void CreatePowerup(float x, float y)
	{
		if (PowerupScene == null) return;
		try
		{
			var powerupInstance = PowerupScene.Instantiate<PowerupMultiplier>();
			if (powerupInstance == null) return;

			powerupInstance.GlobalPosition = new Vector2(x, y);
			powerupsContainer.AddChild(powerupInstance);
			powerups.Add(powerupInstance);
		}
		catch (Exception e)
		{
			GD.PrintErr($"Error creating powerup: {e.Message}");
		}
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
		
		if (platforms.Count < maxPlatforms) GeneratePlatforms();
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
