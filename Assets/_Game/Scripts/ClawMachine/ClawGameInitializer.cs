using UnityEngine;
using UnityEngine.UI;

namespace GameArifiction.ClawMachine
{
    /// <summary>
    /// [기능]: 인형뽑기 씬의 진입점(Composition Root). 싱글톤을 배제하고 퀴즈 데이터 및 관련 뷰 의존성을 런타임 수동 주입합니다.
    /// [작성자]: 윤승종
    /// </summary>
    public class ClawGameInitializer : MonoBehaviour
    {
        #region UI 참조 (Inspector)
        [SerializeField]
        [Tooltip("게임 전체 UI와 입력을 관리하는 최상위 View 객체입니다.")]
        private ClawGameView m_gameView;

        [SerializeField]
        [Tooltip("인형이 배치될 물리 공간의 부모(Dolls_Container) Transform입니다.")]
        private Transform m_dollsContainer;

        [Header("퀴즈 관련 주입 컴포넌트")]
        [SerializeField]
        [Tooltip("인스펙터에 할당할 퀴즈 데이터베이스 스크립터블 오브젝트입니다.")]
        private GamifyKWU.CraneGame.Data.QuizDatabaseSO m_quizDatabase;

        [SerializeField]
        [Tooltip("UI Canvas 상의 퀴즈 패널 뷰 컴포넌트입니다.")]
        private QuizUI_View m_quizUIView;

        [SerializeField]
        [Tooltip("물리 퇴출구 영역 센서 뷰 컴포넌트입니다.")]
        private ClawMachineExitView m_exitView;
        #endregion

        #region 유니티 생명주기 (Unity Lifecycle)
        private void Start()
        {
            // 1. DTO 수신 (5회 기본 도전 기회, 120초 제한시간)
            var contextDTO = new ClawGameContextDTO(5, 120f, null);
            
            // 2. Model 생성 (DTO 데이터 기반)
            var model = new ClawMachineModel(contextDTO.MaxPlayCount, contextDTO.TimeLimitPerPlay);
            
            // 3. ViewModel 생성
            var viewModel = new ClawGameViewModel(model);

            // 4. [퀴즈 복원]: 퀴즈 데이터베이스 무작위 1문제 출제 및 바인딩
            GamifyKWU.CraneGame.Data.QuizData selectedQuiz = null;
            if (m_quizDatabase != null && m_quizDatabase.QuizList != null && m_quizDatabase.QuizList.Count > 0)
            {
                int randomIndex = Random.Range(0, m_quizDatabase.QuizList.Count);
                selectedQuiz = m_quizDatabase.QuizList[randomIndex];
            }

            if (selectedQuiz == null)
            {
                // 데이터베이스 에셋 유실 대비 최상급 예방용 더미 데이터 폴백 구성
                selectedQuiz = new GamifyKWU.CraneGame.Data.QuizData(
                    "UX/UI [?] 에 대해 아십니까?",
                    "아이콘",
                    new System.Collections.Generic.List<string> { "폰트", "팝업", "체크박스" }
                );
            }

            viewModel.SetQuiz(selectedQuiz);
            
            // 5. 객관식 선택지 캡슐 4개 동적 스폰 및 물리 셔플링
            SpawnQuizDolls(viewModel, selectedQuiz);

            // 6. 프리팹 유실 대비 런타임 자체 무결 모달 팝업 조립 및 DI 연동
            SetupReTakePopup(viewModel);
            
            // 7. View 주입 (Dependency Injection)
            if (m_gameView != null)
            {
                m_gameView.Initialize(viewModel);
            }
            else
            {
                Debug.LogError("[ClawGameInitializer] ClawGameView가 할당되지 않았습니다. 인스펙터를 확인하세요.");
            }

            if (m_exitView != null)
            {
                m_exitView.Initialize(viewModel);
                Debug.Log("[ClawGameInitializer] ClawMachineExitView에 대한 의존성 주입 완료.");
            }
            else
            {
                Debug.LogWarning("[ClawGameInitializer] ClawMachineExitView가 지정되지 않았습니다. 물리 감지가 불가능합니다.");
            }

            if (m_quizUIView != null)
            {
                m_quizUIView.Initialize(viewModel);
                Debug.Log("[ClawGameInitializer] QuizUI_View에 대한 의존성 주입 완료.");
            }
            else
            {
                Debug.LogWarning("[ClawGameInitializer] QuizUI_View가 지정되지 않았습니다. 퀴즈 화면 출력이 불가능합니다.");
            }
            
            Debug.Log("[ClawGameInitializer] 퀴즈 기반 인형뽑기 게임 초기화 완료 및 의존성 주입 성공.");
        }
        #endregion

