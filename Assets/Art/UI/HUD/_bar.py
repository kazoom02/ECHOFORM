import cairosvg
fill = '''<svg viewBox="0 0 512 48" xmlns="http://www.w3.org/2000/svg">
<defs>
<linearGradient id="f" x1="0" y1="0" x2="0" y2="1">
 <stop offset="0" stop-color="#aefcff"/><stop offset="0.5" stop-color="#22e0f5"/><stop offset="1" stop-color="#0a9fb5"/>
</linearGradient>
<filter id="g" x="-20%" y="-40%" width="140%" height="180%"><feGaussianBlur stdDeviation="2" result="b"/><feMerge><feMergeNode in="b"/><feMergeNode in="SourceGraphic"/></feMerge></filter>
</defs>
<rect x="4" y="6" width="504" height="36" rx="8" fill="url(#f)" filter="url(#g)"/>
<rect x="4" y="9" width="504" height="8" rx="4" fill="#ffffff" opacity="0.35"/>
</svg>'''
track = '''<svg viewBox="0 0 512 48" xmlns="http://www.w3.org/2000/svg">
<rect x="2" y="4" width="508" height="40" rx="9" fill="#07171b" stroke="#0affff" stroke-opacity="0.4" stroke-width="2"/>
<rect x="2" y="4" width="508" height="40" rx="9" fill="none" stroke="#000" stroke-opacity="0.5" stroke-width="1"/>
</svg>'''
open("InstallBar_Fill.svg","w").write(fill); open("InstallBar_Track.svg","w").write(track)
for s,p in [("InstallBar_Fill.svg","InstallBar_Fill.png"),("InstallBar_Track.svg","InstallBar_Track.png")]:
    cairosvg.svg2png(url=s,write_to=p,output_width=512,output_height=48); print("wrote",p)

from PIL import Image
tr=Image.open("InstallBar_Track.png").convert("RGBA"); fl=Image.open("InstallBar_Fill.png").convert("RGBA")
bg=Image.new("RGBA",(560,180),(18,20,24,255))
for i,frac in enumerate([1.0,0.6,0.25]):
    y=20+i*52
    bg.alpha_composite(tr.resize((520,44)),(20,y))
    w=int(520*frac); 
    if w>0: bg.alpha_composite(fl.resize((520,44)).crop((0,0,w,44)),(20,y))
bg.convert("RGB").save("/sessions/dazzling-loving-ritchie/mnt/outputs/bar_preview.png"); print("preview ok")
