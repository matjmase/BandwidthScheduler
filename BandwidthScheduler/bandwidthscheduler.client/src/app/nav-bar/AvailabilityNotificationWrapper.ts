import { AvailabilityNotificationEntry } from '../models/db/AvailabilityNotificationEntry';
import { INotificationWrapper } from './INotificationWrapper';

export class AvailabilityNotificationWrapper implements INotificationWrapper {
  notificationType: string = 'Availability';
  get timeStamp(): Date {
    return this.avail.timeStamp;
  }
  get startTime(): Date {
    return this.avail.availability.startTime;
  }
  get endTime(): Date {
    return this.avail.availability.endTime;
  }

  constructor(private avail: AvailabilityNotificationEntry) {}
}
