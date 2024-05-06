import { IScheduleProposalRequest } from './IScheduleProposalRequest';
import { IScheduleProposalResponse } from './IScheduleProposalResponse';

export interface IScheduleSubmitRequest {
  ProposalRequest: IScheduleProposalRequest;
  ProposalResponse: IScheduleProposalResponse;
}
