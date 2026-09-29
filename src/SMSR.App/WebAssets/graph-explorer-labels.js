const kinds = { code:'코드', document:'문서', heading:'문서 내부 제목', image:'이미지', video:'영상', audio:'음원', file:'파일', symbol:'심벌', project:'프로젝트' };
const relations = { CONTAINS:'포함', LINKS_TO:'연결', CITES_FILE:'파일 인용', DECLARES:'선언', REFERENCES:'참조', CALLS:'코드 호출', PROJECT_REFERENCE:'프로젝트 참조' };

export const kindName = kind => kinds[kind] || kind;
export const kindClass = kind => ['code','document','image','video','audio'].includes(kind) ? `kind-${kind}` : 'kind-other';
export const relationName = relation => relations[relation] || relation;

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
