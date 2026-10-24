"""Update current board copy; preserve earlier proposal and release sections."""
from pathlib import Path

ROOT=Path(__file__).resolve().parents[1]
path=ROOT/'Planning/Story/Dragon-v1/build-board.py'
text=path.read_text(encoding='utf-8')
changes={
    '2026.10.24 · 초원·숲 v0.9.2 Unity 적용':'2026.10.24 · 세 모험가·전용 스킬 v0.9.3',
    '작은 검사,<br>이번 의뢰는':'작은 모험가,<br>이번 의뢰는',
    '플레이어의 현재 게임용 외형은 상단 Player-v3 모험가가 우선합니다.':'현재 게임용 플레이어는 루카 Player-v3와 다렌·미엘 Player-v4를 사용합니다.',
    '길을 막은 용, 길을 열러 가는 루카.':'길을 막은 용, 길을 열러 가는 모험가.',
    '마을의 의뢰를 맡은 루카는 붉은 봉우리를 향해 떠납니다.':'의뢰를 맡은 모험가는 붉은 봉우리를 향해 떠납니다.',
    '리나는 골렘을 살피고 바룬은 용의 흔적을 짚어줍니다.':'리나는 여행 준비를 챙기고 바룬은 앞길을 막은 기블을 알려줍니다.',
    '루카는 의뢰를 완수하고 마을로 돌아옵니다.':'모험가는 의뢰를 완수하고 마을로 돌아옵니다.',
    '루카의 모션과 발 피벗, 공통 바닥, 도트 체력바와 UI를 연결했습니다.':'루카·다렌·미엘의 모션과 20초 수동 스킬, 선명한 예고·기절바, 도트 UI를 연결했습니다.',
    '21~50의 광산·고산·용 둥지, 전용 지역 배경, 나머지 21~50 전투는 후속 제작입니다. 지면·등장 순서·스킬 규칙을 먼저 정의했습니다.':'21~50의 광산·고산·용 둥지 전투와 전용 지역 배경은 후속 제작입니다. 지면·등장 순서·보스 뒤 이야기를 먼저 정의했습니다.',
    '루카 원본52장(아래 쳐내기 포함66참조), 초원 원본120장, 숲30클립235샘플과2개 배경 레이어, UI v4 아이콘45개가 현재 연결 범위입니다.':'플레이어 3명 원본156장(아래 쳐내기 포함198참조), 초원 원본120장, 숲30클립235샘플, 초상화9장, 배경2개 레이어와 UI v4 아이콘45개가 현재 연결 범위입니다.',
    '<a href="character-skills-v1.json">전용 스킬 규칙</a>':'<a href="character-skills-v2.json">현재 전용 스킬 규칙</a>',
    '<a href="dragon-identity-v1.md">차별성과 재미</a>':'<a href="#game-identity">현재 핵심 재미</a>',
    '<a href="../../Production/dragon-v3-integration.json">Unity 적용 기록</a>':'<a href="../../Production/dragon-v4-integration.json">현재 Unity 적용 기록</a>',
    "'UNITY_V3':(B/'unity-v3-section.html').read_text(encoding='utf-8')":"'UNITY_V3':(B/'unity-v3-section.html').read_text(encoding='utf-8').replace('id=\"unity-build\"','id=\"unity-build-v3\"')",
}
for old,new in changes.items():text=text.replace(old,new)
path.write_text(text,encoding='utf-8')
print('Updated current copy and isolated the archived v3 anchor.')
