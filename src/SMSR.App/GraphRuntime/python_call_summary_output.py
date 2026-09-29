from python_summary_labels import describe


def update(state, spend, work):
    summary=state['f']['valueSummary'];labels=state['labels']
    summary['callEdges']=[state['edges'][key] for key in sorted(state['edges'])]
    def refresh(n):n.update(describe(labels[n['valueId']],spend,work))
    for r in summary['returns']:refresh(r)
    for c in summary['calls']:
        if c['target']:refresh(c['target'])
        if c['receiver']:refresh(c['receiver'])
        for a in c['arguments']:refresh(a)
        c['resultSummary']=describe(labels[c['resultValueId']],spend,work)
