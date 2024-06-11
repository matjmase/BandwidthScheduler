import { IAvailability } from './IAvailability';
import { IUser } from './IUser';

export interface IAvailabilityNotification {
  id: number;
  userId: number;
  availabilityId: number;
  timeStamp: string;
  seen: boolean;
  availability: IAvailability;
  user: IUser;
}
