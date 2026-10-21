# Last Bell Unity MCP

Last Bell 전용 주소는 `http://127.0.0.1:8093/mcp`, Codex 이름은 `lastbellUnity`이다. Unity 프로젝트는 `C:/Github/Test2/Client`, 인스턴스는 `Client@65e60f79bc76ded9`로 고정한다.

## 실행과 연결 확인

PowerShell 7에서 저장소 루트 기준으로 실행한다. `uv`와 Unity `6000.3.15f1`이 필요하다.

```powershell
pwsh -File ./Tools/MCP/start-lastbell-mcp.ps1
pwsh -File ./Tools/MCP/status-lastbell-mcp.ps1
```

Unity에서 `C:/Github/Test2/Client`를 열고 패키지 임포트/컴파일이 끝나면 자동 연결된다. 서버가 늦게 시작해도 15초 간격으로 재시도한다. 수동 메뉴는 `Last Bell > Tools > Connect Project MCP (8093)`이다. 배치 모드와 다른 경로의 프로젝트에서는 연결하지 않는다.

서버 응답만으로 Unity 연결 성공을 판정하지 않는다. `serverInitialized`, `unityConnected`, `projectVerified`를 각각 확인한다. 연결 smoke가 필요할 때만 아래 명령으로 현재 씬을 1회 읽는다. 플레이 모드, 테스트, 씬 저장은 실행하지 않는다.

```powershell
pwsh -File ./Tools/MCP/status-lastbell-mcp.ps1 -Smoke -OutputPath ./Temp/MCP/smoke.json
```

`readSmokePassed: true`가 실제 Unity 읽기 응답까지 받은 상태다. Unity 미연결 상태에서 `-Smoke`는 종료 코드 1을 반환한다.

## 도구와 리소스 호출

```powershell
pwsh -File ./Tools/MCP/invoke-lastbell-mcp.ps1
pwsh -File ./Tools/MCP/invoke-lastbell-mcp.ps1 -Method resources/read -ResourceUri mcpforunity://project/info
pwsh -File ./Tools/MCP/invoke-lastbell-mcp.ps1 -Tool manage_scene -ArgumentsJson '{"action":"get_active"}'
```

호출 스크립트는 매 세션에서 `initialize` → `notifications/initialized` → 인스턴스 검색 → `set_active_instance` → `project/info` 경로 대조를 수행한다. 요청 도구에는 `unity_instance`도 명시하며 다른 인스턴스 지정은 거부한다. 도구 자체의 작업 범위와 경로 인자는 작업 지시에 맞게 사용한다.

Codex 직접 도구 호출에서도 먼저 `mcpforunity://instances`와 `mcpforunity://project/info`를 확인하고 `set_active_instance(instance="Client@65e60f79bc76ded9")`를 적용한다. 세션 선택은 다른 MCP 클라이언트 세션에 전파되지 않는다. 서버의 `LastBellRegistry`가 프로젝트 경로·이름·해시가 다른 Unity 등록을 거부하므로 이 서버에 다른 프로젝트를 등록할 수 없다. 이 검사는 로컬 실수 방지용이며 인증 기능은 아니다.

## 설치 고정 및 설정

- Unity 패키지: `com.coplaydev.unity-mcp` 10.1.0, SimpleGame에서 사용 중이던 커밋 `c14de1e6dc01ab42d2bb358730cff954bce0ce6b`.
- Python 서버: `mcpforunityserver==10.1.0`. `run_lastbell_server.py`는 해당 버전을 확인한 뒤 기존 서버를 사용하며 설치된 패키지 파일을 수정하지 않는다.
- Unity Package Manager가 생성한 잠금 파일에 MCP의 필수 의존성 `newtonsoft-json` 3.2.2와 `unitywebrequest` 1.0.0이 추가된다. 기존 게임 패키지 버전은 유지한다.
- `.codex/config.toml`의 `[mcp_servers.lastbellUnity]`에만 HTTP 주소를 등록한다. `lastbell.config.toml`은 해당 블록의 참고 사본이다. 다른 서버와 사용자 전역 Codex 설정은 보존한다.
- [OpenAI Docs MCP](https://developers.openai.com/learn/docs-mcp)와 [프로젝트 범위 MCP 설정](https://learn.chatgpt.com/docs/extend/mcp?surface=cli)에 근거한다. 프로젝트 설정은 신뢰된 프로젝트에서 적용된다.
- `codex mcp get lastbellUnity --json`으로 설정 로딩을 확인한다. 이미 실행 중인 Codex 채팅의 도구 목록 갱신 여부는 별도이며 PowerShell 호출은 즉시 사용할 수 있다.

## 운영

서버 로그·PID·캐시는 저장소의 무시된 `Temp/MCP/`에 있다. 첫 실행은 Python 의존성을 다운로드한다. 시작 스크립트는 숨김으로 로컬 루프백에만 실행하고, 포트 점유 시 프로세스를 종료하지 않고 오류를 반환한다. 포트 충돌이 실제로 확인된 경우에만 새 포트를 정하고 `common.ps1`, `run_lastbell_server.py`, Editor 연결 코드, Codex 설정과 이 문서를 함께 갱신한다.

Editor 연결 코드는 기존 `LastBell.Editor.asmdef`를 변경하지 않도록 고정 패키지의 공개 API를 reflection으로 호출한다. 패키지 자체의 자동 서버 시작은 Last Bell Editor의 `SessionState`에서만 억제한다. 다른 프로젝트가 저장한 전역 포트로 서버를 만드는 일을 막기 위한 조치이며 사용자 전역 자동 시작 설정은 바꾸지 않는다. 패키지의 연결 API 자체는 전역 EditorPrefs에 URL을 저장하므로 각 프로젝트는 자신의 연결 helper를 사용해야 한다.

재시작이 필요하면 `Temp/MCP/server.pid`의 PID와 실제 명령줄이 이 저장소의 `run_lastbell_server.py`를 가리키는지 먼저 확인한다. 포트 번호만으로 프로세스를 종료하거나 다른 Unity/MCP를 중지하지 않는다. 전체 게임 QA는 별도 통합 완료 시점에 수행한다.
