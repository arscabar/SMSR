export function identity<T>(value: T): T {
  return value;
}

export function select(value: number): number;
export function select(value: string): string;
export function select(value: unknown): unknown {
  return value;
}

export function overwrite(value: number): number {
  value = 0;
  return value;
}

export function choose(value: number, enabled: boolean): number {
  let result = 0;
  if (enabled) {
    result = value;
  }
  return result;
}

export function repeat(value: number, enabled: boolean): number {
  let result = 0;
  while (enabled) {
    result = value;
    enabled = false;
  }
  return result;
}

export function counted(value: number): number {
  let result = 0;
  for (let i = 0; i < 1; result = value) {
    i++;
    continue;
  }
  return result;
}

export function relayed(value: number): number {
  return identity(value);
}

export function erased(value: number): number {
  return overwrite(value);
}

export function decide(enabled: boolean, value: number, fallback: number): number {
  if (enabled) return value;
  return fallback;
}

export function nullSet(value: number | null | undefined, fallback: number): number {
  value ??= fallback;
  return value;
}

export function selected(first: boolean, second: boolean, value: number, fallback: number): number {
  return first && second ? value : fallback;
}

export function endless(): never { while (true) { } }

export function switchPick(mode: number, value: number, fallback: number): number {
  switch (mode) {
    case 0: return value;
    default: return fallback;
    case 1: return value + 1;
  }
}

export function switchFall(mode: number, value: number): number {
  switch (mode) {
    case 0: value++;
    default: value += 2;
    case 1: value += 3; break;
  }
  return value;
}

export function switchRelayed(mode: number, value: number, fallback: number): number {
  return switchPick(mode, value, fallback);
}

export function switchKilled(mode: number, value: number): number {
  switch (mode) {
    case 0: value = 0; break;
    default: value = 1;
  }
  return value;
}

export function switchErased(mode: number, value: number): number {
  return switchKilled(mode, value);
}
