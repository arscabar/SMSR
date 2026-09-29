"""Fixed full-text coverage and offline vector checks."""
import numpy as np
from tokenizers import Tokenizer
from embedding import model
from full_embedding import windows, embed_full

engine = model()
tokenizer = Tokenizer.from_str(engine.model.tokenizer.to_str())
budget = min(192, (tokenizer.truncation or {}).get('max_length', 512) - 2)
tokenizer.no_truncation()
tokenizer.no_padding()
for source in ['한글과 emoji 🐈\n' * 300 + '마지막 문장', 'abc_' * 2000, '']:
    parts = list(windows(source, tokenizer, budget))
    assert ''.join(parts) == source
    assert all(len(tokenizer.encode(part).ids) <= budget for part in parts)
texts = ['온라인 결제 승인과 카드 환불 절차', 'payment authorization and credit card refund',
         'banana pancakes and cooking recipes']
vectors = np.asarray(embed_full(engine, texts))
assert vectors.shape == (3, 384) and np.isfinite(vectors).all()
assert vectors[0] @ vectors[1] > vectors[0] @ vectors[2]
print('Full token-window coverage, Unicode, finite vectors and semantic ranking passed')
