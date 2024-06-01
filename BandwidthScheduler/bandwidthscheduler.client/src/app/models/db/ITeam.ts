import { ICommitment } from './ICommitment';

export interface ITeam {
  id: number;
  name: string;
  commitments: ICommitment[] | undefined;
}
