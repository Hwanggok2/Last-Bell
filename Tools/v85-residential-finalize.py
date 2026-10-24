"""Curate inspected native frames and document source registration, without retouching art."""
import pathlib, json, hashlib
from PIL import Image, ImageDraw
ROOT=pathlib.Path(__file__).resolve().parents[1]
P=ROOT/'Planning/Story/Monster-v85/residential'
jobs=json.loads((P/'jobs.json').read_text(encoding='utf-8'))
# A uniform 6px canvas registration restores the ghost's trail to the same floor.
# No scaling, interpolation, colors, or pixels are changed; raw sources remain intact.
for i in range(9):
    src=Image.open(P/'water_wraith/stun'/f'{i:02}.png').convert('RGBA')
    assert src.getbbox()[1]>=6
    registered=Image.new('RGBA',src.size)
    registered.paste(src,(0,-6))
    assert sorted(c for c in src.getdata() if c[3])==sorted(c for c in registered.getdata() if c[3])
    dest=P/'water_wraith/stun-registered'/f'{i:02}.png';dest.parent.mkdir(exist_ok=True);registered.save(dest)
for j in jobs['jobs']:
    if j['id']=='water_wraith':
        j['clips']['stun']['folder']='stun-registered'
        j['clips']['stun']['registration']={'rawFolder':'stun','dx':0,'dy':-6,'reason':'Trail baseline136 -> common130. Native pixels and dimensions unchanged.'}
    if j['id']=='sewer_rat':
        j['rejectedContactCleanup']={'job':'35066cb7-1f05-4381-b0e4-002db31a6e34','file':'rat-contact-rejected.png','reason':'Projectile removed, but added fur shading broke the approved flat block style. Use original source8 instead.'}
    if j['id']=='drowned_resident':
        j['unusedContactCleanup']={'job':'a6f06af5-1770-4fc5-9e94-c44357b0a1fc','file':'resident-contact-not-selected.png','reason':'Native source10 preserves the intended weapon contact and original style. Cleanup changed plank length unnecessarily.'}
(P/'jobs.json').write_text(json.dumps(jobs,ensure_ascii=False,indent=2)+'\n',encoding='utf-8')
rat_attack=list(range(9))+[13,14,15,16]
review={
 'sewer_rat':{'accepted':True,'worldHeight':.8,
  'idle':{'sourceFrames':list(range(9)),'holdsMs':[180]*9},
  'attack':{'sourceFrames':rat_attack,'holdsMs':[40]*8+[100,70,70,70,70],'contactFrame':8,'contactPoint':{'x':33,'y':103},'excludedFrames':[9,10,11,12],'exclusionReason':'Unrequested white spit projectile and detached trailing pixels, including canvas-edge clipping.'},
  'stun':{'sourceFrames':list(range(9)),'holdsMs':[110]*9},
  'notes':['Bite contact is the mouth, not the long bare tail.','Stun keeps lowered forequarters, slack tail and exposed incisors.']},
 'drowned_resident':{'accepted':True,'worldHeight':1.28,
  'idle':{'sourceFrames':list(range(9)),'holdsMs':[180]*9},
  'attack':{'sourceFrames':list(range(17)),'holdsMs':[40]*10+[60]+[40]*6,'contactFrame':10,'contactPoint':{'x':37,'y':123}},
  'stun':{'sourceFrames':list(range(9)),'holdsMs':[110]*9},
  'notes':['Large hood, short boots and one held plank; this is a flood echo, not a dead resident.','Stun keeps the original plank in hand. Rejected first pose dropped the weapon.']},
 'water_wraith':{'accepted':True,'worldHeight':1.4,
  'idle':{'sourceFrames':[0,1,2,1,0,8,7,8],'holdsMs':[200]*8,'excludedFrames':[3,4,5,6],'exclusionReason':'Even second generated idle rounded the pointed hood in these frames. Curated forward/reverse native frames preserve the approved silhouette.'},
  'attack':{'sourceFrames':list(range(17)),'holdsMs':[80,80,40,60]+[40]*9+[20]*4,'contactFrame':3,'contactPoint':{'x':30,'y':82}},
  'stun':{'sourceFrames':list(range(9)),'holdsMs':[110]*9},
  'notes':['Only five unique native Idle images are used, ordered into an eight-frame1.6s loop; this is disclosed, not nine unique poses.','Stun source frames uniformly registered -6px vertically; no resampling or content edits.']}
}
# Both attacks stay700ms. The wraith has a fast claw contact after200ms;
# the runtime synchronizes its attack event to contactFrame, not a guessed middle index.
for key,r in review.items():
    assert sum(r['attack']['holdsMs'])==700,(key,sum(r['attack']['holdsMs']))
    r['attack']['contactAtMs']=sum(r['attack']['holdsMs'][:r['attack']['contactFrame']])
    r['canvas']={'width':192,'height':160,'pivot':{'x':96,'y':130}}
    r['heroReference']='Client/Assets/Game/Art/Presentation/Clips/hero-v5-idle/01.png'
    r['whiteHitMs']=120;r['deathAnimation']=False
