import { identity, select, overwrite, choose, repeat, counted } from './math';

export const count = identity(42);
export const title = select('example');
export const reset = overwrite(5);
export const chosen = choose(5, true);
export const repeated = repeat(5, true);
export const countedResult = counted(5);
