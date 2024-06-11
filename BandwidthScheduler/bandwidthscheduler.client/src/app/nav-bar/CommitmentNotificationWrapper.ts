import { CommitmentNotificationEntry } from '../models/db/CommitmentNotificationEntry';
import { INotificationWrapper } from './INotificationWrapper';

export class CommitmentNotificationWrapper implements INotificationWrapper {
  notificationType: string = 'Commitment';
  get timeStamp(): Date {
    return this.commit.timeStamp;
  }
  get startTime(): Date {
    return this.commit.commitment.startTime;
  }
  get endTime(): Date {
    return this.commit.commitment.endTime;
  }

  constructor(private commit: CommitmentNotificationEntry) {}
}
