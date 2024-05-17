import { IUser } from './db/IUser';

export interface IAllAndTeamUsers {
  teamUsers: IUser[];
  allOtherUsers: IUser[];
}
