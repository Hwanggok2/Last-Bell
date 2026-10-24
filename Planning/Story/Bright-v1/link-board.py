"""Add a prominent revision entry without replacing existing planning pages."""
from pathlib import Path
import re

base=Path(__file__).resolve().parent
planning=base.parents[1]
for file,href in [(planning/'index.html','Story/Bright-v1/index.html'),(planning/'Presentation/index.html','../Story/Bright-v1/index.html'),(planning/'Story/story-world-v2.html','Bright-v1/index.html')]:
    text=file.read_text(encoding='utf-8-sig')
    banner=f'''<!-- bright-v1:start --><aside id="latest-bright-v1" style="padding:24px max(20px,calc((100% - 1240px)/2));background:#FFF4DD;color:#283F42;border-bottom:4px solid #71543F;font:16px/1.7 'Malgun Gothic',sans-serif"><div style="display:flex;gap:24px;align-items:center;justify-content:space-between;flex-wrap:wrap"><div><span style="font-size:13px;color:#24766D">2026.10.24 · 최신 스토리·디자인 개정안 · 게임 미적용</span><h2 style="margin:6px 0;font-size:26px;color:#283F42">축제 전에, 다섯 종</h2><p style="margin:0;color:#283F42">돌풍으로 엉킨 축제 준비를 정리하는 가벼운 모험. 새 이야기와 밝은 배경·UI 시안 6장을 확인하세요.</p></div><a href="{href}" style="display:inline-block;padding:12px 20px;background:#24766D;color:#FFF4DD;text-decoration:none;font-weight:bold">새 스토리·시안 보기 →</a></div></aside><!-- bright-v1:end -->'''
    text=re.sub(r'<!-- bright-v1:start -->.*?<!-- bright-v1:end -->','',text,flags=re.S)
    text=re.sub(r'(<body\b[^>]*>)',lambda m:m[1]+banner,text,count=1,flags=re.I)
    if file==planning/'index.html':
        text=text.replace('<a href="#story">스토리·세계관</a>',f'<a href="{href}#story">새 스토리·세계관</a>')
        text=text.replace('<h2>스토리 · 마을의 내일을 돌려주는 여정</h2>','<h2>이전 스토리 보관본 · 마을의 내일을 돌려주는 여정</h2><p><a href="Story/Bright-v1/index.html#story">2026.10.24 개정: 축제 전에, 다섯 종 — 새 스토리 보기</a></p>')
    elif file==planning/'Presentation/index.html':
        text=text.replace('<a href="../index.html#story"><b>보스가 감춘 진실 · 스토리 전체 보기 ↗</b></a>',f'<a href="{href}#story"><b>축제 전에, 다섯 종 · 새 스토리 보기 ↗</b></a>')
    file.write_text(text,encoding='utf-8')
    print(file.relative_to(planning))
