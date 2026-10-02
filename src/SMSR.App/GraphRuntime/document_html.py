"""Visible text only; scripts/styles/embedded active content are not evaluated."""
from html.parser import HTMLParser

class Reader(HTMLParser):
    def __init__(self):
        super().__init__(convert_charrefs=True)
        self.hidden = 0
        self.blocks = []
    def handle_starttag(self, tag, attrs):
        if tag in {'script', 'style', 'iframe', 'object', 'template'}:
            self.hidden += 1
    def handle_endtag(self, tag):
        if tag in {'script', 'style', 'iframe', 'object', 'template'} and self.hidden:
            self.hidden -= 1
    def handle_data(self, data):
        if not self.hidden and data.strip():
            self.blocks.append((f'html-line:{self.getpos()[0]}/block:{len(self.blocks)+1}', data.strip()))

def read(raw, _):
    parser = Reader()
    parser.feed(raw.decode('utf-8', errors='strict'))
    yield from parser.blocks
