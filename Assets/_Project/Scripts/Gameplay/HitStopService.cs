namespace WobbleBrawl.Gameplay
{
    public static class HitStopService
    {
        public static void Apply(Fighter attacker, Fighter defender, int frames)
        {
            attacker.Freeze(frames);
            defender.Freeze(frames);
        }
    }
}
