"""Publish v0.9.4 documentation only after final release and scoped QA evidence exist.

Prepared automation: importing this module does not write or run anything.
Default invocation is a read-only preview; --apply explicitly writes documentation.

Example (replace paths and QA wording with the actual final evidence):
  python Tools/finalize-dragon-v5.py --release Planning/Production/dragon-v5-release.json \
    --campaign Planning/Evidence/Dragon-v5/FINAL/campaign.json \
    --qa-report Planning/QA/ACTUAL-REPORT.md \
    --qa-scope "최종 QA 보고서가 승인한 변경 범위" \
    --qa-limit "실제 보고서에 기록한 미실시·미확인 범위" \
    --capture "Planning/Evidence/Dragon-v5/FINAL/screen.png|실제 캡처의 설명" --apply

Use one --qa-limit per limitation, including adopted earlier-candidate evidence or
targeted-only reruns where applicable. This tool never invents check counts, claims
physical input/mobile/audio coverage, runs QA, builds Unity, packages, or pushes.
"""
from __future__ import annotations

import argparse
import hashlib
import html
import json
import os
from pathlib import Path
import re
import subprocess
import sys

ROOT = Path(__file__).resolve().parents[1]
BOARD = ROOT / "Planning/Story/Dragon-v1"
ADVENTURE = BOARD / "Adventure-v5"
START = "<!-- dragon-v5-publication:start -->"
END = "<!-- dragon-v5-publication:end -->"
VERSION = "0.9.4"
DATA_VERSION = "mvp1.17.0"


def require(condition: bool, message: str) -> None:
    if not condition:
        raise ValueError(message)


def source_path(value: str | Path) -> Path:
    path = Path(value)
    path = (path if path.is_absolute() else ROOT / path).resolve()
    require(path.is_relative_to(ROOT), f"Path must stay inside the repository: {path}")
    return path


def relative(path: Path) -> str:
    return path.relative_to(ROOT).as_posix()


def link_from(folder: Path, target: Path) -> str:
    return os.path.relpath(target, folder).replace(os.sep, "/")


def read(path: Path) -> str:
    return path.read_text(encoding="utf-8-sig")


def read_json(path: Path) -> dict:
    value = json.loads(read(path))
    require(isinstance(value, dict), f"Expected JSON object: {path}")
    return value


def json_text(value: dict) -> str:
    return json.dumps(value, ensure_ascii=False, indent=2) + "\n"


def sha256(path: Path) -> str:
    digest = hashlib.sha256()
    with path.open("rb") as stream:
        for chunk in iter(lambda: stream.read(1024 * 1024), b""):
            digest.update(chunk)
    return digest.hexdigest()


def marked(text: str, body: str, anchor: str | None = None) -> str:
    """Replace our own block; refuse damaged/duplicate markers instead of guessing."""
    block = f"{START}\n{body.strip()}\n{END}"
    require(text.count(START) == text.count(END), "Publication markers are unbalanced.")
    require(text.count(START) <= 1, "Duplicate publication blocks require manual review.")
    if START in text:
        start, closing = text.index(START), text.index(END)
        require(start < closing, "Publication marker order is invalid.")
        end = closing + len(END)
        return text[:start] + block + text[end:]
    if anchor is None:
        return block + "\n\n" + text
    require(text.count(anchor) == 1, f"Expected one publication anchor: {anchor}")
    return text.replace(anchor, anchor + "\n" + block + "\n", 1)


def change_known(text: str, old: str, new: str, *, required: bool = False) -> str:
    if old in text:
        require(text.count(old) == 1, f"Ambiguous source fragment: {old[:90]}")
        return text.replace(old, new, 1)
    require(not required or new in text, f"Source changed; review this fragment before publication: {old[:90]}")
    return text


def replace_first(text: str, pattern: str, value: str, label: str) -> str:
    require(re.search(pattern, text, re.S) is not None, f"Missing {label}; preserve and review the changed template.")
    return re.sub(pattern, lambda _: value, text, count=1, flags=re.S)


