import { IScheduleProposalAmount } from './IScheduleProposalAmount';
import { ITeam } from './ITeam';

export interface IScheduleProposalRequest {
  selectedTeam: ITeam;
  proposal: IScheduleProposalAmount[];
}
