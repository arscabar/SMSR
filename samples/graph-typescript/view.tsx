declare namespace JSX {
  interface Element {}
}

function View(props: { value: number }): JSX.Element {
  return {};
}

const view = <View value={1} />;
