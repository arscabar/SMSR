"""Generate a test-only diagram, not an edit of a user's image."""
from pathlib import Path
from PIL import Image, ImageDraw, ImageFont

image = Image.new('RGB', (900, 260), '#101722')
draw = ImageDraw.Draw(image)
font = ImageFont.truetype('C:/Windows/Fonts/segoeui.ttf', 32)
for x, label in [(30, 'Validate'), (330, 'Store'), (630, 'Report')]:
    draw.rounded_rectangle((x, 70, x+240, 190), 14, fill='#205986', outline='#8ac4ff', width=3)
    draw.text((x+55, 110), label, fill='white', font=font)
for x in (270, 570):
    draw.line((x, 130, x+55, 130), fill='white', width=4)
    draw.polygon([(x+55, 130), (x+42, 122), (x+42, 138)], fill='white')
target = Path(__file__).resolve().parents[1] / 'docs/test-data/media-semantic-flow.png'
image.save(target)
print(target)
