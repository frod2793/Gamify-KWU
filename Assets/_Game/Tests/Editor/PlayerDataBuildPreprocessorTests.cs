using System;
using System.Reflection;
using GameArifiction.Player;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace GameArifiction.Tests.Editor
{
    /// <summary>
    /// [기능]: Player 빌드 전에 PlayerSO 진행 데이터와 PlayerPrefs가 초기화되는지 검증합니다.
    /// [작성자]: 윤승종
    /// </summary>
    [TestFixture]
    public sealed class PlayerDataBuildPreprocessorTests
    {
        #region 상수 (Constants)
        private const string PlayerAssetPath = "Assets/_Game/ScriptableObjects/Player/PlayerSO.asset";
        private const string BgmVolumeKey = "BgmVolume";
        private const string SfxVolumeKey = "SfxVolume";
        private const string IsBgmMutedKey = "IsBgmMuted";
        private const string IsSfxMutedKey = "IsSfxMuted";
        private const string SentinelKey = "PlayerDataBuildPreprocessorTests.Sentinel";
        #endregion

        #region 내부 필드 (Private Fields)
        private PlayerSO m_playerSO;
        private Vector2 m_originalLastPosition;
        private bool m_originalHasSavedPosition;
        private MinigameRecord[] m_originalRecords;
        private float m_originalTotalPlayTime;
        private bool m_originalIsIntroPlayed;
        private bool m_originalIsReturnedFromMinigame;
        private PlayerPrefsSnapshot m_playerPrefsSnapshot;
        #endregion

        #region 테스트 생명주기 (Test Lifecycle)
        /// <summary>
        /// [기능]: 실제 PlayerSO와 PlayerPrefs 값을 백업하고 테스트용 진행 데이터를 구성합니다.
        /// [작성자]: 윤승종
        /// [수정 날짜]: 2026-07-14
        /// [마지막 수정 작성자]: 윤승종
        /// [수정 내용]: 빌드 전 초기화 테스트 데이터 구성 추가.
        /// </summary>
        [SetUp]
        public void SetUp()
        {
            m_playerSO = AssetDatabase.LoadAssetAtPath<PlayerSO>(PlayerAssetPath);
            Assert.IsNotNull(m_playerSO, "PlayerSO 테스트 자산을 찾을 수 없습니다.");

            m_originalLastPosition = m_playerSO.LastPosition;
            m_originalHasSavedPosition = m_playerSO.HasSavedPosition;
            m_originalRecords = new MinigameRecord[m_playerSO.MinigameRecords.Count];
            for (int i = 0; i < m_playerSO.MinigameRecords.Count; i++)
            {
                m_originalRecords[i] = m_playerSO.MinigameRecords[i];
            }

            m_originalTotalPlayTime = m_playerSO.TotalMinigamePlayTime;
            m_originalIsIntroPlayed = m_playerSO.IsIntroPlayed;
            m_originalIsReturnedFromMinigame = m_playerSO.IsReturnedFromMinigame;
            m_playerPrefsSnapshot = PlayerPrefsSnapshot.Capture();

            m_playerSO.ResetData();
            m_playerSO.LastPosition = new Vector2(9f, 7f);
            m_playerSO.HasSavedPosition = true;
            m_playerSO.SetMinigameGrade("TimingCatch", MinigameGrade.A);
            m_playerSO.TotalMinigamePlayTime = 12f;
            m_playerSO.IsIntroPlayed = true;
            m_playerSO.IsReturnedFromMinigame = true;

            PlayerPrefs.SetFloat(BgmVolumeKey, 0.25f);
            PlayerPrefs.SetFloat(SfxVolumeKey, 0.5f);
            PlayerPrefs.SetInt(IsBgmMutedKey, 1);
            PlayerPrefs.SetInt(IsSfxMutedKey, 1);
            PlayerPrefs.SetInt(SentinelKey, 1);
            PlayerPrefs.Save();
        }

        /// <summary>
        /// [기능]: 테스트에서 변경한 PlayerSO와 PlayerPrefs 값을 원래 상태로 복원합니다.
        /// [작성자]: 윤승종
        /// [수정 날짜]: 2026-07-14
        /// [마지막 수정 작성자]: 윤승종
        /// [수정 내용]: 프로젝트 데이터 오염 방지 복원 처리 추가.
        /// </summary>
        [TearDown]
        public void TearDown()
        {
            if (m_playerSO != null)
            {
                m_playerSO.ResetData();
                m_playerSO.LastPosition = m_originalLastPosition;
                m_playerSO.HasSavedPosition = m_originalHasSavedPosition;

                for (int i = 0; i < m_originalRecords.Length; i++)
                {
                    m_playerSO.SetMinigameGrade(
                        m_originalRecords[i].MinigameId,
                        m_originalRecords[i].Grade
                    );
                }

                m_playerSO.TotalMinigamePlayTime = m_originalTotalPlayTime;
                m_playerSO.IsIntroPlayed = m_originalIsIntroPlayed;
                m_playerSO.IsReturnedFromMinigame = m_originalIsReturnedFromMinigame;
                EditorUtility.SetDirty(m_playerSO);
                AssetDatabase.SaveAssets();
            }

            m_playerPrefsSnapshot.Restore();
        }
        #endregion

        #region 테스트 (Tests)
        /// <summary>
        /// [기능]: 플레이어 데이터 전처리기가 버전 증가 전처리기보다 먼저 실행되는지 검증합니다.
        /// [작성자]: 윤승종
        /// [수정 날짜]: 2026-07-14
        /// [마지막 수정 작성자]: 윤승종
        /// [수정 내용]: 빌드 콜백 우선순위 검증 추가.
        /// </summary>
        [Test]
        public void CallbackOrder_IsBeforeVersionIncrement()
        {
            object preprocessor = CreatePreprocessor();
            PropertyInfo callbackOrderProperty = preprocessor.GetType().GetProperty("callbackOrder");

            Assert.IsNotNull(callbackOrderProperty, "callbackOrder 프로퍼티가 필요합니다.");
            Assert.AreEqual(-100, callbackOrderProperty.GetValue(preprocessor));
        }

        /// <summary>
        /// [기능]: 빌드 전 초기화가 PlayerSO 진행 데이터와 모든 PlayerPrefs 값을 제거하는지 검증합니다.
        /// [작성자]: 윤승종
        /// [수정 날짜]: 2026-07-14
        /// [마지막 수정 작성자]: 윤승종
        /// [수정 내용]: PlayerSO 및 PlayerPrefs 전체 초기화 검증 추가.
        /// </summary>
        [Test]
        public void ResetBeforeBuild_ClearsPlayerSOAndPlayerPrefs()
        {
            object preprocessor = CreatePreprocessor();
            MethodInfo resetMethod = preprocessor.GetType().GetMethod("ResetBeforeBuild");

            Assert.IsNotNull(resetMethod, "ResetBeforeBuild 공개 메서드가 필요합니다.");

            int resetAssetCount = (int)resetMethod.Invoke(preprocessor, null);

            Assert.GreaterOrEqual(resetAssetCount, 1);
            Assert.AreEqual(Vector2.zero, m_playerSO.LastPosition);
            Assert.IsFalse(m_playerSO.HasSavedPosition);
            Assert.AreEqual(0, m_playerSO.MinigameRecords.Count);
            Assert.AreEqual(0f, m_playerSO.TotalMinigamePlayTime);
            Assert.IsFalse(m_playerSO.IsIntroPlayed);
            Assert.IsFalse(m_playerSO.IsReturnedFromMinigame);
            Assert.IsFalse(PlayerPrefs.HasKey(BgmVolumeKey));
            Assert.IsFalse(PlayerPrefs.HasKey(SfxVolumeKey));
            Assert.IsFalse(PlayerPrefs.HasKey(IsBgmMutedKey));
            Assert.IsFalse(PlayerPrefs.HasKey(IsSfxMutedKey));
            Assert.IsFalse(PlayerPrefs.HasKey(SentinelKey));
        }
        #endregion

        #region 내부 메서드 (Private Methods)
        /// <summary>
        /// [기능]: 현재 AppDomain에서 빌드 전 플레이어 데이터 초기화 전처리기를 찾아 생성합니다.
        /// [작성자]: 윤승종
        /// [수정 날짜]: 2026-07-14
        /// [마지막 수정 작성자]: 윤승종
        /// [수정 내용]: Editor 어셈블리 직접 참조 없는 테스트용 타입 탐색 추가.
        /// </summary>
        private static object CreatePreprocessor()
        {
            const string typeName = "GameArifiction.Editor.PlayerDataBuildPreprocessor";
            Assembly[] assemblies = AppDomain.CurrentDomain.GetAssemblies();

            for (int i = 0; i < assemblies.Length; i++)
            {
                Type preprocessorType = assemblies[i].GetType(typeName);
                if (preprocessorType != null)
                {
                    return Activator.CreateInstance(preprocessorType);
                }
            }

            Assert.Fail($"{typeName} 타입이 필요합니다.");
            return null;
        }
        #endregion

        #region 내부 데이터 구조 (Private Data Structures)
        /// <summary>
        /// [기능]: 프로젝트에서 사용하는 PlayerPrefs 설정값을 테스트 전후로 보존합니다.
        /// [작성자]: 윤승종
        /// </summary>
        private struct PlayerPrefsSnapshot
        {
            public bool HasBgmVolume;
            public float BgmVolume;
            public bool HasSfxVolume;
            public float SfxVolume;
            public bool HasIsBgmMuted;
            public int IsBgmMuted;
            public bool HasIsSfxMuted;
            public int IsSfxMuted;

            /// <summary>
            /// [기능]: 현재 PlayerPrefs 사운드 설정을 스냅샷으로 캡처합니다.
            /// [작성자]: 윤승종
            /// [수정 날짜]: 2026-07-14
            /// [마지막 수정 작성자]: 윤승종
            /// [수정 내용]: 테스트 전 PlayerPrefs 백업 추가.
            /// </summary>
            public static PlayerPrefsSnapshot Capture()
            {
                return new PlayerPrefsSnapshot
                {
                    HasBgmVolume = PlayerPrefs.HasKey(BgmVolumeKey),
                    BgmVolume = PlayerPrefs.GetFloat(BgmVolumeKey),
                    HasSfxVolume = PlayerPrefs.HasKey(SfxVolumeKey),
                    SfxVolume = PlayerPrefs.GetFloat(SfxVolumeKey),
                    HasIsBgmMuted = PlayerPrefs.HasKey(IsBgmMutedKey),
                    IsBgmMuted = PlayerPrefs.GetInt(IsBgmMutedKey),
                    HasIsSfxMuted = PlayerPrefs.HasKey(IsSfxMutedKey),
                    IsSfxMuted = PlayerPrefs.GetInt(IsSfxMutedKey)
                };
            }

            /// <summary>
            /// [기능]: 테스트 전에 캡처한 PlayerPrefs 사운드 설정을 복원합니다.
            /// [작성자]: 윤승종
            /// [수정 날짜]: 2026-07-14
            /// [마지막 수정 작성자]: 윤승종
            /// [수정 내용]: 테스트 후 PlayerPrefs 복원 추가.
            /// </summary>
            public void Restore()
            {
                PlayerPrefs.DeleteAll();

                if (HasBgmVolume)
                {
                    PlayerPrefs.SetFloat(BgmVolumeKey, BgmVolume);
                }

                if (HasSfxVolume)
                {
                    PlayerPrefs.SetFloat(SfxVolumeKey, SfxVolume);
                }

                if (HasIsBgmMuted)
                {
                    PlayerPrefs.SetInt(IsBgmMutedKey, IsBgmMuted);
                }

                if (HasIsSfxMuted)
                {
                    PlayerPrefs.SetInt(IsSfxMutedKey, IsSfxMuted);
                }

                PlayerPrefs.Save();
            }
        }
        #endregion
    }
}
