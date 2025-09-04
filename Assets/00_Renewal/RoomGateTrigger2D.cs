using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.Tilemaps;

[RequireComponent(typeof(BoxCollider2D))]
public class RoomGateTrigger2D : MonoBehaviour
{
    [SerializeField] Tilemap baseMap;
    
    private readonly string playerTag = "Player";
    private readonly string[] combatRoomTags = new[] { "CombatRoom", "BossRoom", "EliteRoom" };  //전투 방으로 판정할 태그 (게이트를 열고/닫는 이벤트용 Tag)

    private RoomPlacer placer;
    private bool lockedOnce;
    private PlacedRoom myRoom;  //내가 속한 배치 방 캐시

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

        bc.offset = (Vector2)localCenter3;
        bc.size = localSize;

        //'씬 루트'가 아니라 현재 방 루트로 캐시
        var roomRootGO = GetRoomRootGO();
        if (placer != null) myRoom = placer.FindPlacedRoomByInstance(roomRootGO);
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
        if (lockedOnce) return;
        if (!other || !other.CompareTag(playerTag)) return;

        Debug.Log(other.gameObject.name);

        if (!placer) placer = FindObjectOfType<RoomPlacer>();
        if (!placer) return;

        lockedOnce = true;
        HandleEnterRoom().Forget();
    }

    private async UniTaskVoid HandleEnterRoom()
    {
        //전투 방 판정 — 전투 방이 아니면 아무 것도 하지 않음
        if (!IsCombatRoom()) return;

        //현재 방만 잠금
        if (placer != null && myRoom != null) placer.LockRoom(myRoom);

        // ===== TODO: '적 전멸' 이벤트로 교체 ===== //
        await UniTask.Delay(3000); 

        //현재 방만 해제
        if (placer != null && myRoom != null) placer.UnlockRoom(myRoom);
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
        var roomsRoot = placer ? placer.roomsRoot : null;

        Transform t = transform;
        //roomsRoot 바로 아래에 걸릴 때까지 부모로 상승
        while (t.parent != null && t.parent != roomsRoot)
            t = t.parent;

        return t.gameObject;
    }
}