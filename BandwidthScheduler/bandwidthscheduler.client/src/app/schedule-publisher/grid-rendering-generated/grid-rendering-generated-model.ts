import { IScheduleProposalRequest } from '../../models/IScheduleProposalRequest';
import { IScheduleProposalResponse } from '../../models/IScheduleProposalResponse';

export interface IGridRenderingGeneratedModel {
  maxNumberOfPeople: number;
  proposal: IScheduleProposalRequest;
  responseRaw: IScheduleProposalResponse[];
}
