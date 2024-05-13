import { IUser } from './IUser';

export interface IAvailability {
  id: number;
  userId: number;
  startTime: string;
  endTime: string;
  user: IUser;
}
