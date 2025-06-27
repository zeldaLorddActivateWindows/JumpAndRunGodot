using Godot;
using System;

public partial class UI : Control
{
    private Label statusLabel;
    private Label doubleJumpLabel;
    private Label scoreLabel;
    private Label heightLabel;
    private Label platformsLabel;
    private Label powerupsLabel;

    public override void _Ready()
    {
        var bottomLeft = GetNode<VBoxContainer>("BottomLeft");
        statusLabel = bottomLeft.GetNode<Label>("StatusLabel");
        doubleJumpLabel = bottomLeft.GetNode<Label>("DoubleJumpLabel");
        scoreLabel = bottomLeft.GetNode<Label>("ScoreLabel");
        heightLabel = bottomLeft.GetNode<Label>("HeightLabel");
        platformsLabel = bottomLeft.GetNode<Label>("PlatformsLabel");
        powerupsLabel = bottomLeft.GetNode<Label>("PowerupsLabel");
    }

    public void UpdateUI(Player player, int platformCount, int powerupCount)
    {
        // Update status
        string groundedText = player.IsGrounded ? "Grounded" : "Airborne";
        statusLabel.Text = $"Status: {groundedText}";
        
        // Update double jump status
        if (!player.IsGrounded)
        {
            string doubleJumpText = player.CanDoubleJump ? "Double Jump Available" : "Double Jump Used";
            doubleJumpLabel.Text = doubleJumpText;
            doubleJumpLabel.Visible = true;
        }
        else
        {
            doubleJumpLabel.Visible = false;
        }
        
        // Update score and height
        scoreLabel.Text = $"Score: {player.Score:F0}";
        float height = Math.Max(0, 500 - player.GlobalPosition.Y);
        heightLabel.Text = $"Height: {height:F0}m";
        
        // Update counts
        platformsLabel.Text = $"Platforms: {platformCount}";
        powerupsLabel.Text = $"Powerups: {powerupCount}";
    }
}