import { CommitmentNotificationEntry } from '../models/db/CommitmentNotificationEntry';
import { INotificationWrapper, NotificationType } from './INotificationWrapper';

export class CommitmentNotificationWrapper implements INotificationWrapper {
  type: NotificationType = NotificationType.Commitment;
  disabled: boolean = false;
  get id(): number {
    return this.commit.id;
  }
  get timeStamp(): Date {
    return this.commit.timeStamp;
  }
  get startTime(): Date {
    return this.commit.commitment.startTime;
  }
  get endTime(): Date {
    return this.commit.commitment.endTime;
  }
  get seen(): boolean {
    return this.commit.seen;
  }
  set seen(value: boolean) {
    this.commit.seen = value;
  }

  constructor(private commit: CommitmentNotificationEntry) {}
}
