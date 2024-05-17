import { IAvailability } from './db/IAvailability';
import { ICommitment } from './db/ICommitment';

export interface IAvailabilityCommitmentResponse {
  availabilities: IAvailability[];
  commitments: ICommitment[];
}