(P/'review-decisions.json').write_text(json.dumps(review,ensure_ascii=False,indent=2)+'\n',encoding='utf-8')
metrics={}
for j in jobs['jobs']:
    key=j['id'];metrics[key]={}
    for motion in ['idle','attack','stun']:
        sel=review[key][motion];ims=[Image.open(P/key/j['clips'][motion]['folder']/f'{i:02}.png').convert('RGBA') for i in sel['sourceFrames']]
        hashes=[hashlib.sha256(im.tobytes()).hexdigest() for im in ims]
        alpha_seam=sum(x!=y for x,y in zip(ims[0].getchannel('A').getdata(),ims[-1].getchannel('A').getdata()))
        color_diffs=[max(abs(a[i]-b[i]) for i in range(3)) for a,b in zip(ims[0].getdata(),ims[-1].getdata()) if a[3] and b[3]]
        assert all(im.getbbox()[0]>0 and im.getbbox()[1]>0 and im.getbbox()[2]<192 and im.getbbox()[3]<160 for im in ims)
        m={'sourceFrames':sel['sourceFrames'],'selectedFrames':len(ims),'uniquePixelFrames':len(set(hashes)),'totalMs':sum(sel['holdsMs']),'seamAlphaChangedPixels':alpha_seam,'seamMeanMaxRGBDifference':round(sum(color_diffs)/len(color_diffs),3),'edgeClipped':False,'binaryAlpha':all(set(im.getchannel('A').getdata())<={0,255} for im in ims)}
        if motion=='idle' and key!='water_wraith':
            rois=[(34,124,136,130)] if key=='sewer_rat' else [(76,124,100,130),(113,124,143,130)]
            m['footAlphaRois']=[{'rect':roi,'changedPixels':[sum(a!=b for a,b in zip(ims[0].getchannel('A').crop(roi).getdata(),im.getchannel('A').crop(roi).getdata())) for im in ims]} for roi in rois]
        metrics[key][motion]=m
        sheet=Image.new('RGB',(1020,((len(ims)+4)//5)*190+30),'#182834');d=ImageDraw.Draw(sheet);d.text((8,8),f'{key} {motion} SELECTED',fill='white')
        displays=[]
        for n,im in enumerate(ims):
            x=n%5*204;y=n//5*190+30;d.text((x+6,y+3),f'{n:02} src{sel["sourceFrames"][n]:02}'+(' CONTACT' if n==sel.get('contactFrame',-1) else ''),fill='#f9d296');sheet.paste(im,(x+6,y+24),im)
            bg=Image.new('RGB',(576,480),'#182834');up=im.resize((576,480),Image.Resampling.NEAREST);bg.paste(up,(0,0),up);displays.append(bg)
        sheet.save(P/f'{key}-{motion}-selected.png')
        holds=sel['holdsMs'].copy()
        if motion=='attack':holds[-1]+=900
        displays[0].save(P/f'{key}-{motion}-selected.gif',save_all=True,append_images=displays[1:],duration=holds,loop=0)
(P/'selected-qa.json').write_text(json.dumps(metrics,indent=2)+'\n',encoding='utf-8')
print('Saved decisions, native-frame selections, normalized-visible seam metrics and source registration proof.')
