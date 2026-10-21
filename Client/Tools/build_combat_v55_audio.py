"""Original 220ms metal-tension cue; no external samples. Rebuild only this v5.5 asset."""
from pathlib import Path
import math
import struct
import wave

destination = Path(__file__).resolve().parents[1] / "Assets/Game/Audio/Presentation/enemy-windup-v55.wav"
with wave.open(str(destination), "wb") as output:
    output.setparams((1, 2, 22050, 0, "NONE", "not compressed"))
    output.writeframes(b"".join(
        struct.pack("<h", int(8000 * (i / 4851) ** .4 * (1 - i / 4851) ** .4 * (
            math.sin(2 * math.pi * (640 * i / 22050 + 480 * (i / 22050) ** 2)) * .65
            + math.sin(2 * math.pi * 1823 * i / 22050) * .2)))
        for i in range(4851)
    ))
