"""Save the agreed game palette as a data strip for PixelLab, not a drawable sprite."""
import pathlib,json
from PIL import Image
source=pathlib.Path(__file__).resolve().parents[1]/'Planning/Story/Monster-v85/residential'
colors=['#061322','#17303d','#34505b','#58747b','#8fa7a2','#c9d6c0','#f9d296','#f4a00f','#966338','#5b3d2e','#352a2b','#59445b','#8a6674','#42665a','#649685','#b6be69']
strip=Image.new('RGB',(16,1));strip.putdata([tuple(bytes.fromhex(c[1:])) for c in colors]);strip.save(source/'palette.png')
(source/'palette.json').write_text(json.dumps({'colors':colors,'heroOriginal':'Client/Assets/Game/Art/Presentation/Clips/hero-v5-idle/01.png','originalHeroDominantRoles':{'outline':'#061322','warmLight':'#f9d296','gold':'#f4a00f'},'notes':'Exact hero outline/light/gold roles; cold residential and warm animal material hues retain identity.'},indent=2),encoding='utf-8')
print('16-color palette strip saved')
