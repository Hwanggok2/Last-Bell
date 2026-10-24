"""Point current planning entry points at the new fantasy revision."""
import re
from pathlib import Path
B=Path(__file__).resolve().parent
P=B.parents[1]
targets=[(P/'index.html','Story/Dragon-v1/index.html'),(P/'Presentation/index.html','../Story/Dragon-v1/index.html'),(P/'Story/Bright-v1/index.html','../Dragon-v1/index.html'),(P/'Story/story-world-v2.html','Dragon-v1/index.html')]
for file,href in targets:
    text=file.read_text(encoding='utf-8-sig')
    for revision in ['bright-v1','dragon-v1']:
        text=re.sub(r'<!-- '+revision+r':start -->.*?<!-- '+revision+r':end -->','',text,flags=re.S)
    detail=href.rsplit('/',1)[0]+'/Adventure-v6/index.html'
    button='display:inline-block;padding:12px 20px;background:#315E4D;color:#FFF1D1;text-decoration:none;font-weight:bold'
    banner=f'''<!-- dragon-v1:start --><aside id="latest-dragon-v1" style="padding:24px max(20px,calc((100% - 1240px)/2));background:#FFF1D1;color:#273D45;border-bottom:4px solid #315E4D;font:16px/1.7 'Malgun Gothic',sans-serif"><span style="font-size:13px;color:#315E4D">2026.10.25 · 최신 기획 · v0.9.6 · Unity 적용·표적 QA 완료 / 실행1~20</span><h2 style="margin:6px 0;font-size:27px;color:#273D45">용 잡으러 갑니다 — 최신 기획서</h2><p style="margin:0 0 16px;color:#273D45">스토리·캐릭터·스테이지 기획과 실제 게임 화면, 동료·회복력·도트 스킬 효과를 아래에서 확인하세요. 아래쪽 본문은 이전 제작 기록입니다.</p><div style="display:flex;gap:12px;flex-wrap:wrap"><a href="{href}" style="{button}">통합 기획서 열기 →</a><a href="{detail}#skill" style="{button}">스킬·도트 이펙트</a><a href="{detail}#companions" style="{button}">동료 스킬·강화</a></div></aside><!-- dragon-v1:end -->'''
    text=re.sub(r'(<body\b[^>]*>)',lambda m:m[1]+banner,text,count=1,flags=re.I)
    if file==P/'index.html':
        text=text.replace('href="Story/Bright-v1/index.html#story"','href="Story/Dragon-v1/index.html#story"')
        text=text.replace('2026.10.24 개정: 축제 전에, 다섯 종 — 새 스토리 보기','2026.10.24 개정: 용 잡으러 갑니다 — 새 판타지 스토리 보기')
    elif file==P/'Presentation/index.html':
        text=text.replace('href="../Story/Bright-v1/index.html#story"','href="../Story/Dragon-v1/index.html#story"').replace('축제 전에, 다섯 종 · 새 스토리 보기','용 잡으러 갑니다 · 새 스토리 보기')
    elif file==P/'Story/Bright-v1/index.html':
        text=text.replace('<title>마지막 종지기 · 축제 전에, 다섯 종</title>','<title>이전안 보관 · 축제 전에, 다섯 종</title>')
        text=text.replace('2026.10.24 · 기획·시안 검토본 · Unity 미적용','이전 축제안 보관본 · 최신 용 토벌안은 상단 링크')
    file.write_text(text,encoding='utf-8')
    print(file.relative_to(P))
