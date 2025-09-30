using Cysharp.Threading.Tasks;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;

[RequireComponent(typeof(BoxCollider2D))]
public class RoomGateTrigger : MonoBehaviour
{
    [SerializeField] Grid grid;
    [SerializeField] Tilemap baseMap;
    
    private readonly string[] combatRoomTags = new[] { ConstClass.Tags.CombatRoom, ConstClass.Tags.EliteRoom, ConstClass.Tags.BossRoom };  //전투 방으로 판정할 태그 (게이트를 열고/닫는 이벤트용 Tag)
    private readonly bool closeWhenSteppedInside = true;  //방 내부 entryDepthCells 칸 수만큼 밟았을 때만 문 닫기
    private readonly int entryDepthCells = 1;   //내부 감지 깊이(타일 수)

    private Player enteredPlayer;  //방에 들어온 캐릭터
    private RoomPlacer placer;
    private bool lockedOnce;

    private void Start()
    {
        InitAsync().Forget();
    }

    //비동기 초기화 (1프레임 양보 필요 시 사용)
    private async UniTaskVoid InitAsync()
    {
        if (!placer) placer = FindObjectOfType<RoomPlacer>();

        var bc = GetComponent<BoxCollider2D>();
        bc.isTrigger = true;

        await UniTask.Yield();

        if (!TryGetRoomWorldBounds(out Bounds worldBounds))
        {
            Debug.Log("[RoomGateTrigger2D] Tilemap_Base을 찾지 못해 트리거 크기 계산 건너뜀");
            return;
        }

        transform.localPosition = Vector3.zero;

        //월드 → 로컬 (이 컴포넌트가 붙은 Transform 기준) 변환
        Vector3 localCenter3 = transform.InverseTransformPoint(worldBounds.center);
        Vector3 localSize3 = transform.InverseTransformVector(worldBounds.size);
        Vector2 localSize = new(Mathf.Abs(localSize3.x), Mathf.Abs(localSize3.y));

        bc.offset = localCenter3;
        bc.size = localSize;
    }

    //'현재 방 루트' 하위에서만 타일맵 경계를 합산
    private bool TryGetRoomWorldBounds(out Bounds worldBounds)
    {
        Bounds baseWorld = GetWorldBoundsFromLocalBounds(baseMap);
        worldBounds = baseWorld;
        return true;
    }

    //월드 경계 변환
    private Bounds GetWorldBoundsFromLocalBounds(Tilemap tm)
    {
        var lb = tm.localBounds;
        Vector3 wmin = tm.transform.TransformPoint(lb.min);
        Vector3 wmax = tm.transform.TransformPoint(lb.max);
        Vector3 min = new(Mathf.Min(wmin.x, wmax.x), Mathf.Min(wmin.y, wmax.y), Mathf.Min(wmin.z, wmax.z));
        Vector3 max = new(Mathf.Max(wmin.x, wmax.x), Mathf.Max(wmin.y, wmax.y), Mathf.Max(wmin.z, wmax.z));
        var b = new Bounds();
        b.SetMinMax(min, max);
        return b;
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (!Application.isPlaying) return;
        if (!other) return;

        var ch = other.GetComponentInParent<Player>();
        if (!ch) return;  //플레이어만 통과

        //'안쪽 ~ 칸 닫기' 모드면 여기서는 닫지 않는다. (OnTriggerStay2D로 처리)
        if (closeWhenSteppedInside) return;
        if (lockedOnce) return;

        if (!placer) placer = FindObjectOfType<RoomPlacer>();
        if (!placer) return;

        enteredPlayer = ch;
        lockedOnce = true;
        HandleEnterRoom();
    }

    private void OnTriggerStay2D(Collider2D other)
    {
        if (!Application.isPlaying) return;
        if (!closeWhenSteppedInside) return;
        if (lockedOnce) return;
        if (!other) return;

        var ch = other.GetComponentInParent<Player>();
        if (!ch) return;  //플레이어만 통과

        if (!placer) placer = FindObjectOfType<RoomPlacer>();
        if (!placer) return;

        //플레이어가 방 경계로부터 'entryDepthCells' 이상 내부로 들어왔는 지 체크
        if (!IsSteppedInsideDepth(other.transform.position)) return;

        enteredPlayer = ch;
        lockedOnce = true;
        HandleEnterRoom();
    }

    private bool IsSteppedInsideDepth(Vector3 playerWorldPos)
    {
        if (!baseMap) return true;
        var bounds = GetWorldBoundsFromLocalBounds(baseMap);

        //Grid/Tile 크기 기준으로 '~ 칸' 깊이 계산
        var cell = grid ? grid.cellSize : Vector3.one;
        float dx = entryDepthCells * Mathf.Abs(cell.x);
        float dy = entryDepthCells * Mathf.Abs(cell.y);

        //방의 '내부' 경계를 ~ 칸만큼 안쪽으로 축소한 사각형
        var innerMin = new Vector2(bounds.min.x + dx, bounds.min.y + dy);
        var innerMax = new Vector2(bounds.max.x - dx, bounds.max.y - dy);

        //플레이어가 이 내부 사각형에 들어왔을 때만 true
        return (playerWorldPos.x >= innerMin.x && playerWorldPos.x <= innerMax.x &&
                playerWorldPos.y >= innerMin.y && playerWorldPos.y <= innerMax.y);
    }

    private void HandleEnterRoom()
    {
        //전투 방이 아니면 중단
        if (!IsCombatRoom()) return;
        if (!TryGetRoomWorldBounds(out Bounds worldBounds)) return;

        //현재 방 루트에서 Encounter 검색
        var roomRoot = GetRoomRootGO();
        var rommEncounter = roomRoot.GetComponentInChildren<RoomEncounter>();

        //플레이어 전투 On
        if (enteredPlayer) rommEncounter.OnPlayerEntered(enteredPlayer, worldBounds);
    }

    private bool IsCombatRoom()
    {
        var roomRoot = GetRoomRootGO();

        //1) Tag 화이트리스트
        foreach (var t in combatRoomTags)
        {
            if (!string.IsNullOrEmpty(t) && roomRoot.CompareTag(t)) return true;
        }
        return false;
    }

    //방 루트 찾기
    private GameObject GetRoomRootGO()
    {
        //placer가 아직 null일 수 있으니 한 번 더 확보
        if (!placer) placer = FindObjectOfType<RoomPlacer>();
        var roomsRoot = placer ? placer.CurRooomsRoot : null;

        Transform t = transform;
        //roomsRoot 바로 아래에 걸릴 때까지 부모로 상승
        while (t.parent != null && t.parent != roomsRoot)
            t = t.parent;

        return t.gameObject;
    }
}