using UnityEngine;

[CreateAssetMenu(menuName = "Dungeon/RoomThemeDB", fileName = "RoomThemeDB")]
public class RoomThemeDB : ScriptableObject
{
    public ThemeSet[] themeSets;

    public bool TryGet(ThemeId theme, out ThemeSet set)
    {
        if (themeSets != null)
        {
            foreach (var ts in themeSets)
            {
                if (ts.theme == theme) { set = ts; return true; }
            }
        }
        set = default;
        return false;
    }
}