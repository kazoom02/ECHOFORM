import cairosvg
on='''<svg viewBox="0 0 80 80" xmlns="http://www.w3.org/2000/svg"><defs>
<radialGradient id="c" cx="0.5" cy="0.5" r="0.5"><stop offset="0" stop-color="#ffd6d9"/><stop offset="0.4" stop-color="#ff3a44"/><stop offset="1" stop-color="#a00d16"/></radialGradient>
<filter id="g" x="-60%" y="-60%" width="220%" height="220%"><feGaussianBlur stdDeviation="3" result="b"/><feMerge><feMergeNode in="b"/><feMergeNode in="SourceGraphic"/></feMerge></filter></defs>
<path d="M40 8 L68 24 L68 56 L40 72 L12 56 L12 24 Z" fill="none" stroke="#ff2a35" stroke-width="3" filter="url(#g)"/>
<circle cx="40" cy="40" r="15" fill="url(#c)" filter="url(#g)"/><circle cx="35" cy="35" r="4" fill="#fff" opacity="0.8"/></svg>'''
off='''<svg viewBox="0 0 80 80" xmlns="http://www.w3.org/2000/svg">
<path d="M40 8 L68 24 L68 56 L40 72 L12 56 L12 24 Z" fill="#1a0c0d" stroke="#5a1c20" stroke-width="3"/>
<circle cx="40" cy="40" r="15" fill="#240d0f" stroke="#401418" stroke-width="1.5"/></svg>'''
open("CopyPip_On.svg","w").write(on); open("CopyPip_Off.svg","w").write(off)
for s,p in [("CopyPip_On.svg","CopyPip_On.png"),("CopyPip_Off.svg","CopyPip_Off.png")]:
    cairosvg.svg2png(url=s,write_to=p,output_width=128,output_height=128); print("wrote",p)
from PIL import Image
on_i=Image.open("CopyPip_On.png"); off_i=Image.open("CopyPip_Off.png")
bg=Image.new("RGBA",(360,90),(18,20,24,255))
for i in range(5): bg.alpha_composite((on_i if i<3 else off_i).resize((56,56)),(20+i*64,16))
bg.convert("RGB").save("/sessions/dazzling-loving-ritchie/mnt/outputs/copypip_preview.png"); print("ok")
