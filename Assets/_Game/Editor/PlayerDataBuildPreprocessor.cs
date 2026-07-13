using System;
using GameArifiction.Player;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEngine;

namespace GameArifiction.Editor
{
    /// <summary>
    /// [기능]: Player 빌드 전에 모든 PlayerSO 진행 데이터와 PlayerPrefs를 초기화합니다.
    /// [작성자]: 윤승종
    /// </summary>
    public sealed class PlayerDataBuildPreprocessor : IPreprocessBuildWithReport
    {
        #region 공개 프로퍼티 (Public Properties)
        /// <summary>
        /// [기능]: 플레이어 데이터 초기화를 다른 빌드 전처리기보다 먼저 실행하도록 우선순위를 제공합니다.
        /// [작성자]: 윤승종
        /// </summary>
        public int callbackOrder => -100;
        #endregion

        #region 빌드 전처리 (Build Preprocessing)
        /// <summary>
        /// [기능]: Player 빌드 직전에 모든 플레이어 데이터를 초기화하고 실패 시 빌드를 중단합니다.
        /// [작성자]: 윤승종
        /// [수정 날짜]: 2026-07-14
        /// [마지막 수정 작성자]: 윤승종
        /// [수정 내용]: PlayerSO와 PlayerPrefs 빌드 전 초기화 기능 추가.
        /// </summary>
        public void OnPreprocessBuild(BuildReport report)
        {
            try
            {
                int resetAssetCount = ResetBeforeBuild();
                Debug.Log($"[PlayerDataBuildPreprocessor] 빌드 전 플레이어 데이터 초기화를 완료했습니다. PlayerSO: {resetAssetCount}개, PlayerPrefs: 전체 삭제");
            }
            catch (Exception exception)
            {
                string message = $"[PlayerDataBuildPreprocessor] 플레이어 데이터 초기화에 실패하여 빌드를 중단합니다: {exception.Message}";
                Debug.LogError(message);
                throw new BuildFailedException(message);
            }
        }

        /// <summary>
        /// [기능]: 프로젝트의 모든 PlayerSO 진행 데이터와 모든 PlayerPrefs 값을 초기화합니다.
        /// [작성자]: 윤승종
        /// [수정 날짜]: 2026-07-14
        /// [마지막 수정 작성자]: 윤승종
        /// [수정 내용]: AssetDatabase 기반 PlayerSO 전수 초기화와 PlayerPrefs 전체 삭제 추가.
        /// </summary>
        public int ResetBeforeBuild()
        {
            string[] playerAssetGuids = AssetDatabase.FindAssets("t:PlayerSO");

            for (int i = 0; i < playerAssetGuids.Length; i++)
            {
                string assetPath = AssetDatabase.GUIDToAssetPath(playerAssetGuids[i]);
                PlayerSO playerSO = AssetDatabase.LoadAssetAtPath<PlayerSO>(assetPath);

                if (playerSO == null)
                {
                    throw new InvalidOperationException($"PlayerSO 자산을 불러올 수 없습니다: {assetPath}");
                }

                playerSO.ResetData();
                EditorUtility.SetDirty(playerSO);
            }

            PlayerPrefs.DeleteAll();
            PlayerPrefs.Save();
            AssetDatabase.SaveAssets();

            return playerAssetGuids.Length;
        }
        #endregion
    }
}
