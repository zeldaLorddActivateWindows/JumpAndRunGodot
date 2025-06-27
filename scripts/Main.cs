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
	private float lastPlatformX = 400; // Track last platform X position
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
	private const float MAX_PLATFORM_DISTANCE = 180f; // Maximum horizontal distance between platforms

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

		// Update camera to follow player both vertically and horizontally
		if (camera != null) 
		{
			Vector2 targetPos = new Vector2(player.GlobalPosition.X, Math.Min(player.GlobalPosition.Y, lastValidY));
			camera.GlobalPosition = targetPos;
		}

		// Update UI position to follow camera/player
		if (ui != null && camera != null)
		{
			ui.GlobalPosition = camera.GlobalPosition - GetViewportRect().Size / 2;
		}

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
		
		// Update falling screen position to follow camera
		if (fallingScreen != null && camera != null)
		{
			fallingScreen.GlobalPosition = camera.GlobalPosition - GetViewportRect().Size / 2;
		}
	}

	private void ShowGameOver()
	{
		fallingScreen?.CallDeferred("set_visible", false);
		gameOverScreen?.CallDeferred("set_visible", true);
		var scoreLabel = gameOverScreen?.GetNodeOrNull<Label>("ScoreLabel");
		if (scoreLabel != null) scoreLabel.Text = $"Final Score: {player?.Score ?? 0:F0}";
		
		// Update game over screen position to follow camera
		if (gameOverScreen != null && camera != null)
		{
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
		
		// Reset UI position
		if (ui != null)
		{
			ui.GlobalPosition = Vector2.Zero;
		}
		
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

		foreach (var platformData in initialPlatforms) 
		{
			CreatePlatform(platformData.x, platformData.y, platformData.width);
			lastPlatformX = platformData.x; // Update last platform X
		}
		lastPlatformY = 120;
	}

	private void GeneratePlatforms()
	{
		while (platforms.Count < maxPlatforms)
		{
			float newY = lastPlatformY - random.Next(60, 120);
			float newWidth = random.Next(80, 150);
			
			// Calculate X position within jumping distance of last platform
			float minDistance = 80f; // Minimum distance to avoid platforms being too close
			float maxDistance = MAX_PLATFORM_DISTANCE; // Maximum distance for reachability
			
			// Generate a random direction and distance
			bool goLeft = random.Next(0, 2) == 0;
			float distance = random.Next((int)minDistance, (int)maxDistance);
			float newX = goLeft ? lastPlatformX - distance : lastPlatformX + distance;
			
			// Ensure platform stays within screen bounds with some margin
			float screenWidth = GetViewportRect().Size.X;
			float margin = 100f;
			newX = Math.Max(margin, Math.Min(newX, screenWidth - newWidth - margin));
			
			// If the clamping moved the platform too far, try the opposite direction
			if (Math.Abs(newX - lastPlatformX) > maxDistance)
			{
				newX = goLeft ? lastPlatformX + distance : lastPlatformX - distance;
				newX = Math.Max(margin, Math.Min(newX, screenWidth - newWidth - margin));
			}
			
			// Final safety check - if still too far, place it closer
			if (Math.Abs(newX - lastPlatformX) > maxDistance)
			{
				newX = lastPlatformX + (goLeft ? -maxDistance + 20 : maxDistance - 20);
				newX = Math.Max(margin, Math.Min(newX, screenWidth - newWidth - margin));
			}

			CreatePlatform(newX, newY, newWidth);
			
			// Occasionally add powerups
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
