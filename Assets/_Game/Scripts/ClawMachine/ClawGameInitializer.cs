using UnityEngine;

namespace GameArifiction.ClawMachine
{
    /// <summary>
    /// [기능]: 인형뽑기 씬의 진입점(Composition Root). 싱글톤을 배제하고 의존성을 수동으로 주입합니다.
    /// [작성자]: 윤승종
    /// </summary>
    public class ClawGameInitializer : MonoBehaviour
    {
        #region UI 참조 (Inspector)
        [SerializeField]
        [Tooltip("게임 전체 UI와 입력을 관리하는 최상위 View 객체입니다.")]
        private ClawGameView m_gameView;
        #endregion

        #region 유니티 생명주기 (Unity Lifecycle)
        private void Start()
        {
            // 1. DTO 수신 (실제 환경에서는 씬 로더나 컨텍스트 매니저로부터 전달받음)
            var contextDTO = new ClawGameContextDTO(5, 60f, null); // 테스트 데이터
            
            // 2. Model 생성 (DTO 데이터 기반)
            var model = new ClawMachineModel(contextDTO.MaxPlayCount, contextDTO.TimeLimitPerPlay);
            
            // 3. ViewModel 생성
            var viewModel = new ClawGameViewModel(model);
            
            // 4. View 주입 (Dependency Injection)
            if (m_gameView != null)
            {
                m_gameView.Initialize(viewModel);
            }
            else
            {
                Debug.LogError("[ClawGameInitializer] ClawGameView가 할당되지 않았습니다. 인스펙터를 확인하세요.");
            }
            
            Debug.Log("[ClawGameInitializer] 인형뽑기 게임 초기화 완료 및 의존성 주입 성공.");
        }
        #endregion
    }
}