def evidence(args: argparse.Namespace) -> dict:
    release_path = source_path(args.release)
    campaign_path = source_path(args.campaign)
    report_path = source_path(args.qa_report)
    require(report_path.suffix.lower() == ".md" and report_path.is_file(), "A real final Markdown QA report is required.")
    report = read(report_path)
    require(len(report.strip()) >= 100 and ("v0.9.4" in report or "Dragon-v5" in report), "QA report must identify this release and contain a result.")
    verdicts = [line for line in report[:3000].splitlines()
                if re.match(r"^\s*(?:\*\*)?(?:승인된 변경 범위 QA|변경 범위 QA|최종 판정|최종 QA 결과|QA 결과|결과\s*[:：]|PASS\b)", line, re.I)]
    bounded_verdict = re.search(r"BOUNDED_OBSERVATION_PASS\s*/\s*DQA5-002_CLOSED\s*/\s*FULL_ACCEPTANCE_NOT_CERTIFIED", report[:3000])
    require(bounded_verdict or any(re.search(r"\bPASS\b|통과", line, re.I)
                and not re.search(r"PENDING|BLOCKED|예정|진행 중|미완료", line, re.I) for line in verdicts),
            "The QA report opening needs an explicit completed PASS verdict, not draft acceptance criteria.")
    release, campaign = read_json(release_path), read_json(campaign_path)
    require(release.get("version") == VERSION and release.get("dataVersion") == DATA_VERSION, "Unexpected release/data version.")
    require(release.get("qaProbeExcluded") is True and release.get("zipIntegrity") == "pass", "Normal release must exclude the QA probe and pass package integrity.")
    require(release.get("buildOptions") == "None", "Expected the normal, non-observed release build.")
    # v5 raw campaigns deliberately certify observation only. Publication also
    # requires the completed independent visual review and its explicit closure
    # of the discovered defect. The raw campaign's NOT_CERTIFIED is preserved.
    review_path = campaign_path.parent / "review/visual-review.json"
    visual = read_json(review_path)
    require(visual.get("visualReview") == "COMPLETED_BOUNDED" and visual.get("visualVerdict") == "TARGET_PASS",
            "Completed bounded visual QA is required; machine observation alone cannot publish.")
    require(visual.get("fullAcceptance") == "NOT_CERTIFIED", "Do not turn bounded QA into full acceptance.")
    assessment = visual.get("releaseAssessment", {})
    require(assessment.get("qaBlockingDefects") == [] and assessment.get("generalBuildPackaging") == "MAY_PROCEED_WITH_RECORDED_LIMITS",
            "Independent QA has not cleared release packaging within its recorded limits.")
    closure = visual.get("issueResolution", {})
    require(closure.get("id") == "DQA5-002" and closure.get("status") == "CLOSED" and closure.get("manualPagesReviewed") == 30,
            "The incorrect Barun portrait must be closed by the 30-page targeted visual review.")
    require(campaign.get("observationPassed") is True and not campaign.get("error"), "Final campaign observation has not passed.")
    adopted = visual.get("adoptedCandidate02", {})
    require(bool(adopted.get("scope")) and bool(adopted.get("excluded")) and bool(visual.get("limits")),
            "Reused evidence, exclusions and review limits must remain explicit.")
    adopted_review_path = source_path(campaign_path.parent / adopted["visualReview"])
    adopted_visual = read_json(adopted_review_path)
    require(adopted_visual.get("visualReview") == "COMPLETED_BOUNDED_WITH_DEFECT"
            and all(f.get("id") == closure["id"] for f in adopted_visual.get("findings", [])),
            "Reused visual evidence contains an unclosed finding.")
    for review_file, reviewed in [(review_path, visual), (adopted_review_path, adopted_visual)]:
        candidate_root = review_file.parent.parent
        require(bool(reviewed.get("rawEvidenceUnmodified")) and bool(reviewed.get("reviewed")), "Missing actual visual QA evidence.")
        for item in reviewed["rawEvidenceUnmodified"]:
            require(sha256(source_path(candidate_root / item["path"])) == item["sha256"], "Raw QA evidence changed after visual review.")
        for item in reviewed["reviewed"]:
            require(sha256(source_path(candidate_root / item["reviewFile"])) == item["sha256"], "A visually reviewed capture changed.")
    adopted_campaign = read_json(adopted_review_path.parent.parent / "campaign.json")
    require(adopted_campaign.get("observationPassed") is True and not adopted_campaign.get("error")
            and len(adopted_campaign.get("runs", [])) == 4
            and all(r.get("passed") is True and r.get("exitCode") == 0 for r in adopted_campaign["runs"]),
            "Reused integrated campaign evidence is incomplete.")
    runs = campaign.get("runs")
    require(isinstance(runs, list) and len(runs) > 0, "Final campaign has no completed runs.")
    require(all(r.get("passed") is True and r.get("exitCode") == 0 and not r.get("timedOut") for r in runs), "Every final campaign run must pass and exit zero.")
    if "expectedProcesses" in campaign:
        require(campaign["expectedProcesses"] == len(runs), "Final campaign is incomplete.")
    archive = release.get("archive", {})
    archive_path = source_path(archive.get("path", ""))
    require(archive_path.is_file() and archive_path.suffix.lower() == ".zip", "Release ZIP is missing.")
    require(archive_path.stat().st_size == archive.get("bytes"), "Release ZIP size differs from its record.")
    require(sha256(archive_path).lower() == str(archive.get("sha256", "")).lower(), "Release ZIP hash differs from its record.")
    listed = release.get("files", [])
    executables = [source_path(f["path"]) for f in listed if f.get("path", "").endswith("/LastBellMvp.exe")]
    require(len(executables) == 1 and executables[0].is_file(), "Release record must identify one existing normal executable.")
    runtimes = [f for f in listed if f.get("path", "").endswith("/Managed/LastBell.Runtime.dll")]
    require(len(runtimes) == 1, "Release record must identify one normal runtime assembly.")
    for item in [next(f for f in listed if source_path(f["path"]) == executables[0]), runtimes[0]]:
        path = source_path(item["path"])
        require(path.is_file() and path.stat().st_size == item.get("bytes") and sha256(path) == item.get("sha256"),
                f"Normal executable/runtime differs from the release record: {path}")
    require(b"DragonThemeSmokeProbe" not in source_path(runtimes[0]["path"]).read_bytes(), "Normal runtime contains the observed QA probe.")
    if release.get("qaReport"):
        require(source_path(release["qaReport"]) == report_path, "Release and publication refer to different final QA reports.")
    integration_path = ROOT / "Planning/Production/dragon-v5-integration.json"
    integration = read_json(integration_path)
    require(integration.get("version") == VERSION and integration.get("dataVersion") == DATA_VERSION
            and integration.get("status") in {"saved-assets-applied-observed-build-ready", "saved-assets-applied-windows-packaged"}
            and integration.get("playableStages") == [1,20], "The saved-assets integration record is missing or incompatible.")
    observed_path = source_path(integration["observedBuild"])
    observed = read_json(observed_path)
    require(observed.get("sourceFrozen") is True and observed.get("version") == VERSION
            and observed.get("dataVersion") == DATA_VERSION, "Observed build handoff is not frozen for this release.")
    require(visual.get("runtimeSha256", "").lower() == observed.get("runtimeSha256", "").lower(),
            "Final visual review does not match the current observed build.")
    observed_exe = source_path(observed["executable"]["path"])
    observed_runtime = observed_exe.parent / "LastBellMvp_Data/Managed/LastBell.Runtime.dll"
    require(observed_exe.is_file() and sha256(observed_exe) == observed["executable"].get("sha256")
            and observed_runtime.is_file() and sha256(observed_runtime) == observed.get("runtimeSha256"),
            "Observed executable/runtime no longer matches its frozen handoff.")
    checks = {key: integration.get(key) for key in ["coreAssertions", "dataTests", "unityEditModePassed"]}
    require(all(isinstance(value, int) and not isinstance(value, bool) and value > 0 for value in checks.values()),
            "Actual Core/data/Unity verification counts must be recorded by the integration owner.")
    art_path = ROOT / "Planning/Production/dragon-v5-art-import.json"
    art = read_json(art_path)
    manifest_path = BOARD / "Production/Companions-v5/manifest.json"
    manifest = read_json(manifest_path)
    require(art.get("applied") is True and art.get("status") == "validated", "Companion art import has not completed.")
    require(sha256(manifest_path) == art.get("sourceSha256"), "Companion source manifest changed after the recorded import.")
    imported_manifest = ROOT / "Client/Assets/Game/Art/Presentation/manifest.json"
    require(sha256(imported_manifest) == art.get("manifestSha256"), "Unity presentation manifest changed after the recorded import.")
    actors = manifest.get("actors", [])
    clips = [clip for actor in actors for clip in actor.get("clips", [])]
    frames = sum(clip.get("frameCount", 0) for clip in clips)
    require(len(actors) == 5 and len(clips) == 15 and frames == art.get("frames") == 98 and art.get("portraits") == 10,
            "The 5-actor/15-clip/98-frame/10-portrait publication facts no longer match the imported art.")
    require(all({c.get("name") for c in actor.get("clips", [])} == {"Idle", "Attack", "Run"} for actor in actors), "Expected Idle, Attack and Run for every companion.")
    captures = []
    for value in args.capture:
        path_text, separator, caption = value.partition("|")
        require(bool(separator and caption.strip()), "--capture format is path|accurate caption.")
        path = source_path(path_text)
        require(path.is_relative_to(ROOT / "Planning/Evidence/Dragon-v5") and path.is_file() and path.suffix.lower() == ".png",
                "Actual capture must be an existing PNG under Planning/Evidence/Dragon-v5, not a planning mockup.")
        captures.append({"path": path, "caption": caption.strip()})
    require(args.qa_scope.strip() and all(item.strip() for item in args.qa_limit), "Explicit QA scope and limitations are required.")
    # Record copied evidence and operator-supplied scope separately. A final targeted
    # campaign does not imply rerunning all earlier functionality or all screenshots.
    return dict(release=release, campaign=campaign, release_path=release_path, campaign_path=campaign_path,
                report_path=report_path, review_path=review_path, adopted_review_path=adopted_review_path,
                archive_path=archive_path, executable=executables[0], art_path=art_path,
                integration_path=integration_path, integration=integration, observed_path=observed_path, checks=checks,
                captures=captures, frames=frames, clips=len(clips), scope=args.qa_scope.strip(), limits=args.qa_limit,
                date=release["builtAtUtc"][:10], runs=len(runs),
                resolutions=sorted({f'{r["width"]}×{r["height"]}' for r in runs if r.get("width") and r.get("height")}))


