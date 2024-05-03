import { IUser } from './IUser';

export interface IAllAndTeamUsers {
  teamUsers: IUser[];
  allOtherUsers: IUser[];
}
