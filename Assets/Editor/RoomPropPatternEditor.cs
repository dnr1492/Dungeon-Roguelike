using UnityEditor;
using UnityEngine;
using UnityEngine.Tilemaps;

[CustomEditor(typeof(RoomPropPattern))]
public class RoomPropPatternEditor : Editor
{
    private int rotIdx = 0;  //회전 상태: 0/1/2/3 → 0/90/180/270

    private void OnEnable()
    {
        Undo.undoRedoPerformed += Repaint;
        EditorApplication.playModeStateChanged += _ => Repaint();
    }

    private void OnDisable()
    {
        Undo.undoRedoPerformed -= Repaint;
        EditorApplication.playModeStateChanged -= _ => Repaint();
    }

    #region 인스펙터 하단에 그림을 출력
    public override void OnInspectorGUI()
    {
        //기본 필드 그리기
        EditorGUI.BeginChangeCheck();
        base.OnInspectorGUI();
        if (EditorGUI.EndChangeCheck()) Repaint();  //allow 체크 변경 시 프리뷰 갱신

        var pat = (RoomPropPattern)target;

        GUILayout.Space(8);
        EditorGUILayout.LabelField("Pattern Preview", EditorStyles.boldLabel);

        //회전 툴바 (허용 시에만 보임)
        using (new EditorGUI.DisabledScope(pat == null || pat.cells == null || pat.cells.Length == 0))
        {
            if (pat != null && pat.allowRotate90)
            {
                var labels = new[] { "0", "90", "180", "270" };

                EditorGUILayout.BeginHorizontal();
                GUILayout.FlexibleSpace();  //오른쪽에 정렬
                int newRot = GUILayout.Toolbar(rotIdx, labels, GUILayout.Width(160));
                EditorGUILayout.EndHorizontal();

                if (newRot != rotIdx) { rotIdx = newRot & 3; Repaint(); }
            }
            else
            {
                rotIdx = 0;
            }
        }

        GUILayout.Space(6);
        var r = GUILayoutUtility.GetRect(240, 240, GUILayout.ExpandWidth(true));
        DrawPreview(r, pat);
    }
    #endregion

    #region 인스펙터 프로젝트 창에 그림을 출력
    public override bool HasPreviewGUI() => true;

    public override void OnPreviewGUI(Rect r, GUIStyle background) => DrawPreview(r, (RoomPropPattern)target);
    #endregion

    private void DrawPreview(Rect r, RoomPropPattern pat)
    {
        if (pat == null || pat.cells == null || pat.cells.Length == 0)
        {
            EditorGUI.DropShadowLabel(r, "No Cells");
            return;
        }

        var list = new System.Collections.Generic.List<(Vector2Int ofs, RoomPropPattern.Cell cell)>(pat.cells.Length);
        foreach (var c in pat.cells)
        {
            var o = c.offset;
            if (pat.allowMirrorX) o = new Vector2Int(-o.x, o.y);
            if (pat.allowMirrorY) o = new Vector2Int(o.x, -o.y);
            if (pat.allowRotate90)
            {
                int k = rotIdx & 3;
                for (int i = 0; i < k; i++) o = new Vector2Int(-o.y, o.x);
            }
            list.Add((o, c));
        }

        int minX = 0, minY = 0, maxX = 0, maxY = 0;
        foreach (var (ofs, cell) in list)
        {
            minX = Mathf.Min(minX, ofs.x);
            minY = Mathf.Min(minY, ofs.y);
            maxX = Mathf.Max(maxX, ofs.x);
            maxY = Mathf.Max(maxY, ofs.y);
        }
        int cols = Mathf.Max(1, maxX - minX + 1);
        int rows = Mathf.Max(1, maxY - minY + 1);

        //배경
        EditorGUI.DrawRect(r, new Color(0.22f, 0.22f, 0.22f, 1f));

        //그릴 영역 (정사각 셀)
        float cellW = r.width / cols, cellH = r.height / rows;
        float scale = Mathf.Min(cellW, cellH);
        var draw = new Rect(
            r.x + (r.width - scale * cols) * .5f,
            r.y + (r.height - scale * rows) * .5f,
            scale * cols, scale * rows
        );

        //그리드 라인
        Handles.color = new Color(0f, 0f, 0f, .25f);
        for (int i = 0; i <= cols; i++)
        {
            float x = draw.x + i * (draw.width / cols);
            Handles.DrawLine(new Vector3(x, draw.y), new Vector3(x, draw.yMax));
        }
        for (int j = 0; j <= rows; j++)
        {
            float y = draw.y + j * (draw.height / rows);
            Handles.DrawLine(new Vector3(draw.x, y), new Vector3(draw.xMax, y));
        }

        //셀 그리기
        foreach (var t in list)
        {
            var tile = t.cell.tile as Tile; if (tile == null || tile.sprite == null) continue;
            var sp = tile.sprite; var tex = sp.texture; if (tex == null) continue;

            var tr = sp.textureRect;
            var uv = new Rect(tr.x / tex.width, tr.y / tex.height, tr.width / tex.width, tr.height / tex.height);

            int cx = t.ofs.x - minX;
            int cy = t.ofs.y - minY;

            var cell = new Rect(
                draw.x + cx * (draw.width / cols),
                draw.y + (rows - 1 - cy) * (draw.height / rows),
                draw.width / cols, draw.height / rows
            );

            var old = GUI.matrix;
            var pivot = cell.center;

            var S = new Vector3(pat.allowMirrorX ? -1f : 1f, pat.allowMirrorY ? -1f : 1f, 1f);
            float angle = (pat.allowRotate90 ? -(rotIdx & 3) * 90f : 0f);

            GUI.matrix = old *
                         Matrix4x4.TRS(pivot, Quaternion.identity, Vector3.one) *
                         Matrix4x4.Rotate(Quaternion.Euler(0, 0, angle)) *
                         Matrix4x4.Scale(S) *
                         Matrix4x4.TRS(-pivot, Quaternion.identity, Vector3.one);

            GUI.DrawTextureWithTexCoords(cell, tex, uv, true);
            GUI.matrix = old;

            //장애물 표시: Obstacle이면 테두리 표시
            if (t.cell.obstacle)
            {
                Handles.color = new Color(1f, .3f, .2f, .85f);
                Handles.DrawAAPolyLine(2f,
                    new Vector3(cell.x, cell.y),
                    new Vector3(cell.xMax, cell.y),
                    new Vector3(cell.xMax, cell.yMax),
                    new Vector3(cell.x, cell.yMax),
                    new Vector3(cell.x, cell.y));
            }
        }
    }
}