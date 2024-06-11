import { IAvailabilityNotification } from './db/IAvailabilityNotification';
import { ICommitmentNotification } from './db/ICommitmentNotification';

export interface INotificationResponse {
  availability: IAvailabilityNotification[];
  commitment: ICommitmentNotification[];
}