        #region 내부 헬퍼 메서드 (Private Methods)
        /// <summary>
        /// [기능]: 출제된 퀴즈의 정답 1개와 오답 3개를 캡슐에 담아 물리 공간에 배치하고 뷰모델에 정답 테이블을 등록합니다.
        /// [작성자]: 윤승종
        /// </summary>
        private void SpawnQuizDolls(ClawGameViewModel viewModel, GamifyKWU.CraneGame.Data.QuizData quiz)
        {
            if (m_dollsContainer == null)
            {
                Debug.LogError("[ClawGameInitializer] Dolls_Container가 지정되지 않았습니다.");
                return;
            }

            GameObject templateCapsule = GameObject.Find("ClawMachine_World/Dolls_Container/Capsule");
            if (templateCapsule == null)
            {
                Debug.LogError("[ClawGameInitializer] 템플릿 Capsule 인스턴스를 씬에서 찾을 수 없습니다.");
                return;
            }

            templateCapsule.SetActive(false); // 원본 숨김

            // 1. 선택지 리스트 조립 (정답 1개 + 오답 최대 3개)
            var choices = new System.Collections.Generic.List<string>();
            choices.Add(quiz.CorrectAnswer);

            int wrongCount = Mathf.Min(quiz.WrongAnswers.Count, 3);
            for (int i = 0; i < wrongCount; i++)
            {
                choices.Add(quiz.WrongAnswers[i]);
            }

            // 2. 선택지 피셔-예이츠 셔플로 순서 무작위화 (정답 캡슐의 편중 방지)
            for (int i = choices.Count - 1; i > 0; i--)
            {
                int r = Random.Range(0, i + 1);
                string temp = choices[i];
                choices[i] = choices[r];
                choices[r] = temp;
            }

            // 뷰모델 정답지 초기 청소
            viewModel.ClearDollAnswers();

            // 3. 캡슐 4개 동적 스폰 및 시각적 색상 분산 연출 (고급 HSL 조화 톤)
            Color[] capsuleColors = new Color[]
            {
                new Color(0.4f, 0.7f, 1.0f, 1.0f), // 하늘빛
                new Color(1.0f, 0.5f, 0.5f, 1.0f), // 코랄빛 레드
                new Color(0.5f, 0.9f, 0.6f, 1.0f), // 그린 계열
                new Color(1.0f, 0.85f, 0.4f, 1.0f) // 옐로우 계열
            };

            for (int i = 0; i < choices.Count; i++)
            {
                GameObject dollGo = Instantiate(templateCapsule, m_dollsContainer);
                if (dollGo != null)
                {
                    string answerText = choices[i];
                    bool isCorrect = (answerText == quiz.CorrectAnswer);
                    
                    dollGo.name = $"Capsule_Answer_{i}";
                    dollGo.SetActive(true);

                    // 물리 분포 배치 (X축 균등 분산하여 중력 낙하 정렬)
                    Vector3 pos = templateCapsule.transform.position;
                    pos.x += -1.5f + (i * 1.0f);
                    pos.y += Random.Range(0.2f, 0.8f);
                    dollGo.transform.position = pos;

                    // 개별 컬러 피드백 반영
                    SpriteRenderer sr = dollGo.GetComponent<SpriteRenderer>();
                    if (sr != null)
                    {
                        sr.color = capsuleColors[i % capsuleColors.Length];
                    }

                    // 캡슐 뷰 초기화
                    ClawMachineDollView dollView = dollGo.GetComponent<ClawMachineDollView>();
                    if (dollView != null)
                    {
                        DollModel dollModel = new DollModel(
                            dollGo.name, 
                            $"Choice_{i}", 
                            1.0f, 
                            false, 
                            answerText, 
                            isCorrect
                        );
                        dollView.Initialize(dollModel);
                    }

                    // 뷰모델 정답지에 등록
                    viewModel.RegisterDollAnswer(dollGo.name, isCorrect);
                }
            }

            Debug.Log($"[ClawGameInitializer] 퀴즈 캡슐 {choices.Count}개 동적 스폰 및 뷰모델 퀴즈 바인딩 완료.");
        }

