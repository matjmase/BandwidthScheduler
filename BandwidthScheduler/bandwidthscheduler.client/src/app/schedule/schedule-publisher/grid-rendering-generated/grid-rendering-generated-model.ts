import { IScheduleProposalRequest } from '../../../models/IScheduleProposalRequest';
import { IScheduleProposalResponse } from '../../../models/IScheduleProposalResponse';
import { CommitmentEntry } from '../../../models/db/CommitmentEntry';

export interface IGridRenderingGeneratedModel {
  maxNumberOfPeople: number;
  existingCommitments: CommitmentEntry[];
  proposal: IScheduleProposalRequest;
  responseRaw: IScheduleProposalResponse;
}
