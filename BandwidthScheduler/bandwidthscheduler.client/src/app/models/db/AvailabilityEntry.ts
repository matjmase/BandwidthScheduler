import { IAvailability } from './IAvailability';
import { IUser } from './IUser';

export class AvailabilityEntry {
  id: number;
  userId: number;
  startTime: Date;
  endTime: Date;
  user: IUser | undefined;

  constructor(resp: IAvailability) {
    this.id = resp.id;
    this.userId = resp.id;
    this.startTime = new Date(Date.parse(resp.startTime));
    this.endTime = new Date(Date.parse(resp.endTime));
    this.user = resp.user;
  }

  public static Default(): AvailabilityEntry {
    return {
      id: 0,
      userId: 0,
      startTime: new Date(),
      endTime: new Date(),
      user: undefined,
    };
  }
}
