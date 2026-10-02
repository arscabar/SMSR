const kinds = { code:'코드 파일', document:'문서', concept:'개념', requirement:'요구사항', rationale:'설계 이유', heading:'문서 내부 제목', image:'이미지', video:'영상', audio:'음원', file:'파일', symbol:'코드 심벌', project:'프로젝트' };
const relations = { REQUIRES:'요구', JUSTIFIES:'설계 근거', DESCRIBES:'설명', SEMANTIC_CANDIDATE:'의미 연결 후보', CONTAINS:'포함', LINKS_TO:'연결', CITES_FILE:'파일 인용', DECLARES:'선언', REFERENCES:'참조', CALLS:'코드 호출', PROJECT_REFERENCE:'프로젝트 참조', METHOD:'메서드 선언', DEFINES:'정의', IMPORTS:'가져오기', IMPORTS_FROM:'가져오기', USES:'사용', INHERITS:'상속', IMPLEMENTS:'구현', TYPE_OF:'타입', INSTANTIATES:'생성', DECORATES:'데코레이터', RETURNS:'반환', PARAMETER:'매개변수' };

export const kindName = kind => kinds[kind] || kind;
export const kindClass = kind => kind==='symbol' ? 'kind-code' : ['concept','requirement','rationale'].includes(kind) ? 'kind-document' : ['code','document','image','video','audio'].includes(kind) ? `kind-${kind}` : 'kind-other';
export const relationName = relation => relations[relation] || relation;
Object.assign(relations, { CODE_BEHIND:'화면 코드', VIEW_MODEL:'화면 모델', HANDLES_EVENT:'이벤트 처리', BINDS_TO:'속성 바인딩', COMMAND_BINDS_TO:'명령 바인딩', CONVERTER:'변환기' });

export function labelLines(label, width = 25) {
  const lines = [''];
  let used = 0;
  const sizeOf = value => [...value].reduce((size, char) => size + (/[^\u0000-\u007f]/.test(char) ? 2 : 1), 0);
  for (const word of String(label).match(/\S+\s*|\s+/gu) || ['']) {
    if (used && used + sizeOf(word) > width) { lines.push(''); used = 0; }
    for (const char of word) {
      const size = sizeOf(char);
      if (used + size > width && lines.at(-1)) { lines.push(''); used = 0; }
      lines[lines.length - 1] += char;
      used += size;
    }
  }
  return lines;
}