def qa_html(ev: dict, folder: Path) -> str:
    e = html.escape
    limitations = "".join(f"<li>{e(value)}</li>" for value in ev["limits"])
    return (f'<p class="note"><b>승인된 변경 범위 QA · PASS WITH LIMITS.</b> {e(ev["scope"])}</p>'
            f'<p class="small">통합 기록: Core {ev["checks"]["coreAssertions"]} assertions · 데이터 {ev["checks"]["dataTests"]} tests · 실제 Unity EditMode {ev["checks"]["unityEditModePassed"]} passed. 이 수치는 최종 화면 campaign과 별도입니다.</p>'
            f'<p class="small">최종 campaign {ev["runs"]}개 프로세스 통과. 이 숫자는 전체 과거 후보·채택 증거의 합계가 아닙니다. '
            '일반 배포판은 패키지 검증이며 별도 전체 플레이 통과로 확대하지 않습니다.</p>'
            f'<ul>{limitations}</ul><p><a href="{e(link_from(folder,ev["report_path"]),quote=True)}">최종 QA 보고서</a> · '
            f'<a href="{e(link_from(folder,ev["campaign_path"]),quote=True)}">최종 실행 기록</a> · '
            f'<a href="{e(link_from(folder,ev["release_path"]),quote=True)}">배포·해시 기록</a></p>')


