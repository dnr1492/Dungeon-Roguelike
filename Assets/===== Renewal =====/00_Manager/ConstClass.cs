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
        public const int SPRITE_PPU = 16;             //������Ʈ ��������Ʈ PPU ������ ����
        public const int DESIGN_HEIGHT_PX = 180;      //���� ���� �ػ� (��: 320x180�̸� 180)
        public const int DESIGN_WIDTH_PX = 320;       //���� ���� �ػ�
        public const bool CONSTANT_VERTICAL = false;  //true: ���� ����, false: ���� ����
    }
}
