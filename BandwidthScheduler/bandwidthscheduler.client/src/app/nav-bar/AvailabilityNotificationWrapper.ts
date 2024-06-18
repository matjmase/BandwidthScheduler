import { AvailabilityNotificationEntry } from '../models/db/AvailabilityNotificationEntry';
import { INotificationWrapper, NotificationType } from './INotificationWrapper';

export class AvailabilityNotificationWrapper implements INotificationWrapper {
  type: NotificationType = NotificationType.Availability;
  disabled: boolean = false;
  get id(): number {
    return this.avail.id;
  }
  get timeStamp(): Date {
    return this.avail.timeStamp;
  }
  get startTime(): Date {
    return this.avail.availability.startTime;
  }
  get endTime(): Date {
    return this.avail.availability.endTime;
  }
  get seen(): boolean {
    return this.avail.seen;
  }
  set seen(value: boolean) {
    this.avail.seen = value;
  }

  constructor(public avail: AvailabilityNotificationEntry) {}
}
