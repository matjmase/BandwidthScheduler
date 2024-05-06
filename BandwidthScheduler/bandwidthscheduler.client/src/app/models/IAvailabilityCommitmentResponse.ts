import { IAvailabilityResponse } from './IAvailabilityResponse';
import { ICommitmentResponse } from './ICommitmentResponse';

export interface IAvailabilityCommitmentResponse {
  availabilities: IAvailabilityResponse[];
  commitments: ICommitmentResponse[];
}
