using System;
using System.Reflection;
using Cysharp.Threading.Tasks;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using GameArifiction.Core.Audio;
using GameArifiction.TimingCatch;

namespace GameArifiction.Tests.Editor
{
    [TestFixture]
    public sealed class TimingCatchSfxTests
    {
        private const string ConfigAssetPath = "Assets/_Game/ScriptableObjects/TimingCatch/TimingCatchConfig.asset";

        private TimingCatchGameConfigSO m_config;

        [SetUp]
        public void SetUp()
        {
            m_config = ScriptableObject.CreateInstance<TimingCatchGameConfigSO>();
            typeof(TimingCatchGameConfigSO)
                .GetField("m_greatSfxPath", BindingFlags.NonPublic | BindingFlags.Instance)
                .SetValue(m_config, "Sounds/SFX/test_great");
        }

        [TearDown]
        public void TearDown() => UnityEngine.Object.DestroyImmediate(m_config);

        [Test]
        public void ViewModel_PlaysSfxOnGreatOnly()
        {
            var sound = new FakeSoundService();
            var model = new TimingCatchGameModel(m_config);
            var viewModel = new TimingCatchGameViewModel(model, new TimingCatchJudgeCalculator(), m_config, sound);
            viewModel.StartGame();
            AdvanceToPlaying(viewModel);

            viewModel.UpdateTick(.5f / model.CurrentSpeed); // 게이지 중앙 → Great
            viewModel.EvaluateInput();
            Assert.AreEqual(1, sound.SfxPlayCount);
            Assert.AreEqual("Sounds/SFX/test_great", sound.LastSfxPath);

            AdvanceToPlaying(viewModel);
            viewModel.UpdateTick(6.01f); // 타임아웃 → Miss
            Assert.AreEqual(1, sound.SfxPlayCount); // Miss는 재생 안 함
        }

        [Test]
        public void ConfigAsset_HasGreatSfxPathAssigned()
        {
            var asset = AssetDatabase.LoadAssetAtPath<TimingCatchGameConfigSO>(ConfigAssetPath);
            Assert.IsNotNull(asset, "TimingCatchConfig.asset 경로가 바뀌었는지 확인");
            Assert.IsFalse(string.IsNullOrWhiteSpace(asset.GreatSfxPath),
                "기획서 p.3 ⑧: Great 효과음 경로가 비어 있음");
        }

        private static void AdvanceToPlaying(TimingCatchGameViewModel viewModel)
        {
            for (int i = 0; i < 10 && viewModel.Phase != TimingCatchPhase.Playing && viewModel.Phase != TimingCatchPhase.Completed; i++)
            {
                viewModel.UpdateTick(10f);
            }
            Assert.AreEqual(TimingCatchPhase.Playing, viewModel.Phase);
        }

        private sealed class FakeSoundService : ISoundService
        {
            public int SfxPlayCount;
            public string LastSfxPath;

            public SoundSettingsDTO Settings => default;

#pragma warning disable 67
            public event Action<float> OnBgmVolumeChanged;
            public event Action<float> OnSfxVolumeChanged;
            public event Action<AudioClip> OnPlayBGMRequested;
            public event Action<AudioClip> OnPlaySFXRequested;
            public event Action OnStopBGMRequested;
            public event Action OnPauseBGMRequested;
            public event Action OnResumeBGMRequested;
            public event Action<AudioClip, float> OnPlayBGMWithFadeRequested;
            public event Action<float> OnStopBGMWithFadeRequested;
#pragma warning restore 67

            public void SetBgmVolume(float volume)
            {
            }

            public void SetSfxVolume(float volume)
            {
            }

            public void SetBgmMute(bool isMute)
            {
            }

            public void SetSfxMute(bool isMute)
            {
            }
            public UniTaskVoid PlayBGM(string clipPath) => default;

            public UniTaskVoid PlaySFX(string clipPath)
            {
                SfxPlayCount++;
                LastSfxPath = clipPath;
                return default;
            }

            public void StopBGM()
            {
            }

            public void PauseBGM()
            {
            }

            public void ResumeBGM()
            {
            }
            public UniTaskVoid PlayBGMWithFade(string clipPath, float duration = 1f) => default;
            public void StopBGMWithFade(float duration = 1f)
            {
            }
        }
    }
}
