import pathlib,re
root=pathlib.Path(__file__).resolve().parents[1]
for relative,href in [('Planning/index.html','Reference/TimingHero-20261023/feedback-v83.html'),('Planning/Reference/TimingHero-20261023/plan-v81.html','feedback-v83.html')]:
    path=root/relative;text=path.read_text(encoding='utf-8')
    if 'id="latest-v83"' in text:continue
    notice=f'<aside id="latest-v83" style="padding:18px;background:#21443c;color:#eee5ce;text-align:center;font:16px/1.7 sans-serif"><b>최신 적용 v8.3 · 골드 HUD / 전장별 몬스터</b><br><a style="color:#f4cb7e" href="{href}">실제 게임 화면 · 새 몬스터 모션 · 재미 분석 보기 →</a></aside>'
    text=re.sub(r'(<body[^>]*>)',lambda m:m.group(1)+notice,text,count=1)
    path.write_text(text,encoding='utf-8')
print('Linked latest v8.3 report from planning index and v8.1 plan')
