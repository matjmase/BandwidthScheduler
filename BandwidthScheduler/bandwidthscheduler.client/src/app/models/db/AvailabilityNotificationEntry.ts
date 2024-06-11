import { AvailabilityEntry } from './AvailabilityEntry';
import { IAvailabilityNotification } from './IAvailabilityNotification';
import { IUser } from './IUser';

export class AvailabilityNotificationEntry {
  id: number;
  userId: number;
  availabilityId: number;
  timeStamp: Date;
  seen: boolean;
  availability: AvailabilityEntry;
  user: IUser;

  constructor(resp: IAvailabilityNotification) {
    this.id = resp.id;
    this.userId = resp.userId;
    this.availabilityId = resp.availabilityId;
    this.timeStamp = new Date(Date.parse(resp.timeStamp));
    this.seen = resp.seen;
    this.availability = new AvailabilityEntry(resp.availability);
    this.user = resp.user;
  }
}
