using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// [기능]: 2D 피하기 미니게임(GradeRunner)에서 가비지 컬렉션(GC) 최소화를 위한 오브젝트 풀링을 기반으로 하늘에서 떨어지는 코드/족보를 적절히 스폰하는 Spawner View.
///         스폰 영역 한계를 스포너 뷰 오브젝트에 부착된 BoxCollider2D 컴포넌트의 가로/세로 바운드 크기로 정밀 연동합니다.
/// [작성자]: 윤승종
/// </summary>
namespace GameArifiction.GradeRunner
{
    public class FallingObjectSpawnerView : MonoBehaviour
    {
        #region UI 참조 (Inspector)

        [Header("스폰 영역 설정")]
        [SerializeField]
        [Tooltip("스폰 가로 폭과 시작 높이를 결정할 스포너 오브젝트의 BoxCollider2D 컴포넌트입니다. 지정하지 않을 시 스포너 자체의 Collider2D를 자동 탐색합니다.")]
        private BoxCollider2D m_spawnAreaCollider;

        [Header("낙하물 프리팹")]
        [SerializeField]
        [Tooltip("코드(장애물)로 스폰할 프리팹 오브젝트입니다.")]
        private GameObject m_codePrefab;

        [SerializeField]
        [Tooltip("족보(아이템)로 스폰할 프리팹 오브젝트입니다.")]
        private GameObject m_cheatSheetPrefab;

        [SerializeField]
        [Tooltip("피해야 할 C# 코드 단어 리스트 에셋(ScriptableObject)입니다.")]
        private GradeRunnerCodeListSO m_codeListSO;

        [Header("스폰 기준 바닥")]
        [SerializeField]
        [Tooltip("낙하물 하강 속도 계산의 종착지가 될 바닥(Ground) Transform입니다.")]
        private Transform m_groundTransform;

        [Header("교수 공격 캐릭터 연동")]
        [SerializeField]
        [Tooltip("코드를 떨어뜨려 공격 연출을 할 씬 상의 교수님 캐릭터 뷰입니다.")]
        private ProfessorView m_professorView;

        #endregion

        #region 내부 필드 (Private Fields)

        private GradeRunnerViewModel m_viewModel;
        
        // 이터레이터 최적화를 위한 사전 용량 지정형 오브젝트 풀 리스트
        private readonly List<FallingObjectView> m_codePool = new List<FallingObjectView>(16);
        private readonly List<FallingObjectView> m_cheatSheetPool = new List<FallingObjectView>(8);

        private float m_minX;
        private float m_maxX;
        private float m_spawnY;
        private float m_groundY;

        #endregion

        #region 유니티 생명주기 (Unity Lifecycle)

        private void OnDestroy()
        {
            UnsubscribeEvents();
        }

        #endregion

        #region 초기화 (Initialization)

        /// <summary>
        /// [기능]: 뷰모델 이벤트를 바인딩하고, 지정된 BoxCollider2D 영역에 맞춰 스폰 가로 경계(X) 및 낙하 시작 높이(Y)를 산출합니다.
        /// [작성자]: 윤승종
        /// </summary>
        public void Initialize(GradeRunnerViewModel viewModel)
        {
            m_viewModel = viewModel;

            // 명시 지정이 없고 스스로에게 BoxCollider2D가 붙어있다면 자체 자동 할당
            if (m_spawnAreaCollider == null)
            {
                m_spawnAreaCollider = GetComponent<BoxCollider2D>();
            }

            CalculateSpawnCoordinates();

            // 스폰 신호 이벤트 바인딩
            if (m_viewModel != null)
            {
                m_viewModel.OnSpawnFallingObject += HandleSpawnFallingObject;
            }

            Debug.Log("[FallingObjectSpawnerView] 스포너 뷰 초기화 및 의존성 주입 완료.");
        }

        private void UnsubscribeEvents()
        {
            if (m_viewModel != null)
            {
                m_viewModel.OnSpawnFallingObject -= HandleSpawnFallingObject;
            }
        }

        #endregion

        #region 내부 메서드 (Private Methods)

        /// <summary>
        /// [기능]: 부착된 BoxCollider2D의 bounds를 기반으로 낙하물이 실질 스폰될 범위(X)와 높이(Y)를 연산하며 콜라이더 누락 시 카메라 연산으로 폴백합니다.
        /// [작성자]: 윤승종
        /// </summary>
        private void CalculateSpawnCoordinates()
        {
            if (m_spawnAreaCollider != null)
            {
                Bounds spawnBounds = m_spawnAreaCollider.bounds;

                m_minX = spawnBounds.min.x;
                m_maxX = spawnBounds.max.x;

                // 스폰 박스 콜라이더 영역의 상단부 Y(또는 센터 Y)를 스폰 높이로 지정
                m_spawnY = spawnBounds.center.y;

                // 혹시 모를 에디터 설정 역전 예방 가드
                if (m_minX >= m_maxX)
                {
                    m_minX = transform.position.x - 5f;
                    m_maxX = transform.position.x + 5f;
                }
            }
            else
            {
                // BoxCollider2D가 통째로 없을 시의 지능형 카메라 뷰포트 연산 폴백 작동
                CalculateCameraViewportBounds();
            }

            // 낙하가 종료되는 바닥 Y좌표
            if (m_groundTransform != null)
            {
                m_groundY = m_groundTransform.position.y;
            }
            else
            {
                m_groundY = -4f;
                Debug.LogWarning("[FallingObjectSpawnerView] 바닥(Ground) 지정 누락으로 강제 Y=-4 기준값을 삼습니다.");
            }
        }

