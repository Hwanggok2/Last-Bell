from pathlib import Path
from PIL import Image
R=Path(__file__).resolve().parent.parent/'Evidence/Presentation-v4'
for kind,ms in [('attack',25),('run',50)]:
    frames=[Image.open(p).convert('RGB').resize((768,432),Image.Resampling.NEAREST) for p in sorted((R/kind).glob('*.png'))]
    frames[0].save(R/f'{kind}.gif',save_all=True,append_images=frames[1:],duration=[ms]*(len(frames)-1)+[400 if kind=='attack' else ms],loop=0,disposal=2)
print('Local rendered animation previews encoded.')
