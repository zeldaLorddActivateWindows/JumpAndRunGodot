using Godot;

public partial class Platform : StaticBody2D
{
	private ColorRect platformVisual;
	private ColorRect platformBorder;
	private CollisionShape2D collisionShape;
	
	public override void _Ready()
	{
		platformVisual = GetNode<ColorRect>("PlatformVisual");
		platformBorder = GetNode<ColorRect>("PlatformBorder");
		collisionShape = GetNode<CollisionShape2D>("CollisionShape2D");
	}
	
	public void SetSize(float width, float height)
	{
		// Update visual elements
		platformVisual.Size = new Vector2(width, height);
		platformVisual.Position = new Vector2(-width / 2, -height / 2);
		
		platformBorder.Size = new Vector2(width, height);
		platformBorder.Position = new Vector2(-width / 2, -height / 2);
		
		// Update collision shape
		var shape = collisionShape.Shape as RectangleShape2D;
		if (shape != null)
		{
			shape.Size = new Vector2(width, height);
		}
	}
}
