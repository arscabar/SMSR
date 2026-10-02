"""Directed cross-community links are summaries of existing facts, never new edges."""
from collections import Counter

def summarize(edges, graph, membership, groups):
    counts, surprises = Counter(), []
    for edge in sorted(edges, key=lambda e: (e['sourceId'], e['targetId'], e['relation'])):
        a, b = edge['sourceId'], edge['targetId']
        if a not in membership or b not in membership or membership[a] == membership[b]:
            continue
        counts[membership[a], membership[b], edge['relation'], edge['confidence'], edge['resolution']] += 1
        if (len(surprises) < 10 and edge['resolution'] == 'RESOLVED' and edge['confidence'] in {'EXTRACTED','EXPLICIT'}
                and graph.nodes[a]['kind'] == graph.nodes[b]['kind'] == 'symbol'
                and edge['relation'] not in {'CONTAINS','DEFINES','METHOD','IMPORTS','IMPORTS_FROM'}):
            surprises.append(dict(edge=edge, reason='서로 다른 구조 그룹의 실제 심벌을 잇는 관계',
                question=f"{graph.nodes[a]['label']}에서 {graph.nodes[b]['label']}로 연결되는 이유는 무엇인가요?"))
    links = [dict(sourceGroup=a,targetGroup=b,relation=r,confidence=c,resolution=s,count=n)
             for (a,b,r,c,s),n in sorted(counts.items(), key=lambda pair: (-pair[1],pair[0]))]
    questions = [s['question'] for s in surprises[:5]]
    questions.extend(f"{g['name']} 그룹의 응집도가 {g['cohesion']:.4f}로 낮은 이유는 무엇인가요?"
                     for g in groups if g['cohesion'] < .05 and g['nodeCount'] >= 5)
    return links, surprises, questions[:10]
