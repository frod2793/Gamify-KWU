#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;
using System.IO;

namespace GameArifiction.EditorTools
{
    /// <summary>
    /// [기능]: 유니티 API를 이용해 메타 파일(.meta) 훼손 없이 최상위 에셋 폴더 및 내부 스크립트 레이어를 상용 규격으로 정돈하는 에디터 유틸리티
    /// [작성자]: [Senior Client Developer - Project Restructurer]
    /// </summary>
    public static class ProjectFolderOrganizer
    {
        private const string GAME_ROOT = "Assets/_Game";
        private const string THIRDPARTY_ROOT = "Assets/ThirdParty";
        private const string SCRIPTS_ROOT = "Assets/_Game/Scripts";

        /// <summary>
        /// [기능]: 에디터 메뉴 [Tools/Project Organizer/Execute Clean Up]를 통해 폴더 정리를 수행합니다.
        /// </summary>
        [MenuItem("Tools/Project Organizer/Execute Clean Up")]
        public static void func_ExecuteCleanUp()
        {
            Debug.Log("[ProjectFolderOrganizer] 안전한 전체 프로젝트 및 스크립트 레이어 정리를 개시합니다.");

            // ==========================================
            // PART 1. 최상위 프로젝트 에셋 폴더 정리 정돈
            // ==========================================

            // 1. 필요한 루트 폴더 동적 생성 가드
            CreateFolderIfNeeded("Assets", "ThirdParty");
            CreateFolderIfNeeded("Assets", "_Game");

            // 2. 자체 제작 리소스 _Game 하위로 메타 세이프 이동
            MoveAssetSafe("Assets/Scenes", $"{GAME_ROOT}/Scenes");
            MoveAssetSafe("Assets/Resources", $"{GAME_ROOT}/Resources");
            MoveAssetSafe("Assets/Settings", $"{GAME_ROOT}/Settings");

            // 3. 서드파티 패키지 격리 이동 (TextMesh Pro는 기본으로 최상위 유지하여 제외함)
            MoveAssetSafe("Assets/EasyTransitions", $"{THIRDPARTY_ROOT}/EasyTransitions");
            MoveAssetSafe("Assets/Holographic Cards", $"{THIRDPARTY_ROOT}/Holographic Cards");
            MoveAssetSafe("Assets/SPUM", $"{THIRDPARTY_ROOT}/SPUM");
            MoveAssetSafe("Assets/VirtualJoystick", $"{THIRDPARTY_ROOT}/VirtualJoystick");

            // 4. 불필요한 템플릿 제거
            DeleteAssetSafe("Assets/TutorialInfo");

            // ==========================================
            // PART 2. 스크립트(Scripts) 내부 MVVM 레이어 정돈
            // ==========================================
            if (AssetDatabase.IsValidFolder(SCRIPTS_ROOT))
            {
                // 1) Player 도메인 정돈
                string playerDir = $"{SCRIPTS_ROOT}/Player";
                if (AssetDatabase.IsValidFolder(playerDir))
                {
                    CreateFolderIfNeeded(playerDir, "Models");
                    CreateFolderIfNeeded(playerDir, "ViewModels");
                    CreateFolderIfNeeded(playerDir, "Views");

                    MoveAssetSafe($"{playerDir}/PlayerModel.cs", $"{playerDir}/Models/PlayerModel.cs");
                    MoveAssetSafe($"{playerDir}/PlayerViewModel.cs", $"{playerDir}/ViewModels/PlayerViewModel.cs");
                    MoveAssetSafe($"{playerDir}/PlayerView.cs", $"{playerDir}/Views/PlayerView.cs");
                }

                // 2) Map 도메인 정돈
                string mapDir = $"{SCRIPTS_ROOT}/Map";
                if (AssetDatabase.IsValidFolder(mapDir))
                {
                    CreateFolderIfNeeded(mapDir, "Models");
                    CreateFolderIfNeeded(mapDir, "ViewModels");
                    CreateFolderIfNeeded(mapDir, "Views");

                    MoveAssetSafe($"{mapDir}/MapModel.cs", $"{mapDir}/Models/MapModel.cs");
                    MoveAssetSafe($"{mapDir}/MapViewModel.cs", $"{mapDir}/ViewModels/MapViewModel.cs");
                    MoveAssetSafe($"{mapDir}/MapView.cs", $"{mapDir}/Views/MapView.cs");
                    MoveAssetSafe($"{mapDir}/PortalView.cs", $"{mapDir}/Views/PortalView.cs");
                }

                // 3) Claw 도메인 정돈 (고도화 물리 기반 스크립트)
                string clawDir = $"{SCRIPTS_ROOT}/Claw";
                if (AssetDatabase.IsValidFolder(clawDir))
                {
                    CreateFolderIfNeeded(clawDir, "Models");
                    CreateFolderIfNeeded(clawDir, "ViewModels");
                    CreateFolderIfNeeded(clawDir, "Views");
                    CreateFolderIfNeeded(clawDir, "Common");

                    MoveAssetSafe($"{clawDir}/ClawModel.cs", $"{clawDir}/Models/ClawModel.cs");
                    MoveAssetSafe($"{clawDir}/ClawViewModel.cs", $"{clawDir}/ViewModels/ClawViewModel.cs");
                    MoveAssetSafe($"{clawDir}/ClawView.cs", $"{clawDir}/Views/ClawView.cs");
                    MoveAssetSafe($"{clawDir}/CapsuleView.cs", $"{clawDir}/Views/CapsuleView.cs");
                    MoveAssetSafe($"{clawDir}/ClawState.cs", $"{clawDir}/Common/ClawState.cs");
                }

                // 4) CardGame 도메인 정돈
                string cardDir = $"{SCRIPTS_ROOT}/CardGame";
                if (AssetDatabase.IsValidFolder(cardDir))
                {
                    CreateFolderIfNeeded(cardDir, "Models");
                    CreateFolderIfNeeded(cardDir, "ViewModels");
                    CreateFolderIfNeeded(cardDir, "Views");
                    CreateFolderIfNeeded(cardDir, "States");
                    CreateFolderIfNeeded(cardDir, "Context");

                    MoveAssetSafe($"{cardDir}/CardModel.cs", $"{cardDir}/Models/CardModel.cs");
                    MoveAssetSafe($"{cardDir}/CardGameViewModel.cs", $"{cardDir}/ViewModels/CardGameViewModel.cs");
                    MoveAssetSafe($"{cardDir}/CardViewModel.cs", $"{cardDir}/ViewModels/CardViewModel.cs");
                    MoveAssetSafe($"{cardDir}/CardView.cs", $"{cardDir}/Views/CardView.cs");
                    MoveAssetSafe($"{cardDir}/GameStates.cs", $"{cardDir}/States/GameStates.cs");
                    MoveAssetSafe($"{cardDir}/IGameState.cs", $"{cardDir}/States/IGameState.cs");
                    MoveAssetSafe($"{cardDir}/CardGameContext.cs", $"{cardDir}/Context/CardGameContext.cs");
                }

                // 5) CraneGame 도메인 정돈 (퀴즈 뽑기 매니저 및 UI)
                string craneDir = $"{SCRIPTS_ROOT}/CraneGame";
                if (AssetDatabase.IsValidFolder(craneDir))
                {
                    CreateFolderIfNeeded(craneDir, "Models");
                    CreateFolderIfNeeded(craneDir, "ViewModels");
                    CreateFolderIfNeeded(craneDir, "Views");

                    MoveAssetSafe($"{craneDir}/QuizData.cs", $"{craneDir}/Models/QuizData.cs");
                    MoveAssetSafe($"{craneDir}/QuizDatabaseSO.cs", $"{craneDir}/Models/QuizDatabaseSO.cs");
                    MoveAssetSafe($"{craneDir}/CraneGameViewModel.cs", $"{craneDir}/ViewModels/CraneGameViewModel.cs");
                    MoveAssetSafe($"{craneDir}/CraneGameManager.cs", $"{craneDir}/Views/CraneGameManager.cs");
                    MoveAssetSafe($"{craneDir}/QuizUI_View.cs", $"{craneDir}/Views/QuizUI_View.cs");
                    MoveAssetSafe($"{craneDir}/SpawnerView.cs", $"{craneDir}/Views/SpawnerView.cs");
                }
            }

            // 변경사항 에셋 데이터베이스 반영 및 갱신
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            EditorUtility.DisplayDialog("정리 완료", "전체 프로젝트 에셋 및 스크립트 MVVM 레이어 정리가 무결하게 완료되었습니다!", "확인");
            Debug.Log("[ProjectFolderOrganizer] 전체 폴더 구조 및 스크립트 정리가 완벽하게 성공했습니다.");
        }