        /// <summary>
        /// [기능]: 스폰 영역 콜라이더가 유실되었을 때 구동되는 안전 카메라 뷰포트 기반 스폰 높이/너비 연산 로직
        /// [작성자]: 윤승종
        /// </summary>
        private void CalculateCameraViewportBounds()
        {
            UnityEngine.Camera cam = UnityEngine.Camera.main;
            if (cam != null)
            {
                Vector3 leftBound = cam.ViewportToWorldPoint(new Vector3(0.05f, 0f, cam.nearClipPlane));
                Vector3 rightBound = cam.ViewportToWorldPoint(new Vector3(0.95f, 0f, cam.nearClipPlane));

                m_minX = leftBound.x;
                m_maxX = rightBound.x;

                Vector3 spawnTop = cam.ViewportToWorldPoint(new Vector3(0.5f, 1.1f, cam.nearClipPlane));
                m_spawnY = spawnTop.y;
            }
            else
            {
                m_minX = -7.5f;
                m_maxX = 7.5f;
                m_spawnY = 6f;
                Debug.LogWarning("[FallingObjectSpawnerView] 카메라를 찾을 수 없어 경계 기본값을 산정합니다.");
            }
        }

        /// <summary>
        /// [기능]: 뷰모델에서 스폰을 요청했을 때, 풀에서 유휴 오브젝트를 찾아 상단 무작위 X위치에 배치한 후 낙하 속도를 동적으로 주입합니다.
        /// [작성자]: 윤승종
        /// </summary>
        private void HandleSpawnFallingObject(FallingObjectType type, CodeColorType codeColor, float fallDuration)
        {
            FallingObjectView objInstance = GetOrCreateObject(type);
            if (objInstance == null)
            {
                return;
            }

            // 가로 범위 무작위 X좌표 추출
            float randomX = Random.Range(m_minX, m_maxX);
            objInstance.transform.position = new Vector3(randomX, m_spawnY, 0f);

            // [스토리 기믹 연동]: 코드 장애물 스폰 시 교수님 캐릭터가 해당 투하 지점 X좌표로 빠르게 이동하여 던지게 연출
            if (type == FallingObjectType.Code && m_professorView != null)
            {
                m_professorView.func_MoveTo(randomX);
            }

            // 속도 = 낙하 총거리(Y 차이) / 소요 희망시간(초)
            float totalDistance = m_spawnY - m_groundY;
            if (totalDistance <= 0f)
            {
                totalDistance = 10f; // 안전 수치
            }
            float speed = totalDistance / fallDuration;

            // 오브젝트 가동 및 상태 초기화 시 무작위 C# 단어 추출 및 전달
            string selectedWord = "";
            if (type == FallingObjectType.Code)
            {
                if (m_codeListSO != null)
                {
                    selectedWord = m_codeListSO.GetRandomCodeWord();
                }
                else
                {
                    selectedWord = "bug";
                }
            }

            objInstance.gameObject.SetActive(true);
            objInstance.Initialize(type, codeColor, speed, selectedWord);
        }

        /// <summary>
        /// [기능]: GC 방지를 위해 foreach 사용을 배제하고 인덱스 루프로 리스트를 탐색하여 비활성화된 오브젝트를 재활용하거나 새로 인스턴스화합니다.
        /// [작성자]: 윤승종
        /// </summary>
        private FallingObjectView GetOrCreateObject(FallingObjectType type)
        {
            List<FallingObjectView> targetPool = (type == FallingObjectType.Code) ? m_codePool : m_cheatSheetPool;
            GameObject prefab = (type == FallingObjectType.Code) ? m_codePrefab : m_cheatSheetPrefab;

            if (prefab == null)
            {
                Debug.LogError($"[FallingObjectSpawnerView] 스폰할 프리팹이 비어있습니다! 타입: {type}");
                return null;
            }

            int poolSize = targetPool.Count;
            for (int i = 0; i < poolSize; i++)
            {
                FallingObjectView element = targetPool[i];
                if (element != null && !element.gameObject.activeSelf)
                {
                    return element;
                }
            }

            GameObject spawnedGo = Instantiate(prefab, transform);
            if (spawnedGo != null)
            {
                FallingObjectView viewComponent = spawnedGo.GetComponent<FallingObjectView>();
                if (viewComponent != null)
                {
                    targetPool.Add(viewComponent);
                    return viewComponent;
                }
            }

            return null;
        }

        #endregion
    }
}
