import { ITeam } from './ITeam';
import { IUser } from './IUser';

export interface ICommitment {
  id: number;
  userId: number;
  teamId: number;
  startTime: string;
  endTime: string;
  team: ITeam;
  user: IUser;
}
