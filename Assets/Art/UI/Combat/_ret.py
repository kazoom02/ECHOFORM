import cairosvg
svg='''<svg viewBox="0 0 256 256" xmlns="http://www.w3.org/2000/svg"><defs>
<filter id="g" x="-30%" y="-30%" width="160%" height="160%"><feGaussianBlur stdDeviation="2.4" result="b"/><feMerge><feMergeNode in="b"/><feMergeNode in="SourceGraphic"/></feMerge></filter></defs>
<g stroke="#0affff" stroke-width="7" fill="none" filter="url(#g)" stroke-linecap="round">
<path d="M20 60 L20 20 L60 20"/><path d="M196 20 L236 20 L236 60"/>
<path d="M236 196 L236 236 L196 236"/><path d="M60 236 L20 236 L20 196"/>
</g>
<g stroke="#5efcff" stroke-width="3" fill="none" filter="url(#g)" opacity="0.9">
<path d="M128 12 L128 30"/><path d="M128 226 L128 244"/><path d="M12 128 L30 128"/><path d="M226 128 L244 128"/>
</g>
<g fill="#0affff" filter="url(#g)"><circle cx="20" cy="20" r="4"/><circle cx="236" cy="20" r="4"/><circle cx="20" cy="236" r="4"/><circle cx="236" cy="236" r="4"/></g>
</svg>'''
cairosvg.svg2png(bytestring=svg.encode(), write_to="TargetReticle.png", output_width=256, output_height=256)
from PIL import Image
r=Image.open("TargetReticle.png").convert("RGBA")
bg=Image.new("RGBA",(300,300),(30,34,40,255)); bg.alpha_composite(r,(22,22))
bg.convert("RGB").save("/sessions/dazzling-loving-ritchie/mnt/outputs/reticle_preview.png")
print("ok")
