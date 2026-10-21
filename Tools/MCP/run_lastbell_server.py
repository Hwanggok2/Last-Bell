"""Run the pinned upstream bridge with registration limited to Last Bell."""
import hashlib
import importlib.metadata
import sys
from pathlib import Path

import main as upstream
from transport.plugin_registry import PluginRegistry

PROJECT_ROOT = "C:/Github/Test2/Client"
PROJECT_HASH = hashlib.sha1(f"{PROJECT_ROOT}/Assets".encode()).hexdigest()[:16]
INSTANCE = f"Client@{PROJECT_HASH}"


class LastBellRegistry(PluginRegistry):
    async def register(self, session_id, project_name, project_hash, unity_version,
                       project_path=None, user_id=None):
        actual = (project_path or "").replace("\\", "/").rstrip("/").casefold()
        if actual != PROJECT_ROOT.casefold() or project_hash != PROJECT_HASH or project_name != "Client":
            raise ValueError(f"This MCP server accepts only {PROJECT_ROOT} ({INSTANCE}).")
        return await super().register(session_id, project_name, project_hash,
                                      unity_version, project_path, user_id=user_id)


def main():
    if importlib.metadata.version("mcpforunityserver") != "10.1.0":
        raise RuntimeError("Last Bell requires mcpforunityserver==10.1.0.")
    # The upstream lifespan constructs this registry. No installed package is edited.
    upstream.PluginRegistry = LastBellRegistry
    runtime = Path(PROJECT_ROOT).parent / "Temp" / "MCP"
    runtime.mkdir(parents=True, exist_ok=True)
    sys.argv = [sys.argv[0], "--transport", "http", "--http-url", "http://127.0.0.1:8093",
                "--http-host", "127.0.0.1", "--http-port", "8093",
                "--default-instance", INSTANCE, "--project-scoped-tools",
                "--pidfile", str(runtime / "server.pid")]
    upstream.main()


if __name__ == "__main__":
    main()
