using Godot;
using System;


public partial class Player : CharacterBody2D
{
	[Signal] public delegate void PlayerDiedEventHandler();

	public string PlayerName { get; set; } = "pexlover";
	public float Score { get; set; } = 0;
	public float XVelocity { get; set; } = 200;
	public float JumpStrength { get; set; } = 400;
	public bool IsGrounded { get; set; } = false;
	public bool CanDoubleJump { get; set; } = true;
	public int Width { get; set; } = 40;
	public int Height { get; set; } = 40;
	
	// Animation
	private AnimatedSprite2D animatedSprite; 
	private string currentDirection = "right"; 
	private bool wasGrounded = false; 
	
	private const float Gravity = 980;
	private const float MaxFallSpeed = 800;
	private const float GroundLevel = 500;
	private bool hasDoubleJumped = false;
	private float highestY = 400;
	private float coyoteTime = 0f;
	private const float CoyoteTimeLimit = 0.1f;
	private float scoreMultiplier = 1.0f;
	private Label nameLabel;

	public override void _Ready()
	{
		nameLabel = GetNode<Label>("NameLabel");
		nameLabel.Text = PlayerName;
		highestY = GlobalPosition.Y;
		animatedSprite = GetNode<AnimatedSprite2D>("AnimatedSprite2D");
		animatedSprite.Play("idle_right");
	}

	public override void _PhysicsProcess(double delta)
	{
		HandleInput((float)delta);

		bool wasGrounded = IsGrounded;
		IsGrounded = IsOnFloor();

		if (!IsGrounded)
		{
			Velocity = new Vector2(Velocity.X, Math.Min(Velocity.Y + Gravity * (float)delta, MaxFallSpeed));
			coyoteTime = wasGrounded ? CoyoteTimeLimit : Math.Max(0, coyoteTime - (float)delta);
			
			// Airborne animation
			if(Velocity.Y < 0){
				animatedSprite.Play("jump" + currentDirection);
			}
			else{
				animatedSprite.Play("fall" + currentDirection);
			}
		}
		else
		{
			coyoteTime = CoyoteTimeLimit;
			hasDoubleJumped = false;
			CanDoubleJump = true;
		}
		
		// Only play idle if not moving horizontally
		if (Mathf.Abs(Velocity.X) < 0.1f)
		{
			animatedSprite.Play("idle_right" + currentDirection);
		}

		MoveAndSlide();
		if (GlobalPosition.Y < highestY)
		{
			highestY = GlobalPosition.Y;
			Score = Math.Max(0, (GroundLevel - highestY) / 10) * scoreMultiplier;
		}
		ClampToScreenBounds();
	}

	public void ApplyScoreMultiplier(float multiplier)
	{
		scoreMultiplier *= multiplier;
		Score *= multiplier;
	}

	public void Reset()
	{
		GlobalPosition = new Vector2(100, 400);
		Velocity = Vector2.Zero;
		Score = 0;
		IsGrounded = false;
		CanDoubleJump = true;
		hasDoubleJumped = false;
		highestY = 400;
		coyoteTime = 0f;
		scoreMultiplier = 1.0f;
	}

	private void ClampToScreenBounds()
	{
		var screenSize = GetViewportRect().Size;
		var pos = GlobalPosition;
		pos.X = Math.Max(Width / 2, Math.Min(pos.X, screenSize.X - Width / 2));
		GlobalPosition = pos;
	}

	private void HandleInput(float delta)
	{
		
		Vector2 velocity = Velocity;
		
		// Horizontal movement and animation
		if (Input.IsActionPressed("move_left")){
			velocity.X = -XVelocity;
			currentDirection = "left";
			if(IsGrounded) animatedSprite.Play("walk_left");
		}
		else if (Input.IsActionPressed("move_right")){
			velocity.X = XVelocity;
			currentDirection = "right";
			if(IsGrounded) animatedSprite.Play("walk_right");
			
		}
		else{
			velocity.X = 0;
		}
		
		// Jumping
		if (Input.IsActionJustPressed("jump"))
		{
			if (IsGrounded || coyoteTime > 0)
			{
				velocity.Y = -JumpStrength;
				IsGrounded = false;
				coyoteTime = 0;
				animatedSprite.Play("jump" + currentDirection);
			}
			else if (CanDoubleJump && !hasDoubleJumped)
			{
				velocity.Y = -JumpStrength;
				hasDoubleJumped = true;
				CanDoubleJump = false;
				animatedSprite.Play("jump"+ currentDirection);
			}
		}
		Velocity = velocity;
	}
}
