"""Update current production links without altering archived proposal data."""
from pathlib import Path

ROOT = Path(__file__).resolve().parents[1]
board = ROOT / "Planning/Story/Dragon-v1/build-board.py"
text = board.read_text(encoding="utf-8")
text = text.replace(
    "의뢰 시작 → 튜토리얼 → 10스테이지 전투 → 토리 대사 → 유물 선택 → 다음 지역. 패배 시 여관 강화·재도전으로 잇습니다.",
    "실행판은 의뢰 시작 → 캐릭터 선택·강화 → 튜토리얼 → 연속 전투입니다. 10보스 뒤 토리 대사를 보고 다음 전장으로 진행하며, 유물은 도전 종료 정산에서 선택합니다.",
)
text = text.replace(
    "GPT 이미지는 분위기와 외형 제안으로, 캐릭터의 균일한 도트 격자·게임용 프레임·배경 레이어 제작은 후속 범위입니다. UI v4 아이콘은 별도 픽셀 마스크로 그려 엔진에 연결했습니다.",
    "GPT 이미지는 분위기와 외형 제안입니다. 실제 게임에는 별도 제작한 온과 적5체형의 native 픽셀 프레임190장, 초원2개 레이어, UI v4 아이콘45개를 연결했습니다. 나머지 구역·NPC 배우는 후속 제작 범위입니다.",
)
text = text.replace(
    "캐릭터·스토리·아트 담당이 병렬로 작업하고 상호 설정을 대조했습니다. 캐릭터·몬스터·배경·전투 장면은 내장 ImageGen으로 제작했고, UI는 코드 기반 배치로 합성했습니다. 브라우저 렌더링·Unity 실행·실기기 QA는 이 기획 작업의 검증 범위에 포함하지 않습니다.",
    "캐릭터·스토리·아트 담당이 병렬로 작업하고 상호 설정을 대조했습니다. 정적 초안은 GPT 이미지와 코드 기반 UI 합성입니다. 실제 게임용 모션은 PixelLab 제작물이며 Unity 실행 검증 결과는 상단 제작 현황에서 확인할 수 있습니다. 실기기 검사는 아직 진행하지 않았습니다.",
)
board.write_text(text, encoding="utf-8")
linker = ROOT / "Planning/Story/Dragon-v1/link-board.py"
text = linker.read_text(encoding="utf-8")
text = text.replace("최신 판타지 전면 개편안 · Unity 미적용", "초원 도트 MVP v0.9.0 · Unity 적용")
text = text.replace(
    "목표는 용을 물리치고 길을 여는 모험. 캐릭터8명 → 이야기 → 50스테이지와 GPT 이미지 초안을 정리했습니다.",
    "초원1–10 새 그래픽·레퍼런스 방향키 배치를 Unity에 연결했습니다. 실제 실행 화면과 제작 현황, 전체50스테이지 구상을 함께 확인할 수 있습니다.",
)
text = text.replace("새 판타지 기획 보기 →", "게임 적용·기획 보기 →")
linker.write_text(text, encoding="utf-8")
print("Updated current production copy; historical concept data retained.")
