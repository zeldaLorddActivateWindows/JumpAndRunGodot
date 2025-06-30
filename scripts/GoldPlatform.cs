using Godot;

namespace JumpAndRun.scripts
{
    public partial class GoldPlatform : Platform
    {
        private bool hasBeenUsed = false;

        public override void _Ready()
        {
            base._Ready();
        }

        public void OnPlayerLanded(Player player)
        {
            if (!hasBeenUsed)
            {
                hasBeenUsed = true;
                player.Score += 100;
                GD.Print($"Gold platform bonus! Updated Score: {player.Score}");
            }
        }
    }
}