def unity_section(ev: dict) -> str:
    e = html.escape
    figures = "".join(f'<figure><a href="{e(link_from(BOARD,c["path"]),quote=True)}"><img src="{e(link_from(BOARD,c["path"]),quote=True)}" alt="{e(c["caption"],quote=True)}"></a><figcaption>{e(c["caption"])}</figcaption></figure>' for c in ev["captures"])
    return f'''<section id="unity-build"><p class="eyebrow">UNITY · V0.9.4 · {e(ev["date"])}</p>
<h2>함께 달리고, 배우고, 여관에서 준비하는 모험.</h2>
<p class="lead">동료·회복력과 Stage5 행동 가이드, 좌우 화자 초상, 실제 도트 ParticleSystem을 저장 씬·프리팹과 Windows 실행판에 연결했습니다.</p>
<div class="actions"><a href="{e(link_from(BOARD,ev["archive_path"]),quote=True)}">Windows v0.9.4 ZIP</a><a href="Adventure-v5/index.html">동료·회복력 상세</a><a href="Production/Companions-v5/review.html">동료 모션 원본</a></div>
<div class="grid3"><article class="card"><h3>함께 달리는 동료</h3><p>5종의 Idle·Attack·Run 15클립, 총98프레임. 새 계정은2토리·12바룬을 만나고, 이전 저장에서 이미 만난 리나·도토도 동료와 서비스를 유지합니다. 모모 아트가 있다는 사실은42스테이지 플레이 구현을 뜻하지 않습니다.</p></article>
<article class="card"><h3>몸으로 읽는 타이밍</h3><p>예고와 후퇴·준비·전진은 실제 적중 시각을 따릅니다. Stage5의 첫 두 타는 위 회피→아래 쳐내기의 새 입력을 확인하고, 이후 정상 속도 전투로 이어집니다.</p></article>
<article class="card"><h3>회복력과 여관</h3><p>회복력은 다섯 번째 성장 스탯입니다. 일반·중간 보스·보스 모두 처치 시 정량 회복을 한 번 적용하며, 보스 추가 완전 회복은 없습니다. 동료 편성·강화·제작·예약은 Core 견적과 확인을 거칩니다.</p></article></div>
<p>플레이어 발화는 왼쪽 초상과 그 오른쪽 이름표, NPC 발화는 오른쪽 초상과 왼쪽 이름표를 사용합니다. 기존9종과 모모의24px 초상을 연결했습니다. 스킬·동료 발사체·강화·축복·회복에는 실제 ParticleSystem을 사용하며, 효과는 별도 전투 판정을 만들지 않습니다.</p>
<p><b>현재 실행 범위는1~20스테이지입니다.</b> 21~50 전투, 새 계정 도토22·리나32·모모42 첫 만남과 다음 회차는 후속 제작입니다. 기존 Dragon 저장 경로를 유지하며 새 저장만 Stage1부터 시작합니다.</p>
{figures}{qa_html(ev, BOARD)}</section>'''


