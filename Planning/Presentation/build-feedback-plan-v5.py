"""Refresh the offline review board's numeric source; does not modify game data."""
import json
import re
from pathlib import Path

root = Path(__file__).parent
data = json.loads((root / "feedback-plan-v5.json").read_text(encoding="utf-8"))
page = root / "feedback-plan-v5.html"
source = page.read_text(encoding="utf-8")
payload = json.dumps(data, ensure_ascii=False).replace("</", "<\\/")
source, count = re.subn(
    r'(<script id="plan-data" type="application/json">).*?(</script>)',
    lambda match: match[1] + payload + match[2], source, flags=re.S,
)
assert count == 1
page.write_text(source, encoding="utf-8")
print("Updated planning-v5 offline review board")
