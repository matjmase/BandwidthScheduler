import { CommitmentEntry } from './CommitmentEntry';
import { ICommitmentNotification } from './ICommitmentNotification';
import { IUser } from './IUser';

export class CommitmentNotificationEntry {
  id: number;
  userId: number;
  commitmentId: number;
  timeStamp: Date;
  seen: boolean;
  commitment: CommitmentEntry;
  user: IUser;

  constructor(resp: ICommitmentNotification) {
    this.id = resp.id;
    this.userId = resp.userId;
    this.commitmentId = resp.commitmentId;
    this.timeStamp = new Date(Date.parse(resp.timeStamp));
    this.seen = resp.seen;
    this.commitment = new CommitmentEntry(resp.commitment);
    this.user = resp.user;
  }
}
