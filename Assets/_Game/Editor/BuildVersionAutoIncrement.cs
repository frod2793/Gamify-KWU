using System;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEngine;

/// <summary>
/// [기능]: 유니티 빌드 시작 시 자동으로 PlayerSettings.bundleVersion을 파싱하여 빌드 버전을 1씩 증가시키는 빌드 전처리 에디터 스크립트.
/// [작성자]: 윤승종
/// </summary>
namespace GameArifiction.Editor
{
    public class BuildVersionAutoIncrement : IPreprocessBuildWithReport
    {
        /// <summary>
        /// [기능]: 빌드 전처리 콜백 우선순위를 설정합니다. (0이 기본)
        /// [작성자]: 윤승종
        /// </summary>
        public int callbackOrder => 0;

        /// <summary>
        /// [기능]: 빌드가 개시될 때 호출되어 현재 bundleVersion을 1씩 자동으로 증가시키고 ProjectSettings를 갱신합니다.
        /// [작성자]: 윤승종
        /// [수정 날짜]: 2026-06-27
        /// [마지막 수정 작성자]: 윤승종
        /// [수정 내용]: 빌드 자동 버전 인상 기능 설계 및 구현
        /// </summary>
        public void OnPreprocessBuild(BuildReport report)
        {
            string currentVersion = PlayerSettings.bundleVersion;
            string nextVersion = IncrementVersion(currentVersion);

            PlayerSettings.bundleVersion = nextVersion;
            Debug.Log($"[BuildVersionAutoIncrement] 빌드가 감지되어 버전을 자동으로 올렸습니다: {currentVersion} -> {nextVersion}");
        }

        /// <summary>
        /// [기능]: 버전 문자열(예: '0.002')의 마지막 숫자를 파싱하여 1을 더하고 원래 포맷에 맞춰 리턴합니다.
        /// [작성자]: 윤승종
        /// </summary>
        private string IncrementVersion(string version)
        {
            if (string.IsNullOrEmpty(version))
            {
                return "0.001";
            }

            try
            {
                string[] parts = version.Split('.');
                if (parts.Length > 0)
                {
                    int lastIdx = parts.Length - 1;
                    string lastPart = parts[lastIdx];

                    // 마지막 세그먼트가 정수인지 판별
                    if (int.TryParse(lastPart, out int lastNum))
                    {
                        lastNum++;
                        // 기존 자릿수(예: '002'의 경우 3자리)를 보존하기 위해 PadLeft 적용
                        parts[lastIdx] = lastNum.ToString().PadLeft(lastPart.Length, '0');
                        return string.Join(".", parts);
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"[BuildVersionAutoIncrement] 버전 자동 증가 중 오류 발생 ({version}): {ex.Message}");
            }

            // 파싱에 예외가 발생하거나 포맷이 다를 경우 폴백 처리
            return "0.003";
        }
    }
}
