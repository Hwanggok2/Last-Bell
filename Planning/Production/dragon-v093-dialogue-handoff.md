# v0.9.3 대사·초상화 인계

2026-10-24. 이 문서는 소스 구현 인계이며 Unity 에셋 적용·실행 QA 완료 기록이 아니다.

## 현재 실행 계약

GameData 원본에 5장면, 총25페이지. Stage2 토리6, Stage7 리나5, Stage10 토리3, Stage15 바룬5, Stage20 기블/도토6. 배열 앞의 기존10/20 순서는 보존했다. 30/40/50 재회·엔딩 대사는 planning story.json에만 있으며 실행 범위가 아니다.

StoryDefinition.kind는 boss/encounter다. Encounter의 bossId는 빈 문자열이다. StoryLineDefinition.portraitId가 추가되었다. 주인공 줄은 speaker='player', portraitId='player'이며 responses에 on/dam/rin 문장을 모두 둔다. ??? 줄은 토리 첫 호출의 unknown 초상화만 쓴다.

Client 연결 필요: StoryView.PortraitId; GetStory()의 선택 주인공 이름/초상/응답 해석; BattlePresentation의 Overlay.ShowStory(story.Speaker,story.Text,story.PortraitId); DefeatEnemy에서 PrepareStory()를 boss 블록 밖으로 이동. Core는 Client 담당 소유다.

기존 seenStoryIds와 sceneId/lineIndex 저장을 그대로 사용한다. 이전 저장에서 이미 본 story_boss_10/20은 새 문구라도 다시 자동재생하지 않는다. 대화 새로보기를 이유로 기존 진행이나 보상을 지우지 않는다.

## 에셋 연결

`DragonDialogueMigration.Apply(MvpView)` 또는 `Apply(PresentationOverlay)`가 기존 저장 프리팹에 Portraits 배열과 StoryPortrait Image를 연결한다. 호출자가 프리팹·씬을 저장한다. 기존 DragonUiMigration 뒤에 호출하면 된다.

사전검증 경로: `Client/Assets/Game/Art/Dialogue/Dragon-v4/{on,dam,rin,tori,rina,barun,doto,gib,unknown}.png`. 아트 담당이 authored48×48 PNG를 만든다. 누락 파일이 있으면 UI변경 전 실패한다. 누락 이미지를 unknown으로 대체하지 않는다.

Point, Sprite/Single, mipmap off, 압축 없음. 하단1280기준184px패널, 144px초상, 본문32pt, 발화자28pt, 다음/건너뛰기144×96. UI전체를 런타임에 재생성하지 않는다. 전투 밖 대화 상태에서만 패널을 보여준다.

## 변경 소스

- GameData/mvp-catalog.json: stories와 onboarding문구만.
- GameData/export_catalog.py: story shape/identity/portrait/response 검증.
- GameData/tests/test_export_catalog.py: 새스토리 문구와계약 테스트.
- Client/Assets/Game/Runtime/Data/GameCatalog.cs: story definition/검증영역만.
- Client/Assets/Game/Runtime/Presentation/PresentationOverlay.cs: portrait serialized binding/화자별표시/패널높이.
- Client/Assets/Game/Editor/DragonDialogueMigration.cs: 기존UI부분이전·에셋binding·validation.
- Planning/Story/Dragon-v1/story.json: 실행25페이지+미래30/40/50문장, 선택주인공정합성, 현재20구역구분.
- Tools/build-dragon-dialogue-review.py: 원본에서 section/html/md 생성. root가보드에dialogue-v4-section.html삽입.
- Planning/Story/Dragon-v1/dialogue-v4-wireframe.svg: 배치명세.

## 확인과 남은 검증

대사소스 작성시점의 GameData unittest23개통과. 검증에는 없는portrait ID, player/portrait불일치, 캐릭터응답누락, 장면중복, 유효하지않은boss ID거절을포함한다. 이후root가추가하는skills schema는최종통합검사대상이다.

이 담당은 Unity Apply/빌드/재생을 실행하지 않았다. authored초상화9개 출고후이미지연결, 실제한글줄바꿈/초상교체/버튼터치영역은 통합QA에서확인해야한다. HTML미리보기는Unity캡처가아니다.
