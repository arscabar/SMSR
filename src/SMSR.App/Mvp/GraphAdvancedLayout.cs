namespace SMSR.App.Mvp;

internal static class GraphAdvancedLayout
{
    internal const string Css = """
        .advanced-intro{color:var(--muted);margin:5px 0 20px}.advanced-group{margin:24px 0}.advanced-group>h2{font-size:17px;margin:0 0 12px}
        .advanced-grid{display:grid;grid-template-columns:repeat(3,minmax(0,1fr));gap:12px;align-items:start}.tool-card{min-width:0;margin:0;padding:0;overflow:hidden}
        .tool-card>summary{list-style:none;cursor:pointer;padding:19px;font-size:16px;font-weight:700;min-height:72px}.tool-card>summary::-webkit-details-marker{display:none}
        .tool-card>summary:after{content:'열기';float:right;color:#78b8ff;font-size:12px}.tool-card[open]>summary:after{content:'닫기'}
        .tool-card[open]{grid-column:1/-1;border-color:#6ca9ec}.tool-body{border-top:1px solid var(--border);padding:16px 19px 19px}
        .tool-body>p{color:var(--muted);margin:0 0 15px}.tool-body>section{margin:0;padding:0;border:0;background:none}.tool-body label{display:block;margin:10px 0 5px;font-weight:600}
        .tool-body input,.tool-body textarea{width:100%;max-width:100%;border:1px solid var(--border2);border-radius:8px}.tool-body button{margin:10px 8px 0 0}
        .tool-limits{margin:18px 0 0;padding:10px 12px;border:1px solid var(--border);border-radius:9px;color:var(--muted)}.tool-limits summary{cursor:pointer;color:var(--text)}
        .tool-limits p{margin:9px 0 0}.tool-body div[role=status]{margin-top:12px}.advanced-note{color:var(--muted);font-size:12px}
        @media(max-width:1100px){.advanced-grid{grid-template-columns:repeat(2,minmax(0,1fr))}}@media(max-width:700px){.advanced-grid{display:block}.tool-card{margin-bottom:10px}}
        """;

    internal const string Script = """
        const tools=[...document.querySelectorAll('main > section.panel')];
        const groups=[
          ['찾기·확인',[[2,'문서 내용에서 찾고 싶나요?','코드·문서 본문의 뜻으로 찾습니다.'],[1,'파일이나 제목을 찾고 싶나요?','이름·경로·제목이 비슷한 항목을 찾습니다.'],[0,'관계 조건을 직접 정하고 싶나요?','전문가용 읽기 전용 질의를 실행합니다.']]],
          ['코드 분석',[[3,'파일 안의 의존성을 확인할까요?','줄 단위 데이터·제어 의존성을 분석합니다. 실행 순서는 아닙니다.'],[4,'C# 파일을 함께 분석할까요?','여러 C# 파일의 호출 관계를 확인합니다.'],[5,'Java 파일을 함께 분석할까요?','여러 Java 파일의 호출 관계를 확인합니다.'],[6,'Java 정의 위치를 찾고 싶나요?','언어 서버로 정의 후보를 확인합니다.'],[7,'JS·TS 파일을 함께 분석할까요?','여러 JavaScript·TypeScript 파일을 분석합니다.']]]
        ];
        let previousGroup;
        for(const [heading,entries] of groups){
          const group=document.createElement('section'),title=document.createElement('h2'),grid=document.createElement('div');
          group.className='advanced-group';grid.className='advanced-grid';title.textContent=heading;group.append(title,grid);
          if(previousGroup) previousGroup.after(group); else tools[0].before(group);
          previousGroup=group;
          for(const [index,name,intro] of entries){
            const section=tools[index],card=document.createElement('details'),summary=document.createElement('summary'),body=document.createElement('div');
            card.className='panel tool-card';body.className='tool-body';summary.textContent=name;
            const description=document.createElement('p');description.textContent=intro;body.append(description);
            const technical=section.querySelector(':scope > h2'),limits=section.querySelector(':scope > p');
            if(technical) technical.remove();
            if(limits){const note=document.createElement('details'),label=document.createElement('summary');note.className='tool-limits';label.textContent='범위와 주의사항';note.append(label,limits);section.append(note)}
            section.classList.remove('panel');body.append(section);card.append(summary,body);grid.append(card);
          }
        }
        """;
}
