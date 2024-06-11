import { ICommitment } from './ICommitment';
import { IUser } from './IUser';

export interface ICommitmentNotification {
  id: number;
  userId: number;
  commitmentId: number;
  timeStamp: string;
  seen: boolean;
  commitment: ICommitment;
  user: IUser;
}
