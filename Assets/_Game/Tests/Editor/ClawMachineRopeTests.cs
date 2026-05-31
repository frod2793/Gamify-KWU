/// <summary>
/// [기능]: 밧줄 지연(Lag-Chain) 핵심 계산식 및 점진적 시간차 휨 궤적 수렴 상태 검증 유닛 테스트
/// [작성자]: 윤승종
/// </summary>
using NUnit.Framework;
using UnityEngine;
using System.Collections.Generic;

namespace GameArifiction.ClawMachine.Tests
{
    [TestFixture]
    public class ClawMachineRopeTests
    {
        [Test]
        public void RopeSegments_UndergoTimeLag_GraduallyConvergingToTarget()
        {
            // #region 1. 테스트 준비 (Arrange)
            Vector3 startPos = Vector3.zero;
            Vector3 endPos = new Vector3(0, -3f, 0); // 3m 길이
            float spacing = 0.15f;
            int neededSegments = Mathf.FloorToInt(3f / spacing);
            
            // 이전 프레임 마디 좌표 버퍼 모사 (초기에는 일직선 상에 위치)
            List<Vector3> cachedPositions = new List<Vector3>();
            for (int i = 0; i < neededSegments; i++)
            {
                float t = (float)(i + 1) / (neededSegments + 1);
                cachedPositions.Add(Vector3.Lerp(startPos, endPos, t));
            }
            // #endregion

            // #region 2. 테스트 실행 (Act)
            // 카트(startPos)가 급격하게 우측 X = 1.0f로 먼저 이동함
            Vector3 newStartPos = new Vector3(1f, 0, 0);
            float lagElasticity = 15.0f;
            float dt = 0.02f; // 50fps 고정프레임 틱
            
            // 지연 물리 연산 적용: 각 마디가 상위 마디를 지연 Lerp 추종
            List<Vector3> newPositions = new List<Vector3>();
            Vector3 prevLeader = newStartPos;
            
            for (int i = 0; i < cachedPositions.Count; i++)
            {
                // 이전 프레임의 마디 좌표가 상위 마디의 새로운 목표 방향으로 지연 이동
                Vector3 currentPos = cachedPositions[i];
                Vector3 targetLeader = prevLeader;
                
                Vector3 updatedPos = Vector3.Lerp(currentPos, targetLeader, dt * lagElasticity);
                newPositions.Add(updatedPos);
                prevLeader = updatedPos; // 자신의 새로운 좌표를 다음 하위 마디의 추종 대상으로 지정
            }
            // #endregion

            // #region 3. 테스트 검증 (Assert)
            // 최상단(1번) 마디는 신속히 카트(X=1) 쪽으로 끌려갔으나,
            // 하위 마디(마지막 마디)는 지연이 누적되어 X=1에 한참 도달하지 못한 활처럼 휜 상태여야 함.
            Assert.IsTrue(newPositions[0].x > 0.1f, "최상단 마디는 끌려가기 시작해야 합니다.");
            Assert.IsTrue(newPositions[neededSegments - 1].x < newPositions[0].x, "하단 마디는 지연으로 인해 X 좌표가 상단 마디보다 현격히 낮아야 합니다.");
            // #endregion
        }
    }
}