        /// <summary>
        /// [기능]: Canvas 하위에 프리팹 레퍼런스 유실 위험이 전혀 없는 안전한 재수강 모달 팝업 UI를 생성하고 바인딩을 주입합니다.
        /// [작성자]: 윤승종
        /// </summary>
        private void SetupReTakePopup(ClawGameViewModel viewModel)
        {
            Canvas canvas = FindFirstObjectByType<Canvas>();
            if (canvas == null)
            {
                Debug.LogError("[ClawGameInitializer] Canvas가 존재하지 않아 재수강 팝업을 조립할 수 없습니다.");
                return;
            }

            // 최상위 모달 게임 오브젝트
            GameObject popupGo = new GameObject("ClawReTakePopupView", typeof(RectTransform));
            popupGo.transform.SetParent(canvas.transform, false);
            
            RectTransform rt = popupGo.GetComponent<RectTransform>();
            if (rt != null)
            {
                rt.anchorMin = new Vector2(0.5f, 0.5f);
                rt.anchorMax = new Vector2(0.5f, 0.5f);
                rt.anchoredPosition = Vector2.zero;
                rt.sizeDelta = new Vector2(500f, 350f);
            }

            // 다크 글래스모피즘 흐림 배경
            GameObject bgGo = new GameObject("Background", typeof(RectTransform), typeof(Image));
            bgGo.transform.SetParent(popupGo.transform, false);
            RectTransform bgRt = bgGo.GetComponent<RectTransform>();
            if (bgRt != null)
            {
                bgRt.anchorMin = Vector2.zero;
                bgRt.anchorMax = Vector2.one;
                bgRt.sizeDelta = Vector2.zero;
            }
            Image bgImg = bgGo.GetComponent<Image>();
            if (bgImg != null)
            {
                bgImg.color = new Color(0.15f, 0.15f, 0.15f, 0.85f);
            }

            // 테두리 패널 바디
            GameObject panelGo = new GameObject("PanelBody", typeof(RectTransform), typeof(Image));
            panelGo.transform.SetParent(popupGo.transform, false);
            RectTransform pRt = panelGo.GetComponent<RectTransform>();
            if (pRt != null)
            {
                pRt.anchorMin = new Vector2(0.5f, 0.5f);
                pRt.anchorMax = new Vector2(0.5f, 0.5f);
                pRt.anchoredPosition = Vector2.zero;
                pRt.sizeDelta = new Vector2(400f, 250f);
            }
            Image pImg = panelGo.GetComponent<Image>();
            if (pImg != null)
            {
                pImg.color = new Color(0.9f, 0.9f, 0.9f, 1.0f);
            }

            // 설명 Text
            GameObject textGo = new GameObject("DescriptionText", typeof(RectTransform), typeof(Text));
            textGo.transform.SetParent(panelGo.transform, false);
            RectTransform tRt = textGo.GetComponent<RectTransform>();
            if (tRt != null)
            {
                tRt.anchorMin = new Vector2(0.5f, 0.6f);
                tRt.anchorMax = new Vector2(0.5f, 0.6f);
                tRt.anchoredPosition = new Vector2(0f, 20f);
                tRt.sizeDelta = new Vector2(360f, 120f);
            }
            Text txt = textGo.GetComponent<Text>();
            if (txt != null)
            {
                txt.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
                txt.fontSize = 18;
                txt.alignment = TextAnchor.MiddleCenter;
                txt.color = Color.black;
            }

            // 버튼 컨테이너
            GameObject btnContainer = new GameObject("ButtonContainer", typeof(RectTransform));
            btnContainer.transform.SetParent(panelGo.transform, false);
            RectTransform cRt = btnContainer.GetComponent<RectTransform>();
            if (cRt != null)
            {
                cRt.anchorMin = new Vector2(0.5f, 0.2f);
                cRt.anchorMax = new Vector2(0.5f, 0.2f);
                cRt.anchoredPosition = new Vector2(0f, -20f);
                cRt.sizeDelta = new Vector2(360f, 50f);
            }

            // 수락 버튼
            GameObject acceptBtnGo = new GameObject("AcceptButton", typeof(RectTransform), typeof(Image), typeof(Button));
            acceptBtnGo.transform.SetParent(btnContainer.transform, false);
            RectTransform aRt = acceptBtnGo.GetComponent<RectTransform>();
            if (aRt != null)
            {
                aRt.anchorMin = new Vector2(0f, 0.5f);
                aRt.anchorMax = new Vector2(0.45f, 0.5f);
                aRt.anchoredPosition = Vector2.zero;
                aRt.sizeDelta = new Vector2(0f, 40f);
            }
            Image aImg = acceptBtnGo.GetComponent<Image>();
            if (aImg != null)
            {
                aImg.color = new Color(0.2f, 0.6f, 0.2f, 1.0f);
            }
            GameObject acceptTextGo = new GameObject("Text", typeof(RectTransform), typeof(Text));
            acceptTextGo.transform.SetParent(acceptBtnGo.transform, false);
            RectTransform atRt = acceptTextGo.GetComponent<RectTransform>();
            if (atRt != null)
            {
                atRt.anchorMin = Vector2.zero;
                atRt.anchorMax = Vector2.one;
                atRt.sizeDelta = Vector2.zero;
            }
            Text aTxt = acceptTextGo.GetComponent<Text>();
            if (aTxt != null)
            {
                aTxt.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
                aTxt.fontSize = 16;
                aTxt.alignment = TextAnchor.MiddleCenter;
                aTxt.color = Color.white;
                aTxt.text = "동의 (재수강)";
            }

            // 거절 버튼
            GameObject rejectBtnGo = new GameObject("RejectButton", typeof(RectTransform), typeof(Image), typeof(Button));
            rejectBtnGo.transform.SetParent(btnContainer.transform, false);
            RectTransform rRt = rejectBtnGo.GetComponent<RectTransform>();
            if (rRt != null)
            {
                rRt.anchorMin = new Vector2(0.55f, 0.5f);
                rRt.anchorMax = new Vector2(1.0f, 0.5f);
                rRt.anchoredPosition = Vector2.zero;
                rRt.sizeDelta = new Vector2(0f, 40f);
            }
            Image rImg = rejectBtnGo.GetComponent<Image>();
            if (rImg != null)
            {
                rImg.color = new Color(0.7f, 0.2f, 0.2f, 1.0f);
            }
            GameObject rejectTextGo = new GameObject("Text", typeof(RectTransform), typeof(Text));
            rejectTextGo.transform.SetParent(rejectBtnGo.transform, false);
            RectTransform rtRt = rejectTextGo.GetComponent<RectTransform>();
            if (rtRt != null)
            {
                rtRt.anchorMin = Vector2.zero;
                rtRt.anchorMax = Vector2.one;
                rtRt.sizeDelta = Vector2.zero;
            }
            Text rTxt = rejectTextGo.GetComponent<Text>();
            if (rTxt != null)
            {
                rTxt.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
                rTxt.fontSize = 16;
                rTxt.alignment = TextAnchor.MiddleCenter;
                rTxt.color = Color.white;
                rTxt.text = "동의 안 함";
            }

            // 컴포넌트 추가 및 주입
            ClawReTakePopupView popupView = popupGo.AddComponent<ClawReTakePopupView>();
            typeof(ClawReTakePopupView).GetField("m_descriptionText", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance).SetValue(popupView, txt);
            typeof(ClawReTakePopupView).GetField("m_acceptButton", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance).SetValue(popupView, acceptBtnGo.GetComponent<Button>());
            typeof(ClawReTakePopupView).GetField("m_rejectButton", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance).SetValue(popupView, rejectBtnGo.GetComponent<Button>());

            popupView.Initialize(viewModel);

            if (m_gameView != null)
            {
                typeof(ClawGameView).GetField("m_reTakePopup", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance).SetValue(m_gameView, popupView);
            }

            Debug.Log("[ClawGameInitializer] 재수강 모달 UI 및 이벤트 바인딩 컴포지션 주입 무결 완료.");
        }
        #endregion
    }
}