def prepare(ev: dict) -> dict[Path, str]:
    updates: dict[Path, str] = {}
    integration_path = ev["integration_path"]
    record = dict(ev["integration"])
    record.update(version=VERSION, dataVersion=DATA_VERSION, status="saved-assets-applied-windows-packaged",
                  runtimeStageRange=[1,20], qaStatus="CHANGED_SCOPE_PASS_WITH_LIMITS",
                  qaReport=relative(ev["report_path"]), release=relative(ev["release_path"]))
    record["documentationPublication"] = dict(
        campaign=relative(ev["campaign_path"]), scope=ev["scope"], limits=ev["limits"],
        finalCampaignProcesses=ev["runs"], finalCampaignResolutions=ev["resolutions"],
        releaseVerification=ev["release"].get("executionEvidence", "Normal release package; not separately played"),
        sourceEvidenceHashes={relative(p):sha256(p) for p in [ev["release_path"],ev["campaign_path"],ev["report_path"],ev["review_path"],ev["adopted_review_path"],ev["art_path"],ev["observed_path"]]},
        integrationChecks=ev["checks"],
        companionActors=5, companionClips=ev["clips"], companionFrames=ev["frames"], portraits=10,
        captures=[dict(path=relative(c["path"]),caption=c["caption"],sha256=sha256(c["path"])) for c in ev["captures"]])
    updates[integration_path] = json_text(record)
    updates[BOARD / "unity-v5-section.html"] = unity_section(ev) + "\n"

    # Persist the insertion in the original board generator, so a later ordinary
    # rebuild does not silently erase the current release or restore duplicate IDs.
    board_path = BOARD / "build-board.py"
    board = read(board_path)
    board = change_known(board, "<main>__ADVENTURE_V5__", "<main>__UNITY_V5____ADVENTURE_V5__", required=True)
    if "'UNITY_V5':" not in board:
        board = change_known(board, "tokens={", "tokens={'UNITY_V5':(B/'unity-v5-section.html').read_text(encoding='utf-8'),", required=True)
    board = change_known(board, "__UNITY_V4____CHARACTER_SKILLS_V2__", '<details class="archive"><summary>이전 Unity 적용 기록 · v0.9.3</summary>__UNITY_V4__</details>__CHARACTER_SKILLS_V2__', required=True)
    old_read = "(B/'unity-v4-section.html').read_text(encoding='utf-8')"
    if old_read + ".replace(" not in board:
        board = change_known(board, old_read, old_read + ".replace('id=\"unity-build\"','id=\"unity-build-v4\"')", required=True)
    board = replace_first(board, r'<span class="status">.*?</span>', f'<span class="status">{ev["date"]} · 동료·회복력·도트 이펙트 v0.9.4</span>', "board hero status")
    board = board.replace('현재 실행판의 대화 배치·만남 · v0.9.3','이전 v0.9.3 대화 배치·만남')
    board = change_known(board,
        '현재 v0.9.3에서 토리·리나·바룬·도토는 이야기 동료입니다. 새 <a href="Adventure-v5/index.html#companions">v5 기획</a>에서는 이들을 여관에서 편성하고 자동 지원 공격을 받습니다.',
        '현재 v0.9.4는 <a href="Adventure-v5/index.html#companions">여관 동료와 자동 지원 공격</a>을 연결했습니다. 새 계정의1~20 첫 만남은 토리·바룬이며 이전 계정의 리나·도토도 유지합니다. 모모는42스테이지 미발견 상태입니다.')
    board = board.replace('초상화9장, 배경2개 레이어', '초상화10장, 동료5종15클립98프레임, 배경2개 레이어')
    board = board.replace('50개의 개별 바닥 원화나NPC의 게임용 배우를 모두 제작한 것이 아닙니다.', '50개의 개별 바닥 원화나21~50 전투를 모두 제작한 것은 아닙니다. 동료5종 배우는 위의 현재 적용 범위에 포함됩니다.')
    board = board.replace('<span>기세</span></div><p style="font:30px Pixel;', '<span>기세</span><span>회복력</span></div><p style="font:30px Pixel;')
    board = board.replace('href="../../Production/dragon-v4-integration.json">현재 Unity 적용 기록', 'href="../../Production/dragon-v5-integration.json">현재 Unity 적용 기록')
    if ev["captures"]:
        first = ev["captures"][0]
        hero = re.search(r'<section class="hero" id="top">.*?</section>', board, re.S)
        require(hero is not None, "Missing existing board hero.")
        old = hero.group(0)
        new = re.sub(r'(<img src=")[^"]+("[^>]*>)', lambda m:m[1]+html.escape(link_from(BOARD,first["path"]),quote=True)+m[2], old, count=1)
        new = re.sub(r'(<img\b[^>]*\balt=")[^"]*(")', lambda m:m[1]+html.escape(first["caption"],quote=True)+m[2],new,count=1)
        new = re.sub(r'<figcaption>.*?</figcaption>', lambda _:f'<figcaption>{html.escape(first["caption"])}</figcaption>',new,count=1,flags=re.S)
        board = board.replace(old,new,1)
    else:
        board = board.replace('Unity v0.9.3 실제 캡처 · 선명한 공격 예고와 기절바, 하단 중앙 전용 스킬', '이전 v0.9.3 참고 캡처 · 새 실행판 적용·QA 범위는 상단 v0.9.4 기록을 따릅니다')
    updates[board_path] = board

    teaser_path = BOARD / "adventure-v5-section.html"
    teaser = read(teaser_path)
    teaser = replace_first(teaser, r'<p class="eyebrow">.*?</p>', '<p class="eyebrow">ADVENTURE V5 · UNITY V0.9.4 · 실행1~20 적용</p>', "adventure teaser status")
    teaser = replace_first(teaser, r'<p class="lead">.*?</p>', '<p class="lead">24px 초상10종과 화자별 좌우 대화, Stage5 연속 공격 가이드, 후퇴 예고·회복력·여관 동료를 실제 실행판에 연결했습니다. 아래 이미지는 설계 시안이며, 실행 검증 범위는 상단 QA 기록을 따릅니다.</p>', "adventure teaser lead")
    teaser = replace_first(teaser, r'<p class="note">.*?</p>', '<p class="note">현재 실행은1~20입니다. 새 계정 토리2·바룬12, 기존 해금 동료와 여관 서비스를 지원합니다. 21~50과 모모42 첫 만남은 후속 제작이며, 기획50층·아트98프레임과 실제 플레이 범위를 구분합니다.</p>', "adventure teaser scope")
    teaser = teaser.replace('다섯 번째 스탯 회복력만큼 한 번 회복하는 안입니다.', '다섯 번째 스탯 회복력만큼 한 번 회복합니다.')
    teaser = teaser.replace('기존 실행판 v0.9.3은 아래에 그대로 보관', '이전 실행판 v0.9.3 기록은 접힌 보관 영역에 유지')
    updates[teaser_path] = teaser

    page_path = ADVENTURE / "page-template.html"
    page = read(page_path)
    status_section = '<section id="unity-v5-status"><p class="eyebrow">실행 적용 · V0.9.4</p><h2>1~20스테이지에 연결한 현재 제작판.</h2><p>동료5종 Idle·Attack·Run 15클립98프레임, 초상10종, 실제 ParticleSystem, 회복력·Stage5 가이드·후퇴 예고를 연결했습니다. 새 계정은 토리·바룬2명, 이전 계정은 기존 해금4명을 지원합니다. 21~50과 모모42 만남은 후속 범위입니다. 아래 합성 시안은 실제 실행 캡처와 구분합니다.</p>' + qa_html(ev, ADVENTURE) + '</section>'
    page = marked(page,status_section,"<main>")
    page = replace_first(page,r'<span class="status">.*?</span>',f'<span class="status">{ev["date"]} · v0.9.4 실행1~20 적용 · 확장 설계 보존</span>',"adventure hero status")
    page = page.replace('현재 실행판의7리나·15바룬·20도토 만남은 이 개정안에서 이동합니다.', '새 계정의 첫 만남은 토리2·바룬12이며, 도토22·리나32·모모42는 후속 구간입니다. 이전7·15·20 대화는 진행 중인 저장을 위해 남겨 둡니다.')
    page = page.replace('<b>현재 원인을 확인했습니다.</b> 보스 처치 분기에서 HP에 최대 체력을 직접 대입합니다. 일반 적·중간 보스 처치에서는 회복하지 않습니다. 새 기획은 이 완전 회복을 제거하고 모든 스테이지에 같은 회복 규칙을 적용합니다.', '<b>v0.9.3 원인과 v0.9.4 변경.</b> 이전 보스 분기의 완전 회복을 제거했습니다. 현재 일반·중간 보스·보스 모두 같은 처치 경계에서 회복력만큼 한 번 회복합니다. 아래 감사 문서는 변경 전 소스를 기록한 보관 근거입니다.')
    page = page.replace('현재의 설명 확인창을 실제 행동으로 끝내는 짧은 연습으로 바꿉니다.', '설명 확인 뒤 두 번의 실제 새 입력으로 완료하는 행동 가이드를 연결했습니다.')
    page = page.replace('<b>Unity 적용 진행 중.</b> 아래는 효과의 시각·종료 계약입니다. 실제 QA 캡처는 통합 적용과 화면 확인을 마친 뒤 추가합니다.', '<b>실제 ParticleSystem 연결.</b> 아래는 적용한 효과의 시각·종료 계약입니다. 실제로 확인한 QA 범위와 남은 한계는 이 페이지 상단의 결과를 따릅니다.')
    page = page.replace('동료5종의 대기·공격58개 고유 포즈를 검수했으며, Unity 연결은 진행 중입니다.', '동료5종의 Idle·Attack·Run 15클립98프레임을 임포트하고 저장 배우에 연결했습니다. 실제 검증 범위는 상단 QA 기록과 구분해 표시합니다.')
    page = page.replace('다섯 동료의 대기·공격 모션', '다섯 동료의 대기·달리기·공격 모션')
    page = page.replace('전체58프레임 보기', '전체98프레임 보기')
    page = page.replace('대기는 각4프레임, 공격은 각8프레임이며 도토는 중복 회복 포즈를 뺀6프레임입니다.',
                        '대기는 각4프레임, 달리기는 각8프레임, 공격은 각8프레임이며 도토 공격은 중복 회복 포즈를 뺀6프레임입니다.')
    page = page.replace('아트 원본 제작·검수 완료. Unity 연결과 실제 동행 전투 검증은 진행 중입니다.',
                        '아트 원본을 저장 배우에 연결했습니다. 실제 동행·공격·달리기를 확인한 범위는 상단 최종 QA 기록을 따릅니다.')
    page = page.replace('새 대화 배치·24px 초상9종, 회복력, 몬스터 후퇴 준비, Stage5 행동 튜토리얼, 토리2·바룬12 만남과 동료 탭을 첫 구현 묶음으로 잡습니다.',
                        '새 대화 배치·24px 초상10종, 회복력, 몬스터 후퇴 준비, Stage5 행동 튜토리얼, 토리2·바룬12 만남과 동료 탭을1~20 구현 묶음에 연결했습니다.')
    page = page.replace('도토22·리나32의 새 첫 만남, 신규 모모42·약초차는 뒤 지역을 제작할 때 연결합니다.', '도토22·리나32·모모42의 새 첫 만남은 뒤 지역을 제작할 때 연결합니다. 모모 아트와 서비스 구현은42스테이지의 자연 진행 해금을 뜻하지 않습니다.')
    page = page.replace('왼쪽 캐릭터 목록1/4, 오른쪽 다섯 스탯 행', '캐릭터 선택 후, 왼쪽 다섯 스탯 목록 / 오른쪽 선택 스탯 상세')
    page = page.replace('각 행 + 버튼116×96, 10칸 성장 표시.', '각 행 + 버튼96×96, 별점·10칸 성장 표시. 선택 영역과 +는 형제 버튼으로 분리합니다.')
    page = page.replace('이 페이지는 확정 기획과 검수한 아트 원본을 보여 줍니다. Unity 적용은 별도 담당이 진행 중이며, 실행판 적용 완료나 플레이 QA 통과를 뜻하지 않습니다. 구현 묶음이 완성되면 전용 QA를 한 번 진행하고, 발견된 문제에 한해 추가 검증합니다.', '이 페이지는 확정 설계·아트와 현재1~20 적용 기록을 함께 보여 줍니다. 새 기능 전체를 무조건 QA 완료로 간주하지 않으며, 실제 승인한 범위·채택 증거·미실시 항목은 상단 최종 보고서를 따릅니다. 시안·계산기·후속50층 설계는 실행 검증 증거가 아닙니다.')
    page = page.replace('Unity 적용 진행 중이며, 완료 여부는 통합 실행 검증 후 갱신합니다.', '현재1~20 적용 및 QA 한계는 상단 실제 결과를 따릅니다.21~50은 후속 제작입니다.')
    page = page.replace('Unity 적용 진행 중', '설계 시안 · v0.9.4 실행 적용 현황은 상단 참조')
    updates[page_path] = page
    for path in [ADVENTURE/"plan.json", ADVENTURE/"companions-plan.json"]:
        value = read_json(path)
        value["status"] = "v0.9.4 Unity1~20 적용 · 후속21~50 설계 보존 · QA는 연결된 최종 보고서 범위만"
        value["implementationRecord"] = relative(integration_path)
        updates[path] = json_text(value)

    readme_path = ROOT / "README.md"
    readme = read(readme_path)
    current = '현재 **v0.9.4 / mvp1.17.0**은 **1~20스테이지 MVP**다. 루카·다렌·미엘과20초 수동 스킬에 실제 도트 ParticleSystem, 동료 편성·지원 공격, 다섯 번째 스탯 회복력, Stage5 행동 가이드, 실제 적중 시각에 맞춘 후퇴 예고를 연결했다. 새 계정은2토리·12바룬을 만나고, 이전 저장의 리나·도토 해금도 유지한다. 화자별 좌우24px 초상10종과 동료5종15클립98프레임을 연결했으며, **21~50 전투와 모모42 첫 만남은 후속 제작**이다. [현재 적용 기록](Planning/Production/dragon-v5-integration.json)을 기준으로 본다.'
    readme = replace_first(readme,r'현재 \*\*v[^\n]+',current,"root README current release")
    readme = readme.replace('이번 작업에서 기존 Unity 판정·성장·경제·저장 규칙은 변경하지 않았다.', '기존 회피·쳐내기 조작을 유지하며, v0.9.4에서 동료·회복력·여관 거래·저장 이전을 추가했다.')
    readme = readme.replace('[Dragon 적용 기록](Planning/Production/dragon-v3-integration.json)', '[Dragon 적용 기록](Planning/Production/dragon-v5-integration.json)')
    qa_md = ('**확인된 QA 범위:** '+ev["scope"]+'\n\n'
             +f'통합 기록: Core {ev["checks"]["coreAssertions"]} assertions / 데이터 {ev["checks"]["dataTests"]} tests / 실제 Unity EditMode {ev["checks"]["unityEditModePassed"]} passed. 최종 화면 campaign은 별도 {ev["runs"]}개 프로세스이며 이전 후보·채택 증거를 합산한 숫자가 아니다.\n\n'
             +"\n".join('- '+x for x in ev["limits"]))
    links = f'[최종 QA]({relative(ev["report_path"])}) · [실행 기록]({relative(ev["campaign_path"])}) · [배포 기록]({relative(ev["release_path"])})'
    public = f'일반 실행판: `{relative(ev["executable"])}` · [v0.9.4 ZIP]({relative(ev["archive_path"])})\n\n{qa_md}\n\n{links}\n\n일반 배포판은 패키지 검증이며, 관측용 실행판의 표적 QA와 별도 전체 플레이 검증을 구분한다.'
    readme = marked(readme,public,current)
    readme = readme.replace('Builds/Dragon-v4/LastBellMvp.exe',relative(ev["executable"])).replace('Builds/Dragon-v4-Windows-0.9.3.zip',relative(ev["archive_path"]))
    updates[readme_path] = readme
    client_path = ROOT / "Client/README.md"
    client = read(client_path)
    if START not in client:
        client = re.sub(r'^# 현재 적용:.*?\n', '# 이전 v0.9.3 제작 기록\n',client,count=1)
        client = '---\n\n'+client
    client_body = f'''# 현재 적용: 동료·회복력·도트 이펙트 v0.9.4 · {ev["date"]}

현재 플레이는1~20스테이지다. 저장 씬·프리팹 적용 및 Windows 패키지를 연결했으며,21~50은 제작 완료 범위가 아니다.

- 플레이어·NPC 발화에 따라24px 초상의 좌우와 이름표 위치가 바뀐다. 기존9종에 모모를 더해10종이다.
- 동료5종의 Idle·Attack·Run 15클립98프레임과 여관 편성·강화·서비스를 연결했다. 새 계정은 토리2·바룬12, 이전 저장은 기존 해금 최대4명을 지원한다. 모모42는 미래 구간 잠금이다.
- 회복력을 다섯 번째 강화 스탯으로 추가했다. 적 처치마다 한 번 정량 회복하며,10·20 보스 뒤 추가 완전 회복은 제거했다.
- Stage5 첫2연타는 위 회피→아래 쳐내기의 새 입력을 확인한다. 몬스터 후퇴·준비·전진과 예고는 실제 적중 시각에 맞춘다.
- 스킬·동료 발사체·강화·축복·회복에 실제 ParticleSystem을 연결했다. 효과는 Core의 판정·타이머를 대신하지 않는다.
- 기존 `Playtests/dragon-0.9.2` 저장 namespace를 유지한다. 버전 변경으로 기존 진행을 초기화하지 않으며, 명시적 CLI save-dir이 우선한다. 새 저장만 최초 흐름에서 Stage1로 시작한다.

{qa_md}

일반 실행판은 `{relative(ev["executable"])}`, 공유 묶음은 [v0.9.4 ZIP]({link_from(ROOT/"Client",ev["archive_path"])}). 일반판은 패키지 검증이고, 관측판의 실제 표적 QA를 일반판 전체 플레이 통과로 확대하지 않는다.

[최종 QA]({link_from(ROOT/"Client",ev["report_path"])}) · [실행 기록]({link_from(ROOT/"Client",ev["campaign_path"])}) · [배포 기록]({link_from(ROOT/"Client",ev["release_path"])}) · [여관 인계](../Planning/Production/dragon-v5-inn-handoff.md) · [Core 인계](../Planning/Production/dragon-v094-client-handoff.md)
'''
    updates[client_path] = marked(client,client_body)
    return updates


