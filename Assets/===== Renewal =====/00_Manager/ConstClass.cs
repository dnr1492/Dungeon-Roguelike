using UnityEngine;

public static class ConstClass
{
    public static class Tags
    {
        public const string Player = "Player";
        public const string CombatRoom = "CombatRoom";
        public const string EliteRoom = "EliteRoom";
        public const string BossRoom = "BossRoom";
        public const string ShopRoom = "ShopRoom";
        public const string EventRoom = "EventRoom";
    }

    public static class Layers
    {
        public const string Player = "Player";
        public const string MonsterBody = "MonsterBody";
        public const string Wall = "Wall";

        public static int Id(string name) => LayerMask.NameToLayer(name);
    }

    public static class Masks
    {
        public static readonly LayerMask Enemy = LayerMask.GetMask(Layers.MonsterBody);
        public static readonly LayerMask Wall = LayerMask.GetMask(Layers.Wall);
    }
}
