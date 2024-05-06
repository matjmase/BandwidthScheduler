import { ITeam } from './ITeam';
import { IUser } from './IUser';

export interface ICommitmentResponse {
  id: number;
  userId: number;
  userEmail: string;
  teamId: number;
  teamName: string;
  startTime: string;
  endTime: string;
}