def atomic_write(path: Path, contents: bytes) -> None:
    require(path.parent.is_dir() and path.resolve().is_relative_to(ROOT), f"Unexpected output path: {path}")
    temporary = path.with_name(path.name + ".v5-publish.tmp")
    require(not temporary.exists(), f"Unfinished temporary document exists: {temporary}")
    try:
        with temporary.open("xb") as stream:
            stream.write(contents)
        os.replace(temporary,path)
    finally:
        if temporary.exists():
            temporary.unlink()


def apply(updates: dict[Path,str]) -> None:
    generated = [BOARD/"index.html", ADVENTURE/"index.html"]
    originals = {p:p.read_bytes() if p.exists() else None for p in [*updates,*generated]}
    try:
        for path,text in updates.items():
            content = text.encode("utf-8")
            if originals[path] != content:
                atomic_write(path,content)
        # Only existing documentation generators; no Unity, game data exporter,
        # test runner, packaging, source-art generator, or network command.
        for script in [ADVENTURE/"build-page.py", BOARD/"build-board.py"]:
            subprocess.run([sys.executable,str(script)],cwd=ROOT,check=True)
        for page in generated:
            content = read(page)
            require("v0.9.4" in content, f"Generated page omitted the current release: {page}")
            require(not re.search(r"__[A-Z0-9_]+__",content), f"Unresolved template token in {page}")
        require(read(BOARD/"index.html").count('id="unity-build"') == 1, "Current build anchor must be unique.")
    except BaseException:
        for path,content in originals.items():
            if content is None:
                if path.exists():
                    path.unlink()
            else:
                atomic_write(path,content)
        raise