        #region 내부 헬퍼 메서드 및 널체크 (Private Helpers & Guards)

        private static void CreateFolderIfNeeded(string parent, string newFolder)
        {
            string targetPath = Path.Combine(parent, newFolder);
            if (!AssetDatabase.IsValidFolder(targetPath))
            {
                AssetDatabase.CreateFolder(parent, newFolder);
                Debug.Log($"[ProjectFolderOrganizer] 신설 루트 생성 완료: {targetPath}");
            }
        }

        private static void MoveAssetSafe(string sourcePath, string destPath)
        {
            if (AssetDatabase.IsValidFolder(sourcePath) || File.Exists(sourcePath))
            {
                string destParent = Path.GetDirectoryName(destPath);
                if (destParent != null && !AssetDatabase.IsValidFolder(destParent))
                {
                    Debug.LogWarning($"[ProjectFolderOrganizer] 대상 경로의 부모가 존재하지 않습니다: {destParent}");
                    return;
                }

                // 유니티 API를 통해 메타 데이터 레퍼런스를 보존하며 안전 이동
                string errorMsg = AssetDatabase.MoveAsset(sourcePath, destPath);
                
                if (string.IsNullOrEmpty(errorMsg))
                {
                    Debug.Log($"[ProjectFolderOrganizer] 이동 성공: {sourcePath} -> {destPath}");
                }
                else
                {
                    Debug.LogError($"[ProjectFolderOrganizer] 이동 실패 ({sourcePath}): {errorMsg}");
                }
            }
        }

        private static void DeleteAssetSafe(string targetPath)
        {
            if (AssetDatabase.IsValidFolder(targetPath) || File.Exists(targetPath))
            {
                bool success = AssetDatabase.DeleteAsset(targetPath);
                if (success)
                {
                    Debug.Log($"[ProjectFolderOrganizer] 가비지 리소스 삭제 완료: {targetPath}");
                }
            }
        }

        #endregion
    }
}
#endif
