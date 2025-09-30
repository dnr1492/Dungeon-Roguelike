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
        public const string Monster = "Monster";
        public const string Wall = "Wall";

        public static int Id(string name) => LayerMask.NameToLayer(name);
    }

    public static class Masks
    {
        public static readonly LayerMask Player = LayerMask.GetMask(Layers.Player);
        public static readonly LayerMask Enemy = LayerMask.GetMask(Layers.Monster);
        public static readonly LayerMask Wall = LayerMask.GetMask(Layers.Wall);
    }

    public static class Camera
    {
        public const int SPRITE_PPU = 16;             //프로젝트 스프라이트 PPU 값으로 설정
        public const int DESIGN_HEIGHT_PX = 180;      //가상 세로 해상도 (예: 320x180이면 180)
        public const int DESIGN_WIDTH_PX = 320;       //가상 가로 해상도
        public const bool CONSTANT_VERTICAL = false;  //true: 세로 고정, false: 가로 고정
    }
}
