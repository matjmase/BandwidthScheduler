import { ITeam } from './ITeam';
import { IUser } from './IUser';

export interface IStaffTeamChangeRequest {
  currentTeam: ITeam;
  toAdd: IUser[];
  toRemove: IUser[];
}
