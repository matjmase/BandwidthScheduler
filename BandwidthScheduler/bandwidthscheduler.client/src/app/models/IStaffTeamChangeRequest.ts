import { ITeam } from './db/ITeam';
import { IUser } from './db/IUser';

export interface IStaffTeamChangeRequest {
  currentTeam: ITeam;
  toAdd: IUser[];
  toRemove: IUser[];
}
