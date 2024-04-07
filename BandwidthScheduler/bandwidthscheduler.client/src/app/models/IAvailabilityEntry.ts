import { IAvailabilityResponse } from './IAvailabilityResponse';

export class AvailabilityEntry {
  startTime: Date;
  endTime: Date;

  constructor(resp: IAvailabilityResponse) {
    this.startTime = new Date(Date.parse(resp.startTime));
    this.endTime = new Date(Date.parse(resp.endTime));
  }
}