def main() -> int:
    parser = argparse.ArgumentParser(description=__doc__,formatter_class=argparse.RawDescriptionHelpFormatter)
    parser.add_argument("--release",required=True,help="Actual v0.9.4 release JSON emitted by package-dragon-v5.py")
    parser.add_argument("--campaign",required=True,help="Actual final passed QA campaign JSON; not an earlier failed candidate")
    parser.add_argument("--qa-report",required=True,help="Actual final QA result Markdown")
    parser.add_argument("--qa-scope",required=True,help="Exact approved scope, including adopted/targeted-only evidence when applicable")
    parser.add_argument("--qa-limit",action="append",required=True,help="Exact QA limitation; repeat for every distinct limitation")
    parser.add_argument("--capture",action="append",default=[],help="Optional actual PNG path|accurate caption; repeat as needed")
    parser.add_argument("--apply",action="store_true",help="Write prepared documentation and regenerate the two existing HTML pages")
    args = parser.parse_args()
    try:
        ev = evidence(args)
        updates = prepare(ev)
        changed = [relative(p) for p,s in updates.items() if not p.exists() or p.read_bytes()!=s.encode("utf-8")]
        if args.apply:
            apply(updates)
        print(json.dumps(dict(mode="applied" if args.apply else "preview-no-files-written",version=VERSION,
                              finalCampaignProcesses=ev["runs"],scope=ev["scope"],limits=ev["limits"],changedDocuments=changed,
                              regeneratedPages=[relative(BOARD/"index.html"),relative(ADVENTURE/"index.html")] if args.apply else []),ensure_ascii=False,indent=2))
        return 0
    except (ValueError,KeyError,TypeError,OSError,subprocess.CalledProcessError) as exc:
        print(f"Publication stopped: {exc}",file=sys.stderr)
        return 1


if __name__ == "__main__":
    raise SystemExit(main())
